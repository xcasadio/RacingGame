using CasaEngine.Framework.Materials.Runtime;
using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Import;

/// <summary>Names of the render states the game's materials set, as MonoGame names its static state objects.</summary>
public static class RaceMaterialStates
{
    public const string CullNone = "CullNone";
    public const string CullCounterClockwise = "CullCounterClockwise";
    public const string AnisotropicWrap = "AnisotropicWrap";
}

/// <summary>Which texture a car material samples as its base colour.</summary>
public enum RaceBaseTextureRole
{
    /// <summary>The texture named by <see cref="RaceMaterialRecipe.BaseTextureFilePath"/>, loaded as it is.</summary>
    File,

    /// <summary>The car texture painted with the selected car colour (the car paint material).</summary>
    CarPainted,

    /// <summary>The car texture as it is, with the colour key (the other car materials that use it).</summary>
    CarUnpainted,
}

/// <summary>
/// Every value of a <c>lit-pbr</c> material of the race world, computed apart from building the material: no graphics
/// device and no loaded texture. Textures are named by file path; render states by name, <c>null</c> where the game leaves
/// the state <c>null</c> (the renderer then applies its default). The builders never set the shadow flags or
/// <c>IsTransparent</c>, so those keep the material defaults and are not part of the recipe.
/// <para/>
/// The surface is described by its PBR family (metallic, roughness, clear coat: <see cref="RacingGamePbrMaterialMapping"/>)
/// and its base colour; the legacy ambient, specular and reflection settings of the former <c>lit-diffuse</c> materials
/// are gone (the ambient and the reflections come from the environment).
/// </summary>
public sealed record RaceMaterialRecipe
{
    public string Name { get; init; } = string.Empty;

    public string? BaseTextureFilePath { get; init; }

    public RaceBaseTextureRole BaseTextureRole { get; init; } = RaceBaseTextureRole.File;

    public string? NormalTextureFilePath { get; init; }

    /// <summary>The detail texture of the material (the terrain's), or <c>null</c> when it has none.</summary>
    public string? DetailTextureFilePath { get; init; }

    /// <summary>Number of times the detail texture repeats over the base texture coordinates.</summary>
    public float DetailTiling { get; init; } = 1f;

    /// <summary>The PBR family of the surface: <see cref="RacingGamePbrMaterialMapping"/>.</summary>
    public RacingGamePbrFamily Family { get; init; } = RacingGamePbrMaterialMapping.Default;

    public float Metallic => Family.Metallic;

    public float Roughness => Family.Roughness;

    public float ClearCoat => Family.ClearCoat;

    public float ClearCoatRoughness => Family.ClearCoatRoughness;

    /// <summary>
    /// The <c>base_color</c>: the diffuse colour of the legacy material with its alpha forced to 255 (a lower alpha would
    /// move the material to the transparent queue); 210 on every car part but the glass (<c>Model.RenderCar</c> of the
    /// original).
    /// </summary>
    public Color BaseColor { get; init; } = Color.White;

    /// <summary>
    /// The emissive colour, a display colour that the engine multiplies by <see cref="EmissiveTextureFilePath"/> when it is set
    /// (LitPbr.fx: <c>emissive = EmissiveColor * EmissiveTexture</c>).
    /// </summary>
    public Vector3 EmissiveColor { get; init; } = Vector3.Zero;

    /// <summary>
    /// The emissive texture, or <c>null</c> when the material has none (the case of every material but the road tunnel's and the
    /// signs'): the cooker then writes no <c>emissive_texture</c> key.
    /// </summary>
    public string? EmissiveTextureFilePath { get; init; }

    /// <summary>
    /// How much the emissive colour receives the shadow, 0 (the engine default: never shadowed) to 1 (shadowed like the rest of the
    /// pixel). The cooker writes no <c>emissive_shadow_factor</c> key while it is 0.
    /// </summary>
    public float EmissiveShadowFactor { get; init; }

    /// <summary>
    /// The flat ambient of the material, a display colour (the engine's <c>ambient_color</c>, engine ADR-0146): the engine adds
    /// base colour x ambient colour to the pixel, detail texture included, without the occlusion of the material, and with the
    /// shadow tint of the race world. (0, 0, 0), the default, adds nothing; the cooker writes no <c>ambient_color</c> key then.
    /// </summary>
    public Vector3 AmbientColor { get; init; } = Vector3.Zero;

    /// <summary>
    /// The occlusion texture (its red channel), or <c>null</c> when the material has none (every material but the road's, the
    /// terrain's, the city ground's and those of the scenery families that lose the light of the sky): the cooker then writes
    /// neither the <c>occlusion_texture</c> nor the <c>occlusion_strength</c> key. The engine's
    /// <c>lit-pbr</c> occlusion, 1 + <see cref="OcclusionStrength"/> x (red - 1), scales the image-based light only, never the
    /// direct light nor the flat ambient of the material.
    /// </summary>
    public string? OcclusionTextureFilePath { get; init; }

    /// <summary>
    /// How much the occlusion texture applies, 0 (none) to 1 (the full red channel). Only cooked, as <c>occlusion_strength</c>, with
    /// an <see cref="OcclusionTextureFilePath"/>.
    /// </summary>
    public float OcclusionStrength { get; init; } = 1f;

    /// <summary>
    /// Whether the material receives the shadow of the world, true unless a recipe says otherwise. Cooked as
    /// <c>receive_shadows</c>. In the race world's shadow mode a surface that does not receive the shadow keeps its reflection of the
    /// environment on the faces turned away from the sun.
    /// </summary>
    public bool ReceiveShadows { get; init; } = true;

    public RenderQueue Queue { get; init; } = RenderQueue.Opaque;

    public float AlphaCutoff { get; init; } = 0.5f;

    public string? RasterizerState { get; init; }

    public string? SamplerState { get; init; }

    public string? BlendState { get; init; }

    public string? DepthStencilState { get; init; }
}
