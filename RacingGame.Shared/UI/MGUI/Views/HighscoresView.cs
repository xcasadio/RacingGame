using MGUI.Core.UI;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class HighscoresView : IMguiScreenView
{
    private readonly Highscores _screen;
    private readonly MGButton[] _levelButtons;
    private readonly MGTextBlock[] _entryTexts;
    private readonly MGButton _backButton;

    public HighscoresView(Highscores screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var band = MguiUiTheme.CreateMenuBand(Window, 165, 390, MguiUiTheme.ScaleThickness(32, 22, 32, 20));
        band.UseResponsiveLayout = true;
        var root = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(12), 0);
        root.HorizontalAlignment = HorizontalAlignment.Center;
        root.VerticalAlignment = VerticalAlignment.Center;

        root.TryAddChild(MguiUiTheme.CreateHeading(Window, "Highscores"));

        var scrollViewer = new MGScrollViewer(Window)
        {
            PreferredWidth = MguiUiTheme.ScaleX(1040),
            PreferredHeight = MguiUiTheme.ScaleY(250),
            AllowClickDragScrolling = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var content = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(10), 0);
        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Top;

        var tabs = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(10));
        _levelButtons = new MGButton[3];
        for (int i = 0; i < _levelButtons.Length; i++)
        {
            int level = i;
            _levelButtons[i] = MguiUiTheme.CreateBandButton(Window, _screen.GetLevelLabel(i), () => _screen.SelectLevel(level));
            tabs.TryAddChild(_levelButtons[i]);
        }
        content.TryAddChild(tabs);

        _entryTexts = new MGTextBlock[10];
        for (int i = 0; i < _entryTexts.Length; i++)
        {
            _entryTexts[i] = MguiUiTheme.CreateBodyText(Window, string.Empty);
            _entryTexts[i].PreferredWidth = MguiUiTheme.ScaleX(520);
            content.TryAddChild(_entryTexts[i]);
        }

        scrollViewer.SetContent(content);
        root.TryAddChild(scrollViewer);

        _backButton = MguiUiTheme.CreateMenuTextButton(Window, "Back", _screen.RequestBack, 150);
        root.TryAddChild(_backButton);

        band.SetContent(root);
        Window.SetContent(band);
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
        int focusedLevel = Array.FindIndex(_levelButtons, button => button.VisualState.IsFocused);
        if (focusedLevel >= 0 && focusedLevel != _screen.SelectedLevel)
        {
            _screen.SelectLevel(focusedLevel);
        }

        for (int i = 0; i < _levelButtons.Length; i++)
        {
            bool isActive = _screen.SelectedLevel == i || _levelButtons[i].VisualState.IsFocused || _levelButtons[i].IsHovered;
            MguiUiTheme.ApplyBandButtonState(_levelButtons[i], isActive);
        }

        var entries = _screen.GetEntries();
        for (int i = 0; i < _entryTexts.Length && i < entries.Count; i++)
        {
            var entry = entries[i];
            _entryTexts[i].Text = $"{entry.Rank,2}.  {entry.Name,-18}  {entry.Time}";
            Color color = i == 0 ? MguiUiTheme.AccentColor : MguiUiTheme.PrimaryTextColor;
            _entryTexts[i].Foreground = new(color, color, color);
        }

        MguiUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
    }
}