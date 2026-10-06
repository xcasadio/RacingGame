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
/// Car selection, loaded from the <c>Screen.CarSelection</c> screen asset (Content/UI/Screens/CarSelection), as in the
/// author's capture of the original game (ADR-0008), with RacingGame's input (<c>git show 4f840a3^:RacingGame.Shared/
/// GameScreens/CarSelection.cs</c>, Update):
/// <list type="bullet">
/// <item>Left (keyboard, D-pad, left stick past 0.5, or a click in the right-hand zone) brings the next car to the front,
/// Right (or a click in the left-hand zone) the previous one;</item>
/// <item>Up and Down pick the previous or next colour; the left mouse button held on a colour square picks it;</item>
/// <item>A, Space, Enter or a click on A SELECT confirm; Escape, B, Back or a click on B BACK go back;</item>
/// <item>a change plays the Highlight sound, as does the mouse entering a colour square or a bottom button; confirming
/// plays ScreenClick, going back ScreenBack.</item>
/// </list>
/// Nothing takes MGUI's keyboard focus, so MGUI's own arrow-key navigation stays out of the way.
/// </summary>
internal sealed class CarSelectionScreen : RaceXamlScreenBase
{
    private const float StickThreshold = 0.5f;
    private static readonly string[] TextNames = ["txtColorLabel", "txtStat0", "txtStat1", "txtStat2", "txtStat3", "txtStat4", "txtStat5"];

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _confirm;
    private readonly Action _back;
    private readonly RaceCarSelectionViewModel _viewModel = new();
    private MGTextBlock[] _texts = [];
    private int _shownCarIndex = -1;
    // The first update after the screen opens takes no key: the key that opened it is still "just pressed".
    private bool _acceptsInput;
    private bool _isLeaving;
    private Point _lastMouse = new(-1, -1);
    private float _lastStickX;
    private float _lastStickY;

