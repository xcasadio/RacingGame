using System.Globalization;
using System.Numerics;
using System.Text.Json.Nodes;
using Assimp;
using CasaEngine.EditorServices.Import;
using LegacyModelGltfConverter;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using GltfImage = SharpGLTF.Schema2.Image;
using GltfMesh = SharpGLTF.Schema2.Mesh;
using GltfNode = SharpGLTF.Schema2.Node;

// Converts the legacy RacingGame .x models to .gltf files the CasaEngine runtime can read:
// - geometry through the editor converter (AssimpToGltfConverter), node transforms decomposed
//   to translation/rotation/scale (GltfStaticModelReader cannot read matrix nodes);
// - diffuse and normal textures of each material attached as external .png images;
// - the legacy material metadata the old runtime importer used stored in the material extras.
// - a TANGENT attribute generated like the legacy content processor on every primitive that has a normal texture;
// - the normal maps given with --normal-map (used by meshes the game generates, so no model references them) converted too.
// - the sky cube given with --sky-cube rewritten in the axes of the converted world (SkyCubeConverter, ADR-0017).
// Usage: LegacyModelGltfConverter <x models directory> <gltf output directory> <png output directory> [--skip <model name>]... [--normal-map <tga file>]... [--sky-cube <dds file>]
// Texture modes, for the textures no model references:
// - LegacyModelGltfConverter --textures <tga directory> <png directory> <texture name>...: converts each named .tga to .png
//   as a colour copy; a name followed by ":normal" is converted as a normal map;
// - LegacyModelGltfConverter --verify-textures <tga directory> <png directory>: decodes every .tga and its .png of the same
//   name and compares their RGBA bytes, first as a colour copy, then, if they differ, as a converted normal map; lists both
//   sets with their counts, and the pairs that match neither and the .tga without .png (exit code 1 if any).

const string Usage = "Usage: LegacyModelGltfConverter <x models directory> <gltf output directory> <png output directory> [--skip <model name>]... [--normal-map <tga file>]... [--sky-cube <dds file>]";
const string TexturesUsage = "Usage: LegacyModelGltfConverter --textures <tga directory> <png directory> <texture name>[:normal]...";
const string VerifyTexturesUsage = "Usage: LegacyModelGltfConverter --verify-textures <tga directory> <png directory>";
const string NormalSuffix = ":normal";

if (args.Length > 0 && args[0] == "--textures")
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine(TexturesUsage);
        return 2;
    }

    string tgaDirectory = Path.GetFullPath(args[1]);
    string pngOutputDirectory = Path.GetFullPath(args[2]);
    int missing = 0;
    for (int i = 3; i < args.Length; i++)
    {
        string argument = args[i];
        bool isNormalMap = argument.EndsWith(NormalSuffix, StringComparison.OrdinalIgnoreCase);
        if (isNormalMap)
        {
            argument = argument[..^NormalSuffix.Length];
        }

        string name = Path.GetFileNameWithoutExtension(argument);
        string tgaPath = Path.Combine(tgaDirectory, name + ".tga");
        if (!File.Exists(tgaPath))
        {
            Console.WriteLine($"{name}.tga: ERROR not found in {tgaDirectory}");
            missing++;
            continue;
        }

        LegacyTextureConverter.ConvertToPng(tgaPath, Path.Combine(pngOutputDirectory, name + ".png"), isNormalMap);
        Console.WriteLine($"{name}.tga: converted to {name}.png{(isNormalMap ? " (normal map)" : string.Empty)}");
    }

    Console.WriteLine($"{args.Length - 3} textures, {missing} not found.");
    return missing == 0 ? 0 : 1;
}

