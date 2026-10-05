using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.BorderBrushes;
using Color = Microsoft.Xna.Framework.Color;
using Thickness = MonoGame.Extended.Thickness;

namespace RacingGameCasaEngine.UI;

/// <summary>
/// Hover, focus and selection looks of the menu screens' buttons, applied in code every frame (RGCE ADR-0001: MGUI XAML
/// visual states cannot express them, see docs/mgui-gaps-from-rgce-xaml-screens.md). The buttons themselves are XAML.
/// The main menu is not restyled here: its view model drives its look (ADR-0005).
/// </summary>
internal static class LegacyMenuUiTheme
{
    public static readonly Color AccentColor = new(255, 156, 0);
    public static readonly Color PrimaryTextColor = Color.White;
    public static readonly Color SubtleBorderColor = new(255, 255, 255, 70);

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
}