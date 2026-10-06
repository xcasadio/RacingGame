using CasaEngine.Engine.Input;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.UI.ViewModels;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Title screen, loaded from the <c>Screen.Splash</c> screen asset (Content/UI/Screens/Splash), as in the author's capture
/// of the original game (ADR-0008). As RacingGame's (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/SplashScreen.cs</c>,
/// Update), a left click anywhere, Space, Escape or the gamepad's Start leave it, with the ScreenBack sound
/// (<c>RacingGameManager.Render</c>); Enter and A do too, as in the main menu.
/// </summary>
internal sealed class SplashScreen : RaceXamlScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _continueToMenu;
    private readonly RaceTitleViewModel _viewModel = new();
    private bool _isLeaving;

    public SplashScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action continueToMenu)
        : base(assetContentManager, "Screen.Splash")
    {
        _game = game;
        _state = state;
        _continueToMenu = continueToMenu;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        window.WindowDataContext = _viewModel;
        UpdateLayout(0.0);
    }

    public override void Update(GameTime gameTime)
    {
        if (!_isLeaving && IsContinueRequested())
        {
            _isLeaving = true;
            _game.MenuSounds?.Play(MenuSound.ScreenBack);
            _continueToMenu();
            return;
        }

        UpdateLayout(gameTime.TotalGameTime.TotalSeconds);
    }

    private bool IsContinueRequested()
    {
        KeyboardManager keyboard = _game.InputComponent.KeyboardManager;
        GamePad gamePad = _game.InputComponent.GamePadManager.GetGamePad(PlayerIndex.One);
        return _game.InputComponent.MouseManager.LeftButtonJustPressed
            || keyboard.IsKeyJustPressed(XnaKeys.Space)
            || keyboard.IsKeyJustPressed(XnaKeys.Escape)
            || keyboard.IsKeyJustPressed(XnaKeys.Enter)
            || (gamePad.IsConnected && (gamePad.StartJustPressed || gamePad.AJustPressed));
    }

    // Lays the decoration, the band and the sprite out for the viewport and the time, as RacingGame did every frame.
    private void UpdateLayout(double totalSeconds)
    {
        UpdateMenuDecoration(_viewModel.Decoration, totalSeconds);
        _viewModel.Update(Root.Metrics.ViewportSize.X, Root.Metrics.ViewportSize.Y, totalSeconds, _state.PinScreenAnimations);
    }
}
