using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment;
using Orientation = MGUI.Core.UI.Orientation;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Thickness = MonoGame.Extended.Thickness;
using VerticalAlignment = MGUI.Core.UI.VerticalAlignment;

namespace RacingGameCasaEngine.UI;

internal static class LegacyMenuUiTheme
{
    public static readonly Color AccentColor = new(255, 156, 0);
    public static readonly Color PrimaryTextColor = Color.White;
    public static readonly Color MutedTextColor = new(212, 212, 212);
    public static readonly Color BandColor = new(0, 0, 0, 132);
    public static readonly Color SubtleBorderColor = new(255, 255, 255, 70);
    private static readonly MGUniformBorderBrush InactiveBorderBrush = new(new Color(28, 28, 28));
    private static readonly MGUniformBorderBrush ActiveBorderBrush = new(new Color(255, 176, 42));
    private static readonly MGUniformBorderBrush FaceBorderBrush = new(new Color(118, 118, 118));
    private static readonly MGUniformBorderBrush FaceActiveBorderBrush = new(new Color(255, 212, 148));

    public static MGWindow CreateRootWindow(UIRoot root)
    {
        var window = RaceUiTheme.CreateFullscreenWindow(root);
        window.BackgroundBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush());
        return window;
    }

    public static MGWindow CreateMenuBackgroundWindow(UIRoot root, Texture2D backgroundTexture)
    {
        return RaceUiTheme.CreateBackgroundWindow(root, backgroundTexture, LegacyMenuUiAtlas.MenuBackground, 0.85f, Stretch.Fill);
    }

    public static MGWindow CreateLogoWindow(UIRoot root, Texture2D backgroundTexture, out MGImage logoImage)
    {
        var window = RaceUiTheme.CreateFullscreenWindow(root, allowsClickThrough: true);
        var overlay = new MGOverlayPanel(window)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        logoImage = CreateLogo(window, backgroundTexture);
        overlay.TryAddChild(logoImage);
        window.SetContent(overlay);
        return window;
    }

    public static MGBorder CreateMenuBand(MGWindow window, int top, int height, Thickness? padding = null)
    {
        return new MGBorder(window, new Thickness(0), new MGUniformBorderBrush(Color.Transparent))
        {
            BackgroundBrush = new VisualStateFillBrush(BandColor.AsFillBrush()),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, top, 0, 0),
            PreferredHeight = height,
            Padding = padding ?? new Thickness(32, 20, 32, 20),
        };
    }

    public static MGTextBlock CreateHeading(MGWindow window, string text)
    {
        return new MGTextBlock(window, text, AccentColor, 26)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        };
    }

    public static MGTextBlock CreateBodyText(MGWindow window, string text, Color? color = null)
    {
        return new MGTextBlock(window, text, color ?? PrimaryTextColor, 14)
        {
            WrapText = true,
            UseResponsiveTextScale = true,
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
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    public static MGButton CreateMenuTextButton(MGWindow window, string text, Action action, int minWidth = 150)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = new MGUniformBorderBrush(new Color(96, 96, 96)),
            BorderThickness = new Thickness(2),
            CornerRadius = new MGCornerRadius(16),
            Padding = new Thickness(20, 10, 20, 11),
            MinHeight = 48,
            MinWidth = minWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
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

    public static MGButton CreateResponsiveMenuTextButton(UIRoot root, MGWindow window, string text, Action action, int minWidth = 150)
    {
        float scale = Math.Clamp(root.Metrics.Scale, 1.0f, 1.75f);
        int verticalPadding = (int)MathF.Round(10f * scale);
        int horizontalPadding = (int)MathF.Round(20f * scale);
        int minHeight = (int)MathF.Round(48f * scale);
        int minScaledWidth = Math.Max(minWidth, (int)MathF.Round(minWidth * scale));

        var button = CreateMenuTextButton(window, text, action, minScaledWidth);
        button.Padding = new Thickness(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding + 1);
        button.MinHeight = minHeight;
        button.PreferredHeight = minHeight;
        return button;
    }

    public static void ApplyMenuTextButtonState(MGButton button, bool isActive)
    {
        button.BorderBrush = new MGUniformBorderBrush(isActive ? new Color(255, 176, 42) : new Color(96, 96, 96));
        button.BorderThickness = new Thickness(isActive ? 3 : 2);
        button.Opacity = isActive ? 1f : 0.94f;
    }

    public static MGButton CreateBandButton(MGWindow window, string text, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BorderBrush = new MGUniformBorderBrush(SubtleBorderColor),
            BorderThickness = new Thickness(2),
            CornerRadius = new MGCornerRadius(10),
            Padding = new Thickness(14, 8, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 24).AsFillBrush()),
        };

        var label = new MGTextBlock(window, text, PrimaryTextColor, 15)
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
        button.BorderBrush = new MGUniformBorderBrush(isActive ? AccentColor : SubtleBorderColor);
        button.BorderThickness = new Thickness(isActive ? 3 : 2);
        if (button.Content is MGTextBlock label)
        {
            label.Foreground = new(isActive ? AccentColor : PrimaryTextColor, isActive ? AccentColor : PrimaryTextColor, isActive ? AccentColor : PrimaryTextColor);
        }
    }

    public static MGButton CreateSpriteButton(MGWindow window, Texture2D texture, Rectangle sourceRect, Action action)
    {
        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var image = new MGImage(window, UiImageResources.AsImage(texture), sourceRect, null, Stretch.Uniform)
        {
            PreferredWidth = 136,
            PreferredHeight = 60,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        button.SetContent(image);
        button.Tag = image;
        return button;
    }

    public static void ApplySpriteButtonState(MGButton button)
    {
        if (button.Tag is MGImage image)
        {
            image.Opacity = button.VisualState.IsFocused || button.IsHovered ? 1f : 0.92f;
        }
    }

    public static MGButton CreateMainMenuIconButton(MGWindow window, Texture2D menuButtonsTexture, Rectangle sourceRect, Action action, out MGBorder face)
    {
        Rectangle iconRect = new(sourceRect.X + 50, sourceRect.Y + 50, sourceRect.Width - 100, sourceRect.Height - 100);

        var button = new MGButton(window, _ => action())
        {
            BackgroundBrush = new VisualStateFillBrush(new Color(52, 52, 52).AsFillBrush()),
            BorderBrush = InactiveBorderBrush,
            BorderThickness = new Thickness(5),
            CornerRadius = new MGCornerRadius(22),
            Padding = new Thickness(0),
            PreferredWidth = 105,
            PreferredHeight = 105,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        face = new MGBorder(window, new Thickness(2), FaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(18),
            BackgroundBrush = new VisualStateFillBrush(new Color(172, 172, 172).AsFillBrush()),
            Padding = new Thickness(6),
            PreferredWidth = 95,
            PreferredHeight = 95,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var middle = new MGBorder(window)
        {
            CornerRadius = new MGCornerRadius(14),
            BackgroundBrush = new VisualStateFillBrush(new Color(224, 224, 224).AsFillBrush()),
            Padding = new Thickness(8),
        };

        var center = new MGBorder(window)
        {
            CornerRadius = new MGCornerRadius(10),
            BackgroundBrush = new VisualStateFillBrush(new Color(248, 248, 248).AsFillBrush()),
            Padding = new Thickness(10),
        };

        center.SetContent(new MGImage(window, UiImageResources.AsImage(menuButtonsTexture), iconRect, null, Stretch.Uniform)
        {
            PreferredWidth = 45,
            PreferredHeight = 45,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        middle.SetContent(center);
        face.SetContent(middle);
        button.SetContent(face);
        return button;
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

    public static Rectangle CalculateLegacyMenuRectangle(Point viewportSize, int relX, int relY, int relWidth, int relHeight)
    {
        float widthFactor = viewportSize.X / 1024.0f;
        float heightFactor = viewportSize.Y / 640.0f;
        return new Rectangle(
            (int)Math.Round(relX * widthFactor),
            (int)Math.Round(relY * heightFactor),
            (int)Math.Round(relWidth * widthFactor),
            (int)Math.Round(relHeight * heightFactor));
    }

    public static Rectangle CalculateLegacyMenuBounceRectangle(Point viewportSize, int relX, int relY, int relWidth, int relHeight, float bounceEffect)
    {
        float widthFactor = viewportSize.X / 1024.0f;
        float heightFactor = viewportSize.Y / 640.0f;
        float middleX = (relX + relWidth / 2f) * widthFactor;
        float middleY = (relY + relHeight / 2f) * heightFactor;
        float scaledWidth = relWidth * widthFactor * bounceEffect;
        float scaledHeight = relHeight * heightFactor * bounceEffect;
        return new Rectangle(
            (int)Math.Round(middleX - scaledWidth / 2f),
            (int)Math.Round(middleY - scaledHeight / 2f),
            (int)Math.Round(scaledWidth),
            (int)Math.Round(scaledHeight));
    }

    private static MGImage CreateLogo(MGWindow window, Texture2D backgroundTexture)
    {
        return new MGImage(window, UiImageResources.AsImage(backgroundTexture), LegacyMenuUiAtlas.RacingGameLogo, null, Stretch.Fill)
        {
            PreferredWidth = 601,
            PreferredHeight = 218,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(362, 36, 0, 0),
        };
    }

}