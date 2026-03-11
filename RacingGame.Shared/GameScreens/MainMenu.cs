using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Sounds;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;

namespace RacingGame.GameScreens;

class MainMenu : IGameScreen, IMguiScreen
{
    private const float TimeOutMenu = 60000.0f;

    private float idleTime = 0.0f;
    private bool musicHasStarted = false;
    private bool _isFinished = false;
    private IMguiScreenView _mguiView;

    internal static Rectangle InterpolateRect(Rectangle rect1, Rectangle rect2, float interpolation)
    {
        return new Rectangle(
            (int)Math.Round(rect1.X * interpolation + rect2.X * (1 - interpolation)),
            (int)Math.Round(rect1.Y * interpolation + rect2.Y * (1 - interpolation)),
            (int)Math.Round(rect1.Width * interpolation + rect2.Width * (1 - interpolation)),
            (int)Math.Round(rect1.Height * interpolation + rect2.Height * (1 - interpolation)));
    }

    public void Update(GameTime gameTime)
    {
        if (!musicHasStarted)
        {
            Sound.Play(Sound.Sounds.MenuMusic);
            musicHasStarted = true;
        }

        if (HasMenuInputActivity())
        {
            idleTime = 0.0f;
        }

        if (Input.KeyboardEscapeJustPressed || Input.GamePadBackJustPressed)
        {
            _isFinished = true;
        }

        idleTime += BaseGame.ElapsedTimeThisFrameInMilliseconds;
        if (idleTime > TimeOutMenu)
        {
            idleTime = 0.0f;
            RacingGameManager.AddGameScreen(new SplashScreen());
        }
    }

    public bool Render()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();
        return _isFinished;
    }

    public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
    {
        _mguiView ??= new MainMenuView(this, host);
        return _mguiView;
    }

    internal void StartGame()
    {
        idleTime = 0.0f;
        RacingGameManager.AddGameScreen(new CarSelection());
    }

    internal void OpenHighscores()
    {
        idleTime = 0.0f;
        RacingGameManager.AddGameScreen(new Highscores());
    }

    internal void OpenOptions()
    {
        idleTime = 0.0f;
        RacingGameManager.AddGameScreen(new Options());
    }

    internal void OpenHelp()
    {
        idleTime = 0.0f;
        RacingGameManager.AddGameScreen(new Help());
    }

    internal void RequestExit()
    {
        idleTime = 0.0f;
        _isFinished = true;
    }

    private static bool HasMenuInputActivity()
    {
        if (BaseGame.MguiUi == null)
            return false;

        var input = BaseGame.MguiUi.Renderer.Input;
        return input.Mouse.MouseMovedRecently ||
               input.Mouse.MouseLeftButtonPressedRecently ||
               input.Keyboard.CurrentKeyPressedEvents.Values.Any(x => x != null) ||
               input.GamePad.HasActivity();
    }
}