if (args.Length > 0 && args[0] == "--verify-textures")
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine(VerifyTexturesUsage);
        return 2;
    }

    string tgaDirectory = Path.GetFullPath(args[1]);
    string pngInputDirectory = Path.GetFullPath(args[2]);
    string[] tgaFiles = Directory.GetFiles(tgaDirectory, "*.tga")
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    int pairs = 0;
    int withoutPng = 0;
    var colourCopies = new List<string>();
    var normalMaps = new List<string>();
    var different = new List<string>();
    foreach (string tgaPath in tgaFiles)
    {
        string name = Path.GetFileNameWithoutExtension(tgaPath);
        string pngPath = Path.Combine(pngInputDirectory, name + ".png");
        if (!File.Exists(pngPath))
        {
            Console.WriteLine($"{name}: no .png");
            withoutPng++;
            continue;
        }

        pairs++;
        string? colourDifference = LegacyTextureConverter.Compare(tgaPath, pngPath, isNormalMap: false);
        if (colourDifference == null)
        {
            colourCopies.Add(name);
        }
        else if (LegacyTextureConverter.Compare(tgaPath, pngPath, isNormalMap: true) == null)
        {
            normalMaps.Add(name);
        }
        else
        {
            Console.WriteLine($"{name}: DIFFERENT neither a colour copy nor a converted normal map; as a colour copy: {colourDifference}");
            different.Add(name);
        }
    }

    Console.WriteLine($"Colour copies ({colourCopies.Count}): {string.Join(", ", colourCopies)}");
    Console.WriteLine($"Converted normal maps ({normalMaps.Count}): {string.Join(", ", normalMaps)}");
    Console.WriteLine($"{tgaFiles.Length} .tga, {pairs} pairs, {colourCopies.Count} colour copies, {normalMaps.Count} converted normal maps, {different.Count} different, {withoutPng} without .png.");
    return different.Count == 0 && withoutPng == 0 ? 0 : 1;
}

var positional = new List<string>();
var skippedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var extraNormalMaps = new List<string>();
string? skyCubePath = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--normal-map" && i + 1 < args.Length)
    {
        extraNormalMaps.Add(args[++i]);
    }
    else if (args[i] == "--sky-cube" && i + 1 < args.Length)
    {
        skyCubePath = args[++i];
    }
    else if (args[i] == "--skip" && i + 1 < args.Length)
    {
        skippedModels.Add(args[++i]);
    }
    else
    {
        positional.Add(args[i]);
    }
}

if (positional.Count != 3)
{
    Console.Error.WriteLine(Usage);
    return 2;
}

string modelsDirectory = Path.GetFullPath(positional[0]);
string gltfDirectory = Path.GetFullPath(positional[1]);
string pngDirectory = Path.GetFullPath(positional[2]);
Directory.CreateDirectory(gltfDirectory);
Directory.CreateDirectory(pngDirectory);

var converter = new ModelConverter(gltfDirectory, pngDirectory);
string[] sourceFiles = Directory.GetFiles(modelsDirectory)
    .Where(path => Path.GetExtension(path).Equals(".x", StringComparison.OrdinalIgnoreCase))
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToArray();

int failures = 0;
int skipped = 0;
foreach (string sourceFile in sourceFiles)
{
    if (skippedModels.Contains(Path.GetFileNameWithoutExtension(sourceFile)))
    {
        Console.WriteLine($"{Path.GetFileName(sourceFile)}: skipped (--skip)");
        skipped++;
        continue;
    }

    IReadOnlyList<string> errors = converter.Convert(sourceFile, out string summary);
    Console.WriteLine($"{Path.GetFileName(sourceFile)}: {summary}");
    foreach (string error in errors)
    {
        Console.WriteLine($"  ERROR {error}");
    }

    failures += errors.Count > 0 ? 1 : 0;
}

int extraNormalMapCount = 0;
foreach (string normalMap in extraNormalMaps)
{
    string sourcePath = Path.GetFullPath(normalMap);
    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR normal map '{normalMap}' not found");
        failures++;
        continue;
    }

    LegacyTextureConverter.ConvertToPng(
        sourcePath,
        Path.Combine(pngDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".png"),
        isNormalMap: true);
    extraNormalMapCount++;
}

