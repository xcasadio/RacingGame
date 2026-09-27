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
using GltfNode = SharpGLTF.Schema2.Node;

// Converts the legacy RacingGame .x models to .gltf files the CasaEngine runtime can read:
// - geometry through the editor converter (AssimpToGltfConverter), node transforms decomposed
//   to translation/rotation/scale (GltfStaticModelReader cannot read matrix nodes);
// - diffuse and normal textures of each material attached as external .png images;
// - the legacy material metadata the old runtime importer used stored in the material extras.
// Usage: LegacyModelGltfConverter <x models directory> <gltf output directory> <png output directory> [--skip <model name>]...

const string Usage = "Usage: LegacyModelGltfConverter <x models directory> <gltf output directory> <png output directory> [--skip <model name>]...";
var positional = new List<string>();
var skippedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--skip" && i + 1 < args.Length)
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

Console.WriteLine($"{sourceFiles.Length} models, {skipped} skipped, {failures} failed, {converter.ConvertedTextureCount} textures converted.");
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

        summary = $"{model.LogicalMeshes.Count} meshes, {model.LogicalMaterials.Count} materials, {effectMaterials} with effect instance, {texturedChannels} textured channels";
        return errors;
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

        string pngFileName = Path.GetFileNameWithoutExtension(sourceTexture) + ".png";
        string pngPath = Path.Combine(_pngDirectory, pngFileName);
        if (_convertedTextures.Add(pngFileName))
        {
            LegacyTextureConverter.ConvertToPng(sourceTexture, pngPath);
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
