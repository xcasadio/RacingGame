using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using RacingGame.GameScreens;
using RacingGame.Graphics;

namespace RacingGame.UI.MGUI.Views;

internal sealed class MainMenuView : IMguiScreenView
{
    private readonly MGButton _playButton;
    private readonly MGButton[] _buttons;
    private readonly MGBorder[] _buttonFaces;
    private readonly MGTextBlock[] _labels;

    private static readonly MGUniformBorderBrush ActiveBorderBrush = new(new Color(255, 176, 42));
    private static readonly MGUniformBorderBrush InactiveBorderBrush = new(new Color(28, 28, 28));
    private static readonly MGUniformBorderBrush FaceBorderBrush = new(new Color(118, 118, 118));
    private static readonly MGUniformBorderBrush FaceActiveBorderBrush = new(new Color(255, 212, 148));
    private static readonly MGCornerRadius ButtonCornerRadius = new(22);

    public MainMenuView(MainMenu screen, MguiUiHost host)
    {
        Window = MguiUiTheme.CreateRootWindow(host);

        var band = MguiUiTheme.CreateMenuBand(Window, 315, 216, MguiUiTheme.ScaleThickness(28, 20, 28, 18));
        band.UseResponsiveLayout = true;

        var content = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(10), 0);
        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Center;

        var buttonScroller = new MGScrollViewer(Window, ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto)
        {
            PreferredWidth = 1120,
            PreferredHeight = 150,
            AllowClickDragScrolling = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var buttonDefinitions = new[]
        {
            new MenuButtonDefinition("Play", "PLAY", UIRenderer.MenuButtonPlayGfxRect, screen.StartGame),
            new MenuButtonDefinition("Highscores", "HIGHSCORES", UIRenderer.MenuButtonHighscoresGfxRect, screen.OpenHighscores),
            new MenuButtonDefinition("Options", "OPTIONS", UIRenderer.MenuButtonOptionsGfxRect, screen.OpenOptions),
            new MenuButtonDefinition("Help", "HELP", UIRenderer.MenuButtonHelpGfxRect, screen.OpenHelp),
            new MenuButtonDefinition("Quit", "QUIT", UIRenderer.MenuButtonQuitGfxRect, screen.RequestExit),
        };
        var buttonsRow = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(10));

        _buttons = buttonDefinitions.Select(x => CreateMenuButton(x.Tooltip, x.SourceRect, x.Action)).ToArray();
        _buttonFaces = _buttons.Select(button => (MGBorder)button.Content).ToArray();
        _labels = buttonDefinitions.Select(x => MguiUiTheme.CreateBodyText(Window, x.Label, MguiUiTheme.SecondaryTextColor)).ToArray();

        for (int i = 0; i < _buttons.Length; i++)
        {
            var item = MguiUiTheme.CreateVerticalStack(Window, 8, 0);
            item.HorizontalAlignment = HorizontalAlignment.Center;
            item.VerticalAlignment = VerticalAlignment.Center;

            _labels[i].HorizontalAlignment = HorizontalAlignment.Center;
            _labels[i].TextAlignment = HorizontalAlignment.Center;

            item.TryAddChild(_buttons[i]);
            item.TryAddChild(_labels[i]);
            buttonsRow.TryAddChild(item);
        }

        buttonScroller.SetContent(buttonsRow);
        content.TryAddChild(buttonScroller);
        band.SetContent(content);
        Window.SetContent(band);

