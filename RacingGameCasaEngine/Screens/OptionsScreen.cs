using System.ComponentModel;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Options, loaded from the <c>Screen.Options</c> screen asset (Content/UI/Screens/Options). The form is bound two ways to
/// <see cref="RaceOptionsViewModel"/>: a change in the form is written to <see cref="RaceFrontEndState"/> at once, and the
/// fields follow the state every frame. The settings are applied and saved by the Back button.
/// </summary>
internal sealed class OptionsScreen : RaceXamlScreenBase
{
    private const int ResolutionCount = 5;
    private static readonly VehicleDrivingMode[] DrivingModes = [VehicleDrivingMode.Arcade, VehicleDrivingMode.Simulation];

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _back;
    private readonly RaceOptionsViewModel _viewModel = new();
    private MGTextBox? _playerName;
    private MGButton[] _resolutionButtons = [];
    private MGButton[] _drivingModeButtons = [];
    private MGButton? _backButton;

    public OptionsScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action back)
        : base(assetContentManager, "Screen.Options")
    {
        _game = game;
        _state = state;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        var metrics = Root.Metrics;
        _viewModel.UpdateLayout(metrics.SafeArea.Y, metrics.SafeArea.Width, metrics.SafeArea.Height, metrics.Scale);
        _viewModel.BackButton.Update(metrics.Scale);
        _viewModel.PlayerName = _state.PlayerName;
        RefreshFromState();
        window.WindowDataContext = _viewModel;
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _playerName = FindControl<MGTextBox>("txtPlayerName");

        _resolutionButtons = new MGButton[ResolutionCount];
        for (int i = 0; i < ResolutionCount; i++)
        {
            int resolutionIndex = i;
            _resolutionButtons[i] = FindControl<MGButton>("btnResolution" + i);
            _resolutionButtons[i].AddCommandHandler((_, _) => _state.SelectedResolutionIndex = resolutionIndex);
            LegacyMenuUiTheme.ApplyBandButtonState(_resolutionButtons[i], false);
        }

        _drivingModeButtons = new MGButton[DrivingModes.Length];
        for (int i = 0; i < DrivingModes.Length; i++)
        {
            VehicleDrivingMode drivingMode = DrivingModes[i];
            _drivingModeButtons[i] = FindControl<MGButton>("btnDrivingMode" + i);
            _drivingModeButtons[i].AddCommandHandler((_, _) => _state.SelectedDrivingMode = drivingMode);
            LegacyMenuUiTheme.ApplyBandButtonState(_drivingModeButtons[i], false);
        }

        _backButton = FindControl<MGButton>("btnBack");
        _backButton.AddCommandHandler((_, _) => ApplyAndClose());

        RefreshButtonStates();
    }

    public override void Show()
    {
        _playerName?.Focus();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);
        RefreshFromState();
        RefreshButtonStates();
    }

    public override void Dispose()
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        base.Dispose();
    }

    private void ApplyAndClose()
    {
        _game.ApplyFrontEndOptions(_state);
        _game.SaveFrontEndOptions(_state);
        _back();
    }

    // A change made in the form (two-way bindings) goes to the front-end state at once, as the code-built screen did.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RaceOptionsViewModel.PlayerName): _state.PlayerName = _viewModel.PlayerName; break;
            case nameof(RaceOptionsViewModel.IsFullscreen): _state.IsFullscreen = _viewModel.IsFullscreen; break;
            case nameof(RaceOptionsViewModel.EnableVSync): _state.EnableVSync = _viewModel.EnableVSync; break;
            case nameof(RaceOptionsViewModel.EnablePostEffects): _state.EnablePostEffects = _viewModel.EnablePostEffects; break;
            case nameof(RaceOptionsViewModel.EnableShadows): _state.EnableShadows = _viewModel.EnableShadows; break;
            case nameof(RaceOptionsViewModel.ShowFps): _state.ShowFps = _viewModel.ShowFps; break;
            case nameof(RaceOptionsViewModel.EnableVibration): _state.EnableVibration = _viewModel.EnableVibration; break;
            case nameof(RaceOptionsViewModel.SoundVolume): _state.SoundVolume = (int)Math.Round(_viewModel.SoundVolume); break;
            case nameof(RaceOptionsViewModel.MusicVolume): _state.MusicVolume = (int)Math.Round(_viewModel.MusicVolume); break;
            case nameof(RaceOptionsViewModel.ControllerSensitivity): _state.ControllerSensitivity = (int)Math.Round(_viewModel.ControllerSensitivity); break;
        }
    }

    // The player name follows the text box only, as before; the other fields follow the state every frame.
    private void RefreshFromState()
    {
        _viewModel.IsFullscreen = _state.IsFullscreen;
        _viewModel.EnableVSync = _state.EnableVSync;
        _viewModel.EnablePostEffects = _state.EnablePostEffects;
        _viewModel.EnableShadows = _state.EnableShadows;
        _viewModel.ShowFps = _state.ShowFps;
        _viewModel.EnableVibration = _state.EnableVibration;
        _viewModel.SoundVolume = _state.SoundVolume;
        _viewModel.MusicVolume = _state.MusicVolume;
        _viewModel.ControllerSensitivity = _state.ControllerSensitivity;
        _viewModel.SoundVolumeText = $"{_state.SoundVolume:0}";
        _viewModel.MusicVolumeText = $"{_state.MusicVolume:0}";
        _viewModel.ControllerSensitivityText = $"{_state.ControllerSensitivity:0}";
    }

    private void RefreshButtonStates()
    {
        for (int i = 0; i < _resolutionButtons.Length; i++)
        {
            bool isActive = _state.SelectedResolutionIndex == i || _resolutionButtons[i].VisualState.IsFocused || _resolutionButtons[i].IsHovered;
            LegacyMenuUiTheme.ApplyBandButtonState(_resolutionButtons[i], isActive);
        }

        for (int i = 0; i < _drivingModeButtons.Length; i++)
        {
            bool isActive = _state.SelectedDrivingMode == DrivingModes[i] || _drivingModeButtons[i].VisualState.IsFocused || _drivingModeButtons[i].IsHovered;
            LegacyMenuUiTheme.ApplyBandButtonState(_drivingModeButtons[i], isActive);
        }

        if (_backButton != null)
        {
            LegacyMenuUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
        }
    }
}
