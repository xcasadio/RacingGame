using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;

namespace RacingGameCasaEngine.Screens;

/// <summary>Highscores, loaded from the <c>Screen.Highscores</c> screen asset (Content/UI/Screens/Highscores).</summary>
internal sealed class HighscoresScreen : RaceXamlScreenBase
{
    // Level tabs of Highscores.xaml (tab*), which are also the keys of RaceFrontEndCatalog.Highscores.
    private static readonly string[] LevelNames = ["Beginner", "Advanced", "Expert"];

    private readonly Action _back;
    private readonly RaceHighscoresViewModel _viewModel = new();
    private string _selectedLevel = "Beginner";
    private MGButton[] _levelButtons = [];
    private MGButton? _backButton;

    public HighscoresScreen(AssetContentManager assetContentManager, Action back)
        : base(assetContentManager, "Screen.Highscores")
    {
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        _viewModel.BackButton.Update(Root.Metrics.Scale);
        RefreshBoard();
        window.WindowDataContext = _viewModel;
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);

        _levelButtons = new MGButton[LevelNames.Length];
        for (int i = 0; i < LevelNames.Length; i++)
        {
            string levelName = LevelNames[i];
            _levelButtons[i] = FindControl<MGButton>("tab" + levelName);
            _levelButtons[i].AddCommandHandler((_, _) => SelectLevel(levelName));
            LegacyMenuUiTheme.ApplyBandButtonState(_levelButtons[i], false);
        }

        _backButton = FindControl<MGButton>("btnBack");
        _backButton.AddCommandHandler((_, _) => _back());
    }

    public override void Show()
    {
        int selectedIndex = Array.IndexOf(LevelNames, _selectedLevel);
        if (selectedIndex >= 0 && selectedIndex < _levelButtons.Length)
        {
            _levelButtons[selectedIndex].Focus();
        }
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);

        for (int i = 0; i < _levelButtons.Length; i++)
        {
            bool isActive = string.Equals(LevelNames[i], _selectedLevel, StringComparison.OrdinalIgnoreCase)
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
        for (int i = 0; i < RaceHighscoresViewModel.EntryCount; i++)
        {
            _viewModel.SetEntry(i, i < entries.Count
                ? $"{i + 1,2}.  {entries[i].PlayerName,-18}  {entries[i].Time}"
                : string.Empty);
        }
    }
}
