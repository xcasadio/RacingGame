using CasaEngine.Engine.Input;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Options, loaded from the <c>Screen.Options</c> screen asset (Content/UI/Screens/Options): the previous arrangement
/// of the options in the menus' style (ADR-0014), with RacingGame's options input
/// (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/Options.cs</c>, Update) extended to every row:
/// <list type="bullet">
/// <item>the typed text edits the player name (printable ASCII, the characters GameFont has; Backspace erases);</item>
/// <item>Up and Down (keyboard, D-pad, left stick past 0.5) move the selection arrow over the rows, with Highlight;
/// Left and Right change the resolution or the driving mode (ButtonClick) or move a slider by 10 (Highlight); Enter or
/// A switch the selected option or the driving mode (ButtonClick);</item>
/// <item>a click on a resolution, a driving mode or an option row changes it (ButtonClick), a click on a slider sets it
/// (Highlight);</item>
/// <item>the Highlight sound plays when the mouse enters any of these areas or the B button;</item>
/// <item>Escape, B, Back or a click on B BACK apply and save the settings and leave with ScreenBack.</item>
/// </list>
/// Every change goes to <see cref="RaceFrontEndState"/> at once; the volumes apply as they change, the other settings
/// when the player leaves. The screen plays ScreenClick when shown, as RacingGame did when it pushed a screen. Nothing
/// takes MGUI's focus.
/// </summary>
internal sealed class OptionsScreen : RaceXamlScreenBase
{
    private const float StickThreshold = 0.5f;
    // The name text box of the code-built screen took 24 characters.
    private const int MaxNameLength = 24;
    private const int SliderStep = 10;

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _back;
    private readonly RaceOptionsViewModel _viewModel = new();
    private readonly Func<string, float> _measure;
    private MGTextBlock[] _texts = [];
    private MGTextBlock[] _resolutionTexts = [];
    private MGTextBlock[] _drivingModeTexts = [];
    private bool _isReadingText;
    // The first update after the screen opens takes no input: the key or click that opened it is still "just pressed".
    private bool _acceptsInput;
    private bool _isLeaving;
    private Point _lastMouse = new(-1, -1);
    private Vector2 _lastStick;

