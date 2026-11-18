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

    // A 4x4 opaque black image: the occlusion texture of the road, see ForRoad.
    public const string RoadOcclusionTextureName = "NoEnvironmentLight.png";
    public const string CityGroundTextureName = "CityGround.png";
    public const string CityGroundNormalTextureName = "CityGroundNormal.png";
    public const string RoadBackTextureName = "RoadBack.png";
    public const string RoadBackNormalTextureName = "RoadBackNormal.png";
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

    // The diffuse colour D of the generated track materials of the original, which the original multiplies into every pixel
    // (texture * (A + D * N.L), NormalMapping.fx) and which the base colour of a lit-pbr material carries here, as on the other
    // machine (CreateLegacyLighting of origin/remaster). Each value is read in RacingGameOriginal/Core:
    // - the road, its underside and the columns are built with Material(diffuse texture, normal texture), which leaves the
    //   default diffuse colour 210 (Tracks/Track.cs:73-79 and :94-96, Graphics/Material.cs:31);
    // - the terrain's diffuse is 234 (Landscapes/Landscape.cs:597-606, "diffuse (also bright)");
    // - the guard rails' diffuse is 182 (Tracks/Track.cs:101-105);
    // - the city ground's diffuse is 200 (Landscapes/Landscape.cs:614-620);
    // - the tunnel's diffuse is 80 (Tracks/Track.cs:84-89), see ForRoadTunnel.
    private const byte LegacyDefaultDiffuse = 210;
    private const byte LegacyTerrainDiffuse = 234;
    private const byte LegacyGuardRailDiffuse = 182;
    private const byte LegacyCityGroundDiffuse = 200;

    // The base colour of the car glass. The original's glass diffuse colour (13) is no reflectance for a metal: the glass came out
    // black. 224 on the three channels is a linear reflectance of about 0.75, the original's reflection at normal incidence.
    // Measured on the rear window of Beginner-start (ADR-0033): the distance to the original falls from 132.9 to 39.5.
    // ADR-0039: the colour is no longer that grey. The glass is a mirror of the sky, so a grey base came out sky blue where the
    // original's glass is a neutral grey; this base compensates the colour of the reflected sky. Found by iteration on the same box
    // (the rule and the steps are in the ADR): the box reads 134.4 / 137.7 / 137.3 for 136.9 / 137.6 / 137.9 in the original.
    private static readonly Color LegacyCarGlassBaseColor = new(255, 174, 148);

    // The signs' emissive level: the emissive colour is the material ambient of the original times this level, on the emissive
    // texture (the base texture). The original lights a sign with texture * material ambient; level 1 is that term unchanged
    // (ADR-0033, chosen among 0.5, 0.75, 1.0 and 1.3 by the rule written before the measurement).
    private const float SignageEmissiveLevel = 1f;

    // The leaves of the palms, designated by their base texture: a grey base level and the alpha test of the original (0.5,
    // NormalMapping.fx) instead of the imported white and the cutoff of the import presentation (ADR-0037). With those the leaves
    // were brighter than the original's and covered more pixels. The cause of the difference in brightness is not established:
    // the level is a calibration against the original's captures, chosen by the rule written before the measurement.
    // The window glass of the buildings (ADR-0038), designated by model and material. The original draws it as the reflection of
    // the sky, not tinted by the glass colour, plus a small ambient: its windows are light on the faces turned away from the sun.
    // Here the glass is a mirror (BuildingGlass) whose base colour tints the reflection, and a face turned away from the sun is in
    // its own shadow. Each value was chosen per material by the rule written before the measurement: the reflectance grey 224 of
    // the car glass (ADR-0033) where it beat the imported colour, and no received shadow where that beat the shadowed glass.
    private const byte WindowGlassBaseLevel = 224;

    private static bool IsWindowGlass(string modelName, string materialName, string windowModel, string windowMaterial)
        => modelName.Equals(windowModel, StringComparison.OrdinalIgnoreCase) && materialName.Equals(windowMaterial, StringComparison.OrdinalIgnoreCase);

    private static readonly string[] PalmLeafTextures = ["PalmLeave.tga", "PalmLeave2.tga"];
    private const byte PalmLeafBaseLevel = 170;
    private const float PalmLeafAlphaCutoff = 0.5f;

    // The tint of a material times its diffuse colour D, rounded as CreateLegacyLighting of the other machine does:
    // (c * D + 127) / 255 per channel. The tint is white when the material has a texture.
    private static Color WithLegacyDiffuse(Color tint, byte diffuse)
    {
        return new Color(
            (byte)((tint.R * diffuse + 127) / 255),
            (byte)((tint.G * diffuse + 127) / 255),
            (byte)((tint.B * diffuse + 127) / 255),
            (byte)255);
    }

    /// <summary>A scenery, post, segment or start light material of the model <paramref name="modelName"/>.</summary>
    public static RaceMaterialRecipe ForScenery(string modelName, StaticModelImportedMaterial importedMaterial)
    {
        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        RacingGamePbrFamily family = RacingGamePbrMaterialMapping.ResolveImportedFamily(importedMaterial);

        // A sign carries the material ambient of the original as an emissive: its own texture times the imported ambient colour
        // (ADR-0033, on the precedent of the tunnel, ADR-0030). lit-pbr has no ambient per material, and without it the signs
        // were nearly black on the faces turned away from the sun.
        bool isSignage = family == RacingGamePbrMaterialMapping.Signage;
        bool isHotel01Glass = IsWindowGlass(modelName, importedMaterial.DisplayName, "Hotel01", "39");
        bool isHotel02Glass = IsWindowGlass(modelName, importedMaterial.DisplayName, "Hotel02", "fenster");
        bool isCasino01Glass = IsWindowGlass(modelName, importedMaterial.DisplayName, "Casino01", "39");
        bool hasNeutralGlassBase = isHotel02Glass || isCasino01Glass;
        bool isPalmLeaf = importedMaterial.DiffuseTextureFilePath != null
            && PalmLeafTextures.Any(texture => RacingGamePbrMaterialMapping.IsSameTexture(texture, importedMaterial.DiffuseTextureFilePath));

        return new RaceMaterialRecipe
        {
            Name = $"{modelName}.{importedMaterial.DisplayName}",
            BaseTextureFilePath = importedMaterial.DiffuseTextureFilePath,
            NormalTextureFilePath = importedMaterial.NormalTextureFilePath,
            Family = family,
            BaseColor = isPalmLeaf ? new Color(PalmLeafBaseLevel, PalmLeafBaseLevel, PalmLeafBaseLevel, (byte)255)
                : hasNeutralGlassBase ? new Color(WindowGlassBaseLevel, WindowGlassBaseLevel, WindowGlassBaseLevel, (byte)255)
                : Opaque(importedMaterial.DiffuseColor),
            ReceiveShadows = !(isHotel01Glass || isHotel02Glass),
            EmissiveColor = isSignage ? importedMaterial.AmbientColor * SignageEmissiveLevel : presentation.EmissiveColor,
            EmissiveTextureFilePath = isSignage ? importedMaterial.DiffuseTextureFilePath : null,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            Queue = presentation.Queue,
            AlphaCutoff = isPalmLeaf ? PalmLeafAlphaCutoff : presentation.AlphaCutoff,
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
                ? LegacyCarGlassBaseColor
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

    // The top of the road takes the RoadSurface family (roughness 0.30) and no environment light (ADR-0034). The original's road has a
    // sharp sun highlight and no sky reflection; the engine's lit-pbr occlusion scales the image-based light only, so a black occlusion
    // texture at full strength removes the sky from the road while the sun highlight stays. The values were chosen by measurement.
    private const float RoadOcclusionStrength = 1f;

    /// <summary>
    /// The road. <paramref name="diffuseTexturePath"/> is the first of <see cref="RoadTextureCandidates"/> that loads, or null;
    /// <paramref name="normalTexturePath"/> is <see cref="RoadNormalTextureName"/> when it exists, or null (the original draws the
    /// road with the material ("Road", "RoadNormal"), Track.cs). The road mesh has tangents, so the engine enables the normal map.
    /// <paramref name="occlusionTexturePath"/> is <see cref="RoadOcclusionTextureName"/> when it exists, or null (the road then keeps
    /// the environment light).
    /// </summary>
    public static RaceMaterialRecipe ForRoad(string trackName, string? diffuseTexturePath, string? normalTexturePath, string? occlusionTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.RoadMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            OcclusionTextureFilePath = occlusionTexturePath,
            OcclusionStrength = RoadOcclusionStrength,
            Family = RacingGamePbrMaterialMapping.RoadSurface,
            BaseColor = WithLegacyDiffuse(diffuseTexturePath != null ? Color.White : new Color(60, 62, 66), LegacyDefaultDiffuse),
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
        };
    }

    /// <summary>
    /// The underside of the road: the original's roadBackMaterial, ("RoadBack", "RoadBackNormal") (Track.cs), drawn like the road
    /// (anisotropic sampler, counter-clockwise culling).
    /// </summary>
    public static RaceMaterialRecipe ForRoadBack(string trackName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.RoadBackMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            Family = RacingGamePbrMaterialMapping.Road,
            BaseColor = WithLegacyDiffuse(diffuseTexturePath != null ? Color.White : new Color(60, 62, 66), LegacyDefaultDiffuse),
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
            SamplerState = RaceMaterialStates.AnisotropicWrap,
        };
    }

    /// <summary>
    /// The city ground plane: the original's cityMat, ("CityGround", "CityGroundNormal") (Landscape.cs), drawn like the road
    /// (counter-clockwise culling). The paths are <see cref="CityGroundTextureName"/> and <see cref="CityGroundNormalTextureName"/>
    /// when they exist, or null.
    /// </summary>
    public static RaceMaterialRecipe ForGround(string trackName, string? diffuseTexturePath, string? normalTexturePath)
    {
        return new RaceMaterialRecipe
        {
            Name = $"{trackName}.GroundMaterial",
            BaseTextureFilePath = diffuseTexturePath,
            NormalTextureFilePath = normalTexturePath,
            Family = RacingGamePbrMaterialMapping.Terrain,
            BaseColor = WithLegacyDiffuse(diffuseTexturePath != null ? Color.White : new Color(182, 166, 118), LegacyCityGroundDiffuse),
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
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
            BaseColor = WithLegacyDiffuse(diffuseTexturePath != null ? Color.White : new Color(182, 166, 118), LegacyTerrainDiffuse),
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
            BaseColor = WithLegacyDiffuse(diffuseTexturePath != null ? Color.White : new Color(182, 182, 182), LegacyGuardRailDiffuse),
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
            BaseColor = WithLegacyDiffuse(diffuseTexturePath != null ? Color.White : new Color(176, 176, 176), LegacyDefaultDiffuse),
            SamplerState = RaceMaterialStates.AnisotropicWrap,
            RasterizerState = RaceMaterialStates.CullCounterClockwise,
        };
    }

    // The tunnel's material ambient A of the original is 182 (Tracks/Track.cs:84-89, "Use mainly ambient color (tunnel uses
    // lightmaps)"). The original renders texture * (A + D * N.L); lit-pbr has no ambient per material, so the term texture * A is an
    // emissive: the emissive colour below times the diffuse texture, added by the engine without the shadow tint (LitPbr.fx:521-557).
    // The level was the author's choice of 0.5 (2026-11-17, ADR-0030, open point O16 of ai-agent/tasks/rgce-pbr-handover-tasks.md): a
    // neutral grey on the three channels, between A / 255 (182 / 255 = 0.714, the original's ambient, measured too bright) and
    // A / 255 times the original's shadow tint (0.25, 0.26, 0.27), about 0.18 (measured too dark: the tunnel barely changed).
    // ADR-0033 replaces it by 0.6 with the shadow reaching the emissive (LegacyTunnelEmissiveShadowFactor 1: shadowed like the rest
    // of the pixel), on the author's own criterion of O16 (the median of the top band of the three tunnel views within 1.5 of the
    // original's 30.382): 29.338 for 28.314, and the mean difference per channel from the original falls from 16.255 to 15.030.
    private static readonly Vector3 LegacyTunnelEmissiveFactor = new(0.6f);

    private const float LegacyTunnelEmissiveShadowFactor = 1f;

    /// <summary>
    /// The road tunnels: RacingGame's roadTunnelMaterial (Track.cs), "mainly ambient color" with ambient (182, 182, 182),
    /// diffuse (80, 80, 80) and specular (64, 64, 64), and the default specular power of its Material class (24). The
    /// original draws the tunnels with culling off, to show their inside and outside. The ambient is the emissive colour
    /// <see cref="LegacyTunnelEmissiveFactor"/> times the emissive texture <paramref name="diffuseTexturePath"/>.
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
            EmissiveColor = LegacyTunnelEmissiveFactor,
            EmissiveTextureFilePath = diffuseTexturePath,
            EmissiveShadowFactor = LegacyTunnelEmissiveShadowFactor,
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
