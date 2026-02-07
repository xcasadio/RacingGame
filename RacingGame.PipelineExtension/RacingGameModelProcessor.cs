using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace RacingGame.PipelineExtension
{
    /// <summary>
    /// RacingGame model processor for .x files.
    /// Converts EffectInstance data from the .x file into EffectMaterialContent so
    /// the runtime Model uses custom Effects (not BasicEffect).
    /// </summary>
    [ContentProcessor(DisplayName = "RacingGame Model (X .x + EffectInstance)")]
    public class RacingGameModelProcessor : ModelProcessor
    {
        // Parsed from the .x file: material name => effect instance info
        private Dictionary<string, XEffectInfo> _effectsByMaterialName =
            new(StringComparer.OrdinalIgnoreCase);

        private string _sourceXFile = "";

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            context.Logger.LogImportantMessage(">>> RacingGameModelProcessor called <<<");

            // 1) Resolve the source .x file path (this one is reliable in MGCB)
            _sourceXFile = context.SourceIdentity?.SourceFilename
                           ?? input.Identity?.SourceFilename
                           ?? "";

            context.Logger.LogMessage($"CWD = '{Environment.CurrentDirectory}'");
            context.Logger.LogMessage($"SourceIdentity = '{context.SourceIdentity?.SourceFilename}'");
            context.Logger.LogMessage($"Input.Identity = '{input.Identity?.SourceFilename}'");
            context.Logger.LogMessage($"Resolved X = '{_sourceXFile}', exists={File.Exists(_sourceXFile)}");

            // 2) Use the parent bone name for nodes that have no name
            UseParentBoneNameIfMeshNameIsNotSet(input);

            // 3) Parse EffectInstance from the .x and inject EffectMaterialContent into the scene tree
            if (!string.IsNullOrWhiteSpace(_sourceXFile) && File.Exists(_sourceXFile))
            {
                _effectsByMaterialName = ParseXEffects(_sourceXFile, context);
                context.Logger.LogImportantMessage($"Parsed {_effectsByMaterialName.Count} EffectInstance(s) from .x");

                if (_effectsByMaterialName.Count > 0)
                    ApplyParsedEffectsToScene(input, context, _effectsByMaterialName, _sourceXFile);
            }
            else
            {
                context.Logger.LogWarning("", context.SourceIdentity, "Cannot read source .x file: '{0}'", _sourceXFile);
            }

            // 4) Tangents (only if missing) to avoid Tangent0 duplicate crash
            GenerateTangentsIfMissing(input, context);

            // 5) Store technique into the mesh node name (old kit behavior)
            StoreEffectTechniqueInMeshName(input, context);

            // 6) Let base ModelProcessor do the conversion/build
            return base.Process(input, context);
        }

        #region Tangents

        private void GenerateTangentsIfMissing(NodeContent input, ContentProcessorContext context)
        {
            if (input is MeshContent mesh)
            {
                // If any geometry already has Tangent0, don't try to regenerate (prevents duplicate channel crash).
                bool hasTangent0 = false;
                string tangentName = VertexChannelNames.Tangent(0);

                foreach (var geom in mesh.Geometry)
                {
                    if (geom.Vertices.Channels.Contains(tangentName))
                    {
                        hasTangent0 = true;
                        break;
                    }
                }

                if (!hasTangent0)
                {
                    context.Logger.LogMessage($"Generating Tangent0 for mesh '{mesh.Name ?? "<no name>"}'");
                    MeshHelper.CalculateTangentFrames(
                        mesh,
                        VertexChannelNames.TextureCoordinate(0),
                        VertexChannelNames.Tangent(0),
                        null // no binormals
                    );
                }
            }

            foreach (NodeContent child in input.Children)
                GenerateTangentsIfMissing(child, context);
        }

        #endregion

        #region Name helper (keep kit behavior)

        private void UseParentBoneNameIfMeshNameIsNotSet(NodeContent input)
        {
            if (string.IsNullOrEmpty(input.Name) &&
                input.Parent != null &&
                !string.IsNullOrEmpty(input.Parent.Name))
            {
                input.Name = input.Parent.Name;
            }

            foreach (NodeContent node in input.Children)
                UseParentBoneNameIfMeshNameIsNotSet(node);
        }

        #endregion

        #region Store technique in mesh name (keep kit behavior)

        private void StoreEffectTechniqueInMeshName(NodeContent input, ContentProcessorContext context)
        {
            if (input is MeshContent mesh)
            {
                foreach (GeometryContent geom in mesh.Geometry)
                {
                    var effectMaterial = geom.Material as EffectMaterialContent;
                    if (effectMaterial == null)
                        continue;

                    if (effectMaterial.OpaqueData.TryGetValue("technique", out object techObj) && techObj != null)
                    {
                        string techStr = Convert.ToString(techObj, CultureInfo.InvariantCulture) ?? "";

                        // Avoid appending multiple times if called on multiple geometries
                        if (!string.IsNullOrEmpty(input.Name) &&
                            !string.IsNullOrEmpty(techStr) &&
                            !input.Name.EndsWith(techStr, StringComparison.Ordinal))
                        {
                            input.Name = input.Name + techStr;
                            context.Logger.LogMessage($"Technique={techStr} => node name '{input.Name}'");
                        }
                    }
                }
            }

            foreach (NodeContent child in input.Children)
                StoreEffectTechniqueInMeshName(child, context);
        }

        #endregion

        #region Parse .x EffectInstance -> dictionary

        private sealed class XEffectInfo
        {
            public string EffectPath = "";
            public Dictionary<string, int> DWords = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, float[]> Floats = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> Strings = new(StringComparer.OrdinalIgnoreCase);
        }

        private static string XUnescape(string s)
        {
            // .x usually stores paths like "..\\shaders\\X.fx"
            return s.Replace(@"\\", @"\").Replace("\\\"", "\"");
        }

        private static int FindMatchingBrace(string s, int openPos)
        {
            int depth = 0;
            bool inString = false;

            for (int i = openPos; i < s.Length; i++)
            {
                char ch = s[i];

                if (ch == '"')
                {
                    // ignore escaped quotes
                    int bs = 0;
                    for (int j = i - 1; j >= 0 && s[j] == '\\'; j--) bs++;
                    if ((bs & 1) == 0)
                        inString = !inString;
                }

                if (inString) continue;

                if (ch == '{') depth++;
                else if (ch == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }

        private static Dictionary<string, XEffectInfo> ParseXEffects(string xPath, ContentProcessorContext context)
        {
            var result = new Dictionary<string, XEffectInfo>(StringComparer.OrdinalIgnoreCase);
            string text = File.ReadAllText(xPath);

            // Material <Name> {
            foreach (Match mm in Regex.Matches(text, @"\bMaterial\s+(?<name>[A-Za-z0-9_]+)\s*\{", RegexOptions.Compiled))
            {
                string matName = mm.Groups["name"].Value;

                int matOpen = text.IndexOf('{', mm.Index);
                int matClose = FindMatchingBrace(text, matOpen);
                if (matClose < 0) continue;

                string matBody = text.Substring(matOpen + 1, matClose - matOpen - 1);

                // EffectInstance {
                Match em = Regex.Match(matBody, @"\bEffectInstance\s*\{", RegexOptions.Compiled);
                if (!em.Success) continue;

                int effOpen = matBody.IndexOf('{', em.Index);
                int effClose = FindMatchingBrace(matBody, effOpen);
                if (effClose < 0) continue;

                string effBody = matBody.Substring(effOpen + 1, effClose - effOpen - 1);

                // first ".fx" string
                Match fxm = Regex.Match(effBody, "\"(?<fx>[^\"]+\\.fx)\"\\s*;", RegexOptions.IgnoreCase);
                if (!fxm.Success) continue;

                var info = new XEffectInfo
                {
                    EffectPath = XUnescape(fxm.Groups["fx"].Value)
                };

                // EffectParamDWord { "name"; 123; }
                foreach (Match dm in Regex.Matches(
                    effBody,
                    @"EffectParamDWord\s*\{\s*""(?<n>[^""]+)""\s*;\s*(?<v>[+-]?\d+)\s*;\s*\}",
                    RegexOptions.Singleline))
                {
                    info.DWords[dm.Groups["n"].Value] = int.Parse(dm.Groups["v"].Value, CultureInfo.InvariantCulture);
                }

                // EffectParamString { "name"; "..\\textures\\X.tga"; }
                foreach (Match sm in Regex.Matches(
                    effBody,
                    @"EffectParamString\s*\{\s*""(?<n>[^""]+)""\s*;\s*""(?<v>[^""]+)""\s*;\s*\}",
                    RegexOptions.Singleline))
                {
                    info.Strings[sm.Groups["n"].Value] = XUnescape(sm.Groups["v"].Value);
                }

                // EffectParamFloats { "name"; count; v1, v2, ...; }
                foreach (Match fm in Regex.Matches(
                    effBody,
                    @"EffectParamFloats\s*\{\s*""(?<n>[^""]+)""\s*;\s*(?<c>\d+)\s*;\s*(?<vals>.*?)\s*;\s*\}",
                    RegexOptions.Singleline))
                {
                    string name = fm.Groups["n"].Value;
                    int count = int.Parse(fm.Groups["c"].Value, CultureInfo.InvariantCulture);

                    var tokens = Regex.Split(fm.Groups["vals"].Value.Trim(), @"[,\s]+");
                    var list = new List<float>(count);

                    foreach (var t in tokens)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        if (float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                            list.Add(f);
                        if (list.Count == count) break;
                    }

                    info.Floats[name] = list.ToArray();
                }

                result[matName] = info;
            }

            return result;
        }

        #endregion

        #region Apply parsed effects to GeometryContent.Material

        private static bool LooksLikeTexturePath(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return false;

            string ext = Path.GetExtension(v).ToLowerInvariant();
            return ext is ".dds" or ".png" or ".jpg" or ".jpeg" or ".tga" or ".bmp";
        }

        private static void ApplyParsedEffectsToScene(
            NodeContent node,
            ContentProcessorContext context,
            Dictionary<string, XEffectInfo> fxByMaterial,
            string xPath)
        {
            string baseDir = Path.GetDirectoryName(xPath) ?? "";

            if (node is MeshContent mesh)
            {
                foreach (GeometryContent geom in mesh.Geometry)
                {
                    var mat = geom.Material;
                    if (mat == null || string.IsNullOrEmpty(mat.Name))
                        continue;

                    if (!fxByMaterial.TryGetValue(mat.Name, out XEffectInfo info))
                        continue;

                    // Already an EffectMaterialContent? Keep it, but ensure technique/paths exist.
                    var fxMat = mat as EffectMaterialContent;
                    if (fxMat == null)
                    {
                        fxMat = new EffectMaterialContent
                        {
                            Name = mat.Name,
                            Identity = mat.Identity
                        };

                        // Copy existing material data
                        foreach (var kv in mat.OpaqueData)
                            fxMat.OpaqueData[kv.Key] = kv.Value;

                        foreach (var kv in mat.Textures)
                            fxMat.Textures[kv.Key] = kv.Value;
                    }

                    // Effect path
                    string fxFull = Path.GetFullPath(Path.Combine(baseDir, info.EffectPath));
                    fxMat.Effect = new ExternalReference<EffectContent>(fxFull, context.SourceIdentity);

                    // DWords/Floats/Strings to OpaqueData (and textures)
                    foreach (var kv in info.DWords)
                        fxMat.OpaqueData[kv.Key] = kv.Value;

                    foreach (var kv in info.Floats)
                        fxMat.OpaqueData[kv.Key] = kv.Value;

                    foreach (var kv in info.Strings)
                    {
                        if (LooksLikeTexturePath(kv.Value))
                        {
                            string texFull = Path.GetFullPath(Path.Combine(baseDir, kv.Value));
                            fxMat.Textures[kv.Key] = new ExternalReference<TextureContent>(texFull, context.SourceIdentity);
                        }
                        else
                        {
                            fxMat.OpaqueData[kv.Key] = kv.Value;
                        }
                    }

                    geom.Material = fxMat;

                    context.Logger.LogMessage(
                        $"Applied FX '{info.EffectPath}' to material '{mat.Name}' (mesh '{mesh.Name ?? "<no name>"}')");
                }
            }

            foreach (NodeContent child in node.Children)
                ApplyParsedEffectsToScene(child, context, fxByMaterial, xPath);
        }

        #endregion
    }
}
