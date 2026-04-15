using System.Runtime.CompilerServices;
using CasaEngine.Framework.UI.Backend.MonoGame.Assets;
using Microsoft.Xna.Framework.Graphics;
using MGUI.Shared.Assets;
using MGUI.Shared.Rendering;

namespace RacingGameCasaEngine.UI;

internal static class UiImageResources
{
    private static readonly ConditionalWeakTable<Texture2D, CasaMonoGameImageResource> ImageCache = new();
    private static readonly ConditionalWeakTable<RenderTarget2D, CasaMonoGameRenderTarget> RenderTargetCache = new();

    public static IUIImageResource AsImage(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        return ImageCache.GetValue(texture, static value => new CasaMonoGameImageResource(value));
    }

    public static IUIRenderTarget AsRenderTarget(RenderTarget2D renderTarget)
    {
        ArgumentNullException.ThrowIfNull(renderTarget);
        return RenderTargetCache.GetValue(renderTarget, static value => new CasaMonoGameRenderTarget(value));
    }
}