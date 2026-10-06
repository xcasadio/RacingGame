using CasaEngine.Core.Log;
using CasaEngine.Framework.Application;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// --capture-ui-screens: walks the 10 UI states (front-end screens, race HUD, pause, race finished) through the flow's
/// automation entry points, captures the back buffer of each once it has settled, and moves the captures into a folder of
/// their own (Screenshots/ui-run-&lt;timestamp&gt;/ui-&lt;state&gt;.png) so scripts/UiCaptureCompare can compare two runs.
/// The run uses a fixed windowed back buffer of <see cref="CaptureWidth"/> x <see cref="CaptureHeight"/>, applied without saving it,
/// so captures do not depend on the persisted display settings. The front-end state is set to the same display settings, so the
/// options applied again when the race world loads keep that size; nothing is saved (ADR-0004).
/// </summary>
internal sealed class UiScreenCaptureValidator
{
    private const int ExpectedCaptureCount = 10;
    private const int CaptureWidth = 1920;
    private const int CaptureHeight = 1080;
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinimumStepInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

    private enum CaptureStep
    {
        WaitForSplash,
        OpenMainMenu,
        CaptureMainMenu,
        OpenHighscores,
        CaptureHighscores,
        OpenOptions,
        CaptureOptions,
        OpenHelp,
        CaptureHelp,
        OpenCarSelection,
        CaptureCarSelection,
        OpenTrackSelection,
        CaptureTrackSelection,
        StartRace,
        CaptureRaceHud,
        PauseRace,
        CapturePause,
        ResumeRace,
        CompleteRace,
        CaptureRaceFinished,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly string _runDirectory;
    private CaptureStep _step = CaptureStep.WaitForSplash;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private TimeSpan? _readySince;
    private bool _started;
    private int _captureCount;

    public UiScreenCaptureValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow)
    {
        _game = game;
        _flow = flow;
        _runDirectory = Path.Combine(_game.GetUserDataDirectory(), "Screenshots", $"ui-run-{DateTime.Now:yyyyMMdd-HHmmss}");
        _game.PreviewUpdate += OnPreviewUpdate;
    }

