using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using RacingGame.GameScreens;
using RacingGame.Graphics;

namespace RacingGame.UI.MGUI.Views;

internal sealed class SplashScreenView : IMguiScreenView
{
    private readonly SplashScreen _screen;
    private readonly MGTextBlock _prompt;

    public SplashScreenView(SplashScreen screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host, true);

        var bandAnchor = new MGBorder(Window, new(0), new MGUniformBorderBrush(Color.Transparent))
        {
            BackgroundBrush = MguiUiTheme.TransparentBackground,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new(0, MguiUiTheme.ScaleY(583), 0, 0),
            PreferredHeight = MguiUiTheme.ScaleY(69),
        };

        _prompt = MguiUiTheme.CreateHeading(Window, "Press Start");
        _prompt.HorizontalAlignment = HorizontalAlignment.Center;
        _prompt.VerticalAlignment = VerticalAlignment.Center;
        bandAnchor.SetContent(_prompt);
        Window.SetContent(bandAnchor);
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