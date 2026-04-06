using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment;
using Thickness = MonoGame.Extended.Thickness;
using VerticalAlignment = MGUI.Core.UI.VerticalAlignment;

namespace RacingGameCasaEngine.Screens;

internal sealed class RaceHudScreen : RaceFrontEndScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _returnToMenu;
    private MGWindow? _window;
    private MGTextBlock? _header;
    private MGTextBlock? _speed;
    private MGTextBlock? _matchState;
    private MGTextBlock? _telemetry;
    private MGTextBlock? _performance;
    private float _fpsAccumulatedSeconds;
    private int _fpsSampleCount;
    private float _displayedFps;

    public RaceHudScreen(RacingGameCasaEngineGame game, RaceFrontEndState state, Action returnToMenu)
        : base(backgroundTexture: null)
    {
        _game = game;
        _state = state;
        _returnToMenu = returnToMenu;
    }

    public override UILayer Layer => UILayer.HUD;

    protected override void BuildScreen(UIRoot root)
    {
        _window = CreateForegroundWindow(root);

        var panel = new MGBorder(_window)
        {
            BackgroundBrush = new VisualStateFillBrush(RaceUiTheme.PanelColor.AsFillBrush()),
            Padding = new Thickness(20),
            Margin = new Thickness(24),
            PreferredWidth = 420,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var content = RaceUiTheme.CreateVerticalStack(_window, spacing: 14);
        _header = RaceUiTheme.CreateTitle(_window, "Race HUD");
        _speed = RaceUiTheme.CreateBody(_window, string.Empty);
        _matchState = RaceUiTheme.CreateBody(_window, string.Empty);
        _telemetry = RaceUiTheme.CreateBody(_window, string.Empty);
        _performance = RaceUiTheme.CreateBody(_window, string.Empty);
        content.TryAddChild(_header);
        content.TryAddChild(_speed);
        content.TryAddChild(_matchState);
        content.TryAddChild(_telemetry);
        content.TryAddChild(_performance);
        content.TryAddChild(RaceUiTheme.CreateSecondaryButton(_window, "Back to main menu", _returnToMenu));

        panel.SetContent(content);
        _window.SetContent(panel);
        RefreshLabels();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateDisplayedFps((float)gameTime.ElapsedGameTime.TotalSeconds);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        RuntimeRaceSession session = _game.RaceSession;
        if (session.IsActive && session.PlayerPawn != null && session.GameMode != null)
        {
            _header!.Text = $"{session.TrackName} | {session.CarName}";
            _speed!.Text = $"Driver: {_state.PlayerName} | Speed: {session.PlayerPawn.CurrentSpeedMph:000} mph";
            _matchState!.Text = BuildStatusText(session.GameMode);
            _telemetry!.Text = $"Lap: {Math.Min(session.GameMode.CompletedLaps + 1, session.GameMode.TotalLaps)}/{session.GameMode.TotalLaps} | Next checkpoint: {session.GameMode.NextCheckpointIndex + 1}/{Math.Max(1, session.GameMode.TotalCheckpoints)} | Position: {FormatVector(session.PlayerPawn.RootComponent?.Position ?? Vector3.Zero)}";
            _performance!.Text = $"FPS: {_displayedFps:0.0} | Debug camera: {(session.IsDebugCameraEnabled ? "On" : "Off")}";
            return;
        }

        _header!.Text = "Race HUD";
        _speed!.Text = $"Driver: {_state.PlayerName} | Car: {RaceFrontEndCatalog.Cars[_state.SelectedCarIndex].Name}";
        _matchState!.Text = "Waiting for race session";
        _telemetry!.Text = $"Track: {RaceFrontEndCatalog.Tracks[_state.SelectedTrackIndex].Name}";
        _performance!.Text = $"FPS: {_displayedFps:0.0}";
    }

    private void UpdateDisplayedFps(float elapsedSeconds)
    {
        if (elapsedSeconds <= 0.0f)
        {
            return;
        }

        _fpsAccumulatedSeconds += elapsedSeconds;
        _fpsSampleCount++;

        if (_fpsAccumulatedSeconds < 0.5f)
        {
            return;
        }

        _displayedFps = _fpsSampleCount / _fpsAccumulatedSeconds;
        _fpsAccumulatedSeconds = 0.0f;
        _fpsSampleCount = 0;
    }

    private static string FormatVector(Vector3 position)
    {
        return $"X {position.X:0.0} | Y {position.Y:0.0} | Z {position.Z:0.0}";
    }

    private static string BuildStatusText(GameFramework.RaceGameMode gameMode)
    {
        if (gameMode.IsRaceFinished)
        {
            return $"Finished | Time: {gameMode.RaceTimeSeconds:0.00}s";
        }

        if (gameMode.IsPaused)
        {
            return $"Paused | Time: {gameMode.RaceTimeSeconds:0.00}s";
        }

        if (gameMode.CountdownSecondsRemaining > 0f)
        {
            return $"Countdown: {MathF.Ceiling(gameMode.CountdownSecondsRemaining)}";
        }

        return $"Match State: {gameMode.MatchState} | Time: {gameMode.RaceTimeSeconds:0.00}s";
    }
}