    private void OnPreviewUpdate(object? sender, TimeSpan totalTime)
    {
        if (!_started)
        {
            _started = true;
            _startedAt = totalTime;
            _lastTransitionAt = totalTime;

            // Fixed selections so two runs show the same car, colour and track.
            _flow.State.SelectedCarIndex = 0;
            _flow.State.SelectedCarColorIndex = 0;
            _flow.State.SelectedTrackIndex = 0;
            _flow.State.PinScreenAnimations = true;

            DisplaySettings current = _game.GetDisplaySettings();
            _game.ApplyDisplaySettings(new DisplaySettings(CaptureWidth, CaptureHeight, false, current.IsVSyncEnabled), persistToProjectSettings: false);
            _flow.State.SelectedResolutionIndex = RacingGameCasaEngineGame.GetResolutionIndex(CaptureWidth, CaptureHeight);
            _flow.State.IsFullscreen = false;
            _flow.State.EnableVSync = current.IsVSyncEnabled;
            Logs.WriteInfo($"UI screen capture started, run folder {_runDirectory}");
        }

        if (_step is CaptureStep.Completed or CaptureStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > Timeout)
        {
            Fail($"UI screen capture timed out at step {_step}.");
            return;
        }

        if (totalTime - _lastTransitionAt < MinimumStepInterval)
        {
            return;
        }

        string? currentState = _game.GameManager.ScreenManager.CurrentState;
        bool frontEndWorld = _game.GameManager.CurrentWorld?.Name == RaceWorldFactory.FrontEndWorldName;
        bool raceWorld = _game.GameManager.CurrentWorld is { } world && RaceWorldFactory.IsRaceWorld(world);

        switch (_step)
        {
            case CaptureStep.WaitForSplash:
                if (IsSettled(frontEndWorld && currentState == RaceFrontEndFlow.SplashStateName && HasCaptureSize(), totalTime))
                {
                    CaptureAndAdvance("splash", CaptureStep.OpenMainMenu, totalTime);
                }
                break;

            case CaptureStep.OpenMainMenu:
                _flow.OpenMainMenuForAutomation();
                Advance(CaptureStep.CaptureMainMenu, totalTime, "Splash -> MainMenu");
                break;

            case CaptureStep.CaptureMainMenu:
                if (IsSettled(currentState == RaceFrontEndFlow.MainMenuStateName, totalTime))
                {
                    CaptureAndAdvance("main-menu", CaptureStep.OpenHighscores, totalTime);
                }
                break;

            case CaptureStep.OpenHighscores:
                _flow.OpenHighscoresForAutomation();
                Advance(CaptureStep.CaptureHighscores, totalTime, "MainMenu -> Highscores");
                break;

            case CaptureStep.CaptureHighscores:
                if (IsSettled(currentState == RaceFrontEndFlow.HighscoresStateName, totalTime))
                {
                    CaptureAndAdvance("highscores", CaptureStep.OpenOptions, totalTime);
                }
                break;

            case CaptureStep.OpenOptions:
                _flow.OpenOptionsForAutomation();
                Advance(CaptureStep.CaptureOptions, totalTime, "Highscores -> Options");
                break;

            case CaptureStep.CaptureOptions:
                if (IsSettled(currentState == RaceFrontEndFlow.OptionsStateName, totalTime))
                {
                    CaptureAndAdvance("options", CaptureStep.OpenHelp, totalTime);
                }
                break;

            case CaptureStep.OpenHelp:
                _flow.OpenHelpForAutomation();
                Advance(CaptureStep.CaptureHelp, totalTime, "Options -> Help");
                break;

            case CaptureStep.CaptureHelp:
                if (IsSettled(currentState == RaceFrontEndFlow.HelpStateName, totalTime))
                {
                    CaptureAndAdvance("help", CaptureStep.OpenCarSelection, totalTime);
                }
                break;

            case CaptureStep.OpenCarSelection:
                _flow.OpenCarSelectionForAutomation();
                Advance(CaptureStep.CaptureCarSelection, totalTime, "Help -> CarSelection");
                break;

            case CaptureStep.CaptureCarSelection:
                if (IsSettled(currentState == RaceFrontEndFlow.CarSelectionStateName, totalTime))
                {
                    CaptureAndAdvance("car-selection", CaptureStep.OpenTrackSelection, totalTime);
                }
                break;

            case CaptureStep.OpenTrackSelection:
                _flow.OpenTrackSelectionForAutomation();
                Advance(CaptureStep.CaptureTrackSelection, totalTime, "CarSelection -> TrackSelection");
                break;

            case CaptureStep.CaptureTrackSelection:
                if (IsSettled(currentState == RaceFrontEndFlow.TrackSelectionStateName, totalTime))
                {
                    CaptureAndAdvance("track-selection", CaptureStep.StartRace, totalTime);
                }
                break;

            case CaptureStep.StartRace:
                _flow.StartRaceForAutomation();
                Advance(CaptureStep.CaptureRaceHud, totalTime, "TrackSelection -> RaceHud");
                break;

            case CaptureStep.CaptureRaceHud:
                if (IsSettled(
                        currentState == RaceFrontEndFlow.RaceHudStateName
                        && raceWorld
                        && _game.RaceSession.IsActive
                        && _game.RaceSession.GameMode is { CountdownSecondsRemaining: <= 0f, IsPaused: false, IsRaceFinished: false },
                        totalTime))
                {
                    CaptureAndAdvance("race-hud", CaptureStep.PauseRace, totalTime);
                }
                break;

            case CaptureStep.PauseRace:
                if (_game.RaceSession.GameMode is { IsPaused: false } gameModeToPause)
                {
                    gameModeToPause.TogglePause();
                    Advance(CaptureStep.CapturePause, totalTime, "RaceHud -> Paused");
                }
                break;

            case CaptureStep.CapturePause:
                if (IsSettled(_game.RaceSession.GameMode is { IsPaused: true }, totalTime))
                {
                    CaptureAndAdvance("pause", CaptureStep.ResumeRace, totalTime);
                }
                break;

            case CaptureStep.ResumeRace:
                if (_game.RaceSession.GameMode is { IsPaused: true } gameModeToResume)
                {
                    gameModeToResume.TogglePause();
                    Advance(CaptureStep.CompleteRace, totalTime, "Paused -> RaceHud");
                }
                break;

            case CaptureStep.CompleteRace:
                if (_game.RaceSession.GameMode is { IsPaused: false, IsRaceFinished: false } gameModeToComplete)
                {
                    gameModeToComplete.CompleteRaceForAutomation();
                    Advance(CaptureStep.CaptureRaceFinished, totalTime, "RaceHud -> race finished");
                }
                break;

            case CaptureStep.CaptureRaceFinished:
                if (IsSettled(currentState == RaceFrontEndFlow.RaceHudStateName && _game.RaceSession.GameMode is { IsRaceFinished: true }, totalTime))
                {
                    CaptureAndAdvance("race-finished", CaptureStep.ReturnToFrontEnd, totalTime);
                }
                break;

            case CaptureStep.ReturnToFrontEnd:
                _flow.ReturnToFrontEndForAutomation();
                Advance(CaptureStep.Completed, totalTime, "RaceHud -> MainMenu");
                Complete();
                break;
        }
    }

    private bool HasCaptureSize()
    {
        DisplaySettings settings = _game.GetDisplaySettings();
        return settings.Width == CaptureWidth && settings.Height == CaptureHeight;
    }

    // True once the condition has held for SettleDelay, so a screen is captured after it has been laid out and drawn.
    private bool IsSettled(bool condition, TimeSpan totalTime)
    {
        if (!condition)
        {
            _readySince = null;
            return false;
        }

        _readySince ??= totalTime;
        return totalTime - _readySince.Value >= SettleDelay;
    }

    private void CaptureAndAdvance(string stateName, CaptureStep nextStep, TimeSpan totalTime)
    {
        string capturePath = _game.CaptureScreenshotWithStem($"ui-{stateName}");
        if (string.IsNullOrWhiteSpace(capturePath))
        {
            Fail($"UI screen capture failed for state '{stateName}'.");
            return;
        }

        Directory.CreateDirectory(_runDirectory);
        File.Move(capturePath, Path.Combine(_runDirectory, $"ui-{stateName}.png"), overwrite: true);
        _captureCount++;
        Advance(nextStep, totalTime, $"Captured {stateName}");
    }

    private void Advance(CaptureStep nextStep, TimeSpan totalTime, string message)
    {
        Logs.WriteInfo($"UI screen capture: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
        _readySince = null;
    }

    private void Complete()
    {
        if (_captureCount != ExpectedCaptureCount)
        {
            Fail($"UI screen capture produced {_captureCount} screenshot(s), expected {ExpectedCaptureCount}.");
            return;
        }

        Logs.WriteInfo($"UI screen capture completed ({_captureCount} screenshot(s)) in {_runDirectory}");
        Environment.ExitCode = 0;
        _step = CaptureStep.Completed;
        _game.Exit();
    }

    private void Fail(string message)
    {
        Logs.WriteError(message);
        Environment.ExitCode = 1;
        _step = CaptureStep.Failed;
        _game.Exit();
    }
}
