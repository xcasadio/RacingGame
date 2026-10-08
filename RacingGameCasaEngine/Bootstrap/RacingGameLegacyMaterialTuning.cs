using CasaEngine.Framework.Assets.Loaders;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Bootstrap;

internal readonly record struct RacingGameLegacyMaterialRuntimeTuning(
    bool EnableReflection,
    float SpecularColorScale,
    float SpecularPowerScale,
    float? MaxSpecularPower)
{
    public Vector3 ApplySpecularColor(Vector3 specularColor)
        => Vector3.Clamp(specularColor * SpecularColorScale, Vector3.Zero, Vector3.One);

    public float ApplySpecularPower(float specularPower)
    {
        float tunedPower = specularPower * SpecularPowerScale;
        if (MaxSpecularPower.HasValue)
        {
            tunedPower = Math.Min(tunedPower, MaxSpecularPower.Value);
        }

        return Math.Max(tunedPower, 0.0f);
    }
}

internal static class RacingGameLegacyMaterialTuning
{
    private static readonly string[] MasonryTextures =
    [
        "Building",
        "building2",
        "building3",
        "building4",
        "building5",
        "Hotel01",
        "ruin",
        "Ruin01",
        "Stone04",
        "Stone5",
    ];

    private static readonly string[] VegetationTextures =
    [
        "plants",
        "PalmLeave",
    ];

    private static readonly string[] SignageTextures =
    [
        "Schild",
        "Schild2",
        "Schild_Kurve_links",
        "SignWarning",
        "banner",
        "banner2",
        "banner3",
        "roadsign1",
        "roadsign2",
        "Goal",
        "plazaschild",
        "plazacasino",
        "ladyluck",
    ];

    private static readonly string[] SignalTextures =
    [
        "Light",
        "TLight",
        "streetlamp",
        "streetlamp2",
    ];

    private static readonly string[] MatteIndustrialTextures =
    [
        "Hydrant",
        "garbagecan",
        "OilWell",
        "Oiltank",
        "Leitplanke",
        "gelaender",
        "Windmill",
    ];

    public static bool ShouldEnableReflection(string sourceAssetName, StaticModelImportedMaterial importedMaterial)
    {
        string effectFileName = Path.GetFileName(importedMaterial.EffectFilePath ?? string.Empty);
        if (effectFileName.Equals("ReflectionSimpleGlass.fx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static RacingGameLegacyMaterialRuntimeTuning EvaluateRuntimeTuning(
        string sourceAssetName,
        StaticModelImportedMaterial importedMaterial)
    {
        string effectFileName = Path.GetFileName(importedMaterial.EffectFilePath ?? string.Empty);
        bool enableReflection = importedMaterial.UsesReflection && ShouldEnableReflection(sourceAssetName, importedMaterial);

        if (effectFileName.Equals("ReflectionSimpleGlass.fx", StringComparison.OrdinalIgnoreCase))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(true, 1.0f, 1.0f, null);
        }

        string textureName = Path.GetFileNameWithoutExtension(importedMaterial.DiffuseTextureFilePath ?? string.Empty);
        if (IsVegetationTexture(textureName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.08f, 0.35f, 8.0f);
        }

        if (IsMasonryTexture(textureName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.12f, 0.50f, 12.0f);
        }

        if (IsSignageTexture(textureName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.18f, 0.45f, 12.0f);
        }

        if (IsSignalTexture(textureName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.12f, 0.45f, 10.0f);
        }

        if (IsMatteIndustrialTexture(textureName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.22f, 0.60f, 14.0f);
        }

        if (IsCarSurface(sourceAssetName, importedMaterial))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(enableReflection, 1.0f, 1.0f, enableReflection ? null : 18.0f);
        }

        return new RacingGameLegacyMaterialRuntimeTuning(enableReflection, 1.0f, 1.0f, null);
    }

    private static bool IsCarSurface(string sourceAssetName, StaticModelImportedMaterial importedMaterial)
    {
        if (sourceAssetName.StartsWith("Car", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string textureName = Path.GetFileNameWithoutExtension(importedMaterial.DiffuseTextureFilePath ?? string.Empty);
        return textureName.Equals("RacerCar", StringComparison.OrdinalIgnoreCase)
            || textureName.Equals("CarSelectionPlate", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMasonryTexture(string textureName)
        => ContainsTexture(MasonryTextures, textureName);

    private static bool IsVegetationTexture(string textureName)
        => ContainsTexture(VegetationTextures, textureName);

    private static bool IsSignageTexture(string textureName)
        => ContainsTexture(SignageTextures, textureName);

    private static bool IsSignalTexture(string textureName)
        => ContainsTexture(SignalTextures, textureName);

    private static bool IsMatteIndustrialTexture(string textureName)
        => ContainsTexture(MatteIndustrialTextures, textureName);

    // Legacy texture names without extension: the project holds the .tga textures as .png files of the same name.
    private static bool ContainsTexture(string[] candidates, string textureName)
        => candidates.Any(candidate => candidate.Equals(textureName, StringComparison.OrdinalIgnoreCase));
}