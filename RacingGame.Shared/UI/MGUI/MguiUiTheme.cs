using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using MonoGame.Extended;
using System.Reflection;
using XamlDocumentSource = MGUI.Core.UI.XAML.XamlDocumentSource;

namespace RacingGame.UI.MGUI;

internal static class MguiUiTheme
{
    private const string ButtonThemesResourceName = "RacingGame.UI.MGUI.Themes.RacingGameButtonThemes.xaml";
    private const string PrimaryButtonThemeName = "RacingGame.Button.Primary";
    private const string SecondaryButtonThemeName = "RacingGame.Button.Secondary";
    private const string BandButtonThemeName = "RacingGame.Button.Band";
    private const string ActiveBandButtonThemeName = "RacingGame.Button.Band.Active";
    private const string MenuTextButtonThemeName = "RacingGame.Button.MenuText";
    private const string ActiveMenuTextButtonThemeName = "RacingGame.Button.MenuText.Active";

    public static readonly Color AccentColor = new(255, 156, 0);
    public static readonly Color AccentMutedColor = new(204, 110, 24);
    public static readonly Color PanelColor = new(8, 10, 16, 215);
    public static readonly Color PanelBorderColor = new(255, 156, 0, 180);
    public static readonly Color PrimaryTextColor = Color.White;
    public static readonly Color SecondaryTextColor = new(205, 212, 224);
    public static readonly Color SuccessColor = Color.LightGreen;
    public static readonly Color DangerColor = new(255, 104, 92);
    public static readonly VisualStateFillBrush TransparentBackground = new(Color.Transparent.AsFillBrush());
    private static readonly MGUniformBorderBrush BandButtonActiveBorderBrush = new(new Color(255, 176, 42, 220));
    private static readonly MGUniformBorderBrush BandButtonInactiveBorderBrush = new(new Color(255, 255, 255, 40));
    private static readonly MGCornerRadius MenuButtonCornerRadius = new(16);

    public static int ScaleX(int xAt1280) => xAt1280;
    public static int ScaleY(int yAt720) => yAt720;
    public static int ScaleFont(int sizeAt720) => Math.Max(10, sizeAt720);
    public static Thickness ScaleThickness(int horizontalAt1280, int verticalAt720)
        => new(horizontalAt1280, verticalAt720, horizontalAt1280, verticalAt720);
    public static Thickness ScaleThickness(int leftAt1280, int topAt720, int rightAt1280, int bottomAt720)
        => new(leftAt1280, topAt720, rightAt1280, bottomAt720);

    public static void LoadButtonThemes(MGDesktop desktop)
    {
        string xaml = GeneralUtils.ReadEmbeddedResourceAsString(Assembly.GetExecutingAssembly(), ButtonThemesResourceName);
        desktop.Resources.LoadThemesFromXaml(XamlDocumentSource.FromString(xaml, ButtonThemesResourceName));
    }

    public static MGWindow CreateRootWindow(MguiUiHost host, bool allowsClickThrough = false)
    {
        var window = host.CreateFullscreenWindow(allowsClickThrough);
        window.BackgroundBrush = TransparentBackground;
        return window;
    }

    public static MGResponsiveRoot CreateResponsiveRoot(MGWindow window)
    {
        return new MGResponsiveRoot(window);
    }

    public static MGBorder CreateMenuBand(MGWindow window, int topAt720, int heightAt720, Thickness? padding = null)
    {
        return new MGBorder(window, new(0), new MGUniformBorderBrush(Color.Transparent))
        {
            BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 132).AsFillBrush()),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new(0, topAt720, 0, 0),
            PreferredHeight = heightAt720,
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
            Padding = new(padding),
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
            UseResponsiveTextScale = true,
        };
    }

    public static MGTextBlock CreateSubheading(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, SecondaryTextColor, ScaleFont(14))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            WrapText = true,
            UseResponsiveTextScale = true,
        };
    }

    public static MGTextBlock CreateBodyText(MGWindow window, string text, Color? color = null)
    {
        return new MGTextBlock(window, text, color ?? PrimaryTextColor, ScaleFont(14))
        {
            WrapText = true,
            UseResponsiveTextScale = true,
        };
    }

    public static MGButton CreatePrimaryButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = new MGUniformBorderBrush(Color.Black),
            BorderThickness = new(2),
            CornerRadius = new MGCornerRadius(12),
            Padding = new(18, 10, 18, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        ApplyButtonTheme(button, PrimaryButtonThemeName);
        button.SetContent(new MGTextBlock(window, text, null, ScaleFont(16))
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
            BorderBrush = new MGUniformBorderBrush(AccentMutedColor),
            BorderThickness = new(2),
            CornerRadius = new MGCornerRadius(12),
            Padding = new(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        ApplyButtonTheme(button, SecondaryButtonThemeName);
        button.SetContent(new MGTextBlock(window, text, null, ScaleFont(15))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        });
        return button;
    }

    public static MGButton CreateMenuTextButton(MGWindow window, string text, Action action, int minWidthAt1280 = 0)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = new MGUniformBorderBrush(new Color(96, 96, 96)),
            BorderThickness = new(2),
            CornerRadius = MenuButtonCornerRadius,
            Padding = new(20, 10, 20, 11),
            MinHeight = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        ApplyButtonTheme(button, MenuTextButtonThemeName);

        if (minWidthAt1280 > 0)
        {
            button.MinWidth = minWidthAt1280;
        }

        button.SetContent(new MGTextBlock(window, text, null, ScaleFont(15))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        });

        ApplyMenuTextButtonState(button, false);
        return button;
    }

    public static void ApplyMenuTextButtonState(MGButton button, bool isActive)
    {
        ApplyButtonTheme(button, isActive ? ActiveMenuTextButtonThemeName : MenuTextButtonThemeName);
        button.BorderBrush = isActive
            ? new MGUniformBorderBrush(new Color(255, 176, 42))
            : new MGUniformBorderBrush(new Color(96, 96, 96));
        button.BorderThickness = new(isActive ? 3 : 2);
        button.Opacity = isActive ? 1f : 0.94f;
    }

    public static MGButton CreateBandButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = BandButtonInactiveBorderBrush,
            BorderThickness = new(2),
            CornerRadius = new MGCornerRadius(10),
            Padding = new(14, 8, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        ApplyButtonTheme(button, BandButtonThemeName);

        var label = new MGTextBlock(window, text, null, ScaleFont(15))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        };

        button.SetContent(label);
        ApplyBandButtonState(button, false);
        return button;
    }

    public static void ApplyBandButtonState(MGButton button, bool isActive)
    {
        ApplyButtonTheme(button, isActive ? ActiveBandButtonThemeName : BandButtonThemeName);
        button.BorderBrush = isActive ? BandButtonActiveBorderBrush : BandButtonInactiveBorderBrush;
        button.BorderThickness = new(isActive ? 3 : 2);

        if (button.Content is MGTextBlock label)
        {
            label.Opacity = isActive ? 1f : 0.92f;
        }
    }

    private static void ApplyButtonTheme(MGButton button, string themeName)
    {
        MGTheme theme = button.SelfOrParentWindow.GetResources().GetThemeOrDefault(themeName, button.GetTheme(), false);
        button.EnsureResourceScope().DefaultTheme = theme;
        button.DefaultTextForeground.SetAll(theme.TextBlockFallbackForeground.GetValue(true).NormalValue);
    }
}