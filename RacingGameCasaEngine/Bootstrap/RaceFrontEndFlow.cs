using CasaEngine.Framework.GUI;
using RacingGameCasaEngine.Screens;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RaceFrontEndFlow
{
    internal const string SplashStateName = "Splash";
    internal const string MainMenuStateName = "MainMenu";
    internal const string CarSelectionStateName = "CarSelection";
    internal const string TrackSelectionStateName = "TrackSelection";
    internal const string OptionsStateName = "Options";
    internal const string HelpStateName = "Help";
    internal const string HighscoresStateName = "Highscores";
    internal const string RaceHudStateName = "RaceHud";

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state = new();
    private bool _factoriesRegistered;
    private string? _pendingStateAfterWorldLoad;

    public RaceFrontEndFlow(RacingGameCasaEngineGame game)
    {
        _game = game;
        _game.SyncOptionsState(_state);
    }

    internal RaceFrontEndState State => _state;

    public void InitializeForCurrentWorld()
    {
        if (!_factoriesRegistered)
        {
            RegisterFactories();
            _factoriesRegistered = true;
        }

        if (!string.IsNullOrWhiteSpace(_pendingStateAfterWorldLoad))
        {
            _game.GameManager.ScreenManager.TransitionTo(_pendingStateAfterWorldLoad);
            _pendingStateAfterWorldLoad = null;
            return;
        }

        _game.GameManager.ScreenManager.TransitionTo(SplashStateName);
    }

    private void RegisterFactories()
    {
        GameScreenManager screenManager = _game.GameManager.ScreenManager;
        screenManager.RegisterFactory(SplashStateName, () => new SplashScreen(_game.MenuBackgroundTexture, OpenMainMenu));
        screenManager.RegisterFactory(MainMenuStateName, () => new MainMenuScreen(_game.MenuBackgroundTexture, _game.MenuButtonsTexture, OpenCarSelection, OpenHighscores, OpenOptions, OpenHelp, RequestExit));
        screenManager.RegisterFactory(CarSelectionStateName, () => new CarSelectionScreen(_game.MenuBackgroundTexture, _game.MenuButtonsTexture, _state, OpenTrackSelection, OpenMainMenu));
        screenManager.RegisterFactory(TrackSelectionStateName, () => new TrackSelectionScreen(_game.MenuBackgroundTexture, _game.MenuButtonsTexture, _state, StartRace, OpenCarSelection));
        screenManager.RegisterFactory(OptionsStateName, () => new OptionsScreen(_game, _game.MenuBackgroundTexture, _game.MenuButtonsTexture, _state, OpenMainMenu));
        screenManager.RegisterFactory(HelpStateName, () => new HelpScreen(_game.MenuBackgroundTexture, _game.MenuButtonsTexture, OpenMainMenu));
        screenManager.RegisterFactory(HighscoresStateName, () => new HighscoresScreen(_game.MenuBackgroundTexture, _game.MenuButtonsTexture, OpenMainMenu));
        screenManager.RegisterFactory(RaceHudStateName, () => new RaceHudScreen(_game, _state, ReturnToFrontEnd));
    }

    private void OpenMainMenu()
    {
        _game.GameManager.ScreenManager.TransitionTo(MainMenuStateName);
    }

    private void OpenCarSelection()
    {
        _game.GameManager.ScreenManager.TransitionTo(CarSelectionStateName);
    }

    private void OpenTrackSelection()
    {
        _game.GameManager.ScreenManager.TransitionTo(TrackSelectionStateName);
    }

    private void OpenOptions()
    {
        _game.GameManager.ScreenManager.TransitionTo(OptionsStateName);
    }

    private void OpenHelp()
    {
        _game.GameManager.ScreenManager.TransitionTo(HelpStateName);
    }

    private void OpenHighscores()
    {
        _game.GameManager.ScreenManager.TransitionTo(HighscoresStateName);
    }

    private void StartRace()
    {
        _pendingStateAfterWorldLoad = RaceHudStateName;
        _game.GameManager.SetWorldToLoad(RaceWorldFactory.CreateRaceWorld(_state));
    }

    private void ReturnToFrontEnd()
    {
        _pendingStateAfterWorldLoad = MainMenuStateName;
        _game.GameManager.SetWorldToLoad(RaceWorldFactory.CreateFrontEndWorld());
    }

    private void RequestExit()
    {
        _game.Exit();
    }

    internal void OpenMainMenuForAutomation() => OpenMainMenu();

    internal void OpenCarSelectionForAutomation() => OpenCarSelection();

    internal void OpenTrackSelectionForAutomation() => OpenTrackSelection();

    internal void OpenOptionsForAutomation() => OpenOptions();

    internal void ApplyOptionsAndReturnToMainMenuForAutomation(Action<RaceFrontEndState> configureState)
    {
        configureState(_state);
        _game.ApplyFrontEndOptions(_state);
        OpenMainMenu();
    }

    internal void OpenHelpForAutomation() => OpenHelp();

    internal void OpenHighscoresForAutomation() => OpenHighscores();

    internal void StartRaceForAutomation() => StartRace();

    internal void ReturnToFrontEndForAutomation() => ReturnToFrontEnd();
}