using MGUI.Core.UI;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class HighscoresView : IMguiScreenView
{
    private readonly Highscores _screen;
    private readonly MGButton[] _levelButtons;
    private readonly MGTextBlock[] _entryTexts;

    public HighscoresView(Highscores screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var panel = MguiUiTheme.CreatePanel(Window, 24);
        var root = MguiUiTheme.CreateVerticalStack(Window, 10, 0);

        root.TryAddChild(MguiUiTheme.CreateHeading(Window, "Highscores"));
        root.TryAddChild(MguiUiTheme.CreateSubheading(Window, "Switch between circuits to inspect the best lap times recorded in local settings."));

        var tabs = MguiUiTheme.CreateHorizontalStack(Window, 8);
        _levelButtons = new MGButton[3];
        for (int i = 0; i < _levelButtons.Length; i++)
        {
            int level = i;
            _levelButtons[i] = MguiUiTheme.CreateSecondaryButton(Window, _screen.GetLevelLabel(i), () => _screen.SelectLevel(level));
            tabs.TryAddChild(_levelButtons[i]);
        }
        root.TryAddChild(tabs);

        _entryTexts = new MGTextBlock[10];
        for (int i = 0; i < _entryTexts.Length; i++)
        {
            _entryTexts[i] = MguiUiTheme.CreateBodyText(Window, string.Empty);
            root.TryAddChild(_entryTexts[i]);
        }

        root.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Back", _screen.RequestBack));

        panel.SetContent(root);
        Window.SetContent(panel);
    }

    public MGWindow Window { get; }
    public MGElement InitialFocusElement => _levelButtons[_screen.SelectedLevel];
    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        Refresh();
        _levelButtons[_screen.SelectedLevel].Focus();
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
        for (int i = 0; i < _levelButtons.Length; i++)
            _levelButtons[i].BorderThickness = _screen.SelectedLevel == i ? new(3) : new(1);

        var entries = _screen.GetEntries();
        for (int i = 0; i < _entryTexts.Length && i < entries.Count; i++)
        {
            var entry = entries[i];
            _entryTexts[i].Text = $"{entry.Rank,2}.  {entry.Name,-18}  {entry.Time}";
            Color color = i == 0 ? MguiUiTheme.AccentColor : MguiUiTheme.PrimaryTextColor;
            _entryTexts[i].Foreground = new(color, color, color);
        }
    }
}