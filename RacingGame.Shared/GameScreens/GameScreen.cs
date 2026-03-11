using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Shaders;
using RacingGame.Sounds;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;
namespace RacingGame.GameScreens;

/// <summary>
/// GameScreen, just manages the on screen display for the game.
/// </summary>
class GameScreen : IGameScreen, IMguiScreen
{
    #region Variables
    private bool _isFinished = false;
    private IMguiScreenView _mguiView;
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

        return _isFinished;
    }
    #endregion

    public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
    {
        _mguiView ??= new GameHudView(this, host);
        return _mguiView;
    }

    internal int CurrentGameTime => (int)RacingGameManager.Player.GameTimeMilliseconds;
    internal int BestLapTime => (int)RacingGameManager.Player.BestTimeMilliseconds;
    internal int CurrentLapDisplay => RacingGameManager.Player.CurrentLap + 1;
    internal int SpeedDisplay => (int)Math.Round(RacingGameManager.Player.Speed * CarPhysics.MeterPerSecToMph);
    internal int GearDisplay => 1 + (int)(5 * RacingGameManager.Player.Speed / CarPhysics.MaxPossibleSpeed);
    internal string TrackName => RacingGameManager.Landscape.CurrentTrackName;
    internal IReadOnlyList<int> TopLapTimes => Highscores.GetTop5LapTimes(TrackSelection.SelectedTrackNumber);
    internal bool IsGameOver => RacingGameManager.Player.GameOver;
    internal string GameOverTitle => RacingGameManager.Player.WonGame ? "Victory! You won." : "Game Over! You lost.";
    internal string ExitHint => RacingGameManager.Player.GameOver ? "Press Space, A, B, X, or click to return to menu." : "Esc or Back returns to menu.";

    internal IReadOnlyList<string> GetGameOverLines()
    {
        var lines = new List<string>();
        var lapTimes = RacingGameManager.Player.LapTimes;
        for (int i = 0; i < lapTimes.Count; i++)
            lines.Add($"Lap {i + 1} Time: {FormatLapTime(lapTimes[i])}");

        int rank = Highscores.GetRankFromCurrentTime(RacingGameManager.Player.LevelNum, BestLapTime);
        lines.Add($"Rank: {1 + rank}");
        return lines;
    }

    private static string FormatLapTime(float seconds)
    {
        int totalCentiseconds = (int)(seconds * 100);
        int totalSeconds = totalCentiseconds / 100;
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}.{totalCentiseconds % 100:00}";
    }
}