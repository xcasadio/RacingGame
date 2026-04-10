using Microsoft.Xna.Framework;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Screens;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RaceRuntimeUiCoordinator
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly Action _returnToFrontEnd;
    private PauseScreen? _pauseScreen;
    private RaceGameMode? _trackedGameMode;
    private ViewId _trackedViewId = ViewId.Empty;

    public RaceRuntimeUiCoordinator(RacingGameCasaEngineGame game, Action returnToFrontEnd)
    {
        _game = game;
        _returnToFrontEnd = returnToFrontEnd;
    }

    public void ResetForCurrentWorld()
    {
        TearDownTransientScreens();
        _trackedGameMode = null;
        _trackedViewId = ViewId.Empty;
    }

    public void Update(GameTime gameTime)
    {
        _ = gameTime;

        if (!TryResolveContext(out RuntimeRaceSession session, out RaceGameMode gameMode, out RacingPlayerController playerController))
        {
            ResetForCurrentWorld();
            return;
        }

        ViewId viewId = playerController.AssignedViewId;
        if (viewId.IsEmpty)
        {
            TearDownTransientScreens();
            return;
        }

        if (!ReferenceEquals(_trackedGameMode, gameMode) || _trackedViewId != viewId)
        {
            TearDownTransientScreens();
            _trackedGameMode = gameMode;
            _trackedViewId = viewId;
        }

        if (gameMode.IsRaceFinished)
        {
            HidePauseScreen();
            return;
        }

        if (gameMode.IsPaused)
        {
            ShowPauseScreen(session, viewId);
        }
        else
        {
            HidePauseScreen();
        }
    }

    public void ReturnToFrontEndForAutomation()
    {
        ReturnToFrontEnd();
    }

    private bool TryResolveContext(out RuntimeRaceSession session, out RaceGameMode gameMode, out RacingPlayerController playerController)
    {
        session = _game.RaceSession;
        gameMode = null!;
        playerController = null!;

        if (!session.IsActive || session.GameMode == null || session.PlayerController == null)
        {
            return false;
        }

        gameMode = session.GameMode;
        playerController = session.PlayerController;
        return true;
    }

    private void ShowPauseScreen(RuntimeRaceSession session, ViewId viewId)
    {
        _ = session;

        if (_pauseScreen != null)
        {
            return;
        }

        _pauseScreen = new PauseScreen(_game, ResumeRace, ReturnToFrontEnd);
        _game.GameManager.ScreenManager.PushScreen(_pauseScreen, viewId);
    }

    private void HidePauseScreen()
    {
        if (_pauseScreen == null || _trackedViewId.IsEmpty)
        {
            return;
        }

        _game.GameManager.ScreenManager.RemoveScreen(_pauseScreen, _trackedViewId);
        _pauseScreen = null;
    }

    private void ResumeRace()
    {
        RuntimeRaceSession session = _game.RaceSession;
        if (session.GameMode?.IsPaused != true)
        {
            return;
        }

        session.GameMode.TogglePause();

        bool canDrive = !session.IsDebugCameraEnabled
            && session.GameMode.CountdownSecondsRemaining <= 0f
            && !session.GameMode.IsRaceFinished;
        if (session.PlayerController != null)
        {
            session.PlayerController.IsInputEnable = canDrive;
        }

        if (session.PlayerPawn != null)
        {
            session.PlayerPawn.InputEnabled = canDrive;
        }

        HidePauseScreen();
    }

    private void ReturnToFrontEnd()
    {
        RuntimeRaceSession session = _game.RaceSession;
        if (session.GameMode?.IsPaused == true)
        {
            session.GameMode.TogglePause();
        }

        TearDownTransientScreens();
        _returnToFrontEnd();
    }

    private void TearDownTransientScreens()
    {
        HidePauseScreen();
    }
}