    public CarSelectionScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action confirm, Action back)
        : base(assetContentManager, "Screen.CarSelection")
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
        var colors = RaceFrontEndCatalog.CarColors;
        if (colors.Count != RaceCarSelectionViewModel.SwatchCount)
        {
            throw new InvalidOperationException(
                $"CarSelection.xaml has {RaceCarSelectionViewModel.SwatchCount} colour squares; the catalogue has {colors.Count} colours.");
        }

        for (int i = 0; i < colors.Count; i++)
        {
            _viewModel.Swatches[i].SetColor(colors[i].Value);
        }

        _texts = new MGTextBlock[TextNames.Length];
        for (int i = 0; i < TextNames.Length; i++)
        {
            _texts[i] = FindControl<MGTextBlock>(TextNames[i]);
            _texts[i].RenderTransform.Origin = Vector2.Zero;
        }

        window.WindowDataContext = _viewModel;
        RefreshSelection();
        UpdateLayout(0.0);
    }

    public override void Update(GameTime gameTime)
    {
        HandleInput();
        if (_isLeaving)
        {
            return;
        }

        RefreshSelection();
        CarSelectionCarousel carousel = _game.CarSelectionCarousel;
        carousel.Request(_state.SelectedCarIndex, _state.SelectedCarColorIndex, _state.PinScreenAnimations, _state.EnableShadows,
            Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y);
        _viewModel.CarouselImage = carousel.TextureData;
        UpdateLayout(gameTime.TotalGameTime.TotalSeconds);
    }

    private void HandleInput()
    {
        KeyboardManager keyboard = _game.InputComponent.KeyboardManager;
        MouseManager mouse = _game.InputComponent.MouseManager;
        GamePad gamePad = _game.InputComponent.GamePadManager.GetGamePad(PlayerIndex.One);
        Point position = mouse.Position;
        float stickX = gamePad.IsConnected ? gamePad.LeftStickX : 0f;
        float stickY = gamePad.IsConnected ? gamePad.LeftStickY : 0f;

        if (!_acceptsInput)
        {
            _acceptsInput = true;
            _lastMouse = position;
            _lastStickX = stickX;
            _lastStickY = stickY;
            return;
        }

        // Input.MouseInBox played Highlight whenever the mouse entered a box it tested.
        int swatch = _viewModel.GetSwatchAt(position.X, position.Y);
        if ((swatch >= 0 && swatch != _viewModel.GetSwatchAt(_lastMouse.X, _lastMouse.Y))
            || (_viewModel.IsOverSelectButton(position.X, position.Y) && !_viewModel.IsOverSelectButton(_lastMouse.X, _lastMouse.Y))
            || (_viewModel.IsOverBackButton(position.X, position.Y) && !_viewModel.IsOverBackButton(_lastMouse.X, _lastMouse.Y)))
        {
            PlaySound(MenuSound.Highlight);
        }

        if (swatch >= 0 && mouse.LeftButtonPressed && swatch != _state.SelectedCarColorIndex)
        {
            SelectColor(swatch);
        }

        bool click = mouse.LeftButtonJustPressed;
        bool left = keyboard.IsKeyJustPressed(XnaKeys.Left)
            || (gamePad.IsConnected && gamePad.DPadLeftJustPressed)
            || (stickX < -StickThreshold && _lastStickX >= -StickThreshold)
            || (click && _viewModel.IsInTurnLeftZone(position.X, position.Y));
        bool right = keyboard.IsKeyJustPressed(XnaKeys.Right)
            || (gamePad.IsConnected && gamePad.DPadRightJustPressed)
            || (stickX > StickThreshold && _lastStickX <= StickThreshold)
            || (click && _viewModel.IsInTurnRightZone(position.X, position.Y));
        int carCount = RaceFrontEndCatalog.Cars.Count;
        if (left)
        {
            SelectCar((_state.SelectedCarIndex + 1) % carCount);
        }
        else if (right)
        {
            SelectCar((_state.SelectedCarIndex + carCount - 1) % carCount);
        }

        bool up = keyboard.IsKeyJustPressed(XnaKeys.Up)
            || (gamePad.IsConnected && gamePad.DPadUpJustPressed)
            || (stickY > StickThreshold && _lastStickY <= StickThreshold);
        bool down = keyboard.IsKeyJustPressed(XnaKeys.Down)
            || (gamePad.IsConnected && gamePad.DPadDownJustPressed)
            || (stickY < -StickThreshold && _lastStickY >= -StickThreshold);
        int colorCount = RaceCarSelectionViewModel.SwatchCount;
        if (up)
        {
            SelectColor((_state.SelectedCarColorIndex + colorCount - 1) % colorCount);
        }
        else if (down)
        {
            SelectColor((_state.SelectedCarColorIndex + 1) % colorCount);
        }

        _lastMouse = position;
        _lastStickX = stickX;
        _lastStickY = stickY;

        if (keyboard.IsKeyJustPressed(XnaKeys.Space)
            || keyboard.IsKeyJustPressed(XnaKeys.Enter)
            || (gamePad.IsConnected && gamePad.AJustPressed)
            || (click && _viewModel.IsOverSelectButton(position.X, position.Y)))
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

    private void SelectCar(int index)
    {
        _state.SelectedCarIndex = index;
        PlaySound(MenuSound.Highlight);
    }

    private void SelectColor(int index)
    {
        _state.SelectedCarColorIndex = index;
        PlaySound(MenuSound.Highlight);
    }

    private void Leave(MenuSound sound, Action action)
    {
        _isLeaving = true;
        PlaySound(sound);
        action();
    }

    private void PlaySound(MenuSound sound) => _game.MenuSounds?.Play(sound);

    // RacingGame's property rows of the selected car (CarSelection.Render); only Max Speed shows a figure.
    private void RefreshSelection()
    {
        _viewModel.SelectedColorIndex = _state.SelectedCarColorIndex;
        if (_shownCarIndex == _state.SelectedCarIndex)
        {
            return;
        }

        _shownCarIndex = _state.SelectedCarIndex;
        CarSelectionBars bars = RaceFrontEndCatalog.Cars[_shownCarIndex].SelectionBars;
        _viewModel.SetStat(0, $"Max Speed: {bars.MaxSpeedMph}mph", bars.MaxSpeed);
        _viewModel.SetStat(1, "Acceleration:", bars.Acceleration);
        _viewModel.SetStat(2, "Car Mass:", bars.Mass);
        _viewModel.SetStat(3, "Braking:", bars.Braking);
        _viewModel.SetStat(4, "Friction:", bars.Friction);
        _viewModel.SetStat(5, "Engine:", bars.Engine);
    }

    // Lays the screen out for the viewport, the time and the mouse, and scales the texts as RacingGame's TextureFont did.
    private void UpdateLayout(double totalSeconds)
    {
        UpdateMenuDecoration(_viewModel.Decoration, totalSeconds);
        Point mouse = _game.InputComponent.MouseManager.Position;
        (int plateLeft, int plateRight) = _game.CarSelectionCarousel.FrontPlateEdges;
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, totalSeconds, _state.PinScreenAnimations, mouse.X, mouse.Y, plateLeft, plateRight);

        var scale = new Vector2(_viewModel.TextScaleX, _viewModel.TextScaleY);
        for (int i = 0; i < _texts.Length; i++)
        {
            _texts[i].RenderTransform.Scale = scale;
        }
    }
}
