using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Responsive;
using MonoGame.Extended;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGameCasaEngine.Screens;

internal sealed class CarSelectionScreen : RaceFrontEndScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _confirm;
    private readonly Action _back;
    private MGButton? _previousButton;
    private MGButton? _nextButton;
    private MGButton? _selectButton;
    private MGButton? _cancelButton;
    private MGTextBlock? _summary;
    private readonly List<MGTextBlock> _statLabels = [];
    private readonly List<MGProgressBar> _statBars = [];
    private readonly List<MGButton> _colorButtons = [];
    private CarSelectionPreviewRenderer? _carPreviewRenderer;
    private MGImage? _carPreviewImage;

    public CarSelectionScreen(RacingGameCasaEngineGame game, Texture2D? backgroundTexture, Texture2D? buttonsTexture, RaceFrontEndState state, Action confirm, Action back)
        : base(backgroundTexture, buttonsTexture)
    {
        _game = game;
        _state = state;
        _confirm = confirm;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        if (ButtonsTexture == null)
        {
            throw new InvalidOperationException("Legacy menu atlas is required for the car selection screen.");
        }

        var window = CreateForegroundWindow(root);
        var rootPanel = new MGOverlayPanel(window)
        {
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Stretch,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Stretch,
        };

        var band = LegacyMenuUiTheme.CreateMenuBand(window, 191, 439, new Thickness(28, 14, 28, 18));
        var bandContent = new MGOverlayPanel(window)
        {
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Stretch,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Stretch,
        };

        _previousButton = CreateArrowButton(window, "<", MoveToPreviousCar);
        _nextButton = CreateArrowButton(window, ">", MoveToNextCar);
        _previousButton.ResponsiveAnchor = ResponsiveAnchor.MiddleLeft;
        _nextButton.ResponsiveAnchor = ResponsiveAnchor.MiddleRight;
        bandContent.TryAddChild(_previousButton, new Thickness(0, 0, 0, 84));
        bandContent.TryAddChild(_nextButton, new Thickness(0, 0, 0, 84));

        _carPreviewRenderer = new CarSelectionPreviewRenderer(_game);

        var mainRow = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 38);
        mainRow.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        mainRow.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

        var previewPanel = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 10);
        previewPanel.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        previewPanel.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;
        previewPanel.PreferredWidth = 340;

        var previewLabel = LegacyMenuUiTheme.CreateBodyText(window, "Preview", LegacyMenuUiTheme.AccentColor);
        previewLabel.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        previewLabel.TextAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        previewLabel.WrapText = false;
        previewPanel.TryAddChild(previewLabel);

        var previewFrame = new MGBorder(window, new Thickness(2), new MGUniformBorderBrush(new Color(255, 255, 255, 70)))
        {
            CornerRadius = new MGCornerRadius(18),
            BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 24).AsFillBrush()),
            Padding = new Thickness(0),
            PreferredWidth = 340,
            PreferredHeight = 220,
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        };
        _carPreviewImage = new MGImage(window, _carPreviewRenderer.TextureData, null, Stretch.Uniform)
        {
            PreferredWidth = 336,
            PreferredHeight = 216,
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        };
        previewFrame.SetContent(_carPreviewImage);
        previewPanel.TryAddChild(previewFrame);

        var statsPanel = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 8);
        statsPanel.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Stretch;
        statsPanel.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;
        statsPanel.PreferredWidth = 300;

        _summary = LegacyMenuUiTheme.CreateBodyText(window, string.Empty, LegacyMenuUiTheme.AccentColor);
        _summary.WrapText = true;
        statsPanel.TryAddChild(_summary);
        for (int i = 0; i < 4; i++)
        {
            var item = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 4);
            item.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Stretch;
            item.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

            var statLabel = LegacyMenuUiTheme.CreateBodyText(window, string.Empty);
            statLabel.WrapText = false;
            _statLabels.Add(statLabel);
            var progress = new MGProgressBar(window, 0, 100, 0, 10, false)
            {
                PreferredWidth = 240,
                BorderThickness = new Thickness(0),
                CornerRadius = new MGUI.Core.UI.MGCornerRadius(0),
                BackgroundBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
                CompletedBrush = new VisualStateFillBrush(Color.White.AsFillBrush()),
                IncompleteBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
                HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Left,
            };
            _statBars.Add(progress);
            item.TryAddChild(statLabel);
            item.TryAddChild(progress);
            statsPanel.TryAddChild(item);
        }

        mainRow.TryAddChild(previewPanel);
        mainRow.TryAddChild(statsPanel);
        bandContent.TryAddChild(mainRow, new Thickness(58, 20, 58, 108));

        var colorPanel = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 8);
        colorPanel.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        colorPanel.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;
        colorPanel.ResponsiveAnchor = ResponsiveAnchor.BottomCenter;
        colorPanel.TryAddChild(LegacyMenuUiTheme.CreateBodyText(window, "Car Color:", LegacyMenuUiTheme.PrimaryTextColor));

        var colorsRow = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 10);
        for (int i = 0; i < RaceFrontEndCatalog.CarColors.Count; i++)
        {
            int colorIndex = i;
            var button = new MGButton(window, _ => SelectColor(colorIndex))
            {
                BackgroundBrush = new VisualStateFillBrush(RaceFrontEndCatalog.CarColors[i].Value.AsFillBrush()),
                BorderThickness = new Thickness(2),
                BorderBrush = new MGUniformBorderBrush(Color.White * 0.6f),
                PreferredWidth = 56,
                PreferredHeight = 56,
                Padding = new Thickness(0),
                HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
                VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
            };
            button.SetContent(new MGTextBlock(window, string.Empty, Color.White, 10));
            _colorButtons.Add(button);
            colorsRow.TryAddChild(button);
        }
        colorPanel.TryAddChild(colorsRow);
        bandContent.TryAddChild(colorPanel, new Thickness(0, 0, 0, 4));

        band.SetContent(bandContent);

        var actions = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 14);
        actions.ResponsiveAnchor = ResponsiveAnchor.BottomRight;
        _selectButton = LegacyMenuUiTheme.CreateSpriteButton(window, ButtonsTexture, LegacyMenuUiAtlas.BottomButtonA, _confirm);
        _cancelButton = LegacyMenuUiTheme.CreateSpriteButton(window, ButtonsTexture, LegacyMenuUiAtlas.BottomButtonB, _back);
        actions.TryAddChild(_selectButton);
        actions.TryAddChild(_cancelButton);
        rootPanel.TryAddChild(band);
        rootPanel.TryAddChild(actions, new Thickness(0, 0, 48, 20));

        window.SetContent(rootPanel);

        RefreshSelection();
    }

    public override void Show()
    {
        _previousButton?.Focus();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(gameTime.TotalGameTime.TotalSeconds);
        _carPreviewRenderer?.Update(gameTime, _state.SelectedCarIndex, _state.SelectedCarColorIndex);
        RefreshSelection();
        if (_previousButton != null)
        {
            LegacyMenuUiTheme.ApplyBandButtonState(_previousButton, _previousButton.VisualState.IsFocused || _previousButton.IsHovered);
        }
        if (_nextButton != null)
        {
            LegacyMenuUiTheme.ApplyBandButtonState(_nextButton, _nextButton.VisualState.IsFocused || _nextButton.IsHovered);
        }
        if (_selectButton != null) LegacyMenuUiTheme.ApplySpriteButtonState(_selectButton);
        if (_cancelButton != null) LegacyMenuUiTheme.ApplySpriteButtonState(_cancelButton);
    }

    private void MoveToPreviousCar()
    {
        _state.SelectedCarIndex = (_state.SelectedCarIndex + RaceFrontEndCatalog.Cars.Count - 1) % RaceFrontEndCatalog.Cars.Count;
    }

    private void MoveToNextCar()
    {
        _state.SelectedCarIndex = (_state.SelectedCarIndex + 1) % RaceFrontEndCatalog.Cars.Count;
    }

    private void SelectColor(int index)
    {
        _state.SelectedCarColorIndex = index;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        var car = RaceFrontEndCatalog.Cars[_state.SelectedCarIndex];
        if (_summary != null)
        {
            _summary.Text = $"{car.Name}\n{car.Summary}";
        }

        for (int i = 0; i < _statLabels.Count; i++)
        {
            _statLabels[i].Text = i < car.Stats.Count ? car.Stats[i] : string.Empty;
            if (i < _statBars.Count)
            {
                _statBars[i].Value = GetStatBarValue(_state.SelectedCarIndex, i);
            }
        }

        for (int i = 0; i < _colorButtons.Count; i++)
        {
            bool selected = _state.SelectedCarColorIndex == i;
            _colorButtons[i].BorderThickness = selected ? new Thickness(4) : new Thickness(2);
            _colorButtons[i].BorderBrush = selected
                ? new MGUniformBorderBrush(LegacyMenuUiTheme.AccentColor)
                : new MGUniformBorderBrush(Color.White * 0.6f);
        }
    }

    private static MGButton CreateArrowButton(MGWindow window, string text, Action action)
    {
        var button = LegacyMenuUiTheme.CreateBandButton(window, text, action);
        button.MinWidth = 72;
        button.MinHeight = 96;
        if (button.Tag is MGTextBlock label)
        {
            label.FontSize = 30;
        }
        else if (button.Content is MGTextBlock contentLabel)
        {
            contentLabel.FontSize = 30;
        }
        return button;
    }

    private static float GetStatBarValue(int carIndex, int statIndex)
    {
        float[][] values =
        [
            [68f, 54f, 72f, 61f],
            [82f, 77f, 48f, 55f],
            [74f, 63f, 86f, 79f],
        ];
        return values[Math.Clamp(carIndex, 0, values.Length - 1)][Math.Clamp(statIndex, 0, values[0].Length - 1)];
    }
}