        _playButton = _buttons[0];
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => _playButton;

    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        InitialFocusElement.Focus();
        RefreshVisualState();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        RefreshVisualState();
    }

    private MGButton CreateMenuButton(string tooltip, Rectangle sourceRect, Action action)
    {
        Rectangle iconRect = GetIconRect(sourceRect);
        var button = new MGButton(Window, _ => action())
        {
            BackgroundBrush = CreateOuterButtonBrush(false),
            BorderBrush = InactiveBorderBrush,
            BorderThickness = new(5),
            CornerRadius = ButtonCornerRadius,
            Padding = new(0),
            PreferredWidth = 105,
            PreferredHeight = 105,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var face = new MGBorder(Window, new(2), FaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(18),
            BackgroundBrush = CreateFaceOuterBrush(false),
            Padding = new(6),
            Margin = new(0),
            PreferredWidth = 95,
            PreferredHeight = 95,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var middlePlate = new MGBorder(Window, new(0), FaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(14),
            BackgroundBrush = CreateFaceMiddleBrush(false),
            Padding = new(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var centerPlate = new MGBorder(Window, new(0), FaceBorderBrush)
        {
            CornerRadius = new MGCornerRadius(10),
            BackgroundBrush = CreateFaceCenterBrush(false),
            Padding = new(10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        centerPlate.SetContent(new MGImage(Window, BaseGame.UI.Buttons.XnaTexture, iconRect, null, Stretch.Uniform)
        {
            PreferredWidth = 45,
            PreferredHeight = 45,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });

        middlePlate.SetContent(centerPlate);
        face.SetContent(middlePlate);

        button.SetContent(face);

        return button;
    }

    private static VisualStateFillBrush CreateOuterButtonBrush(bool isActive)
    {
        Color baseColor = isActive ? new Color(146, 84, 18) : new Color(52, 52, 52);
        return new VisualStateFillBrush(baseColor.AsFillBrush(), Color.White * 0.08f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateFaceOuterBrush(bool isActive)
    {
        Color color = isActive ? new Color(184, 184, 184) : new Color(172, 172, 172);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.04f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateFaceMiddleBrush(bool isActive)
    {
        Color color = isActive ? new Color(228, 223, 214) : new Color(224, 224, 224);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.05f, PressedModifierType.Darken, 0.10f);
    }

    private static VisualStateFillBrush CreateFaceCenterBrush(bool isActive)
    {
        Color color = isActive ? new Color(255, 248, 236) : new Color(248, 248, 248);
        return new VisualStateFillBrush(color.AsFillBrush(), Color.White * 0.03f, PressedModifierType.Darken, 0.10f);
    }

    private static Rectangle GetIconRect(Rectangle sourceRect)
    {
        const int cropInset = 50;
        return new Rectangle(
            sourceRect.X + cropInset,
            sourceRect.Y + cropInset,
            sourceRect.Width - cropInset * 2,
            sourceRect.Height - cropInset * 2);
    }

    private void RefreshVisualState()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            bool isActive = _buttons[i].VisualState.IsFocused || _buttons[i].IsHovered;
            _buttons[i].BorderThickness = new(5);
            _buttons[i].BorderBrush = isActive ? ActiveBorderBrush : InactiveBorderBrush;
            _buttonFaces[i].BorderBrush = isActive ? FaceActiveBorderBrush : FaceBorderBrush;
            _buttonFaces[i].Margin = new(0);
            _buttonFaces[i].BackgroundBrush = CreateFaceOuterBrush(isActive);
            if (_buttonFaces[i].Content is MGBorder middlePlate)
            {
                middlePlate.BackgroundBrush = CreateFaceMiddleBrush(isActive);
                if (middlePlate.Content is MGBorder centerPlate)
                {
                    centerPlate.BackgroundBrush = CreateFaceCenterBrush(isActive);
                }
            }
            _buttons[i].BackgroundBrush = CreateOuterButtonBrush(isActive);
            _labels[i].Foreground = isActive
                ? new(MguiUiTheme.AccentColor, MguiUiTheme.AccentColor, MguiUiTheme.AccentColor)
                : new(MguiUiTheme.SecondaryTextColor, MguiUiTheme.SecondaryTextColor, MguiUiTheme.SecondaryTextColor);
            _labels[i].Opacity = isActive ? 1f : 0.72f;
        }
    }
    private sealed record MenuButtonDefinition(string Tooltip, string Label, Rectangle SourceRect, Action Action);
}