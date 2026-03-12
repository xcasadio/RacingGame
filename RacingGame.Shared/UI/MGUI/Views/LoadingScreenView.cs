using MGUI.Core.UI;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class LoadingScreenView : IMguiScreenView
{
    private readonly LoadingScreen _screen;
    private readonly MGTextBlock _title;
    private readonly MGTextBlock _status;
    private readonly MGProgressBar _progress;

    public LoadingScreenView(LoadingScreen screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host, true);

        var root = MguiUiTheme.CreateVerticalStack(Window, 12, 0);
        root.HorizontalAlignment = HorizontalAlignment.Center;
        root.VerticalAlignment = VerticalAlignment.Center;

        _title = MguiUiTheme.CreateHeading(Window, screen.LoadingTitle);
        _status = MguiUiTheme.CreateSubheading(Window, screen.LoadingStatus);
        _progress = new MGProgressBar(Window, 0, 100, 0, 18, true)
        {
            PreferredWidth = MguiUiTheme.ScaleX(360),
            NumberFormat = "0",
            ValueDisplayFormat = MGProgressBar.RecommendedPercentageValueDisplayFormat,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        root.TryAddChild(_title);
        root.TryAddChild(_status);
        root.TryAddChild(_progress);
        Window.SetContent(root);
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
        _title.Text = _screen.LoadingTitle;
        _status.Text = _screen.LoadingStatus;
        _progress.Value = _screen.LoadProgress * 100f;
    }
}