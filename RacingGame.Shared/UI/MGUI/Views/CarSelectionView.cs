using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Responsive;
using MonoGame.Extended;
using RacingGame.GameScreens;
using RacingGame.Graphics;

namespace RacingGame.UI.MGUI.Views;

internal sealed class CarSelectionView : IMguiScreenView
{
    private readonly CarSelection _screen;
    private readonly MGButton _nextButton;
    private readonly List<MGTextBlock> _statLabels;
    private readonly List<MGProgressBar> _statBars;
    private readonly List<MGButton> _colorButtons;
    private readonly MGButton _previousButton;
    private readonly MGButton _selectButton;
    private readonly MGButton _cancelButton;

    public CarSelectionView(CarSelection screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);
        var root = new MGOverlayPanel(Window)
        {
            UseResponsiveLayout = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var band = MguiUiTheme.CreateMenuBand(Window, 191, 439, new Thickness(28, 14, 28, 18));

        var bandContent = new MGOverlayPanel(Window)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _previousButton = CreateArrowButton("<", screen.MoveToPreviousCar);
        _nextButton = CreateArrowButton(">", screen.MoveToNextCar);
        _previousButton.ResponsiveAnchor = ResponsiveAnchor.MiddleLeft;
        _nextButton.ResponsiveAnchor = ResponsiveAnchor.MiddleRight;
        bandContent.TryAddChild(_previousButton, new Thickness(0, 0, 0, 84));
        bandContent.TryAddChild(_nextButton, new Thickness(0, 0, 0, 84));

        var statsPanel = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(8), 0);
        statsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        statsPanel.VerticalAlignment = VerticalAlignment.Center;
        statsPanel.PreferredWidth = 300;
        statsPanel.ResponsiveAnchor = ResponsiveAnchor.MiddleRight;
        _statLabels = new();
        _statBars = new();
        foreach (var stat in screen.GetCurrentCarStatEntries())
        {
            var item = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(4), 0);
            item.HorizontalAlignment = HorizontalAlignment.Stretch;
            item.VerticalAlignment = VerticalAlignment.Center;

            var label = MguiUiTheme.CreateBodyText(Window, stat.Label, MguiUiTheme.PrimaryTextColor);
            label.WrapText = false;
            _statLabels.Add(label);

            var progress = new MGProgressBar(Window, 0, 100, stat.FillPercent * 100f, MguiUiTheme.ScaleY(10), false)
            {
                PreferredWidth = 240,
                BorderThickness = new(0),
                CornerRadius = new MGCornerRadius(0),
                BackgroundBrush = MguiUiTheme.TransparentBackground,
                CompletedBrush = new VisualStateFillBrush(Color.White.AsFillBrush()),
                IncompleteBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            _statBars.Add(progress);

            item.TryAddChild(label);
            item.TryAddChild(progress);
            statsPanel.TryAddChild(item);
        }
        bandContent.TryAddChild(statsPanel, new Thickness(0, 0, 0, 84));

        var colorPanel = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(8), 0);
        colorPanel.HorizontalAlignment = HorizontalAlignment.Center;
        colorPanel.VerticalAlignment = VerticalAlignment.Center;
        colorPanel.ResponsiveAnchor = ResponsiveAnchor.BottomCenter;
        colorPanel.TryAddChild(MguiUiTheme.CreateBodyText(Window, "Car Color:", MguiUiTheme.PrimaryTextColor));

        var colors = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(10));
        _colorButtons = new();
        for (int i = 0; i < screen.AvailableColors.Count; i++)
        {
            int colorIndex = i;
            var button = new MGButton(Window, _ => screen.SelectCarColor(colorIndex))
            {
                BackgroundBrush = new VisualStateFillBrush(screen.AvailableColors[i].AsFillBrush()),
                BorderThickness = new(2),
                BorderBrush = new MGUniformBorderBrush(Color.White * 0.6f),
                PreferredWidth = 56,
                PreferredHeight = 56,
                Padding = new(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.SetContent(new MGTextBlock(Window, "", Color.White, 10));
            _colorButtons.Add(button);
            colors.TryAddChild(button);
        }
        colorPanel.TryAddChild(colors);
        bandContent.TryAddChild(colorPanel, new Thickness(0, 0, 0, 4));

        band.SetContent(bandContent);

        var actions = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(14));
        actions.ResponsiveAnchor = ResponsiveAnchor.BottomRight;
        _selectButton = CreateSpriteButton(UIRenderer.BottomButtonAButtonGfxRect, screen.ConfirmSelection);
        _cancelButton = CreateSpriteButton(UIRenderer.BottomButtonBButtonGfxRect, screen.RequestBack);
        actions.TryAddChild(_selectButton);
        actions.TryAddChild(_cancelButton);
        root.TryAddChild(band);
        root.TryAddChild(actions, new Thickness(0, 0, 48, 20));

        Window.SetContent(root);
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => _previousButton;

    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        Refresh();
        InitialFocusElement.Focus();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        Refresh();
    }

    private void Refresh()
    {
        var stats = _screen.GetCurrentCarStatEntries();
        for (int i = 0; i < _statLabels.Count && i < stats.Count; i++)
        {
            _statLabels[i].Text = stats[i].Label;
            _statBars[i].Value = stats[i].FillPercent * 100f;
        }

        for (int i = 0; i < _colorButtons.Count; i++)
        {
            bool selected = _screen.CurrentCarColor == i;
            _colorButtons[i].BorderThickness = selected ? new(4) : new(2);
            _colorButtons[i].BorderBrush = selected
                ? new MGUniformBorderBrush(MguiUiTheme.AccentColor)
                : new MGUniformBorderBrush(Color.White * 0.6f);
        }

        ApplyArrowState(_previousButton);
        ApplyArrowState(_nextButton);
        ApplySpriteButtonState(_selectButton);
        ApplySpriteButtonState(_cancelButton);
    }

    private MGButton CreateArrowButton(string text, Action action)
    {
        var button = MguiUiTheme.CreateBandButton(Window, text, action);
        button.MinWidth = 72;
        button.MinHeight = 96;
        if (button.Tag is MGTextBlock label)
        {
            label.FontSize = 30;
        }
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

        var image = new MGImage(Window, UiImageResources.AsImage(BaseGame.UI.Buttons.XnaTexture), sourceRect, null, Stretch.Uniform)
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

    private void ApplyArrowState(MGButton button)
    {
        MguiUiTheme.ApplyBandButtonState(button, button.VisualState.IsFocused || button.IsHovered);
    }

    private static void ApplySpriteButtonState(MGButton button)
    {
        if (button.Tag is MGImage image)
        {
            image.Opacity = button.VisualState.IsFocused || button.IsHovered ? 1f : 0.92f;
        }
    }
}