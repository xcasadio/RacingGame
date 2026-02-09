using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RacingGame.PipelineExtension;

[ContentProcessor(DisplayName = "RacingGame Model (MonoGame)")]
public sealed class RacingGameModelProcessor : ModelProcessor
{
    // --------------------------------------------------------------------
    // Entry point
    // --------------------------------------------------------------------
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        context.Logger.LogImportantMessage(">>> RacingGameModelProcessor called <<<");

        string xPath = GetSourcePath(input);
        context.Logger.LogMessage($"CWD = '{Directory.GetCurrentDirectory()}'");
        context.Logger.LogMessage($"SourceIdentity = '{xPath}'");

        if (string.IsNullOrWhiteSpace(xPath) || !File.Exists(xPath))
        {
            throw new InvalidContentException($"Cannot resolve .x path from Identity. Got '{xPath}'");
        }

        var effectsByMaterial = ParseEffectInstancesFromX(xPath, context);
        context.Logger.LogImportantMessage($"Parsed {effectsByMaterial.Count} EffectInstance(s) from .x");

        ApplyEffectsRecursive(input, context, xPath, effectsByMaterial);

        // Tangents (safe)
        GenerateTangentsRecursive(input, context);

        // Let default processor build bones, meshes, XNB, etc.
        return base.Process(input, context);
    }

    // --------------------------------------------------------------------
    // Resolve Identity.SourceFilename safely
    // --------------------------------------------------------------------
    private static string GetSourcePath(NodeContent input)
    {
        // In MonoGame content pipeline, NodeContent.Identity.SourceFilename is the real file path.
        var src = input?.Identity?.SourceFilename ?? string.Empty;

        // Normalize to OS path form.
        src = src.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return src;
    }

    // --------------------------------------------------------------------
    // Parse .x : Material <name> { ... EffectInstance { ... } ... }
    // --------------------------------------------------------------------
    private sealed class XEffectInstance
    {
        public string MaterialName = "";
        public string FxPath = "";
        public Dictionary<string, int> Dwords = new(StringComparer.Ordinal);
        public Dictionary<string, float[]> Floats = new(StringComparer.Ordinal);
        public Dictionary<string, string> Strings = new(StringComparer.Ordinal);
    }

    private static Dictionary<string, XEffectInstance> ParseEffectInstancesFromX(
        string xPath,
        ContentProcessorContext context)
    {
        string text = File.ReadAllText(xPath);

        context.Logger.LogMessage($"EffectInstance occurrences = {Regex.Matches(text, @"\\bEffectInstance\\b").Count}");
        context.Logger.LogMessage($"Material occurrences       = {Regex.Matches(text, @"\\bMaterial\\b").Count}");

        var result = new Dictionary<string, XEffectInstance>(StringComparer.Ordinal);

        int i = 0;
        while (true)
        {
            int matIdx = IndexOfToken(text, "Material", i);
            if (matIdx < 0)
            {
                break;
            }

            // Read material identifier after "Material"
            int nameStart = matIdx + "Material".Length;
            SkipWhitespace(text, ref nameStart);

            string matName = ReadIdentifier(text, ref nameStart);
            if (string.IsNullOrWhiteSpace(matName))
            {
                i = matIdx + 8;
                continue;
            }

            int braceOpen = text.IndexOf('{', nameStart);
            if (braceOpen < 0)
            {
                break;
            }

            string matBody = ExtractBraceBlock(text, braceOpen, out int braceClose);

            // Find EffectInstance inside this material block
            int j = 0;
            while (true)
            {
                int effIdx = IndexOfToken(matBody, "EffectInstance", j);
                if (effIdx < 0)
                {
                    break;
                }

                int effBraceOpen = matBody.IndexOf('{', effIdx);
                if (effBraceOpen < 0)
                {
                    break;
                }

                string effBody = ExtractBraceBlock(matBody, effBraceOpen, out int effBraceClose);

                var inst = ParseSingleEffectInstance(matName, effBody, context);

                // Keep the last EffectInstance if multiple (rare)
                if (!string.IsNullOrWhiteSpace(inst.FxPath))
                {
                    result[matName] = inst;
                }

                j = effBraceClose + 1;
            }

            i = braceClose + 1;
        }

        return result;
    }

    private static XEffectInstance ParseSingleEffectInstance(
        string materialName,
        string effBody,
        ContentProcessorContext context)
    {
        var inst = new XEffectInstance { MaterialName = materialName };

        // EffectFilename { "..\\shaders\\NormalMapping.fx"; }
        // 1) Format A: EffectFilename { "..\\shaders\\X.fx"; }
        var mFx = Regex.Match(
            effBody,
            @"EffectFilename\s*\{\s*""(?<path>[^""]+\.fx)""\s*;\s*\}",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (mFx.Success)
        {
            inst.FxPath = mFx.Groups["path"].Value;
        }
        else
        {
            // 2) Format B: "..\\shaders\\X.fx";
            var mFx2 = Regex.Match(
                effBody,
                @"""(?<path>[^""]+\.fx)""\s*;",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (mFx2.Success)
            {
                inst.FxPath = mFx2.Groups["path"].Value;
            }
        }

        // EffectParamDWord { "technique"; 4; }
        foreach (Match m in Regex.Matches(
                     effBody,
                     @"EffectParamDWord\s*\{\s*""(?<name>[^""]+)""\s*;\s*(?<val>\d+)\s*;\s*\}",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            string name = m.Groups["name"].Value;
            string valStr = m.Groups["val"].Value;

            if (int.TryParse(valStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val))
            {
                inst.Dwords[name] = val;
            }
        }

        // EffectParamString { "Texture"; "..\\textures\\RacerCar.tga"; }
        foreach (Match m in Regex.Matches(
                     effBody,
                     @"EffectParamString\s*\{\s*""(?<name>[^""]+)""\s*;\s*""(?<val>[^""]*)""\s*;\s*\}",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            string name = m.Groups["name"].Value;
            string val = m.Groups["val"].Value;
            inst.Strings[name] = val;
        }

        // EffectParamFloats { "diffuseColor"; 1.0,1.0,1.0,1.0;; }
        foreach (Match m in Regex.Matches(
                     effBody,
                     @"EffectParamFloats\s*\{\s*""(?<name>[^""]+)""\s*;\s*(?<vals>[^;]+?)\s*;;\s*\}",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            string name = m.Groups["name"].Value;
            string vals = m.Groups["vals"].Value;

            float[] floats = ParseFloatList(vals);
            if (floats.Length > 0)
            {
                inst.Floats[name] = floats;
            }
        }

        return inst;
    }

    private static float[] ParseFloatList(string csv)
    {
        // Split on commas, parse invariant culture
        var parts = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<float>(parts.Length);

        foreach (var raw in parts)
        {
            string s = raw.Trim();
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                list.Add(f);
            }
        }

        return list.ToArray();
    }

    // --------------------------------------------------------------------
    // Apply EffectMaterialContent to matching materials
    // --------------------------------------------------------------------
    private static void ApplyEffectsRecursive(
        NodeContent node,
        ContentProcessorContext context,
        string xPath,
        Dictionary<string, XEffectInstance> effectsByMaterial)
    {
        if (node is MeshContent mesh)
        {
            foreach (var geom in mesh.Geometry)
            {
                string matName = geom.Material?.Name ?? "";

                if (!effectsByMaterial.TryGetValue(matName, out var inst))
                {
                    continue;
                }

                string fxAbs = ResolvePathRelativeToX(xPath, inst.FxPath);
                if (!File.Exists(fxAbs))
                {
                    throw new InvalidContentException($"FX file not found for material '{matName}': '{fxAbs}'");
                }

                var effectMat = new EffectMaterialContent
                {
                    Effect = new ExternalReference<EffectContent>(fxAbs)
                };

                // Copy original textures if any (optional, harmless)
                if (geom.Material is BasicMaterialContent basicMat)
                {
                    foreach (var kv in basicMat.Textures)
                        effectMat.Textures[kv.Key] = kv.Value;
                }

                // Apply textures from EffectParamString (these are the important ones)
                foreach (var kv in inst.Strings)
                {
                    if (LooksLikeFilePath(kv.Value))
                    {
                        string texAbs = ResolvePathRelativeToX(xPath, kv.Value);
                        if (!File.Exists(texAbs))
                        {
                            throw new InvalidContentException($"Texture not found '{texAbs}' for param '{kv.Key}' material '{matName}'");
                        }

                        effectMat.Textures[kv.Key] = new ExternalReference<TextureContent>(texAbs);
                    }
                    else
                    {
                        // If it's not a path, keep as opaque string
                        effectMat.OpaqueData[kv.Key] = kv.Value;
                    }
                }

                // Store float params with the RIGHT runtime type (NO float[])
                foreach (var kv in inst.Floats)
                    effectMat.OpaqueData[kv.Key] = ConvertFloatsToBestType(kv.Value);

                // Keep dwords as metadata (avoid forcing SetValue(int) on a float param)
                // Still useful for your “technique => rename mesh” logic.
                foreach (var kv in inst.Dwords)
                    effectMat.OpaqueData[kv.Key] = kv.Value;

                geom.Material = effectMat;

                // Optional: append technique to mesh/node name like the old sample did
                if (inst.Dwords.TryGetValue("technique", out int tech) && !string.IsNullOrEmpty(mesh.Name))
                {
                    if (!mesh.Name.EndsWith(tech.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                    {
                        mesh.Name = mesh.Name + tech.ToString(CultureInfo.InvariantCulture);
                    }
                }

                context.Logger.LogImportantMessage(
                    $"Applied FX '{inst.FxPath}' to material '{matName}' (mesh '{mesh.Name}')");
            }
        }

        foreach (var child in node.Children)
            ApplyEffectsRecursive(child, context, xPath, effectsByMaterial);
    }

    private static object ConvertFloatsToBestType(float[] v)
    {
        // This is the key fix: don't serialize float[] unless you really need arrays.
        return v.Length switch
        {
            1 => v[0],
            2 => new Vector2(v[0], v[1]),
            3 => new Vector3(v[0], v[1], v[2]),
            4 => new Vector4(v[0], v[1], v[2], v[3]),
            _ => throw new InvalidContentException(
                $"Unsupported float param length {v.Length}. " +
                "If you truly need an HLSL float array, handle it explicitly here.")
        };
    }

    // --------------------------------------------------------------------
    // Tangents (safe)
    // --------------------------------------------------------------------
    private static void GenerateTangentsRecursive(NodeContent node, ContentProcessorContext context)
    {
        if (node is MeshContent mesh)
        {
            // Only generate if none exists anywhere in the mesh
            string tangentName = VertexChannelNames.Tangent(0);

            bool anyHasTangent = mesh.Geometry.Any(g =>
                g.Vertices != null &&
                g.Vertices.Channels != null &&
                g.Vertices.Channels.Contains(tangentName));

            if (!anyHasTangent)
            {
                // Need texcoords to compute tangents
                string tex0 = VertexChannelNames.TextureCoordinate(0);
                bool hasTex0 = mesh.Geometry.Any(g =>
                    g.Vertices != null &&
                    g.Vertices.Channels != null &&
                    g.Vertices.Channels.Contains(tex0));

                if (hasTex0)
                {
                    context.Logger.LogImportantMessage($"Generating Tangent0 for mesh '{mesh.Name}'");
                    MeshHelper.CalculateTangentFrames(mesh, tex0, tangentName, null);
                }
            }
        }

        foreach (var child in node.Children)
            GenerateTangentsRecursive(child, context);
    }

    // --------------------------------------------------------------------
    // Path helpers
    // --------------------------------------------------------------------
    private static string ResolvePathRelativeToX(string xPath, string rel)
    {
        string xDir = Path.GetDirectoryName(xPath) ?? Directory.GetCurrentDirectory();

        string normalized = rel.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        string combined = Path.GetFullPath(Path.Combine(xDir, normalized));
        return combined;
    }

    private static bool LooksLikeFilePath(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        // crude but effective: has an extension and contains a slash/backslash or starts with "."
        return (s.Contains("\\") || s.Contains("/") || s.StartsWith(".", StringComparison.Ordinal)) &&
               Path.HasExtension(s);
    }

    // --------------------------------------------------------------------
    // Small parsing helpers
    // --------------------------------------------------------------------
    private static int IndexOfToken(string text, string token, int start)
    {
        return text.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
    }

    private static void SkipWhitespace(string text, ref int i)
    {
        while (i < text.Length && char.IsWhiteSpace(text[i]))
            i++;
    }

    private static string ReadIdentifier(string text, ref int i)
    {
        SkipWhitespace(text, ref i);

        int start = i;
        while (i < text.Length)
        {
            char c = text[i];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                i++;
                continue;
            }
            break;
        }

        return text.Substring(start, i - start).Trim();
    }

    private static string ExtractBraceBlock(string text, int braceOpenIndex, out int braceCloseIndex)
    {
        // Returns inside of { ... } (without the outer braces)
        int depth = 0;

        for (int i = braceOpenIndex; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    braceCloseIndex = i;
                    return text.Substring(braceOpenIndex + 1, i - braceOpenIndex - 1);
                }
            }
        }

        throw new InvalidContentException("Unmatched braces while parsing .x file.");
    }
}