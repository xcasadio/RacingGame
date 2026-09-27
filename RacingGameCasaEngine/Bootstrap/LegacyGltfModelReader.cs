using System.Text.Json.Nodes;
using CasaEngine.Framework.Assets.Loaders;
using Microsoft.Xna.Framework;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Reads a legacy RacingGame model converted to glTF by scripts/LegacyModelGltfConverter and restores the
/// material metadata the removed CasaEngine StaticModelImporter read from the original .x file
/// (CasaEngine 295db0c6, StaticModelImporter.cs:159-200 and 345-392). The converter stores that metadata
/// in the material extras: "legacyMaterial" (Assimp material values) and "legacyEffect" (EffectInstance).
/// Self-contained on purpose (CasaEngine, SharpGLTF and the BCL only): the metadata parity probe compiles it as is.
/// </summary>
internal static class LegacyGltfModelReader
{
    private static readonly GltfStaticModelReader Reader = new();

    public static bool IsFileSupported(string filePath) => Reader.IsFileSupported(filePath);

    /// <summary>
    /// Reads <paramref name="filePath"/>, rebuilds the legacy material metadata, then applies
    /// <paramref name="legacyMaterialImportProfile"/> (null keeps the neutral profile the reader applied),
    /// in the order of the old importer so the profile sees the effect metadata.
    /// </summary>
    public static StaticModelImportResult ReadWithMetadata(string filePath, ILegacyMaterialImportProfile? legacyMaterialImportProfile)
    {
        StaticModelImportResult result = Reader.ReadWithMetadata(filePath, null);
        if (result.Materials.Count == 0)
        {
            return result;
        }

        ModelRoot root = ModelRoot.Load(filePath, new ReadSettings { Validation = ValidationMode.Skip });
        for (int i = 0; i < result.Materials.Count; i++)
        {
            StaticModelImportedMaterial material = result.Materials[i];
            JsonNode? extras = material.MaterialIndex >= 0 && material.MaterialIndex < root.LogicalMaterials.Count
                ? root.LogicalMaterials[material.MaterialIndex].Extras
                : null;

            ApplyLegacyMaterial(material, extras?["legacyMaterial"] as JsonObject);
            ApplyLegacyEffect(material, extras?["legacyEffect"] as JsonObject, filePath);

            if (legacyMaterialImportProfile != null)
            {
                ApplyLegacyImportProfile(material, filePath, legacyMaterialImportProfile);
            }
        }

        return result;
    }

    // Material values of the old BuildMaterials (StaticModelImporter.cs:172-186).
    private static void ApplyLegacyMaterial(StaticModelImportedMaterial material, JsonObject? legacyMaterial)
    {
        if (legacyMaterial == null)
        {
            return;
        }

        material.AmbientColor = Vector3.Zero;
        material.DiffuseColor = legacyMaterial["diffuse"] is JsonArray diffuse ? ToXnaColor(ToFloats(diffuse)) : Color.White;
        material.EmissiveColor = legacyMaterial["emissive"] is JsonArray emissive ? ToVector3(ToFloats(emissive)) : Vector3.Zero;
        material.SpecularColor = legacyMaterial["specular"] is JsonArray specular ? ToVector3(ToFloats(specular)) : new Vector3(0.5f);
        material.SpecularPower = legacyMaterial["shininess"] is JsonValue shininess ? shininess.GetValue<float>() : 16.0f;
    }

