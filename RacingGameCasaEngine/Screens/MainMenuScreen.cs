using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.UI;
using HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace RacingGameCasaEngine.Screens;

internal sealed class MainMenuScreen : RaceFrontEndScreenBase
{
    private readonly Action _openCarSelection;
    private readonly Action _openHighscores;
    private readonly Action _openOptions;
    private readonly Action _openHelp;
    private readonly Action _requestExit;

    private MGButton? _playButton;
    private MGButton[] _buttons = [];
    private MGBorder[] _buttonFaces = [];
    private MGTextBlock[] _labels = [];

    public MainMenuScreen(Texture2D? backgroundTexture, Texture2D? buttonsTexture, Action openCarSelection, Action openHighscores, Action openOptions, Action openHelp, Action requestExit)
        : base(backgroundTexture, buttonsTexture)
    {
        _openCarSelection = openCarSelection;
        _openHighscores = openHighscores;
        _openOptions = openOptions;
        _openHelp = openHelp;
        _requestExit = requestExit;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        if (ButtonsTexture == null)
        {
            throw new InvalidOperationException("Legacy menu atlas is required for the main menu.");
        }

        var window = CreateForegroundWindow(root);
        var band = LegacyMenuUiTheme.CreateMenuBand(window, 315, 216, new MonoGame.Extended.Thickness(28, 20, 28, 18));
        var content = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 10);
        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

        var buttonDefinitions = new[]
        {
            new MenuButtonDefinition("PLAY", LegacyMenuUiAtlas.MenuButtonPlay, _openCarSelection),
            new MenuButtonDefinition("HIGHSCORES", LegacyMenuUiAtlas.MenuButtonHighscores, _openHighscores),
            new MenuButtonDefinition("OPTIONS", LegacyMenuUiAtlas.MenuButtonOptions, _openOptions),
            new MenuButtonDefinition("HELP", LegacyMenuUiAtlas.MenuButtonHelp, _openHelp),
            new MenuButtonDefinition("QUIT", LegacyMenuUiAtlas.MenuButtonQuit, _requestExit),
        };

        var buttonsRow = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 10);
        _buttons = new MGButton[buttonDefinitions.Length];
        _buttonFaces = new MGBorder[buttonDefinitions.Length];
        _labels = new MGTextBlock[buttonDefinitions.Length];

        for (int i = 0; i < buttonDefinitions.Length; i++)
        {
            var item = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 8);
            item.HorizontalAlignment = HorizontalAlignment.Center;
            item.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

            _buttons[i] = LegacyMenuUiTheme.CreateMainMenuIconButton(window, ButtonsTexture, buttonDefinitions[i].SourceRect, buttonDefinitions[i].Action, out MGBorder face);
            _buttonFaces[i] = face;
            _labels[i] = LegacyMenuUiTheme.CreateBodyText(window, buttonDefinitions[i].Label, LegacyMenuUiTheme.MutedTextColor);
            _labels[i].HorizontalAlignment = HorizontalAlignment.Center;
            _labels[i].TextAlignment = HorizontalAlignment.Center;

            item.TryAddChild(_buttons[i]);
            item.TryAddChild(_labels[i]);
            buttonsRow.TryAddChild(item);
        }

        content.TryAddChild(buttonsRow);
        band.SetContent(content);
        window.SetContent(band);
        _playButton = _buttons[0];
    }

    public override void Show()
    {
        _playButton?.Focus();
    }

    public override void Update(GameTime gameTime)
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            bool isActive = _buttons[i].VisualState.IsFocused || _buttons[i].IsHovered;
            LegacyMenuUiTheme.ApplyMainMenuButtonState(_buttons[i], _buttonFaces[i], _labels[i], isActive);
        }
    }

    private sealed record MenuButtonDefinition(string Label, Rectangle SourceRect, Action Action);
}