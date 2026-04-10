using CasaEngine.Core.Log;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class FrontEndNavigationSmokeValidator
{
    private enum ValidationStep
    {
        WaitForSplash,
        VisitHighscores,
        ReturnFromHighscores,
        VisitOptions,
        ApplyOptions,
        VerifyOptionsApplied,
        VisitHelp,
        ReturnFromHelp,
        OpenCarSelection,
        OpenTrackSelection,
        StartRace,
        PauseRace,
        ResumeRace,
        CompleteRace,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private ValidationStep _step = ValidationStep.WaitForSplash;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private bool _started;
    private int _expectedWidth;
    private int _expectedHeight;

    public FrontEndNavigationSmokeValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow)
    {
        _game = game;
        _flow = flow;
        _game.PreviewUpdate += OnPreviewUpdate;
    }

    private void OnPreviewUpdate(object? sender, TimeSpan totalTime)
    {
        if (!_started)
        {
            _started = true;
            _startedAt = totalTime;
            _lastTransitionAt = totalTime;
            Logs.WriteInfo("Smoke validation started for front-end navigation");
        }

        if (_step == ValidationStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > TimeSpan.FromSeconds(20))
        {
            Fail("Smoke validation timed out before completing the front-end sequence.");
            return;
        }

        if (totalTime - _lastTransitionAt < TimeSpan.FromMilliseconds(150))
        {
            return;
        }

        string? currentState = _game.GameManager.ScreenManager.CurrentState;
        string? currentWorldName = _game.GameManager.CurrentWorld?.Name;

        switch (_step)
        {
            case ValidationStep.WaitForSplash when currentState == RaceFrontEndFlow.SplashStateName:
                _flow.OpenMainMenuForAutomation();
                Advance(ValidationStep.VisitHighscores, totalTime, "Splash -> MainMenu");
                break;

            case ValidationStep.VisitHighscores when currentState == RaceFrontEndFlow.MainMenuStateName:
                _flow.OpenHighscoresForAutomation();
                Advance(ValidationStep.ReturnFromHighscores, totalTime, "MainMenu -> Highscores");
                break;

            case ValidationStep.ReturnFromHighscores when currentState == RaceFrontEndFlow.HighscoresStateName:
                _flow.OpenMainMenuForAutomation();
                Advance(ValidationStep.VisitOptions, totalTime, "Highscores -> MainMenu");
                break;

            case ValidationStep.VisitOptions when currentState == RaceFrontEndFlow.MainMenuStateName:
                _flow.OpenOptionsForAutomation();
                Advance(ValidationStep.ApplyOptions, totalTime, "MainMenu -> Options");
                break;

            case ValidationStep.ApplyOptions when currentState == RaceFrontEndFlow.OptionsStateName:
                int nextResolutionIndex = _flow.State.SelectedResolutionIndex == 0 ? 1 : 0;
                (_expectedWidth, _expectedHeight) = nextResolutionIndex == 0 ? (1280, 720) : (1920, 1080);
                _flow.ApplyOptionsAndReturnToMainMenuForAutomation(state =>
                {
                    state.SelectedResolutionIndex = nextResolutionIndex;
                });
                Advance(ValidationStep.VerifyOptionsApplied, totalTime, "Options -> MainMenu (apply resolution)");
                break;

            case ValidationStep.VerifyOptionsApplied when currentState == RaceFrontEndFlow.MainMenuStateName:
                var displaySettings = _game.GetDisplaySettings();
                if (displaySettings.Width != _expectedWidth || displaySettings.Height != _expectedHeight)
                {
                    Fail($"Smoke validation expected resolution {_expectedWidth}x{_expectedHeight} but found {displaySettings.Width}x{displaySettings.Height} after returning from options.");
                    return;
                }

                Advance(ValidationStep.VisitHelp, totalTime, "Verified applied resolution on MainMenu");
                break;

            case ValidationStep.VisitHelp when currentState == RaceFrontEndFlow.MainMenuStateName:
                _flow.OpenHelpForAutomation();
                Advance(ValidationStep.ReturnFromHelp, totalTime, "MainMenu -> Help");
                break;

            case ValidationStep.ReturnFromHelp when currentState == RaceFrontEndFlow.HelpStateName:
                _flow.OpenMainMenuForAutomation();
                Advance(ValidationStep.OpenCarSelection, totalTime, "Help -> MainMenu");
                break;

            case ValidationStep.OpenCarSelection when currentState == RaceFrontEndFlow.MainMenuStateName:
                _flow.OpenCarSelectionForAutomation();
                Advance(ValidationStep.OpenTrackSelection, totalTime, "MainMenu -> CarSelection");
                break;

            case ValidationStep.OpenTrackSelection when currentState == RaceFrontEndFlow.CarSelectionStateName:
                _flow.OpenTrackSelectionForAutomation();
                Advance(ValidationStep.StartRace, totalTime, "CarSelection -> TrackSelection");
                break;

            case ValidationStep.StartRace when currentState == RaceFrontEndFlow.TrackSelectionStateName:
                _flow.StartRaceForAutomation();
                Advance(ValidationStep.PauseRace, totalTime, "TrackSelection -> RaceHud");
                break;

            case ValidationStep.PauseRace when currentState == RaceFrontEndFlow.RaceHudStateName
                && currentWorldName != null
                && RaceWorldFactory.IsRaceWorld(_game.GameManager.CurrentWorld!)
                && _game.RaceSession.GameMode is { CountdownSecondsRemaining: <= 0f, IsPaused: false } gameModeToPause:
                gameModeToPause.TogglePause();
                Advance(ValidationStep.ResumeRace, totalTime, "RaceHud -> Paused");
                break;

            case ValidationStep.ResumeRace when currentState == RaceFrontEndFlow.RaceHudStateName
                && _game.RaceSession.GameMode is { IsPaused: true } gameModeToResume:
                gameModeToResume.TogglePause();
                Advance(ValidationStep.CompleteRace, totalTime, "Paused -> RaceHud");
                break;

            case ValidationStep.CompleteRace when currentState == RaceFrontEndFlow.RaceHudStateName
                && _game.RaceSession.GameMode is { IsPaused: false, IsRaceFinished: false } gameModeToComplete:
                gameModeToComplete.CompleteRaceForAutomation();
                Advance(ValidationStep.ReturnToFrontEnd, totalTime, "RaceHud -> GameOver");
                break;

            case ValidationStep.ReturnToFrontEnd when currentState == RaceFrontEndFlow.RaceHudStateName
                && currentWorldName != null
                && RaceWorldFactory.IsRaceWorld(_game.GameManager.CurrentWorld!)
                && _game.RaceSession.GameMode?.IsRaceFinished == true:
                _flow.ReturnToFrontEndForAutomation();
                Advance(ValidationStep.Completed, totalTime, "RaceHud -> MainMenu");
                break;

            case ValidationStep.Completed when currentState == RaceFrontEndFlow.MainMenuStateName
                && currentWorldName == RaceWorldFactory.FrontEndWorldName:
                Complete();
                break;
        }

        if (_step == ValidationStep.Completed
            && currentState == RaceFrontEndFlow.MainMenuStateName
            && currentWorldName == RaceWorldFactory.FrontEndWorldName)
        {
            Complete();
        }
    }

    private void Advance(ValidationStep nextStep, TimeSpan totalTime, string message)
    {
        Logs.WriteInfo($"Smoke validation: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
    }

    private void Complete()
    {
        Logs.WriteInfo("Smoke validation completed successfully");
        Environment.ExitCode = 0;
        _step = ValidationStep.Completed;
        _game.Exit();
    }

    private void Fail(string message)
    {
        Logs.WriteError(message);
        Environment.ExitCode = 1;
        _step = ValidationStep.Failed;
        _game.Exit();
    }
}