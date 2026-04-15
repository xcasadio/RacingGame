using System;
using System.Runtime.CompilerServices;
using MGUI.Shared.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGame.UI.MGUI;

internal static class UiImageResources
{
    private static readonly ConditionalWeakTable<Texture2D, MonoGameImageResource> ImageCache = new();

    public static IUIImageResource AsImage(Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        return ImageCache.GetValue(texture, static value => new MonoGameImageResource(value));
    }
}