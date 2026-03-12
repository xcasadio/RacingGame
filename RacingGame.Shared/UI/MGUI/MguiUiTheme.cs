using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MonoGame.Extended;
using RacingGame.Graphics;

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
    private static readonly MGUniformBorderBrush MenuButtonActiveBorderBrush = new(new Color(255, 176, 42));
    private static readonly MGUniformBorderBrush MenuButtonInactiveBorderBrush = new(new Color(28, 28, 28));
    private static readonly MGUniformBorderBrush MenuButtonFaceBorderBrush = new(new Color(118, 118, 118));
    private static readonly MGUniformBorderBrush MenuButtonFaceActiveBorderBrush = new(new Color(255, 212, 148));
    private static readonly MGUniformBorderBrush BandButtonActiveBorderBrush = new(new Color(255, 176, 42, 220));
    private static readonly MGUniformBorderBrush BandButtonInactiveBorderBrush = new(new Color(255, 255, 255, 40));
    private static readonly MGCornerRadius MenuButtonCornerRadius = new(16);

    public static int ScaleX(int xAt1280) => (int)Math.Round(xAt1280 * BaseGame.Width / 1280.0f);
    public static int ScaleY(int yAt720) => (int)Math.Round(yAt720 * BaseGame.Height / 720.0f);
    public static int ScaleFont(int sizeAt720) => Math.Max(10, ScaleY(sizeAt720));
    public static Thickness ScaleThickness(int horizontalAt1280, int verticalAt720)
        => new(ScaleX(horizontalAt1280), ScaleY(verticalAt720), ScaleX(horizontalAt1280), ScaleY(verticalAt720));
    public static Thickness ScaleThickness(int leftAt1280, int topAt720, int rightAt1280, int bottomAt720)
        => new(ScaleX(leftAt1280), ScaleY(topAt720), ScaleX(rightAt1280), ScaleY(bottomAt720));

    public static MGWindow CreateRootWindow(MguiUiHost host, bool allowsClickThrough = false)
    {
        var window = host.CreateFullscreenWindow(allowsClickThrough);
        window.BackgroundBrush = TransparentBackground;
        return window;
    }

    public static MGBorder CreateMenuBand(MGWindow window, int topAt720, int heightAt720, Thickness? padding = null)
    {
        return new MGBorder(window, new(0), new MGUniformBorderBrush(Color.Transparent))
        {
            BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 132).AsFillBrush()),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new(0, ScaleY(topAt720), 0, 0),
            PreferredHeight = ScaleY(heightAt720),
            Padding = padding ?? ScaleThickness(28, 18, 28, 18),
        };
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
            Padding = ScaleThickness(padding, padding),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static MGTextBlock CreateHeading(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, AccentColor, ScaleFont(26))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        };
    }

    public static MGTextBlock CreateSubheading(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, SecondaryTextColor, ScaleFont(14))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            WrapText = true,
        };
    }

    public static MGTextBlock CreateBodyText(MGWindow window, string text, Color? color = null)
    {
        return new MGTextBlock(window, text, color ?? PrimaryTextColor, ScaleFont(14))
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
            Padding = ScaleThickness(18, 10, 18, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.SetContent(new MGTextBlock(window, text, Color.Black, ScaleFont(16))
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
            Padding = ScaleThickness(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.SetContent(new MGTextBlock(window, text, PrimaryTextColor, ScaleFont(15))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        });
        return button;
    }

    public static MGButton CreateMenuTextButton(MGWindow window, string text, Action action, int minWidthAt1280 = 0)
    {
        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = CreateMenuButtonOuterBrush(false),
            BorderBrush = MenuButtonInactiveBorderBrush,
            BorderThickness = new(Math.Max(2, ScaleX(5))),
            CornerRadius = MenuButtonCornerRadius,
            Padding = new(0),
            MinHeight = ScaleY(48),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };

        if (minWidthAt1280 > 0)
        {
            button.MinWidth = ScaleX(minWidthAt1280);
        }

        var outerFace = new MGBorder(window, new(2), MenuButtonFaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(12),
            BackgroundBrush = CreateMenuButtonOuterFaceBrush(false),
            Padding = ScaleThickness(6, 6, 6, 6),
            Margin = new(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var middlePlate = new MGBorder(window, new(0), MenuButtonFaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(10),
            BackgroundBrush = CreateMenuButtonMiddleBrush(false),
            Padding = ScaleThickness(14, 7, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var centerPlate = new MGBorder(window, new(0), MenuButtonFaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(8),
            BackgroundBrush = CreateMenuButtonCenterBrush(false),
            Padding = ScaleThickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var label = new MGTextBlock(window, text, SecondaryTextColor, ScaleFont(15))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        };

        centerPlate.SetContent(label);
        middlePlate.SetContent(centerPlate);
        outerFace.SetContent(middlePlate);
        button.SetContent(outerFace);
        button.Tag = new MenuTextButtonParts(outerFace, middlePlate, centerPlate, label);
        ApplyMenuTextButtonState(button, false);
        return button;
    }

    public static void ApplyMenuTextButtonState(MGButton button, bool isActive)
    {
        if (button.Tag is not MenuTextButtonParts parts)
        {
            return;
        }

        button.BorderBrush = isActive ? MenuButtonActiveBorderBrush : MenuButtonInactiveBorderBrush;
        button.BorderThickness = new(Math.Max(2, ScaleX(5)));
        button.BackgroundBrush = CreateMenuButtonOuterBrush(isActive);
        parts.OuterFace.BorderBrush = isActive ? MenuButtonFaceActiveBorderBrush : MenuButtonFaceBorderBrush;
        parts.OuterFace.BackgroundBrush = CreateMenuButtonOuterFaceBrush(isActive);
        parts.MiddlePlate.BackgroundBrush = CreateMenuButtonMiddleBrush(isActive);
        parts.CenterPlate.BackgroundBrush = CreateMenuButtonCenterBrush(isActive);
        Color foreground = isActive ? AccentColor : SecondaryTextColor;
        parts.Label.Foreground = new(foreground, foreground, foreground);
        parts.Label.Opacity = isActive ? 1f : 0.9f;
    }

    public static MGButton CreateBandButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = CreateBandButtonBrush(false),
            BorderBrush = BandButtonInactiveBorderBrush,
            BorderThickness = new(Math.Max(1, ScaleX(2))),
            CornerRadius = new MGCornerRadius(10),
            Padding = ScaleThickness(14, 8, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };

        var label = new MGTextBlock(window, text, PrimaryTextColor, ScaleFont(15))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
        };

        button.SetContent(label);
        button.Tag = label;
        ApplyBandButtonState(button, false);
        return button;
    }

    public static void ApplyBandButtonState(MGButton button, bool isActive)
    {
        button.BackgroundBrush = CreateBandButtonBrush(isActive);
        button.BorderBrush = isActive ? BandButtonActiveBorderBrush : BandButtonInactiveBorderBrush;
        button.BorderThickness = new(Math.Max(1, ScaleX(isActive ? 3 : 2)));

        if (button.Tag is MGTextBlock label)
        {
            Color foreground = isActive ? AccentColor : PrimaryTextColor;
            label.Foreground = new(foreground, foreground, foreground);
            label.Opacity = isActive ? 1f : 0.92f;
        }
    }

    private static VisualStateFillBrush CreateMenuButtonOuterBrush(bool isActive)
    {
        Color baseColor = isActive ? new Color(146, 84, 18) : new Color(52, 52, 52);
        return new VisualStateFillBrush(baseColor.AsFillBrush(), Color.White * 0.08f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateMenuButtonOuterFaceBrush(bool isActive)
    {
        Color color = isActive ? new Color(184, 184, 184) : new Color(172, 172, 172);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.04f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateMenuButtonMiddleBrush(bool isActive)
    {
        Color color = isActive ? new Color(228, 223, 214) : new Color(224, 224, 224);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.05f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateMenuButtonCenterBrush(bool isActive)
    {
        Color color = isActive ? new Color(255, 248, 236) : new Color(248, 248, 248);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.03f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateBandButtonBrush(bool isActive)
    {
        Color color = isActive ? new Color(0, 0, 0, 168) : new Color(0, 0, 0, 132);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.05f, PressedModifierType.Darken, 0.08f);
    }

    private sealed record MenuTextButtonParts(MGBorder OuterFace, MGBorder MiddlePlate, MGBorder CenterPlate, MGTextBlock Label);
}