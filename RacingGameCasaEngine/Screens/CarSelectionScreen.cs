using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.BorderBrushes;
using MonoGame.Extended;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Screens;

/// <summary>Car selection, loaded from the <c>Screen.CarSelection</c> screen asset (Content/UI/Screens/CarSelection).</summary>
internal sealed class CarSelectionScreen : RaceXamlScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _confirm;
    private readonly Action _back;
    private readonly RaceCarSelectionViewModel _viewModel = new();
    private MGButton? _previousButton;
    private MGButton? _nextButton;
    private MGButton? _selectButton;
    private MGButton? _cancelButton;
    private MGButton[] _colorButtons = [];
    private CarSelectionPreviewRenderer? _carPreviewRenderer;

    public CarSelectionScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action confirm, Action back)
        : base(assetContentManager, "Screen.CarSelection")
    {
        _game = game;
        _state = state;
        _confirm = confirm;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        var colors = RaceFrontEndCatalog.CarColors;
        if (colors.Count != RaceCarSelectionViewModel.SwatchCount)
        {
            throw new InvalidOperationException(
                $"CarSelection.xaml has {RaceCarSelectionViewModel.SwatchCount} colour swatches; the catalogue has {colors.Count} colours.");
        }

        _carPreviewRenderer = new CarSelectionPreviewRenderer(_game);
        _viewModel.PreviewImage = _carPreviewRenderer.TextureData;
        for (int i = 0; i < colors.Count; i++)
        {
            _viewModel.SetSwatchColor(i, colors[i].Value);
        }

        RefreshSelection();
        window.WindowDataContext = _viewModel;
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);

        _previousButton = FindControl<MGButton>("btnPrevious");
        _previousButton.AddCommandHandler((_, _) => MoveToPreviousCar());
        LegacyMenuUiTheme.ApplyBandButtonState(_previousButton, false);
        _nextButton = FindControl<MGButton>("btnNext");
        _nextButton.AddCommandHandler((_, _) => MoveToNextCar());
        LegacyMenuUiTheme.ApplyBandButtonState(_nextButton, false);

        _colorButtons = new MGButton[colors.Count];
        for (int i = 0; i < colors.Count; i++)
        {
            int colorIndex = i;
            _colorButtons[i] = FindControl<MGButton>("swatch" + i);
            _colorButtons[i].AddCommandHandler((_, _) => SelectColor(colorIndex));
        }

        // LegacyMenuUiTheme.ApplySpriteButtonState dims the image a sprite button carries in its Tag.
        _selectButton = FindControl<MGButton>("btnSelect");
        _selectButton.Tag = FindControl<MGImage>("imgSelect");
        _selectButton.AddCommandHandler((_, _) => _confirm());
        _cancelButton = FindControl<MGButton>("btnCancel");
        _cancelButton.Tag = FindControl<MGImage>("imgCancel");
        _cancelButton.AddCommandHandler((_, _) => _back());

        RefreshSwatchStates();
    }

    public override void Show()
    {
        _previousButton?.Focus();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);
        _carPreviewRenderer?.Update(gameTime, _state.SelectedCarIndex, _state.SelectedCarColorIndex);
        RefreshSelection();
        RefreshSwatchStates();
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

    public override void Dispose()
    {
        base.Dispose();
        _carPreviewRenderer?.Dispose();
        _carPreviewRenderer = null;
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
        RefreshSwatchStates();
    }

    private void RefreshSelection()
    {
        var car = RaceFrontEndCatalog.Cars[_state.SelectedCarIndex];
        _viewModel.Summary = $"{car.Name}\n{car.Summary}";

        for (int i = 0; i < RaceCarSelectionViewModel.StatCount; i++)
        {
            CarSelectionDisplayStat? stat = i < car.SelectionStats.Count
                ? car.SelectionStats[i]
                : null;

            _viewModel.SetStatLabel(i, stat?.Label ?? string.Empty);
            _viewModel.SetStatFill(i, stat?.FillPercent ?? 0f);
        }
    }

    // Selection restyle of the swatches, as the code-built screen applied it.
    private void RefreshSwatchStates()
    {
        for (int i = 0; i < _colorButtons.Length; i++)
        {
            bool selected = _state.SelectedCarColorIndex == i;
            _colorButtons[i].BorderThickness = selected ? new Thickness(4) : new Thickness(2);
            _colorButtons[i].BorderBrush = selected
                ? new MGUniformBorderBrush(LegacyMenuUiTheme.AccentColor)
                : new MGUniformBorderBrush(Color.White * 0.6f);
        }
    }
}
