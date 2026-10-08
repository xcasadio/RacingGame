using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Materials.Runtime;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Import;

/// <summary>
/// The pure functions that compute a <see cref="RaceMaterialRecipe"/> for each path that builds a game material: the
/// scenery, posts, segments and start lights, the car, the car selection plates, and the track
/// materials written in code. The logic moved here from the builders, unchanged; the asset cooker writes each recipe
/// as a cooked material, which the game loads from the catalogue.
/// </summary>
public static class RaceMaterialRecipes
{
    public const string LegacyCarModelName = "Car";
    public const string CarNormalTextureName = "RacerCarNormal";
    // RacingGame's plate (NormalMapping.fx, ReflectionSpecular) added a Fresnel-weighted sky reflection to its texture.
    public const float PlateReflectionAmount = 0.35f;

    public const string GuardRailTextureName = "Leitplanke.png";
    public const string GuardRailNormalTextureName = "LeitplankeNormal.png";
    public const string RoadColumnTextureName = "RoadCement.png";
    public const string RoadColumnNormalTextureName = "RoadCementNormal.png";

    public static readonly string[] CarDiffuseTextureNames = ["RacerCar", "RacerCar2", "RacerCar3"];
    public static readonly string[] RoadTextureCandidates = ["Road.png", "track.png", "RoadCement.png"];

    /// <summary>The ground and the terrain take the first of these that loads.</summary>
    public static readonly string[] GroundTextureCandidates = ["Landscape.png", "CityGround.png"];

    public static readonly string[] TerrainNormalTextureCandidates = ["LandscapeNormal.png", "CityGroundNormal.png"];

    private const float LegacyCarReflectionMultiplyBase = 0.85f;
    private const float LegacyCarReflectionMultiplyFactor = 0.75f;

    /// <summary>A scenery, post, segment or start light material of the model <paramref name="modelName"/>.</summary>
    public static RaceMaterialRecipe ForScenery(string modelName, StaticModelImportedMaterial importedMaterial)
    {
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning(modelName, importedMaterial);
        (bool useSceneReflectionCube, string? reflectionCubeFilePath) = ResolveReflection(tuning, importedMaterial);

        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        Vector3 specularColor = tuning.ApplySpecularColor(importedMaterial.SpecularColor);
        float specularPower = Math.Clamp(tuning.ApplySpecularPower(importedMaterial.SpecularPower), 2f, 48f);

        return new RaceMaterialRecipe
        {
            Name = $"{modelName}.{importedMaterial.DisplayName}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            NormalTextureFilePath = importedMaterial.NormalTextureFilePath,
            ReflectionCubeFilePath = reflectionCubeFilePath,
            UseSceneReflectionCube = useSceneReflectionCube,
            DiffuseColor = importedMaterial.DiffuseColor,
            AmbientColor = presentation.AmbientColor,
            EmissiveColor = presentation.EmissiveColor,
            SpecularColor = specularColor,
            SpecularPower = specularPower,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            Queue = presentation.Queue,
            AlphaCutoff = presentation.AlphaCutoff,
            RasterizerState = presentation.DisableBackfaceCulling ? RaceMaterialStates.CullNone : null,
        };
    }

    /// <summary>
    /// A material of the car <paramref name="carIndex"/>. <paramref name="texturesDirectory"/> is the project's
    /// Textures directory, where the car's normal map is.
    /// </summary>
    public static RaceMaterialRecipe ForCar(StaticModelImportedMaterial importedMaterial, int carIndex, string texturesDirectory)
    {
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning(LegacyCarModelName, importedMaterial);
        string? normalTexturePath = ResolveCarNormalTexturePath(importedMaterial, texturesDirectory);
        RaceBaseTextureRole baseTextureRole = UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath)
            ? IsPaintMaterial(importedMaterial) ? RaceBaseTextureRole.CarPainted : RaceBaseTextureRole.CarUnpainted
            : RaceBaseTextureRole.File;
        (bool useSceneReflectionCube, string? reflectionCubeFilePath) = ResolveReflection(tuning, importedMaterial);

        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        Vector3 specularColor = tuning.ApplySpecularColor(importedMaterial.SpecularColor);
        float specularPower = Math.Clamp(tuning.ApplySpecularPower(importedMaterial.SpecularPower), 2f, 48f);
        bool useLegacyCarReflectionBlend = UsesLegacyCarReflectionBlend(importedMaterial);

