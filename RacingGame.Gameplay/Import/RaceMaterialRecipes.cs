using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Materials.Runtime;
using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Import;

/// <summary>
/// The pure functions that compute a <see cref="RaceMaterialRecipe"/> for each path that builds a game material: the
/// scenery, posts, segments and start lights, the car, the car selection plates, and the track
/// materials written in code. The asset cooker writes each recipe as a cooked <c>lit-pbr</c> material, which the game
/// loads from the catalogue; the PBR family of each surface comes from <see cref="RacingGamePbrMaterialMapping"/>.
/// </summary>
public static class RaceMaterialRecipes
{
    public const string LegacyCarModelName = "Car";
    public const string CarNormalTextureName = "RacerCarNormal";

    public const string GuardRailTextureName = "Leitplanke.png";
    public const string GuardRailNormalTextureName = "LeitplankeNormal.png";
    public const string RoadColumnTextureName = "RoadCement.png";
    public const string RoadColumnNormalTextureName = "RoadCementNormal.png";
    public const string RoadNormalTextureName = "RoadNormal.png";
    public const string RoadTunnelTextureName = "RoadTunnel.png";
    public const string RoadTunnelNormalTextureName = "RoadTunnelNormal.png";

    // Landscape.cs: DiffuseWithDetail20 multiplies the terrain by 2 * LandscapeDetail sampled at the diffuse coordinates
    // times DetailFactor, set to 24 by ShaderEffect.cs.
    public const string TerrainDetailTextureName = "LandscapeDetail.png";
    public const float TerrainDetailTiling = 24f;

    public static readonly string[] CarDiffuseTextureNames = ["RacerCar", "RacerCar2", "RacerCar3"];
    public static readonly string[] RoadTextureCandidates = ["Road.png", "track.png", "RoadCement.png"];

    /// <summary>The ground and the terrain take the first of these that loads.</summary>
    public static readonly string[] GroundTextureCandidates = ["Landscape.png", "CityGround.png"];

    public static readonly string[] TerrainNormalTextureCandidates = ["LandscapeNormal.png", "CityGroundNormal.png"];

    // The original draws every car part but the glass with the default diffuse colour, whatever its .X material says
    // (Model.RenderCar: Material.DefaultDiffuseColor).
    private static readonly Color LegacyCarDefaultDiffuseColor = new(210, 210, 210, 255);

    /// <summary>A scenery, post, segment or start light material of the model <paramref name="modelName"/>.</summary>
    public static RaceMaterialRecipe ForScenery(string modelName, StaticModelImportedMaterial importedMaterial)
    {
        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);

        return new RaceMaterialRecipe
        {
            Name = $"{modelName}.{importedMaterial.DisplayName}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            NormalTextureFilePath = importedMaterial.NormalTextureFilePath,
            Family = RacingGamePbrMaterialMapping.ResolveImportedFamily(importedMaterial),
            BaseColor = Opaque(importedMaterial.DiffuseColor),
            EmissiveColor = presentation.EmissiveColor,
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
        string? normalTexturePath = ResolveCarNormalTexturePath(importedMaterial, texturesDirectory);
        RaceBaseTextureRole baseTextureRole = UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath)
            ? IsPaintMaterial(importedMaterial) ? RaceBaseTextureRole.CarPainted : RaceBaseTextureRole.CarUnpainted
            : RaceBaseTextureRole.File;

        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);

        return new RaceMaterialRecipe
        {
            Name = $"{LegacyCarModelName}.{importedMaterial.DisplayName}.Car{carIndex}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            BaseTextureRole = baseTextureRole,
            NormalTextureFilePath = normalTexturePath,
            Family = RacingGamePbrMaterialMapping.ResolveCarFamily(importedMaterial),
            BaseColor = RacingGamePbrMaterialMapping.IsCarGlassMaterial(importedMaterial)
                ? Opaque(importedMaterial.DiffuseColor)
                : LegacyCarDefaultDiffuseColor,
            EmissiveColor = presentation.EmissiveColor,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            Queue = presentation.Queue,
            AlphaCutoff = presentation.AlphaCutoff,
            RasterizerState = presentation.DisableBackfaceCulling ? RaceMaterialStates.CullNone : null,
        };
    }

    /// <summary>A material of the car selection plate model <paramref name="plateModelName"/>.</summary>
    public static RaceMaterialRecipe ForPlate(string plateModelName, StaticModelImportedMaterial importedMaterial)
    {
        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        return new RaceMaterialRecipe
        {
            Name = $"{plateModelName}.{importedMaterial.DisplayName}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            NormalTextureFilePath = importedMaterial.NormalTextureFilePath,
            Family = RacingGamePbrMaterialMapping.CarouselPlate,
            BaseColor = Opaque(importedMaterial.DiffuseColor),
            EmissiveColor = presentation.EmissiveColor,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            Queue = presentation.Queue,
        };
    }

    /// <summary>
    /// The road. <paramref name="diffuseTexturePath"/> is the first of <see cref="RoadTextureCandidates"/> that loads, or null;
    /// <paramref name="normalTexturePath"/> is <see cref="RoadNormalTextureName"/> when it exists, or null (the original draws the
    /// road with the material ("Road", "RoadNormal"), Track.cs). The road mesh has tangents, so the engine enables the normal map.
    /// </summary>
    public static RaceMaterialRecipe ForRoad(string trackName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.RoadMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            Family = RacingGamePbrMaterialMapping.Road,
            BaseColor = diffuseTexturePath != null ? Color.White : new Color(60, 62, 66),
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
            Family = RacingGamePbrMaterialMapping.Terrain,
            BaseColor = diffuseTexturePath != null ? Color.White : new Color(182, 166, 118),
            SamplerState = RaceMaterialStates.AnisotropicWrap,
        };
    }

    /// <summary>
    /// The terrain. The paths are the first of <see cref="GroundTextureCandidates"/> and <see cref="TerrainNormalTextureCandidates"/> that
    /// load, or null; <paramref name="detailTexturePath"/> is <see cref="TerrainDetailTextureName"/> when it exists, or null.
    /// </summary>
    public static RaceMaterialRecipe ForTerrain(string trackName, string? diffuseTexturePath, string? normalTexturePath, string? detailTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.TerrainMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            DetailTextureFilePath = detailTexturePath,
            DetailTiling = TerrainDetailTiling,
            Family = RacingGamePbrMaterialMapping.Terrain,
            BaseColor = diffuseTexturePath != null ? Color.White : new Color(182, 166, 118),
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
            Family = RacingGamePbrMaterialMapping.GuardRail,
            BaseColor = diffuseTexturePath != null ? Color.White : new Color(182, 182, 182),
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
            Family = RacingGamePbrMaterialMapping.Road,
            BaseColor = diffuseTexturePath != null ? Color.White : new Color(176, 176, 176),
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
        };
    }

    /// <summary>
    /// The road tunnels: RacingGame's roadTunnelMaterial (Track.cs), "mainly ambient color" with ambient (182, 182, 182),
    /// diffuse (80, 80, 80) and specular (64, 64, 64), and the default specular power of its Material class (24). The
    /// original draws the tunnels with culling off, to show their inside and outside.
    /// </summary>
    public static RaceMaterialRecipe ForRoadTunnel(string trackName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.RoadTunnelMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            Family = RacingGamePbrMaterialMapping.Road,
            BaseColor = new Color(80, 80, 80),
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullNone,
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

    // The diffuse colour as a base colour: its alpha forced to 255.
    private static Color Opaque(Color color) => new(color.R, color.G, color.B, (byte)255);

    internal static bool IsPaintMaterial(StaticModelImportedMaterial importedMaterial)
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
