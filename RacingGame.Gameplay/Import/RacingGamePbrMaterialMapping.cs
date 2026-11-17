using CasaEngine.Framework.Assets.Loaders;

namespace RacingGame.Gameplay.Import;

/// <summary>
/// The family table of the surfaces of the race world and the rules that pick a family: one place for the PBR values the
/// recipes carry. Ported from the other machine's <c>RacingGamePbrMaterialMapping</c> (origin/remaster), which builds the
/// materials in code; here the recipes carry the values and the asset cooker writes them.
/// </summary>
public static class RacingGamePbrMaterialMapping
{
    /// <summary>Terrain and flat ground: dielectric, fully rough.</summary>
    public static readonly RacingGamePbrFamily Terrain = new(nameof(Terrain), 0f, 1f);

    /// <summary>Road, road underside, tunnel, columns: rough dielectric.</summary>
    public static readonly RacingGamePbrFamily Road = new(nameof(Road), 0f, 0.85f);

    /// <summary>Guard rails (built in code, and the imported Leitplanke texture): metal.</summary>
    public static readonly RacingGamePbrFamily GuardRail = new(nameof(GuardRail), 1f, 0.45f);

    /// <summary>Lamp posts and signals (textures Light, TLight, streetlamp, streetlamp2): metal.</summary>
    public static readonly RacingGamePbrFamily LampPost = new(nameof(LampPost), 1f, 0.5f);

    /// <summary>Masonry: rough dielectric.</summary>
    public static readonly RacingGamePbrFamily Masonry = new(nameof(Masonry), 0f, 0.9f);

    /// <summary>Signs and banners: dielectric.</summary>
    public static readonly RacingGamePbrFamily Signage = new(nameof(Signage), 0f, 0.6f);

    /// <summary>Vegetation (plants, PalmLeave): rough dielectric, alpha cut-out kept by the recipe.</summary>
    public static readonly RacingGamePbrFamily Vegetation = new(nameof(Vegetation), 0f, 0.95f);

    /// <summary>Matte industrial surfaces: dielectric.</summary>
    public static readonly RacingGamePbrFamily MatteIndustrial = new(nameof(MatteIndustrial), 0f, 0.7f);

    /// <summary>Glass of the buildings: an opaque smooth metal, a mirror of the sky.</summary>
    public static readonly RacingGamePbrFamily BuildingGlass = new(nameof(BuildingGlass), 1f, 0.05f);

    /// <summary>Car paint: dielectric under a clear coat.</summary>
    public static readonly RacingGamePbrFamily CarPaint = new(nameof(CarPaint), 0f, 0.5f, 1f, 0.05f);

    /// <summary>Car chrome: smooth metal.</summary>
    public static readonly RacingGamePbrFamily CarChrome = new(nameof(CarChrome), 1f, 0.1f);

    /// <summary>Car windows: opaque smooth metal.</summary>
    public static readonly RacingGamePbrFamily CarGlass = new(nameof(CarGlass), 1f, 0.05f);

    /// <summary>Car tyres: rough dielectric.</summary>
    public static readonly RacingGamePbrFamily CarTyre = new(nameof(CarTyre), 0f, 0.9f);

    /// <summary>Plate of the car selection carousel.</summary>
    public static readonly RacingGamePbrFamily CarouselPlate = new(nameof(CarouselPlate), 1f, 0.3f);

    /// <summary>Anything without a matching family.</summary>
    public static readonly RacingGamePbrFamily Default = new(nameof(Default), 0f, 0.8f);

    /// <summary>The legacy effect of the glass of the buildings and of the car.</summary>
    public const string GlassEffectFileName = "ReflectionSimpleGlass.fx";

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

    private static readonly string[] LampPostTextures =
    [
        "Light.tga",
        "TLight.tga",
        "streetlamp.tga",
        "streetlamp2.tga",
    ];

    private static readonly string[] GuardRailTextures =
    [
        "Leitplanke.tga",
    ];

    private static readonly string[] MatteIndustrialTextures =
    [
        "Hydrant.tga",
        "garbagecan.tga",
        "OilWell.tga",
        "Oiltank.tga",
        "gelaender.tga",
        "Windmill.tga",
    ];

    public static bool IsMasonryTexture(string textureFileName) => ContainsTexture(MasonryTextures, textureFileName);

