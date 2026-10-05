using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.BorderBrushes;
using MGUI.Core.UI.Brushes.FillBrushes;
using MGUI.Shared.Helpers;
using Color = Microsoft.Xna.Framework.Color;
using Thickness = MonoGame.Extended.Thickness;

namespace RacingGameCasaEngine.UI;

/// <summary>
/// Hover, focus and selection looks of the menu screens' buttons, applied in code every frame (RGCE ADR-0001: MGUI XAML
/// visual states cannot express them, see docs/mgui-gaps-from-rgce-xaml-screens.md). The buttons themselves are XAML.
/// </summary>
internal static class LegacyMenuUiTheme
{
    public static readonly Color AccentColor = new(255, 156, 0);
    public static readonly Color PrimaryTextColor = Color.White;
    public static readonly Color MutedTextColor = new(212, 212, 212);
    public static readonly Color SubtleBorderColor = new(255, 255, 255, 70);
    private static readonly MGUniformBorderBrush InactiveBorderBrush = new(new Color(28, 28, 28));
    private static readonly MGUniformBorderBrush ActiveBorderBrush = new(new Color(255, 176, 42));
    private static readonly MGUniformBorderBrush FaceBorderBrush = new(new Color(118, 118, 118));
    private static readonly MGUniformBorderBrush FaceActiveBorderBrush = new(new Color(255, 212, 148));

    public static void ApplyMenuTextButtonState(MGButton button, bool isActive)
    {
        button.BorderBrush = new MGUniformBorderBrush(isActive ? new Color(255, 176, 42) : new Color(96, 96, 96));
        button.BorderThickness = new Thickness(isActive ? 3 : 2);
        button.Opacity = isActive ? 1f : 0.94f;
    }

    public static void ApplyBandButtonState(MGButton button, bool isActive)
    {
        button.BorderBrush = new MGUniformBorderBrush(isActive ? AccentColor : SubtleBorderColor);
        button.BorderThickness = new Thickness(isActive ? 3 : 2);
        if (button.Content is MGTextBlock label)
        {
            label.Foreground = new(isActive ? AccentColor : PrimaryTextColor, isActive ? AccentColor : PrimaryTextColor, isActive ? AccentColor : PrimaryTextColor);
        }
    }

    public static void ApplySpriteButtonState(MGButton button)
    {
        if (button.Tag is MGImage image)
        {
            image.Opacity = button.VisualState.IsFocused || button.IsHovered ? 1f : 0.92f;
        }
    }

    public static void ApplyMainMenuButtonState(MGButton button, MGBorder face, MGTextBlock label, bool isActive)
    {
        button.BorderBrush = isActive ? ActiveBorderBrush : InactiveBorderBrush;
        button.BorderThickness = new Thickness(5);
        face.BorderBrush = isActive ? FaceActiveBorderBrush : FaceBorderBrush;
        face.BackgroundBrush = new VisualStateFillBrush((isActive ? new Color(184, 184, 184) : new Color(172, 172, 172)).AsFillBrush());
        if (face.Content is MGBorder middle)
        {
            middle.BackgroundBrush = new VisualStateFillBrush((isActive ? new Color(228, 223, 214) : new Color(224, 224, 224)).AsFillBrush());
            if (middle.Content is MGBorder center)
            {
                center.BackgroundBrush = new VisualStateFillBrush((isActive ? new Color(255, 248, 236) : new Color(248, 248, 248)).AsFillBrush());
            }
        }
        button.BackgroundBrush = new VisualStateFillBrush((isActive ? new Color(146, 84, 18) : new Color(52, 52, 52)).AsFillBrush());
        label.Foreground = new(isActive ? AccentColor : MutedTextColor, isActive ? AccentColor : MutedTextColor, isActive ? AccentColor : MutedTextColor);
        label.Opacity = isActive ? 1f : 0.72f;
    }
}