        return new RaceMaterialRecipe
        {
            Name = $"{LegacyCarModelName}.{importedMaterial.DisplayName}.Car{carIndex}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            BaseTextureRole = baseTextureRole,
            NormalTextureFilePath = normalTexturePath,
            ReflectionCubeFilePath = reflectionCubeFilePath,
            UseSceneReflectionCube = useSceneReflectionCube,
            DiffuseColor = importedMaterial.DiffuseColor,
            AmbientColor = presentation.AmbientColor,
            EmissiveColor = presentation.EmissiveColor,
            SpecularColor = specularColor,
            SpecularPower = specularPower,
            ReflectionAddAmount = useLegacyCarReflectionBlend ? 0f : 1f,
            ReflectionMultiplyBase = useLegacyCarReflectionBlend ? LegacyCarReflectionMultiplyBase : 1f,
            ReflectionMultiplyFactor = useLegacyCarReflectionBlend ? LegacyCarReflectionMultiplyFactor : 0f,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            Queue = presentation.Queue,
            AlphaCutoff = presentation.AlphaCutoff,
            RasterizerState = presentation.DisableBackfaceCulling ? RaceMaterialStates.CullNone : null,
        };
    }

    /// <summary>A material of the car selection plate model <paramref name="plateModelName"/>: no per-name tuning.</summary>
    public static RaceMaterialRecipe ForPlate(string plateModelName, StaticModelImportedMaterial importedMaterial)
    {
        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        return new RaceMaterialRecipe
        {
            Name = $"{plateModelName}.{importedMaterial.DisplayName}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            NormalTextureFilePath = importedMaterial.NormalTextureFilePath,
            DiffuseColor = importedMaterial.DiffuseColor,
            AmbientColor = presentation.AmbientColor,
            EmissiveColor = presentation.EmissiveColor,
            SpecularColor = importedMaterial.SpecularColor,
            SpecularPower = Math.Clamp(importedMaterial.SpecularPower, 2f, 48f),
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            Queue = presentation.Queue,
            UseSceneReflectionCube = importedMaterial.UsesReflection,
            ReflectionAddAmount = PlateReflectionAmount,
        };
    }

    /// <summary>The road. <paramref name="diffuseTexturePath"/> is the first of <see cref="RoadTextureCandidates"/> that loads, or null.</summary>
    public static RaceMaterialRecipe ForRoad(string trackName, string? diffuseTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.RoadMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            DiffuseColor = diffuseTexturePath != null ? Color.White : new Color(60, 62, 66),
            EmissiveColor = new Vector3(0.015f, 0.015f, 0.015f),
            SpecularColor = new Vector3(0.14f),
            SpecularPower = 18f,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
        };
    }

    /// <summary>The flat ground. <paramref name="diffuseTexturePath"/> is the first of <see cref="GroundTextureCandidates"/> that loads, or null.</summary>
    public static RaceMaterialRecipe ForGround(string trackName, string? diffuseTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.GroundMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            DiffuseColor = diffuseTexturePath != null ? Color.White : new Color(182, 166, 118),
            EmissiveColor = new Vector3(0.01f, 0.01f, 0.008f),
            SpecularColor = new Vector3(0.05f),
            SpecularPower = 6f,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
        };
    }

    /// <summary>The terrain. The paths are the first of <see cref="GroundTextureCandidates"/> and <see cref="TerrainNormalTextureCandidates"/> that load, or null.</summary>
    public static RaceMaterialRecipe ForTerrain(string trackName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.TerrainMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            DiffuseColor = diffuseTexturePath != null ? Color.White : new Color(182, 166, 118),
            EmissiveColor = new Vector3(0.01f, 0.01f, 0.008f),
            SpecularColor = new Vector3(0.05f),
            SpecularPower = 6f,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
        };
    }

    /// <summary>A guard rail of the side <paramref name="sideName"/>.</summary>
    public static RaceMaterialRecipe ForGuardRail(string trackName, string sideName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.GuardRailMaterial.{sideName}",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            DiffuseColor = diffuseTexturePath != null ? Color.White : new Color(182, 182, 182),
            EmissiveColor = new Vector3(0.03f, 0.03f, 0.03f),
            SpecularColor = new Vector3(0.88f, 0.88f, 0.88f),
            SpecularPower = 16f,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
        };
    }

    /// <summary>The road columns.</summary>
    public static RaceMaterialRecipe ForRoadColumn(string trackName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.ColumnMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            DiffuseColor = diffuseTexturePath != null ? Color.White : new Color(176, 176, 176),
            EmissiveColor = new Vector3(0.01f, 0.01f, 0.01f),
            SpecularColor = new Vector3(0.18f),
            SpecularPower = 8f,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
        };
    }

    public static bool UsesCarDiffuseTexture(string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return false;
        }

        string textureName = Path.GetFileNameWithoutExtension(texturePath);
        for (int index = 0; index < CarDiffuseTextureNames.Length; index++)
        {
            if (textureName.Equals(CarDiffuseTextureNames[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // The scene cube when the material reflects without naming a cube of its own; a named cube is loaded from its file.
    private static (bool UseSceneReflectionCube, string? ReflectionCubeFilePath) ResolveReflection(
        RacingGameLegacyMaterialRuntimeTuning tuning,
        StaticModelImportedMaterial importedMaterial)
    {
        bool useSceneReflectionCube = tuning.EnableReflection
            && RaceSkySystem.ShouldUseSceneReflectionCube(importedMaterial.ReflectionTextureFilePath);
        string? reflectionCubeFilePath = tuning.EnableReflection && !useSceneReflectionCube
            ? importedMaterial.ReflectionTextureFilePath
            : null;
        return (useSceneReflectionCube, reflectionCubeFilePath);
    }

    private static bool IsPaintMaterial(StaticModelImportedMaterial importedMaterial)
    {
        string displayName = importedMaterial.DisplayName;
        if (displayName.Contains("chrome", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("gummi", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("fenster", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (displayName.Contains("lack", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("paint", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return importedMaterial.UsesReflection && UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath);
    }

    private static bool UsesLegacyCarReflectionBlend(StaticModelImportedMaterial importedMaterial)
    {
        if (!importedMaterial.UsesReflection)
        {
            return false;
        }

        string effectFileName = Path.GetFileName(importedMaterial.EffectFilePath ?? string.Empty);
        if (!effectFileName.Equals("NormalMapping.fx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string displayName = importedMaterial.DisplayName;
        return !displayName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            && !displayName.Contains("fenster", StringComparison.OrdinalIgnoreCase)
            && !displayName.Contains("gummi", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveCarNormalTexturePath(StaticModelImportedMaterial importedMaterial, string texturesDirectory)
    {
        if (UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath)
            || UsesCarNormalTexture(importedMaterial.NormalTextureFilePath))
        {
            return Path.Combine(texturesDirectory, CarNormalTextureName + ".png");
        }

        return importedMaterial.NormalTextureFilePath;
    }

    private static bool UsesCarNormalTexture(string? texturePath)
    {
        return !string.IsNullOrWhiteSpace(texturePath)
            && Path.GetFileNameWithoutExtension(texturePath).Equals(CarNormalTextureName, StringComparison.OrdinalIgnoreCase);
    }
}
