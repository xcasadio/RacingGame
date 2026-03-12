using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Responsive;
using MonoGame.Extended;
using RacingGame.GameScreens;
using RacingGame.Graphics;

namespace RacingGame.UI.MGUI.Views;

internal sealed class TrackSelectionView : IMguiScreenView
{
    private readonly TrackSelection _screen;
    private readonly MGButton[] _trackButtons;
    private readonly MGTextBlock[] _trackLabels;
    private readonly MGButton _selectButton;
    private readonly MGButton _backButton;
    private readonly MGBorder[] _trackFrames;

    public TrackSelectionView(TrackSelection screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);
        var root = new MGOverlayPanel(Window)
        {
            UseResponsiveLayout = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var band = MguiUiTheme.CreateMenuBand(Window, 248, 315, new Thickness(32, 22, 32, 18));

        var stack = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(48));
        stack.HorizontalAlignment = HorizontalAlignment.Center;
        stack.VerticalAlignment = VerticalAlignment.Center;

        _trackButtons = new MGButton[3];
        _trackLabels = new MGTextBlock[3];
        _trackFrames = new MGBorder[3];
        string[] names = ["Beginner", "Advanced", "Expert"];
        Rectangle[] trackRects =
        {
            UIRenderer.TrackButtonBeginnerGfxRect,
            UIRenderer.TrackButtonAdvancedGfxRect,
            UIRenderer.TrackButtonExpertGfxRect,
        };

        for (int i = 0; i < names.Length; i++)
        {
            int trackIndex = i;
            var item = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(8), 0);
            item.HorizontalAlignment = HorizontalAlignment.Center;
            item.VerticalAlignment = VerticalAlignment.Center;

            _trackButtons[i] = CreateTrackButton(trackRects[i], () => _screen.SelectTrack(trackIndex), out MGBorder frame);
            _trackFrames[i] = frame;
            _trackLabels[i] = MguiUiTheme.CreateBodyText(Window, names[i], MguiUiTheme.PrimaryTextColor);
            _trackLabels[i].HorizontalAlignment = HorizontalAlignment.Center;
            _trackLabels[i].TextAlignment = HorizontalAlignment.Center;

            item.TryAddChild(_trackButtons[i]);
            item.TryAddChild(_trackLabels[i]);
            stack.TryAddChild(item);
        }

        band.SetContent(stack);

        var actions = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(14));
        actions.ResponsiveAnchor = ResponsiveAnchor.BottomRight;
        _selectButton = CreateSpriteButton(UIRenderer.BottomButtonAButtonGfxRect, _screen.ConfirmSelection);
        _backButton = CreateSpriteButton(UIRenderer.BottomButtonBButtonGfxRect, _screen.RequestBack);
        actions.TryAddChild(_selectButton);
        actions.TryAddChild(_backButton);
        root.TryAddChild(band);
        root.TryAddChild(actions, new Thickness(0, 0, 48, 20));

        Window.SetContent(root);
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => _trackButtons[TrackSelection.SelectedTrackNumber];

    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        UpdateButtonLabels();
        InitialFocusElement.Focus();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        UpdateButtonLabels();
    }

    private void UpdateButtonLabels()
    {
        string[] names = ["Beginner", "Advanced", "Expert"];
        for (int i = 0; i < names.Length; i++)
        {
            bool selected = TrackSelection.SelectedTrackNumber == i;
            _trackLabels[i].Text = names[i];
            _trackLabels[i].DefaultTextForeground = selected
                ? new VisualStateSetting<Color?>(MguiUiTheme.AccentColor, MguiUiTheme.AccentColor, MguiUiTheme.AccentColor)
                : new VisualStateSetting<Color?>(MguiUiTheme.PrimaryTextColor, MguiUiTheme.PrimaryTextColor, MguiUiTheme.PrimaryTextColor);
            _trackFrames[i].BorderBrush = selected
                ? new MGUniformBorderBrush(MguiUiTheme.AccentColor)
                : new MGUniformBorderBrush(new Color(255, 255, 255, 70));
            _trackFrames[i].BorderThickness = selected ? new(4) : new(2);
        }

        ApplySpriteButtonState(_selectButton);
        ApplySpriteButtonState(_backButton);
    }

    private MGButton CreateTrackButton(Rectangle sourceRect, Action action, out MGBorder frame)
    {
        var button = new MGButton(Window, _ => action())
        {
            BackgroundBrush = MguiUiTheme.TransparentBackground,
            BorderThickness = new(0),
            Padding = new(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        frame = new MGBorder(Window, new(2), new MGUniformBorderBrush(new Color(255, 255, 255, 70)))
        {
            CornerRadius = new MGCornerRadius(18),
            BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 24).AsFillBrush()),
            Padding = new(0),
            PreferredWidth = 172,
            PreferredHeight = 286,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        frame.SetContent(new MGImage(Window, BaseGame.UI.Buttons.XnaTexture, sourceRect, null, Stretch.Uniform)
        {
            PreferredWidth = 168,
            PreferredHeight = 280,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });

        button.SetContent(frame);
        return button;
    }

    private MGButton CreateSpriteButton(Rectangle sourceRect, Action action)
    {
        var button = new MGButton(Window, _ => action())
        {
            BackgroundBrush = MguiUiTheme.TransparentBackground,
            BorderThickness = new(0),
            Padding = new(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var image = new MGImage(Window, BaseGame.UI.Buttons.XnaTexture, sourceRect, null, Stretch.Uniform)
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

    private static void ApplySpriteButtonState(MGButton button)
    {
        if (button.Tag is MGImage image)
        {
            image.Opacity = button.VisualState.IsFocused || button.IsHovered ? 1f : 0.92f;
        }
    }
}