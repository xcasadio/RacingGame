using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class GameHudView : IMguiScreenView
{
    private readonly GameScreen _screen;
    private readonly MGTextBlock _lap;
    private readonly MGTextBlock _currentTime;
    private readonly MGTextBlock _bestTime;
    private readonly MGTextBlock _trackName;
    private readonly MGTextBlock[] _topTimes;
    private readonly MGTextBlock _speed;
    private readonly MGTextBlock _gear;
    private readonly MGBorder _gameOverPanel;
    private readonly MGTextBlock _gameOverTitle;
    private readonly MGTextBlock[] _gameOverLines;
    private readonly MGTextBlock _exitHint;

    public GameHudView(GameScreen screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host, true);

        var root = new MGDockPanel(Window, false);

        var leftPanel = MguiUiTheme.CreatePanel(Window, 16);
        leftPanel.HorizontalAlignment = HorizontalAlignment.Left;
        leftPanel.VerticalAlignment = VerticalAlignment.Top;
        leftPanel.Margin = new(16);
        leftPanel.PreferredWidth = 260;

        var leftStack = MguiUiTheme.CreateVerticalStack(Window, 8, 0);
        _lap = MguiUiTheme.CreateHeading(Window, string.Empty);
        _lap.HorizontalAlignment = HorizontalAlignment.Left;
        _lap.TextAlignment = HorizontalAlignment.Left;
        _currentTime = MguiUiTheme.CreateBodyText(Window, string.Empty, MguiUiTheme.AccentColor);
        _bestTime = MguiUiTheme.CreateBodyText(Window, string.Empty);
        leftStack.TryAddChild(_lap);
        leftStack.TryAddChild(_currentTime);
        leftStack.TryAddChild(_bestTime);
        leftPanel.SetContent(leftStack);
        root.TryAddChild(leftPanel, Dock.Left);

        var rightPanel = MguiUiTheme.CreatePanel(Window, 16);
        rightPanel.HorizontalAlignment = HorizontalAlignment.Right;
        rightPanel.VerticalAlignment = VerticalAlignment.Top;
        rightPanel.Margin = new(16);
        rightPanel.PreferredWidth = 300;

        var rightStack = MguiUiTheme.CreateVerticalStack(Window, 8, 0);
        _trackName = MguiUiTheme.CreateHeading(Window, string.Empty);
        _trackName.HorizontalAlignment = HorizontalAlignment.Left;
        _trackName.TextAlignment = HorizontalAlignment.Left;
        rightStack.TryAddChild(_trackName);
        rightStack.TryAddChild(MguiUiTheme.CreateBodyText(Window, "Top 5", MguiUiTheme.AccentColor));
        _topTimes = new MGTextBlock[5];
        for (int i = 0; i < _topTimes.Length; i++)
        {
            _topTimes[i] = MguiUiTheme.CreateBodyText(Window, string.Empty);
            rightStack.TryAddChild(_topTimes[i]);
        }
        rightPanel.SetContent(rightStack);
        root.TryAddChild(rightPanel, Dock.Right);

        var bottomPanel = MguiUiTheme.CreatePanel(Window, 16);
        bottomPanel.HorizontalAlignment = HorizontalAlignment.Right;
        bottomPanel.VerticalAlignment = VerticalAlignment.Bottom;
        bottomPanel.Margin = new(16);
        bottomPanel.PreferredWidth = 220;

        var bottomStack = MguiUiTheme.CreateVerticalStack(Window, 4, 0);
        _speed = MguiUiTheme.CreateHeading(Window, string.Empty);
        _gear = MguiUiTheme.CreateBodyText(Window, string.Empty, MguiUiTheme.SecondaryTextColor);
        bottomStack.TryAddChild(_speed);
        bottomStack.TryAddChild(_gear);
        bottomPanel.SetContent(bottomStack);
        root.TryAddChild(bottomPanel, Dock.Bottom);

        _gameOverPanel = MguiUiTheme.CreatePanel(Window, 24);
        _gameOverPanel.HorizontalAlignment = HorizontalAlignment.Center;
        _gameOverPanel.VerticalAlignment = VerticalAlignment.Center;
        _gameOverPanel.PreferredWidth = 420;

        var gameOverStack = MguiUiTheme.CreateVerticalStack(Window, 8, 0);
        _gameOverTitle = MguiUiTheme.CreateHeading(Window, string.Empty);
        gameOverStack.TryAddChild(_gameOverTitle);
        _gameOverLines = new MGTextBlock[4];
        for (int i = 0; i < _gameOverLines.Length; i++)
        {
            _gameOverLines[i] = MguiUiTheme.CreateBodyText(Window, string.Empty);
            gameOverStack.TryAddChild(_gameOverLines[i]);
        }
        _exitHint = MguiUiTheme.CreateSubheading(Window, string.Empty);
        gameOverStack.TryAddChild(_exitHint);
        _gameOverPanel.SetContent(gameOverStack);
        root.TryAddChild(_gameOverPanel, Dock.Left);

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
        _lap.Text = $"Lap {_screen.CurrentLapDisplay}";
        _currentTime.Text = $"Current: {FormatMilliseconds(_screen.CurrentGameTime)}";
        _bestTime.Text = $"Best: {FormatMilliseconds(_screen.BestLapTime)}";
        _trackName.Text = _screen.TrackName;

        var topTimes = _screen.TopLapTimes;
        for (int i = 0; i < _topTimes.Length; i++)
        {
            bool isBest = i < topTimes.Count && _screen.BestLapTime == topTimes[i] && _screen.BestLapTime > 0;
            _topTimes[i].Text = i < topTimes.Count ? $"{i + 1}. {FormatMilliseconds(topTimes[i])}" : string.Empty;
            Color color = isBest ? MguiUiTheme.AccentColor : MguiUiTheme.PrimaryTextColor;
            _topTimes[i].Foreground = new(color, color, color);
        }

        _speed.Text = $"{_screen.SpeedDisplay} mph";
        _gear.Text = $"Gear {_screen.GearDisplay}";

        _gameOverPanel.Visibility = _screen.IsGameOver ? Visibility.Visible : Visibility.Collapsed;
        _gameOverTitle.Text = _screen.GameOverTitle;
        var lines = _screen.GetGameOverLines();
        for (int i = 0; i < _gameOverLines.Length; i++)
            _gameOverLines[i].Text = i < lines.Count ? lines[i] : string.Empty;
        _exitHint.Text = _screen.ExitHint;
    }

    private static string FormatMilliseconds(int timeMilliseconds)
    {
        return
            (timeMilliseconds < 0 ? "-" : "") +
            ((Math.Abs(timeMilliseconds) / 1000) / 60) + ":" +
            ((Math.Abs(timeMilliseconds) / 1000) % 60).ToString("00") + "." +
            ((Math.Abs(timeMilliseconds) / 10) % 100).ToString("00");
    }
}