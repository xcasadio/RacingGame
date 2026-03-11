using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;

namespace RacingGame.UI.MGUI;

internal static class MguiUiTheme
{
    public static readonly Color AccentColor = new(255, 156, 0);
    public static readonly Color AccentMutedColor = new(204, 110, 24);
    public static readonly Color PanelColor = new(8, 10, 16, 215);
    public static readonly Color PanelBorderColor = new(255, 156, 0, 180);
    public static readonly Color PrimaryTextColor = Color.White;
    public static readonly Color SecondaryTextColor = new(205, 212, 224);
    public static readonly Color SuccessColor = Color.LightGreen;
    public static readonly Color DangerColor = new(255, 104, 92);
    public static readonly VisualStateFillBrush TransparentBackground = new(Color.Transparent.AsFillBrush());

    public static MGWindow CreateRootWindow(MguiUiHost host, bool allowsClickThrough = false)
    {
        var window = host.CreateFullscreenWindow(allowsClickThrough);
        window.BackgroundBrush = TransparentBackground;
        return window;
    }

    public static MGStackPanel CreateVerticalStack(MGWindow window, int spacing, int padding)
    {
        return new MGStackPanel(window, Orientation.Vertical)
        {
            Spacing = spacing,
            Padding = new(padding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    public static MGStackPanel CreateHorizontalStack(MGWindow window, int spacing)
    {
        return new MGStackPanel(window, Orientation.Horizontal)
        {
            Spacing = spacing,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static MGBorder CreatePanel(MGWindow window, int padding = 24)
    {
        return new MGBorder(window, new(2), new MGUniformBorderBrush(PanelBorderColor))
        {
            BackgroundBrush = new VisualStateFillBrush(PanelColor.AsFillBrush()),
            Padding = new(padding),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static MGTextBlock CreateHeading(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, AccentColor, 26)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        };
    }

    public static MGTextBlock CreateSubheading(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, SecondaryTextColor, 14)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            WrapText = true,
        };
    }

    public static MGTextBlock CreateBodyText(MGWindow window, string text, Color? color = null)
    {
        return new MGTextBlock(window, text, color ?? PrimaryTextColor, 14)
        {
            WrapText = true,
        };
    }

    public static MGButton CreatePrimaryButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = new VisualStateFillBrush(AccentColor.AsFillBrush()),
            BorderBrush = new MGUniformBorderBrush(Color.Black),
            BorderThickness = new(1),
            Padding = new(18, 10, 18, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.SetContent(new MGTextBlock(window, text, Color.Black, 16)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        });
        return button;
    }

    public static MGButton CreateSecondaryButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = new VisualStateFillBrush(new Color(36, 44, 58, 220).AsFillBrush()),
            BorderBrush = new MGUniformBorderBrush(AccentMutedColor),
            BorderThickness = new(1),
            Padding = new(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.SetContent(new MGTextBlock(window, text, PrimaryTextColor, 15)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        });
        return button;
    }
}