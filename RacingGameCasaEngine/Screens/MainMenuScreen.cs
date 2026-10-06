using CasaEngine.Engine.Input;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI.ViewModels;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Main menu, loaded from the <c>Screen.MainMenu</c> screen asset (Content/UI/Screens/MainMenu), with the selection of
/// RacingGame's XNA menu (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/MainMenu.cs</c>, Update; ADR-0006):
/// <list type="bullet">
/// <item>one button is always selected: the one of the last visit, Play at the first;</item>
/// <item>Left and Right, on the keyboard, the D-pad or the left stick (past 0.5; RacingGame used 0.75), move the
/// selection around the row; held, the first repeat comes after about 0.5 s, then one every 250 ms, as in RacingGame;</item>
/// <item>the mouse selects the button under it, once it has moved since the last key or pad move;</item>
/// <item>Space, Enter or A activate the selected button, a click the clicked one.</item>
/// </list>
/// The buttons do not take MGUI's keyboard focus, so MGUI's own arrow-key navigation does not move it too.
/// </summary>
internal sealed class MainMenuScreen : RaceXamlScreenBase
{
    // Button name suffixes in MainMenu.xaml (btn*), in the order of the actions and of RaceMainMenuViewModel.Buttons.
    private static readonly string[] EntryNames = ["Play", "Highscores", "Options", "Help", "Quit"];
    private const double RepeatSeconds = 0.25;
    private const float StickThreshold = 0.5f;

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action[] _actions;
    private readonly RaceMainMenuViewModel _viewModel = new();

    private MGButton[] _buttons = [];
    private bool _ignoreMouse = true;
    // The first update after the menu opens takes no activation key: the key that opened it is still "just pressed".
    private bool _acceptsActivation;
    private bool _wasLeftHeld;
    private bool _wasRightHeld;
    private double _leftHeldSeconds;
    private double _rightHeldSeconds;

    public MainMenuScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state,
        Action openCarSelection, Action openHighscores, Action openOptions, Action openHelp, Action requestExit)
        : base(assetContentManager, "Screen.MainMenu")
    {
        _game = game;
        _state = state;
        _actions = [openCarSelection, openHighscores, openOptions, openHelp, requestExit];
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        window.WindowDataContext = _viewModel;
        _viewModel.Reset(_state.SelectedMainMenuButton);
        UpdateMenuDecoration(_viewModel.Decoration, 0.0);
        UpdateMenuLayout(0.0);

        _buttons = new MGButton[EntryNames.Length];
        for (int i = 0; i < EntryNames.Length; i++)
        {
            _buttons[i] = FindControl<MGButton>("btn" + EntryNames[i]);
            _buttons[i].IsFocusable = false;

            int index = i;
            _buttons[i].AddCommandHandler((_, _) => Activate(index));
        }
    }

    public override void Update(GameTime gameTime)
    {
        double elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        HandleInput(elapsedSeconds);
        UpdateMenuDecoration(_viewModel.Decoration, gameTime.TotalGameTime.TotalSeconds);
        UpdateMenuLayout(elapsedSeconds);
    }

    private void HandleInput(double elapsedSeconds)
    {
        KeyboardManager keyboard = _game.InputComponent.KeyboardManager;
        MouseManager mouse = _game.InputComponent.MouseManager;
        GamePad gamePad = _game.InputComponent.GamePadManager.GetGamePad(PlayerIndex.One);

        if (mouse.HasMoved || mouse.LeftButtonJustPressed)
        {
            _ignoreMouse = false;
        }

        if (!_ignoreMouse)
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i].IsHovered)
                {
                    Select(i);
                }
            }
        }

        bool leftHeld = keyboard.IsKeyPressed(XnaKeys.Left) || (gamePad.IsConnected && (gamePad.DPadLeftPressed || gamePad.LeftStickX < -StickThreshold));
        bool rightHeld = keyboard.IsKeyPressed(XnaKeys.Right) || (gamePad.IsConnected && (gamePad.DPadRightPressed || gamePad.LeftStickX > StickThreshold));
        int count = EntryNames.Length;
        if (IsRepeatDue(leftHeld, ref _wasLeftHeld, ref _leftHeldSeconds, elapsedSeconds))
        {
            Select((_viewModel.SelectedIndex + count - 1) % count);
            _ignoreMouse = true;
        }
        else if (IsRepeatDue(rightHeld, ref _wasRightHeld, ref _rightHeldSeconds, elapsedSeconds))
        {
            Select((_viewModel.SelectedIndex + 1) % count);
            _ignoreMouse = true;
        }

        if (_acceptsActivation
            && (keyboard.IsKeyJustPressed(XnaKeys.Space) || keyboard.IsKeyJustPressed(XnaKeys.Enter) || (gamePad.IsConnected && gamePad.AJustPressed)))
        {
            Activate(_viewModel.SelectedIndex);
        }

        _acceptsActivation = true;
    }

    // RacingGame's hold repeat: a move when the direction is first pressed (which also takes 250 ms off the held time),
    // then one each time more than 250 ms have been held, so the first repeat comes after about 0.5 s.
    private static bool IsRepeatDue(bool held, ref bool wasHeld, ref double heldSeconds, double elapsedSeconds)
    {
        if (!held)
        {
            wasHeld = false;
            heldSeconds = 0;
            return false;
        }

        bool justPressed = !wasHeld;
        wasHeld = true;
        heldSeconds += elapsedSeconds;
        if (justPressed || heldSeconds > RepeatSeconds)
        {
            heldSeconds -= RepeatSeconds;
            return true;
        }

        return false;
    }

    private void Select(int index)
    {
        _viewModel.SelectedIndex = index;
        _state.SelectedMainMenuButton = index;
    }

    private void Activate(int index)
    {
        Select(index);
        _actions[index]();
    }

    // Lays the band and the buttons out for the viewport, as RacingGame's main menu did every frame.
    private void UpdateMenuLayout(double elapsedSeconds)
    {
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, elapsedSeconds);
    }
}
