using CasaEngine.Framework.Materials.Runtime;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGame.Gameplay.Import;

/// <summary>Builds a game <see cref="LitDiffuseMaterial"/> from a <see cref="RaceMaterialRecipe"/> and the textures it names.</summary>
internal static class RaceMaterialBuilder
{
    public static LitDiffuseMaterial Create(
        RaceMaterialRecipe recipe,
        Texture2D? baseColor,
        Texture2D? normalMap,
        TextureCube? reflectionCube)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        return new LitDiffuseMaterial
        {
            Name = recipe.Name,
            BasColor = baseColor,
            NormalMap = normalMap,
            ReflectionCube = reflectionCube,
            UseSceneReflectionCube = recipe.ResolveUseSceneReflectionCube(reflectionCube != null),
            DiffuseColor = recipe.DiffuseColor,
            AmbientColor = recipe.AmbientColor,
            EmissiveColor = recipe.EmissiveColor,
            SpecularColor = recipe.SpecularColor,
            SpecularPower = recipe.SpecularPower,
            ReflectionAddAmount = recipe.ReflectionAddAmount,
            ReflectionMultiplyBase = recipe.ReflectionMultiplyBase,
            ReflectionMultiplyFactor = recipe.ReflectionMultiplyFactor,
            Queue = recipe.Queue,
            AlphaCutoff = recipe.AlphaCutoff,
            RasterizerState = ToRasterizerState(recipe.RasterizerState),
            SamplerState = ToSamplerState(recipe.SamplerState),
            BlendState = ToBlendState(recipe.BlendState),
            DepthStencilState = ToDepthStencilState(recipe.DepthStencilState),
        };
    }

    private static RasterizerState? ToRasterizerState(string? name) => name switch
    {
        null => null,
        RaceMaterialStates.CullNone => RasterizerState.CullNone,
        RaceMaterialStates.CullCounterClockwise => RasterizerState.CullCounterClockwise,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown rasterizer state."),
    };

    private static SamplerState? ToSamplerState(string? name) => name switch
    {
        null => null,
        RaceMaterialStates.AnisotropicWrap => SamplerState.AnisotropicWrap,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown sampler state."),
    };

    private static BlendState? ToBlendState(string? name) => name == null
        ? null
        : throw new ArgumentOutOfRangeException(nameof(name), name, "The game sets no blend state.");

    private static DepthStencilState? ToDepthStencilState(string? name) => name == null
        ? null
        : throw new ArgumentOutOfRangeException(nameof(name), name, "The game sets no depth state.");
}
