using CasaEngine.Engine.Input;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using Point = Microsoft.Xna.Framework.Point;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Track selection, loaded from the <c>Screen.TrackSelection</c> screen asset (Content/UI/Screens/TrackSelection), as
/// RacingGame's (ADR-0010), with its input (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/TrackSelection.cs</c>,
/// Update):
/// <list type="bullet">
/// <item>Left and Right (keyboard, D-pad, left stick past 0.5) select the previous or next card, wrapping, with the
/// ButtonClick sound;</item>
/// <item>the mouse selects the card under it, once it has moved since the last key or pad move; a click on a card selects
/// it and starts the race;</item>
/// <item>A, Space, Enter or a click on A SELECT start the race with ScreenClick; Escape, B, Back or a click on B BACK go
/// back with ScreenBack;</item>
/// <item>the Highlight sound plays when the mouse enters a card or a bottom button.</item>
/// </list>
/// Nothing takes MGUI's keyboard focus, so MGUI's own arrow-key navigation stays out of the way.
/// </summary>
internal sealed class TrackSelectionScreen : RaceXamlScreenBase
{
    private const float StickThreshold = 0.5f;

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _confirm;
    private readonly Action _back;
    private readonly RaceTrackSelectionViewModel _viewModel = new();
    private bool _ignoreMouse = true;
    // The first update after the screen opens takes no key: the key that opened it is still "just pressed".
    private bool _acceptsInput;
    private bool _isLeaving;
    private Point _lastMouse = new(-1, -1);
    private float _lastStickX;

    public TrackSelectionScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action confirm, Action back)
        : base(assetContentManager, "Screen.TrackSelection")
    {
        _game = game;
        _state = state;
        _confirm = confirm;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        if (RaceFrontEndCatalog.Tracks.Count != RaceTrackSelectionViewModel.CardCount)
        {
            throw new InvalidOperationException(
                $"TrackSelection.xaml has {RaceTrackSelectionViewModel.CardCount} track cards; the catalogue has {RaceFrontEndCatalog.Tracks.Count} tracks.");
        }

        window.WindowDataContext = _viewModel;
        _viewModel.Reset(_state.SelectedTrackIndex);
        UpdateLayout(0.0, 0.0);
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        if (_isLeaving)
        {
            return;
        }

        UpdateLayout(gameTime.ElapsedGameTime.TotalSeconds, gameTime.TotalGameTime.TotalSeconds);
    }

    private void HandleInput()
    {
        KeyboardManager keyboard = _game.InputComponent.KeyboardManager;
        MouseManager mouse = _game.InputComponent.MouseManager;
        GamePad gamePad = _game.InputComponent.GamePadManager.GetGamePad(PlayerIndex.One);
        Point position = mouse.Position;
        float stickX = gamePad.IsConnected ? gamePad.LeftStickX : 0f;

        if (!_acceptsInput)
        {
            _acceptsInput = true;
            _lastMouse = position;
            _lastStickX = stickX;
            return;
        }

        if (mouse.HasMoved || mouse.LeftButtonJustPressed)
        {
            _ignoreMouse = false;
        }

        // Input.MouseInBox played Highlight whenever the mouse entered a box it tested.
        int card = _viewModel.GetCardAt(position.X, position.Y);
        if ((card >= 0 && card != _viewModel.GetCardAt(_lastMouse.X, _lastMouse.Y))
            || (_viewModel.IsOverSelectButton(position.X, position.Y) && !_viewModel.IsOverSelectButton(_lastMouse.X, _lastMouse.Y))
            || (_viewModel.IsOverBackButton(position.X, position.Y) && !_viewModel.IsOverBackButton(_lastMouse.X, _lastMouse.Y)))
        {
            PlaySound(MenuSound.Highlight);
        }

        if (!_ignoreMouse && card >= 0)
        {
            Select(card);
        }

        bool left = keyboard.IsKeyJustPressed(XnaKeys.Left)
            || (gamePad.IsConnected && gamePad.DPadLeftJustPressed)
            || (stickX < -StickThreshold && _lastStickX >= -StickThreshold);
        bool right = keyboard.IsKeyJustPressed(XnaKeys.Right)
            || (gamePad.IsConnected && gamePad.DPadRightJustPressed)
            || (stickX > StickThreshold && _lastStickX <= StickThreshold);
        int count = RaceTrackSelectionViewModel.CardCount;
        if (left)
        {
            PlaySound(MenuSound.ButtonClick);
            Select((_state.SelectedTrackIndex + count - 1) % count);
            _ignoreMouse = true;
        }
        else if (right)
        {
            PlaySound(MenuSound.ButtonClick);
            Select((_state.SelectedTrackIndex + 1) % count);
            _ignoreMouse = true;
        }

        _lastMouse = position;
        _lastStickX = stickX;

        bool click = mouse.LeftButtonJustPressed;
        if ((click && card >= 0)
            || (click && _viewModel.IsOverSelectButton(position.X, position.Y))
            || keyboard.IsKeyJustPressed(XnaKeys.Space)
            || keyboard.IsKeyJustPressed(XnaKeys.Enter)
            || (gamePad.IsConnected && gamePad.AJustPressed))
        {
            Leave(MenuSound.ScreenClick, _confirm);
        }
        else if (keyboard.IsKeyJustPressed(XnaKeys.Escape)
            || (gamePad.IsConnected && (gamePad.BJustPressed || gamePad.BackJustPressed))
            || (click && _viewModel.IsOverBackButton(position.X, position.Y)))
        {
            Leave(MenuSound.ScreenBack, _back);
        }
    }

    private void Select(int index)
    {
        _state.SelectedTrackIndex = index;
        _viewModel.SelectedIndex = index;
    }

    private void Leave(MenuSound sound, Action action)
    {
        _isLeaving = true;
        PlaySound(sound);
        action();
    }

    private void PlaySound(MenuSound sound) => _game.MenuSounds?.Play(sound);

    // Lays the screen out for the viewport, the time and the mouse, as RacingGame did every frame.
    private void UpdateLayout(double elapsedSeconds, double totalSeconds)
    {
        UpdateMenuDecoration(_viewModel.Decoration, totalSeconds);
        Point mouse = _game.InputComponent.MouseManager.Position;
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, elapsedSeconds, _state.PinScreenAnimations, mouse.X, mouse.Y);
    }
}