    public OptionsScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action back)
        : base(assetContentManager, "Screen.Options")
    {
        _game = game;
        _state = state;
        _back = back;
        _measure = game.MeasureGameFontText;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        string[] labels = RacingGameCasaEngineGame.GetResolutionLabels();
        if (labels.Length != RaceOptionsViewModel.ResolutionCount)
        {
            throw new InvalidOperationException(
                $"Options.xaml has {RaceOptionsViewModel.ResolutionCount} resolution labels; the game has {labels.Length} resolutions with Auto.");
        }

        _viewModel.ResolutionLabel0 = labels[0];
        _viewModel.ResolutionLabel1 = labels[1];
        _viewModel.ResolutionLabel2 = labels[2];
        _viewModel.ResolutionLabel3 = labels[3];
        _viewModel.ResolutionLabel4 = labels[4];

        _resolutionTexts = FindTexts("txtResolution", RaceOptionsViewModel.ResolutionCount);
        _drivingModeTexts = [FindControl<MGTextBlock>("txtArcade"), FindControl<MGTextBlock>("txtSimulation")];
        _texts =
        [
            .. FindTexts("txtLabel", RaceOptionsViewModel.RowCount), FindControl<MGTextBlock>("txtName"), .. _resolutionTexts,
            .. _drivingModeTexts, FindControl<MGTextBlock>("txtSoundValue"), FindControl<MGTextBlock>("txtMusicValue"),
            FindControl<MGTextBlock>("txtSensitivityValue"),
        ];
        foreach (MGTextBlock text in _texts)
        {
            text.RenderTransform.Origin = Vector2.Zero;
        }

        window.WindowDataContext = _viewModel;
        UpdateLayout(0.0);
    }

    public override void Show()
    {
        PlaySound(MenuSound.ScreenClick);
        if (!_isReadingText)
        {
            _game.Window.TextInput += OnTextInput;
            _isReadingText = true;
        }
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        if (_isLeaving)
        {
            return;
        }

        UpdateLayout(gameTime.TotalGameTime.TotalSeconds);
    }

    public override void Dispose()
    {
        StopReadingText();
        base.Dispose();
    }

    // Input.HandleKeyboardInput: the typed characters edit the name.
    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!_acceptsInput || _isLeaving)
        {
            return;
        }

        string name = _state.PlayerName;
        if (e.Key == XnaKeys.Back)
        {
            if (name.Length > 0)
            {
                _state.PlayerName = name[..^1];
            }
        }
        else if (e.Character >= ' ' && e.Character <= '~' && name.Length < MaxNameLength)
        {
            _state.PlayerName = name + e.Character;
        }
    }

    private void HandleInput()
    {
        KeyboardManager keyboard = _game.InputComponent.KeyboardManager;
        MouseManager mouse = _game.InputComponent.MouseManager;
        GamePad gamePad = _game.InputComponent.GamePadManager.GetGamePad(PlayerIndex.One);
        Point position = mouse.Position;
        Vector2 stick = gamePad.IsConnected ? new Vector2(gamePad.LeftStickX, gamePad.LeftStickY) : Vector2.Zero;

        if (!_acceptsInput)
        {
            _acceptsInput = true;
            _lastMouse = position;
            _lastStick = stick;
            return;
        }

        // Input.MouseInBox played Highlight whenever the mouse entered a box it tested.
        int resolution = _viewModel.GetResolutionAt(position.X, position.Y);
        int drivingMode = _viewModel.GetDrivingModeAt(position.X, position.Y);
        int stop = _viewModel.GetStopAt(position.X, position.Y);
        if ((resolution >= 0 && resolution != _viewModel.GetResolutionAt(_lastMouse.X, _lastMouse.Y))
            || (drivingMode >= 0 && drivingMode != _viewModel.GetDrivingModeAt(_lastMouse.X, _lastMouse.Y))
            || (stop >= 0 && stop != _viewModel.GetStopAt(_lastMouse.X, _lastMouse.Y))
            || (_viewModel.IsOverBackButton(position.X, position.Y) && !_viewModel.IsOverBackButton(_lastMouse.X, _lastMouse.Y)))
        {
            PlaySound(MenuSound.Highlight);
        }

        _lastMouse = position;

        bool click = mouse.LeftButtonJustPressed;
        if (click && resolution >= 0)
        {
            PlaySound(MenuSound.ButtonClick);
            _state.SelectedResolutionIndex = resolution;
        }
        else if (click && drivingMode >= 0)
        {
            PlaySound(MenuSound.ButtonClick);
            _state.SelectedDrivingMode = drivingMode == 1 ? VehicleDrivingMode.Simulation : VehicleDrivingMode.Arcade;
        }
        else if (click && stop >= 0)
        {
            if (RaceOptionsViewModel.IsSlider(stop))
            {
                PlaySound(MenuSound.Highlight);
                SetSlider(stop, _viewModel.GetSliderValueAt(stop, position.X));
            }
            else
            {
                PlaySound(MenuSound.ButtonClick);
                Toggle(stop);
            }
        }

        bool up = keyboard.IsKeyJustPressed(XnaKeys.Up)
            || (gamePad.IsConnected && gamePad.DPadUpJustPressed)
            || (stick.Y > StickThreshold && _lastStick.Y <= StickThreshold);
        bool down = keyboard.IsKeyJustPressed(XnaKeys.Down)
            || (gamePad.IsConnected && gamePad.DPadDownJustPressed)
            || (stick.Y < -StickThreshold && _lastStick.Y >= -StickThreshold);
        bool left = keyboard.IsKeyJustPressed(XnaKeys.Left)
            || (gamePad.IsConnected && gamePad.DPadLeftJustPressed)
            || (stick.X < -StickThreshold && _lastStick.X >= -StickThreshold);
        bool right = keyboard.IsKeyJustPressed(XnaKeys.Right)
            || (gamePad.IsConnected && gamePad.DPadRightJustPressed)
            || (stick.X > StickThreshold && _lastStick.X <= StickThreshold);
        _lastStick = stick;

        int count = RaceOptionsViewModel.StopCount;
        if (up)
        {
            PlaySound(MenuSound.Highlight);
            _viewModel.SelectedStop = (_viewModel.SelectedStop + count - 1) % count;
        }
        else if (down)
        {
            PlaySound(MenuSound.Highlight);
            _viewModel.SelectedStop = (_viewModel.SelectedStop + 1) % count;
        }

        int selected = _viewModel.SelectedStop;
        if (left || right)
        {
            int direction = left ? -1 : 1;
            if (selected == RaceOptionsViewModel.StopResolution)
            {
                PlaySound(MenuSound.ButtonClick);
                int resolutions = RaceOptionsViewModel.ResolutionCount;
                _state.SelectedResolutionIndex = (Math.Clamp(_state.SelectedResolutionIndex, 0, resolutions - 1) + direction + resolutions) % resolutions;
            }
            else if (selected == RaceOptionsViewModel.StopDrivingMode)
            {
                PlaySound(MenuSound.ButtonClick);
                SwitchDrivingMode();
            }
            else if (RaceOptionsViewModel.IsSlider(selected))
            {
                PlaySound(MenuSound.Highlight);
                SetSlider(selected, GetSlider(selected) + direction * SliderStep);
            }
        }

        if ((keyboard.IsKeyJustPressed(XnaKeys.Enter) || (gamePad.IsConnected && gamePad.AJustPressed))
            && selected != RaceOptionsViewModel.StopResolution && !RaceOptionsViewModel.IsSlider(selected))
        {
            PlaySound(MenuSound.ButtonClick);
            if (selected == RaceOptionsViewModel.StopDrivingMode)
            {
                SwitchDrivingMode();
            }
            else
            {
                Toggle(selected);
            }
        }

        if (keyboard.IsKeyJustPressed(XnaKeys.Escape)
            || (gamePad.IsConnected && (gamePad.BJustPressed || gamePad.BackJustPressed))
            || (click && _viewModel.IsOverBackButton(position.X, position.Y)))
        {
            ApplyAndClose();
        }
    }

    private void SwitchDrivingMode()
    {
        _state.SelectedDrivingMode = _state.SelectedDrivingMode == VehicleDrivingMode.Simulation ? VehicleDrivingMode.Arcade : VehicleDrivingMode.Simulation;
    }

    private void Toggle(int stop)
    {
        switch (stop)
        {
            case RaceOptionsViewModel.StopFullscreen: _state.IsFullscreen = !_state.IsFullscreen; break;
            case RaceOptionsViewModel.StopVSync: _state.EnableVSync = !_state.EnableVSync; break;
            case RaceOptionsViewModel.StopPostEffects: _state.EnablePostEffects = !_state.EnablePostEffects; break;
            case RaceOptionsViewModel.StopShadows: _state.EnableShadows = !_state.EnableShadows; break;
            case RaceOptionsViewModel.StopShowFps: _state.ShowFps = !_state.ShowFps; break;
            case RaceOptionsViewModel.StopVibration: _state.EnableVibration = !_state.EnableVibration; break;
        }
    }

    private int GetSlider(int stop) => stop switch
    {
        RaceOptionsViewModel.StopSound => _state.SoundVolume,
        RaceOptionsViewModel.StopMusic => _state.MusicVolume,
        _ => _state.ControllerSensitivity,
    };

    // A slider's value, 0 to 100; the volumes apply at once, as RacingGame's Sound.SetVolumes did every frame.
    private void SetSlider(int stop, int value)
    {
        value = Math.Clamp(value, 0, 100);
        switch (stop)
        {
            case RaceOptionsViewModel.StopSound: _state.SoundVolume = value; break;
            case RaceOptionsViewModel.StopMusic: _state.MusicVolume = value; break;
            case RaceOptionsViewModel.StopSensitivity: _state.ControllerSensitivity = value; break;
        }

        _game.ApplyFrontEndVolumes(_state);
    }

    // Every exit applies and saves the settings, as RacingGame's did (ADR-0014).
    private void ApplyAndClose()
    {
        _isLeaving = true;
        StopReadingText();
        PlaySound(MenuSound.ScreenBack);
        _game.ApplyFrontEndOptions(_state);
        _game.SaveFrontEndOptions(_state);
        _back();
    }

    private void StopReadingText()
    {
        if (_isReadingText)
        {
            _game.Window.TextInput -= OnTextInput;
            _isReadingText = false;
        }
    }

    private MGTextBlock[] FindTexts(string prefix, int count)
    {
        var texts = new MGTextBlock[count];
        for (int i = 0; i < count; i++)
        {
            texts[i] = FindControl<MGTextBlock>(prefix + i);
        }

        return texts;
    }

    private void PlaySound(MenuSound sound) => _game.MenuSounds?.Play(sound);

    // Lays the screen out for the viewport, the time, the mouse and the settings, then scales and colours the texts as
    // RacingGame's TextureFont did.
    private void UpdateLayout(double totalSeconds)
    {
        UpdateMenuDecoration(_viewModel.Decoration, totalSeconds);
        Point mouse = _game.InputComponent.MouseManager.Position;
        var values = new RaceOptionsValues(
            _state.PlayerName,
            _state.SelectedResolutionIndex,
            _state.IsFullscreen,
            _state.SelectedDrivingMode == VehicleDrivingMode.Simulation,
            _state.EnablePostEffects,
            _state.EnableShadows,
            _state.EnableVSync,
            _state.ShowFps,
            _state.EnableVibration,
            _state.SoundVolume,
            _state.MusicVolume,
            _state.ControllerSensitivity);
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, mouse.X, mouse.Y, totalSeconds, _state.PinScreenAnimations, values, _measure);

        var scale = new Vector2(_viewModel.TextScaleX, _viewModel.TextScaleY);
        foreach (MGTextBlock text in _texts)
        {
            text.RenderTransform.Scale = scale;
        }

        for (int i = 0; i < _resolutionTexts.Length; i++)
        {
            SetForeground(_resolutionTexts[i], _viewModel.ResolutionColors[i]);
        }

        for (int i = 0; i < _drivingModeTexts.Length; i++)
        {
            SetForeground(_drivingModeTexts[i], _viewModel.DrivingModeColors[i]);
        }
    }

    private static void SetForeground(MGTextBlock text, Color color)
    {
        if (text.ActualForeground != color)
        {
            text.Foreground = new(color, color, color);
        }
    }
}
