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
        "Building.tga",
        "building2.tga",
        "building3.tga",
        "building4.tga",
        "building5.tga",
        "Hotel01.tga",
        "ruin.tga",
        "Ruin01.tga",
        "Stone04.tga",
        "Stone5.tga",
    ];

    private static readonly string[] VegetationTextures =
    [
        "plants.tga",
        "PalmLeave.tga",
    ];

    private static readonly string[] SignageTextures =
    [
        "Schild.tga",
        "Schild2.tga",
        "Schild_Kurve_links.tga",
        "SignWarning.tga",
        "banner.tga",
        "banner2.tga",
        "banner3.tga",
        "roadsign1.tga",
        "roadsign2.tga",
        "Goal.tga",
        "plazaschild.tga",
        "plazacasino.tga",
        "ladyluck.tga",
    ];

    private static readonly string[] SignalTextures =
    [
        "Light.tga",
        "TLight.tga",
        "streetlamp.tga",
        "streetlamp2.tga",
    ];

    private static readonly string[] MatteIndustrialTextures =
    [
        "Hydrant.tga",
        "garbagecan.tga",
        "OilWell.tga",
        "Oiltank.tga",
        "Leitplanke.tga",
        "gelaender.tga",
        "Windmill.tga",
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

        string textureFileName = Path.GetFileName(importedMaterial.DiffuseTextureFilePath ?? string.Empty);
        if (IsVegetationTexture(textureFileName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.08f, 0.35f, 8.0f);
        }

        if (IsMasonryTexture(textureFileName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.12f, 0.50f, 12.0f);
        }

        if (IsSignageTexture(textureFileName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.18f, 0.45f, 12.0f);
        }

        if (IsSignalTexture(textureFileName))
        {
            return new RacingGameLegacyMaterialRuntimeTuning(false, 0.12f, 0.45f, 10.0f);
        }

        if (IsMatteIndustrialTexture(textureFileName))
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

        string textureFileName = Path.GetFileName(importedMaterial.DiffuseTextureFilePath ?? string.Empty);
        return textureFileName.Equals("RacerCar.tga", StringComparison.OrdinalIgnoreCase)
            || textureFileName.Equals("CarSelectionPlate.tga", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMasonryTexture(string textureFileName)
        => ContainsTexture(MasonryTextures, textureFileName);

    private static bool IsVegetationTexture(string textureFileName)
        => ContainsTexture(VegetationTextures, textureFileName);

    private static bool IsSignageTexture(string textureFileName)
        => ContainsTexture(SignageTextures, textureFileName);

    private static bool IsSignalTexture(string textureFileName)
        => ContainsTexture(SignalTextures, textureFileName);

    private static bool IsMatteIndustrialTexture(string textureFileName)
        => ContainsTexture(MatteIndustrialTextures, textureFileName);

    private static bool ContainsTexture(string[] candidates, string textureFileName)
        => candidates.Any(candidate => candidate.Equals(textureFileName, StringComparison.OrdinalIgnoreCase));
}