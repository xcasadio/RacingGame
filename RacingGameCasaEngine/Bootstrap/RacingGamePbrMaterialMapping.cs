using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Metallic-roughness parameters of one family of surfaces of the race world (plan rgce-pbr-race-world, "Correspondance").
/// Starting values, tuned by eye afterwards: no metallic-roughness texture is authored, each material carries scalars.
/// </summary>
internal readonly record struct RacingGamePbrFamily(float Metallic, float Roughness, float ClearCoat = 0f, float ClearCoatRoughness = 0.1f);

/// <summary>
/// The single place that turns a legacy surface description (the properties of the former LitDiffuseMaterial) into a
/// <see cref="LitPbrMaterial"/>: the family table and the rules common to every conversion.
/// </summary>
internal static class RacingGamePbrMaterialMapping
{
    /// <summary>Terrain: dielectric, fully rough.</summary>
    public static readonly RacingGamePbrFamily Terrain = new(0f, 1f);

    /// <summary>Road, road underside, tunnel, columns, city ground: rough dielectric.</summary>
    public static readonly RacingGamePbrFamily Road = new(0f, 0.85f);

    /// <summary>Guard rails (built in code, and the imported Leitplanke texture): metal.</summary>
    public static readonly RacingGamePbrFamily GuardRail = new(1f, 0.45f);

    /// <summary>Lamp posts and signals (textures Light, TLight, streetlamp, streetlamp2): metal.</summary>
    public static readonly RacingGamePbrFamily LampPost = new(1f, 0.5f);

    /// <summary>Masonry: rough dielectric.</summary>
    public static readonly RacingGamePbrFamily Masonry = new(0f, 0.9f);

    /// <summary>Signs and banners: dielectric.</summary>
    public static readonly RacingGamePbrFamily Signage = new(0f, 0.6f);

    /// <summary>Vegetation (plants, PalmLeave): rough dielectric, alpha cut-out kept by the caller.</summary>
    public static readonly RacingGamePbrFamily Vegetation = new(0f, 0.95f);

    /// <summary>Matte industrial surfaces: dielectric.</summary>
    public static readonly RacingGamePbrFamily MatteIndustrial = new(0f, 0.7f);

    /// <summary>Glass of the buildings: an opaque smooth metal, a mirror of the sky.</summary>
    public static readonly RacingGamePbrFamily BuildingGlass = new(1f, 0.05f);

    /// <summary>Car paint: dielectric under a clear coat.</summary>
    public static readonly RacingGamePbrFamily CarPaint = new(0f, 0.5f, 1f, 0.05f);

    /// <summary>Car chrome: smooth metal.</summary>
    public static readonly RacingGamePbrFamily CarChrome = new(1f, 0.1f);

    /// <summary>Car windows: opaque smooth metal, as they are drawn today.</summary>
    public static readonly RacingGamePbrFamily CarGlass = new(1f, 0.05f);

    /// <summary>Car tyres: rough dielectric.</summary>
    public static readonly RacingGamePbrFamily CarTyre = new(0f, 0.9f);

    /// <summary>Plate of the car selection carousel.</summary>
    public static readonly RacingGamePbrFamily CarouselPlate = new(1f, 0.3f);

    /// <summary>Anything without a matching family.</summary>
    public static readonly RacingGamePbrFamily Default = new(0f, 0.8f);

    /// <summary>
    /// Builds the PBR material of a surface. Common rules: the diffuse texture becomes the base colour texture and the normal
    /// map stays; the former DiffuseColor becomes BaseColor with its alpha forced to 255 (a lower alpha would move the
    /// material to the transparent queue); the emissive colour is kept; the per-material ambient colour, specular colour and
    /// power and reflection settings are dropped (the ambient comes from the environment); queue, alpha cut-off, culling and
    /// sampling are kept as given.
    /// </summary>
    public static LitPbrMaterial Create(
        string name,
        RacingGamePbrFamily family,
        Texture2D? baseColorTexture,
        Texture2D? normalTexture,
        Color? diffuseColor,
        RasterizerState rasterizerState,
        SamplerState samplerState,
        Vector3 emissiveColor = default,
        Texture2D? detailTexture = null,
        float detailTiling = 1f,
        RenderQueue queue = RenderQueue.Opaque,
        float alphaCutoff = 0.5f)
    {
        Color color = diffuseColor ?? Color.White;
        return new LitPbrMaterial
        {
            Name = name,
            BaseColorTexture = baseColorTexture,
            NormalTexture = normalTexture,
            DetailTexture = detailTexture,
            DetailTiling = detailTiling,
            BaseColor = new Color(color.R, color.G, color.B, (byte)255),
            Metallic = family.Metallic,
            Roughness = family.Roughness,
            ClearCoat = family.ClearCoat,
            ClearCoatRoughness = family.ClearCoatRoughness,
            EmissiveColor = emissiveColor,
            Queue = queue,
            AlphaCutoff = alphaCutoff,
            RasterizerState = rasterizerState,
            SamplerState = samplerState,
        };
    }
}
