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
/// Every value of a game <c>LitDiffuseMaterial</c>, computed apart from building the material: no graphics device and
/// no loaded texture. Textures are named by file path; render states by name, <c>null</c> where the game leaves the
/// state <c>null</c> (the renderer then applies its default). The builders never set the shadow flags or
/// <c>IsTransparent</c>, so those keep the material defaults and are not part of the recipe.
/// </summary>
public sealed record RaceMaterialRecipe
{
    public string Name { get; init; } = string.Empty;

    public string? BaseTextureFilePath { get; init; }

    public RaceBaseTextureRole BaseTextureRole { get; init; } = RaceBaseTextureRole.File;

    public string? NormalTextureFilePath { get; init; }

    /// <summary>
    /// The reflection cube file the material loads, or <c>null</c> when it names none. When the file cannot be loaded
    /// the material takes the scene cube instead: see <see cref="ResolveUseSceneReflectionCube"/>.
    /// </summary>
    public string? ReflectionCubeFilePath { get; init; }

    public bool UseSceneReflectionCube { get; init; }

    public Color DiffuseColor { get; init; } = Color.White;

    public Vector3 AmbientColor { get; init; } = Vector3.Zero;

    public Vector3 EmissiveColor { get; init; } = Vector3.Zero;

    public Vector3 SpecularColor { get; init; } = new(0.5f);

    public float SpecularPower { get; init; } = 16.0f;

    public float ReflectionAddAmount { get; init; } = 1.0f;

    public float ReflectionMultiplyBase { get; init; } = 1.0f;

    public float ReflectionMultiplyFactor { get; init; }

    public RenderQueue Queue { get; init; } = RenderQueue.Opaque;

    public float AlphaCutoff { get; init; } = 0.5f;

    public string? RasterizerState { get; init; }

    public string? SamplerState { get; init; }

    public string? BlendState { get; init; }

    public string? DepthStencilState { get; init; }

    /// <summary>
    /// Whether the material samples the scene reflection cube, given whether <see cref="ReflectionCubeFilePath"/>
    /// loaded: a named cube that cannot be loaded falls back to the scene cube.
    /// </summary>
    public bool ResolveUseSceneReflectionCube(bool reflectionCubeLoaded)
        => UseSceneReflectionCube || (ReflectionCubeFilePath != null && !reflectionCubeLoaded);
}
