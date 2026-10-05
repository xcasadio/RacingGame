using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;

namespace RacingGameCasaEngine.Screens;

/// <summary>Main menu, loaded from the <c>Screen.MainMenu</c> screen asset (Content/UI/Screens/MainMenu).</summary>
internal sealed class MainMenuScreen : RaceXamlScreenBase
{
    // Element name suffixes in MainMenu.xaml (btn*, face*, lbl*), in the order of the actions.
    private static readonly string[] EntryNames = ["Play", "Highscores", "Options", "Help", "Quit"];

    private readonly Action[] _actions;
    private readonly RaceMainMenuViewModel _viewModel = new();

    private MGButton[] _buttons = [];
    private MGBorder[] _buttonFaces = [];
    private MGTextBlock[] _labels = [];

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

        _buttons = new MGButton[EntryNames.Length];
        _buttonFaces = new MGBorder[EntryNames.Length];
        _labels = new MGTextBlock[EntryNames.Length];

        for (int i = 0; i < EntryNames.Length; i++)
        {
            _buttons[i] = FindControl<MGButton>("btn" + EntryNames[i]);
            _buttonFaces[i] = FindControl<MGBorder>("face" + EntryNames[i]);
            _labels[i] = FindControl<MGTextBlock>("lbl" + EntryNames[i]);

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

        for (int i = 0; i < _buttons.Length; i++)
        {
            bool isActive = _buttons[i].VisualState.IsFocused || _buttons[i].IsHovered;
            LegacyMenuUiTheme.ApplyMainMenuButtonState(_buttons[i], _buttonFaces[i], _labels[i], isActive);
        }
    }
}
