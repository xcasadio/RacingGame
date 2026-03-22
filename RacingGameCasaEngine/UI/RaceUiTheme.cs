using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment;
using Orientation = MGUI.Core.UI.Orientation;
using Thickness = MonoGame.Extended.Thickness;
using VerticalAlignment = MGUI.Core.UI.VerticalAlignment;

namespace RacingGameCasaEngine.UI;

internal static class RaceUiTheme
{
    public static readonly Color AccentColor = new(255, 156, 0);
    public static readonly Color PanelColor = new(8, 12, 18, 220);
    public static readonly Color SecondaryPanelColor = new(16, 20, 28, 196);
    public static readonly Color PrimaryTextColor = Color.White;
    public static readonly Color SecondaryTextColor = new(205, 212, 224);

    public static MGWindow CreateFullscreenWindow(UIRoot root, bool allowsClickThrough = false)
    {
        if (!root.Desktop.Resources.TryGetTexture("CheckMark_64x64", out _))
        {
            root.Desktop.LoadDefaultResources();
        }

        int width = Math.Max(1, root.Metrics.ViewportSize.X);
        int height = Math.Max(1, root.Metrics.ViewportSize.Y);

        var window = new MGWindow(root.Desktop, 0, 0, width, height)
        {
            IsTitleBarVisible = false,
            IsUserResizable = false,
            AllowsClickThrough = allowsClickThrough,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            BackgroundBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
        };

        window.WindowWidth = width;
        window.WindowHeight = height;
        return window;
    }

    public static MGWindow CreateBackgroundWindow(UIRoot root, Microsoft.Xna.Framework.Graphics.Texture2D texture)
    {
        var window = CreateFullscreenWindow(root, allowsClickThrough: true);
        window.SetContent(new MGImage(window, texture, null, Color.White, Stretch.UniformToFill)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        });
        return window;
    }

    public static MGBorder CreatePanel(MGWindow window, int preferredWidth)
    {
        return new MGBorder(window, new Thickness(2), new MGUniformBorderBrush(AccentColor))
        {
            BackgroundBrush = new VisualStateFillBrush(PanelColor.AsFillBrush()),
            Padding = new Thickness(28),
            PreferredWidth = preferredWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static MGStackPanel CreateVerticalStack(MGWindow window, int spacing)
    {
        return new MGStackPanel(window, Orientation.Vertical)
        {
            Spacing = spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
    }

    public static MGStackPanel CreateHorizontalStack(MGWindow window, int spacing)
    {
        return new MGStackPanel(window, Orientation.Horizontal)
        {
            Spacing = spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static MGBorder CreateInfoBand(MGWindow window)
    {
        return new MGBorder(window, new Thickness(1), new MGUniformBorderBrush(new Color(255, 220, 160)))
        {
            BackgroundBrush = new VisualStateFillBrush(new Color(54, 36, 14, 220).AsFillBrush()),
            Padding = new Thickness(12, 8, 12, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
    }

    public static MGTextBlock CreateTitle(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, AccentColor, 28)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            WrapText = true,
            UseResponsiveTextScale = true,
        };
    }

    public static MGTextBlock CreateBody(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, SecondaryTextColor, 14)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextAlignment = HorizontalAlignment.Center,
            WrapText = true,
            UseResponsiveTextScale = true,
        };
    }

    public static MGButton CreatePrimaryButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = new MGUniformBorderBrush(Color.Black),
            BorderThickness = new Thickness(2),
            BackgroundBrush = new VisualStateFillBrush(AccentColor.AsFillBrush()),
            Padding = new Thickness(18, 12, 18, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };

        button.SetContent(new MGTextBlock(window, text, PrimaryTextColor, 16)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        });

        return button;
    }

    public static MGButton CreateSecondaryButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = new MGUniformBorderBrush(AccentColor),
            BorderThickness = new Thickness(2),
            BackgroundBrush = new VisualStateFillBrush(SecondaryPanelColor.AsFillBrush()),
            Padding = new Thickness(18, 10, 18, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };

        button.SetContent(new MGTextBlock(window, text, PrimaryTextColor, 15)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        });

        return button;
    }
}