using System.Runtime.CompilerServices;
using CasaEngine.Framework.UI.Backend.MonoGame.Assets;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Rendering;

namespace RacingGameCasaEngine.UI;

internal static class UiImageResources
{
    private static readonly ConditionalWeakTable<RenderTarget2D, CasaMonoGameRenderTarget> RenderTargetCache = new();

    public static IUIRenderTarget AsRenderTarget(RenderTarget2D renderTarget)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        return RenderTargetCache.GetValue(renderTarget, static value => new CasaMonoGameRenderTarget(value));
    }
}