    public static bool IsVegetationTexture(string textureFileName) => ContainsTexture(VegetationTextures, textureFileName);

    public static bool IsSignageTexture(string textureFileName) => ContainsTexture(SignageTextures, textureFileName);

    public static bool IsLampPostTexture(string textureFileName) => ContainsTexture(LampPostTextures, textureFileName);

    public static bool IsGuardRailTexture(string textureFileName) => ContainsTexture(GuardRailTextures, textureFileName);

    /// <summary>Matte industrial textures, the guard rail texture (Leitplanke) excluded: it belongs to <see cref="GuardRail"/>.</summary>
    public static bool IsMatteIndustrialTexture(string textureFileName) => ContainsTexture(MatteIndustrialTextures, textureFileName);

    /// <summary>Whether the material uses the legacy glass effect (<see cref="GlassEffectFileName"/>).</summary>
    public static bool UsesGlassEffect(StaticModelImportedMaterial importedMaterial)
        => Path.GetFileName(importedMaterial.EffectFilePath ?? string.Empty).Equals(GlassEffectFileName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Family of an imported scenery material: the glass of the buildings is recognised by its legacy effect
    /// (ReflectionSimpleGlass.fx), every other family by the file name of the diffuse texture; anything else is
    /// <see cref="Default"/>.
    /// </summary>
    public static RacingGamePbrFamily ResolveImportedFamily(StaticModelImportedMaterial importedMaterial)
    {
        if (UsesGlassEffect(importedMaterial))
        {
            return BuildingGlass;
        }

        string textureFileName = Path.GetFileName(importedMaterial.DiffuseTextureFilePath ?? string.Empty);
        if (IsGuardRailTexture(textureFileName))
        {
            return GuardRail;
        }

        if (IsLampPostTexture(textureFileName))
        {
            return LampPost;
        }

        if (IsMasonryTexture(textureFileName))
        {
            return Masonry;
        }

        if (IsSignageTexture(textureFileName))
        {
            return Signage;
        }

        if (IsVegetationTexture(textureFileName))
        {
            return Vegetation;
        }

        if (IsMatteIndustrialTexture(textureFileName))
        {
            return MatteIndustrial;
        }

        return Default;
    }

    /// <summary>
    /// Whether a car material is a window: its name holds <c>glass</c> or <c>fenster</c>, or it uses the legacy glass effect
    /// (the cooked glass of the car is named <c>Standard 19</c> and is only recognisable by its effect).
    /// </summary>
    public static bool IsCarGlassMaterial(StaticModelImportedMaterial importedMaterial)
    {
        string displayName = importedMaterial.DisplayName;
        return displayName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("fenster", StringComparison.OrdinalIgnoreCase)
            || importedMaterial.EffectFilePath?.Contains("ReflectionSimpleGlass", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Family of a car material (the rule of <c>LegacyCarVisualFactory.ResolveFamily</c> of origin/remaster): windows, then
    /// chrome, tyres (<c>gummi</c>), paint (<see cref="RaceMaterialRecipes.IsPaintMaterial"/>), anything else
    /// <see cref="Default"/>.
    /// </summary>
    public static RacingGamePbrFamily ResolveCarFamily(StaticModelImportedMaterial importedMaterial)
    {
        if (IsCarGlassMaterial(importedMaterial))
        {
            return CarGlass;
        }

        string displayName = importedMaterial.DisplayName;
        if (displayName.Contains("chrome", StringComparison.OrdinalIgnoreCase))
        {
            return CarChrome;
        }

        if (displayName.Contains("gummi", StringComparison.OrdinalIgnoreCase))
        {
            return CarTyre;
        }

        return RaceMaterialRecipes.IsPaintMaterial(importedMaterial) ? CarPaint : Default;
    }

    private static bool ContainsTexture(string[] candidates, string textureFileName)
        => candidates.Any(candidate => IsSameTexture(candidate, textureFileName));

    /// <summary>
    /// Two texture file names designate the same texture when they differ only by case and extension: the legacy models name
    /// their textures .tga, while the converted content of the project folder only has the .png files, and the importer keeps
    /// whichever file it finds.
    /// </summary>
    public static bool IsSameTexture(string textureFileName, string otherTextureFileName)
        => Path.GetFileNameWithoutExtension(textureFileName).Equals(Path.GetFileNameWithoutExtension(otherTextureFileName), StringComparison.OrdinalIgnoreCase);
}
