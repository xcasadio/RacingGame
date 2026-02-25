using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Shaders;
using RacingGame.Sounds;
namespace RacingGame.GameScreens;

/// <summary>
/// GameScreen, just manages the on screen display for the game.
/// </summary>
class GameScreen : IGameScreen
{
    #region Variables
    private bool _isFinished = false;
    #endregion

    #region Constructor
    /// <summary>
    /// Create game screen
    /// </summary>
    public GameScreen()
    {
        // Load level
        RacingGameManager.LoadLevel(TrackSelection.SelectedTrack);

        // Reset player variables (start new game, reset time and position)
        RacingGameManager.Player.Reset();

        // Fix light direction (was changed by CarSelection screen!)
        // LightDirection will normalize
        BaseGame.LightDirection = LensFlare.DefaultLightPos;

        // Start gear sound
        Sound.StartGearSound();

        // Play game music
        Sound.Play(Sound.Sounds.GameMusic);
    }
    #endregion

    #region Update
    /// <summary>
    /// Process input and audio logic. Returns to menu on ESC / Back or after
    /// the game-over screen is dismissed.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Update engine sound every frame
        Sound.UpdateGearSound(RacingGameManager.Player.Speed,
            RacingGameManager.Player.Acceleration);

        bool exitPressed =
            Input.KeyboardEscapeJustPressed ||
            Input.GamePadBackJustPressed ||
            (RacingGameManager.Player.GameOver &&
             (Input.KeyboardSpaceJustPressed ||
              Input.GamePadAJustPressed ||
              Input.GamePadBJustPressed ||
              Input.GamePadXJustPressed ||
              Input.MouseLeftButtonJustPressed));

        if (exitPressed && !_isFinished)
        {
            _isFinished = true;
            Sound.StopGearSound();
            Sound.Play(Sound.Sounds.MenuMusic);
        }
    }
    #endregion

    #region Render
    /// <summary>
    /// Render game screen — drawing only.
    /// </summary>
    public bool Render()
    {
        ShadowMapShader.PrepareGameShadows();

        // This starts both menu and in game post screen shader!
        if (BaseGame.UsePostScreenShaders)
        {
            BaseGame.UI.PostScreenGlowShader.Start();
        }

        // Render background sky and lensflare.
        BaseGame.UI.RenderGameBackground();

        // Render landscape with track and all objects
        RacingGameManager.Landscape.Render();

        // Render car with matrix we got from CarPhysics
        RacingGameManager.CarModel.RenderCar(
            RacingGameManager.CurrentCarNumber,
            RacingGameManager.CarColor,
            false,
            RacingGameManager.Player.CarRenderMatrix);

        // And flush all models to be rendered
        BaseGame.MeshRenderManager.Render();

        // Use data from best replay for the shadow car
        Matrix bestReplayCarMatrix =
            RacingGameManager.Landscape.BestReplay.GetCarMatrixAtTime(
                RacingGameManager.Player.GameTimeMilliseconds / 1000.0f);
        // For rendering rotate car to stay correctly on the road
        bestReplayCarMatrix =
            Matrix.CreateRotationX(MathHelper.Pi / 2.0f) *
            Matrix.CreateRotationZ(MathHelper.Pi) *
            bestReplayCarMatrix;

        // Also render the shadow car (if the game has started)!
        if (RacingGameManager.Player.GameTimeMilliseconds > 0)
        {
            RacingGameManager.CarModel.RenderCar(
                0, RacingGameManager.CarColor,
                true, bestReplayCarMatrix);
        }

        // Show shadows we calculated above
        if (BaseGame.AllowShadowMapping)
        {
            ShaderEffect.shadowMapping.ShowShadows();
        }

        // Apply post screen shader here before doing the UI
        if (BaseGame.UsePostScreenShaders)
        {
            BaseGame.UI.PostScreenGlowShader.Show();
        }

        // Show on screen UI for the game.
        BaseGame.UI.RenderGameUI(
            (int)RacingGameManager.Player.GameTimeMilliseconds,
            (int)RacingGameManager.Player.BestTimeMilliseconds,
            RacingGameManager.Player.CurrentLap + 1,
            RacingGameManager.Player.Speed * CarPhysics.MeterPerSecToMph,
            1 + (int)(5 * RacingGameManager.Player.Speed /
                      CarPhysics.MaxPossibleSpeed),
            0.5f * RacingGameManager.Player.Speed /
            CarPhysics.MaxPossibleSpeed +
            0.5f * RacingGameManager.Player.Acceleration,
            RacingGameManager.Landscape.CurrentTrackName,
            Highscores.GetTop5LapTimes(TrackSelection.SelectedTrackNumber));

        // Render game-over overlay if applicable (victory/defeat message + stats)
        RacingGameManager.Player.RenderGameOver();

        return _isFinished;
    }
    #endregion
}