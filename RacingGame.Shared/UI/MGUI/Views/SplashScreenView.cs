using MGUI.Core.UI;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class SplashScreenView : IMguiScreenView
{
    private readonly SplashScreen _screen;
    private readonly MGTextBlock _prompt;

    public SplashScreenView(SplashScreen screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host, true);

        _prompt = MguiUiTheme.CreateHeading(Window, "Press Start");
        _prompt.HorizontalAlignment = HorizontalAlignment.Center;
        _prompt.VerticalAlignment = VerticalAlignment.Center;
        Window.SetContent(_prompt);
    }

    public MGWindow Window { get; }
    public MGElement InitialFocusElement => null;
    public bool BlocksGameplayInput => false;

    public void Activate()
    {
        Refresh();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        Refresh();
    }

    private void Refresh()
    {
        _prompt.Text = _screen.ShouldShowPrompt ? "Press Start" : string.Empty;
    }
}