if (skyCubePath != null)
{
    string skySourcePath = Path.GetFullPath(skyCubePath);
    string skyDestinationPath = Path.Combine(pngDirectory, Path.GetFileName(skySourcePath));
    try
    {
        if (string.Equals(skySourcePath, skyDestinationPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The sky cube would overwrite its own source: use another png output directory.");
        }

        Console.WriteLine(SkyCubeConverter.Convert(skySourcePath, skyDestinationPath));
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException)
    {
        Console.WriteLine($"ERROR sky cube '{skyCubePath}': {ex.Message}");
        failures++;
    }
}

Console.WriteLine($"{sourceFiles.Length} models, {skipped} skipped, {failures} failed, {converter.ConvertedTextureCount} textures converted, {extraNormalMapCount} extra normal map(s) converted.");
return failures == 0 ? 0 : 1;

internal sealed class ModelConverter
{
    // Same post-processing as the old runtime importer (CasaEngine 295db0c6, StaticModelImporter.cs:53-58);
    // only its material list is used here.
    private const PostProcessSteps MaterialImportSteps =
        PostProcessSteps.Triangulate
        | PostProcessSteps.FlipUVs
        | PostProcessSteps.JoinIdenticalVertices
        | PostProcessSteps.GenerateSmoothNormals
        | PostProcessSteps.FlipWindingOrder
        | PostProcessSteps.GlobalScale;

    private const float MatrixTolerance = 1e-4f;

    private readonly string _gltfDirectory;
    private readonly string _pngDirectory;
    private readonly HashSet<string> _convertedTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _normalMapTextures = new(StringComparer.OrdinalIgnoreCase);

    public ModelConverter(string gltfDirectory, string pngDirectory)
    {
        _gltfDirectory = gltfDirectory;
        _pngDirectory = pngDirectory;
    }

    public int ConvertedTextureCount => _convertedTextures.Count;

    public IReadOnlyList<string> Convert(string sourceFile, out string summary)
    {
        var errors = new List<string>();
        summary = "not converted";
        string modelName = Path.GetFileNameWithoutExtension(sourceFile);

        IReadOnlyDictionary<string, LegacyEffectInstance> effects = LegacyXFileParser.ParseLegacyEffectInstances(sourceFile);
        IReadOnlyList<Assimp.Material> assimpMaterials = ReadAssimpMaterials(sourceFile);

        string temporaryGlb = Path.Combine(Path.GetTempPath(), "LegacyModelGltfConverter", Guid.NewGuid().ToString("N") + ".glb");
        ModelRoot model;
        try
        {
            AssimpToGltfConverter.Convert(sourceFile, temporaryGlb);
            model = ModelRoot.Load(temporaryGlb, new ReadSettings { Validation = ValidationMode.Skip });
        }
        finally
        {
            if (File.Exists(temporaryGlb))
            {
                File.Delete(temporaryGlb);
            }
        }

        DecomposeNodeTransforms(model, errors);

        var assimpByName = new Dictionary<string, Assimp.Material>(StringComparer.Ordinal);
        foreach (Assimp.Material assimpMaterial in assimpMaterials)
        {
            if (!assimpByName.TryAdd(assimpMaterial.Name ?? string.Empty, assimpMaterial))
            {
                errors.Add($"duplicate Assimp material name '{assimpMaterial.Name}'");
            }
        }

        var gltfNames = model.LogicalMaterials.Select(material => material.Name ?? string.Empty).ToList();
        if (gltfNames.Count != assimpMaterials.Count
            || gltfNames.Distinct(StringComparer.Ordinal).Count() != gltfNames.Count
            || gltfNames.Any(name => !assimpByName.ContainsKey(name)))
        {
            errors.Add($"glTF materials [{string.Join(", ", gltfNames)}] do not match Assimp materials [{string.Join(", ", assimpMaterials.Select(m => m.Name))}]");
        }

        var images = new Dictionary<string, GltfImage>(StringComparer.OrdinalIgnoreCase);
        int texturedChannels = 0;
        int effectMaterials = 0;
        foreach (SharpGLTF.Schema2.Material material in model.LogicalMaterials)
        {
            string materialName = material.Name ?? string.Empty;
            var extras = new JsonObject();
            if (assimpByName.TryGetValue(materialName, out Assimp.Material? assimpMaterial))
            {
                extras["legacyMaterial"] = BuildLegacyMaterial(assimpMaterial);
            }

            if (effects.TryGetValue(materialName, out LegacyEffectInstance? effect))
            {
                effectMaterials++;
                extras["legacyEffect"] = BuildLegacyEffect(effect);
                texturedChannels += AttachTexture(model, material, "BaseColor", effect, "diffuseTexture", sourceFile, images, errors);
                texturedChannels += AttachTexture(model, material, "Normal", effect, "normalTexture", sourceFile, images, errors);
            }

            material.Extras = extras;
        }

        int tangentPrimitives = AddTangents(model, errors);

        if (errors.Count == 0)
        {
            string gltfPath = Path.Combine(_gltfDirectory, modelName + ".gltf");
            model.SaveGLTF(gltfPath, new WriteSettings
            {
                Validation = ValidationMode.Skip,
                ImageWriting = ResourceWriteMode.SatelliteFile,
                JsonIndented = true,
            });
        }

        summary = $"{model.LogicalMeshes.Count} meshes, {model.LogicalMaterials.Count} materials, {effectMaterials} with effect instance, {texturedChannels} textured channels, {tangentPrimitives} primitives with tangents";
        return errors;
    }

    // The legacy content processor (RacingGameModelProcessor) generates the tangents with
    // MeshHelper.CalculateTangentFrames (no binormal channel): per triangle the direction of increasing U
    // (Lengyel), accumulated on the three vertices without weighting, then made orthogonal to the vertex
    // normal and normalised. Measured on the built legacy models (xnb), this reproduces them, including for a
    // vertex whose triangles all have a degenerate UV mapping: the legacy tangent is then cross(normal, +X)
    // (cross(normal, +Y) when the normal is parallel to X: not observed in the legacy models, chosen here).
    // The legacy shader builds the tangent frame as (cross(normal, tangent), tangent, normal) in model space,
    // with no correction for mirrored UVs; CasaEngine builds cross(normal, tangent) * w in world space. The
    // legacy model space and the glTF one (node transforms included) coincide, and no world transform of
    // the legacy models or of the glTF files has a negative determinant, so w = +1 reproduces the legacy
    // frame everywhere (w is a constant here, not the glTF handedness of the UV mapping).
    // Only the primitives that have a normal texture get tangents: the others never use them.
    private static int AddTangents(ModelRoot model, List<string> errors)
    {
        int count = 0;
        foreach (GltfMesh mesh in model.LogicalMeshes)
        {
            foreach (MeshPrimitive primitive in mesh.Primitives)
            {
                if (primitive.Material?.FindChannel("Normal")?.Texture == null)
                {
                    continue;
                }

                IList<Vector3>? positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
                IList<Vector3>? normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                IList<Vector2>? uvs = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                if (positions == null || normals == null || uvs == null
                    || normals.Count != positions.Count || uvs.Count != positions.Count)
                {
                    errors.Add($"mesh '{mesh.Name}': a primitive with a normal texture lacks POSITION, NORMAL or TEXCOORD_0");
                    continue;
                }

                Vector4[] tangents = ComputeTangents(positions, normals, uvs, primitive.GetTriangleIndices());
                var bytes = new byte[tangents.Length * 16];
                for (int i = 0; i < tangents.Length; i++)
                {
                    BitConverter.TryWriteBytes(bytes.AsSpan(i * 16, 4), tangents[i].X);
                    BitConverter.TryWriteBytes(bytes.AsSpan((i * 16) + 4, 4), tangents[i].Y);
                    BitConverter.TryWriteBytes(bytes.AsSpan((i * 16) + 8, 4), tangents[i].Z);
                    BitConverter.TryWriteBytes(bytes.AsSpan((i * 16) + 12, 4), tangents[i].W);
                }

                BufferView view = model.UseBufferView(bytes, 0, null, 16, BufferMode.ARRAY_BUFFER);
                Accessor accessor = model.CreateAccessor("TANGENT");
                accessor.SetVertexData(view, 0, tangents.Length, DimensionType.VEC4, EncodingType.FLOAT, false);
                primitive.SetVertexAccessor("TANGENT", accessor);
                count++;
            }
        }

        return count;
    }

    private static Vector4[] ComputeTangents(
        IList<Vector3> positions,
        IList<Vector3> normals,
        IList<Vector2> uvs,
        IEnumerable<(int A, int B, int C)> triangles)
    {
        var accumulated = new Vector3[positions.Count];
        foreach ((int a, int b, int c) in triangles)
        {
            Vector3 e1 = positions[b] - positions[a];
            Vector3 e2 = positions[c] - positions[a];
            Vector2 d1 = uvs[b] - uvs[a];
            Vector2 d2 = uvs[c] - uvs[a];
            float determinant = (d1.X * d2.Y) - (d2.X * d1.Y);
            if (determinant == 0f)
            {
                continue; // degenerate UV mapping: no direction of increasing U
            }

            float r = 1f / determinant;
            Vector3 direction = ((d2.Y * e1) - (d1.Y * e2)) * r;
            if (!float.IsFinite(direction.X + direction.Y + direction.Z))
            {
                continue;
            }

            accumulated[a] += direction;
            accumulated[b] += direction;
            accumulated[c] += direction;
        }

        var tangents = new Vector4[positions.Count];
        for (int i = 0; i < tangents.Length; i++)
        {
            Vector3 normal = normals[i];
            Vector3 tangent = accumulated[i] - (normal * Vector3.Dot(normal, accumulated[i]));
            if (tangent.LengthSquared() < 1e-20f)
            {
                // No usable UV gradient: same fallback as the legacy processor.
                tangent = Vector3.Cross(normal, Vector3.UnitX);
                if (tangent.LengthSquared() < 1e-12f)
                {
                    tangent = Vector3.Cross(normal, Vector3.UnitY);
                }
            }

            tangent = Vector3.Normalize(tangent);
            tangents[i] = new Vector4(tangent, 1f);
        }

        return tangents;
    }

    private static IReadOnlyList<Assimp.Material> ReadAssimpMaterials(string sourceFile)
    {
        using var context = new AssimpContext();
        Assimp.Scene scene = context.ImportFile(sourceFile, MaterialImportSteps);
        return scene.Materials;
    }

    private static void DecomposeNodeTransforms(ModelRoot model, List<string> errors)
    {
        foreach (GltfNode node in model.LogicalNodes)
        {
            Matrix4x4 original = node.LocalMatrix;
            node.LocalTransform = node.LocalTransform.GetDecomposed();
            Matrix4x4 recomposed = node.LocalMatrix;
            if (!AreClose(original, recomposed))
            {
                errors.Add($"node '{node.Name}' matrix changed after decomposition");
            }
        }
    }

    private static bool AreClose(Matrix4x4 a, Matrix4x4 b)
    {
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                if (MathF.Abs(a[row, column] - b[row, column]) > MatrixTolerance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private int AttachTexture(
        ModelRoot model,
        SharpGLTF.Schema2.Material material,
        string channelName,
        LegacyEffectInstance effect,
        string effectParameter,
        string sourceFile,
        Dictionary<string, GltfImage> images,
        List<string> errors)
    {
        if (!effect.Strings.TryGetValue(effectParameter, out string? texturePath))
        {
            return 0;
        }

        string? sourceTexture = ResolveTexturePath(sourceFile, texturePath);
        if (sourceTexture == null)
        {
            errors.Add($"material '{material.Name}': {effectParameter} '{texturePath}' not found");
            return 0;
        }

        if (!Path.GetExtension(sourceTexture).Equals(".tga", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"material '{material.Name}': {effectParameter} '{texturePath}' is not a .tga texture");
            return 0;
        }

        MaterialChannel? channel = material.FindChannel(channelName);
        if (channel == null)
        {
            errors.Add($"material '{material.Name}': no {channelName} channel");
            return 0;
        }

        bool isNormalMap = effectParameter == "normalTexture";
        string pngFileName = Path.GetFileNameWithoutExtension(sourceTexture) + ".png";
        string pngPath = Path.Combine(_pngDirectory, pngFileName);
        if (_convertedTextures.Add(pngFileName))
        {
            LegacyTextureConverter.ConvertToPng(sourceTexture, pngPath, isNormalMap);
            _normalMapTextures[pngFileName] = isNormalMap;
        }
        else if (_normalMapTextures[pngFileName] != isNormalMap)
        {
            errors.Add($"material '{material.Name}': texture '{pngFileName}' is used both as a colour texture and as a normal map");
            return 0;
        }

        if (!images.TryGetValue(pngFileName, out GltfImage? image))
        {
            image = model.CreateImage(Path.GetFileNameWithoutExtension(pngFileName));
            image.Content = new MemoryImage(pngPath);
            image.AlternateWriteFileName = Path.GetRelativePath(_gltfDirectory, pngPath).Replace('\\', '/');
            images.Add(pngFileName, image);
        }

        channel.Value.SetTexture(0, image);
        return 1;
    }

    private static JsonObject BuildLegacyMaterial(Assimp.Material material)
    {
        return new JsonObject
        {
            ["diffuse"] = material.HasColorDiffuse ? ToJsonArray(material.ColorDiffuse.X, material.ColorDiffuse.Y, material.ColorDiffuse.Z, material.ColorDiffuse.W) : null,
            ["specular"] = material.HasColorSpecular ? ToJsonArray(material.ColorSpecular.X, material.ColorSpecular.Y, material.ColorSpecular.Z) : null,
            ["emissive"] = material.HasColorEmissive ? ToJsonArray(material.ColorEmissive.X, material.ColorEmissive.Y, material.ColorEmissive.Z) : null,
            ["shininess"] = material.HasShininess ? JsonValue.Create(material.Shininess) : null,
        };
    }

    private static JsonObject BuildLegacyEffect(LegacyEffectInstance effect)
    {
        var dwords = new JsonObject();
        foreach ((string name, int value) in effect.Dwords.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            dwords[name] = value;
        }

        var floats = new JsonObject();
        foreach ((string name, float[] values) in effect.Floats.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            floats[name] = ToJsonArray(values);
        }

        var strings = new JsonObject();
        foreach ((string name, string value) in effect.Strings.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            strings[name] = value;
        }

        return new JsonObject
        {
            ["file"] = effect.EffectFilePath,
            ["dwords"] = dwords,
            ["floats"] = floats,
            ["strings"] = strings,
        };
    }

    private static JsonArray ToJsonArray(params float[] values)
    {
        var array = new JsonArray();
        foreach (float value in values)
        {
            array.Add(JsonValue.Create(value));
        }

        return array;
    }

    // Same resolution as the old runtime importer (CasaEngine 295db0c6, StaticModelImporter.cs:679-697).
    private static string? ResolveTexturePath(string modelFilePath, string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath) || texturePath.StartsWith('*'))
        {
            return null;
        }

        string modelDirectory = Path.GetDirectoryName(modelFilePath)!;
        string candidate = Path.GetFullPath(Path.Combine(modelDirectory, texturePath));
        if (File.Exists(candidate))
        {
            return candidate;
        }

        candidate = Path.Combine(modelDirectory, Path.GetFileName(texturePath));
        return File.Exists(candidate)
            ? candidate
            : null;
    }
}