    // Effect instance values of the old ApplyLegacyEffectMetadata (StaticModelImporter.cs:345-392).
    private static void ApplyLegacyEffect(StaticModelImportedMaterial material, JsonObject? legacyEffect, string modelFilePath)
    {
        if (legacyEffect == null)
        {
            return;
        }

        material.EffectFilePath = ResolveRelativePath(modelFilePath, legacyEffect["file"]?.GetValue<string>());

        if (legacyEffect["dwords"]?["technique"] is JsonValue technique)
        {
            material.LegacyTechniqueIndex = technique.GetValue<int>();
        }

        var floats = legacyEffect["floats"] as JsonObject;
        if (TryReadFloats(floats, "ambientColor", 3, out float[] ambientColor))
        {
            material.AmbientColor = ToVector3(ambientColor);
        }

        if (TryReadFloats(floats, "diffuseColor", 3, out float[] diffuseColor))
        {
            material.DiffuseColor = ToXnaColor(diffuseColor);
        }

        if (TryReadFloats(floats, "specularColor", 3, out float[] specularColor))
        {
            material.SpecularColor = ToVector3(specularColor);
        }

        if (TryReadFloats(floats, "shininess", 1, out float[] shininess))
        {
            material.SpecularPower = shininess[0];
        }

        var strings = legacyEffect["strings"] as JsonObject;
        if (strings?["diffuseTexture"] is JsonValue diffuseTexture)
        {
            material.DiffuseTextureFilePath = ResolveTexturePath(modelFilePath, diffuseTexture.GetValue<string>()) ?? material.DiffuseTextureFilePath;
        }

        if (strings?["normalTexture"] is JsonValue normalTexture)
        {
            material.NormalTextureFilePath = ResolveTexturePath(modelFilePath, normalTexture.GetValue<string>()) ?? material.NormalTextureFilePath;
        }

        if (strings?["reflectionCubeTexture"] is JsonValue reflectionTexture)
        {
            string reflectionTexturePath = reflectionTexture.GetValue<string>();
            material.ReflectionTextureFilePath = ResolveTexturePath(modelFilePath, reflectionTexturePath)
                ?? ResolveRelativePath(modelFilePath, reflectionTexturePath);
        }
    }

    // Same assignments as GltfStaticModelReader.ApplyLegacyImportProfile (GltfStaticModelReader.cs:505-520).
    private static void ApplyLegacyImportProfile(
        StaticModelImportedMaterial material,
        string modelFilePath,
        ILegacyMaterialImportProfile legacyMaterialImportProfile)
    {
        string modelName = Path.GetFileNameWithoutExtension(modelFilePath);
        LegacyMaterialImportInterpretation interpretation = legacyMaterialImportProfile.Interpret(new LegacyMaterialImportContext(
            SourceAssetPath: modelFilePath,
            SourceAssetName: modelName,
            ImportedMaterial: material));

        material.SurfaceIntent = interpretation.SurfaceIntent;
        material.AlphaCutoutHint = interpretation.AlphaCutout;
        material.BrightAmbientHint = interpretation.BrightAmbient;
        material.UsesReflection |= interpretation.Reflection;
    }

    private static bool TryReadFloats(JsonObject? floats, string key, int minimumCount, out float[] values)
    {
        values = floats?[key] is JsonArray array ? ToFloats(array) : Array.Empty<float>();
        return values.Length >= minimumCount;
    }

    private static float[] ToFloats(JsonArray array)
    {
        var values = new float[array.Count];
        for (int i = 0; i < array.Count; i++)
        {
            values[i] = array[i]!.GetValue<float>();
        }

        return values;
    }

    private static Vector3 ToVector3(float[] values) => new(values[0], values[1], values[2]);

    // Same conversion as the old importer (TryReadColor and ToXnaColor with ClampByte).
    private static Color ToXnaColor(float[] values) => new(
        ClampByte(values[0]),
        ClampByte(values[1]),
        ClampByte(values[2]),
        ClampByte(values.Length >= 4 ? values[3] : 1.0f));

    private static byte ClampByte(float value)
    {
        float scaled = value <= 1.0f
            ? value * 255.0f
            : value;
        scaled = Math.Clamp(scaled, 0.0f, 255.0f);
        return (byte)scaled;
    }

    // Same resolution as the old importer (StaticModelImporter.cs:679-697).
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

    // Same as the old importer (StaticModelImporter.cs:699-708).
    private static string ResolveRelativePath(string modelFilePath, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        string modelDirectory = Path.GetDirectoryName(modelFilePath)!;
        return Path.GetFullPath(Path.Combine(modelDirectory, relativePath));
    }
}
