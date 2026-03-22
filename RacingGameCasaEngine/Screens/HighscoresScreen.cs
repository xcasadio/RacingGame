using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGameCasaEngine.Screens;

internal sealed class HighscoresScreen : RaceFrontEndScreenBase
{
    private readonly Action _back;
    private string _selectedLevel = "Beginner";
    private readonly List<MGTextBlock> _entryLabels = [];
    private readonly List<MGButton> _levelButtons = [];
    private MGButton? _backButton;

    public HighscoresScreen(Texture2D? backgroundTexture, Texture2D? buttonsTexture, Action back)
        : base(backgroundTexture, buttonsTexture)
    {
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        var window = CreateForegroundWindow(root);
        var band = LegacyMenuUiTheme.CreateMenuBand(window, 165, 390, new MonoGame.Extended.Thickness(32, 22, 32, 20));
        var layout = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 12);
        layout.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        layout.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;
        layout.TryAddChild(LegacyMenuUiTheme.CreateHeading(window, "Highscores"));

        var scrollViewer = new MGScrollViewer(window)
        {
            PreferredWidth = 1040,
            PreferredHeight = 250,
            AllowClickDragScrolling = false,
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        };

        var content = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 10);
        content.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        content.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Top;

        var tabs = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 10);
        foreach (var levelName in new[] { "Beginner", "Advanced", "Expert" })
        {
            string capturedName = levelName;
            var button = LegacyMenuUiTheme.CreateBandButton(window, capturedName, () => SelectLevel(capturedName));
            _levelButtons.Add(button);
            tabs.TryAddChild(button);
        }
        content.TryAddChild(tabs);

        for (int i = 0; i < 10; i++)
        {
            var label = LegacyMenuUiTheme.CreateBodyText(window, string.Empty);
            label.PreferredWidth = 520;
            _entryLabels.Add(label);
            content.TryAddChild(label);
        }
        scrollViewer.SetContent(content);
        layout.TryAddChild(scrollViewer);

        _backButton = LegacyMenuUiTheme.CreateMenuTextButton(window, "Back", _back);
        layout.TryAddChild(_backButton);

        band.SetContent(layout);
        window.SetContent(band);
        RefreshBoard();
    }

    public override void Show()
    {
        int selectedIndex = Array.IndexOf(GetLevelNames(), _selectedLevel);
        if (selectedIndex >= 0 && selectedIndex < _levelButtons.Count)
        {
            _levelButtons[selectedIndex].Focus();
        }
    }

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        for (int i = 0; i < _levelButtons.Count; i++)
        {
            bool isActive = string.Equals(GetLevelNames()[i], _selectedLevel, StringComparison.OrdinalIgnoreCase)
                || _levelButtons[i].VisualState.IsFocused
                || _levelButtons[i].IsHovered;
            LegacyMenuUiTheme.ApplyBandButtonState(_levelButtons[i], isActive);
        }

        if (_backButton != null)
        {
            LegacyMenuUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
        }
    }

    private void SelectLevel(string levelName)
    {
        _selectedLevel = levelName;
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        var entries = RaceFrontEndCatalog.Highscores[_selectedLevel];
        for (int i = 0; i < _entryLabels.Count; i++)
        {
            _entryLabels[i].Text = i < entries.Count
                ? $"{i + 1,2}.  {entries[i].PlayerName,-18}  {entries[i].Time}"
                : string.Empty;
        }
    }

    private static string[] GetLevelNames() => ["Beginner", "Advanced", "Expert"];
}