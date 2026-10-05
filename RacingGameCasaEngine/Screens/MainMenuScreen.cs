using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.UI.ViewModels;

namespace RacingGameCasaEngine.Screens;

/// <summary>Main menu, loaded from the <c>Screen.MainMenu</c> screen asset (Content/UI/Screens/MainMenu).</summary>
internal sealed class MainMenuScreen : RaceXamlScreenBase
{
    // Button name suffixes in MainMenu.xaml (btn*), in the order of the actions and of RaceMainMenuViewModel.Buttons.
    private static readonly string[] EntryNames = ["Play", "Highscores", "Options", "Help", "Quit"];

    private readonly Action[] _actions;
    private readonly RaceMainMenuViewModel _viewModel = new();

    private MGButton[] _buttons = [];

    public MainMenuScreen(AssetContentManager assetContentManager, Action openCarSelection, Action openHighscores, Action openOptions, Action openHelp, Action requestExit)
        : base(assetContentManager, "Screen.MainMenu")
    {
        _actions = [openCarSelection, openHighscores, openOptions, openHelp, requestExit];
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        window.WindowDataContext = _viewModel;
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);
        UpdateMenuLayout(0.0);

        _buttons = new MGButton[EntryNames.Length];
        for (int i = 0; i < EntryNames.Length; i++)
        {
            _buttons[i] = FindControl<MGButton>("btn" + EntryNames[i]);

            Action action = _actions[i];
            _buttons[i].AddCommandHandler((_, _) => action());
        }
    }

    public override void Show()
    {
        if (_buttons.Length > 0)
        {
            _buttons[0].Focus();
        }
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);
        UpdateMenuLayout(gameTime.ElapsedGameTime.TotalSeconds);
    }

    // Lays the band and the buttons out for the viewport, as RacingGame's main menu did every frame.
    private void UpdateMenuLayout(double elapsedSeconds)
    {
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, elapsedSeconds);
    }
}
