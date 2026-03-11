using RacingGame.GameScreens;
using RacingGame.Graphics;
using RacingGame.Sounds;

namespace RacingGame.GameLogic;

/// <summary>
/// Player game-logic class. Owns the car physics (via <see cref="CarPhysics"/> inheritance)
/// and composes a <see cref="ChaseCamera"/> instance rather than inheriting from it.
/// This removes the conceptually incorrect "camera IS-A car physics" relationship.
/// Note: This class is instanced once for the current player. For multiplayer you would
/// create one Player per participant.
/// </summary>
public class Player : CarPhysics
{
    #region Variables
    /// <summary>
    /// Remember all lap times for the victory screen.
    /// </summary>
    private List<float> lapTimes = new List<float>();

    internal IReadOnlyList<float> LapTimes => lapTimes;

    /// <summary>
    /// The number of laps in each race
    /// </summary>
    private const int LapCount = 3;

    /// <summary>
    /// Add lap time
    /// </summary>
    /// <param name="setLapTime">Lap time</param>
    public void AddLapTime(float setLapTime)
    {
        lapTimes.Add(setLapTime);
    }

    /// <summary>
    /// The amount of time (in milliseconds) the car has
    /// been in the air since last touching the ground
    /// If the car is in the air and does not reach the
    /// ground again for too long, its game over!
    /// </summary>
    private float inAirTimeMilliseconds = 0.0f;

    /// <summary>
    /// The amount of time (in milliseconds) the car must be
    /// in the air before game over occurs
    /// </summary>
    private const float InAirTimeoutMilliseconds = 3000.0f;

    // Game over camera
    /// <summary>Period (ms) of one full orbit of the game-over camera around the car.</summary>
    private const float GameOverCameraRotationPeriodMs = 2593.0f;
    #endregion

    #region Camera composition
    /// <summary>
    /// Chase camera that follows this player's car.
    /// Owned by Player rather than inherited, so that camera behaviour is
    /// fully decoupled from car physics.
    /// </summary>
    public ChaseCamera Camera { get; }

    /// <summary>Current camera position in world space.</summary>
    public Vector3 CameraPosition => Camera.CameraPosition;

    /// <inheritdoc/>
    public override bool FreeCamera
    {
        get => Camera.FreeCamera;
        set => Camera.FreeCamera = value;
    }

    /// <inheritdoc/>
    public override void SetCameraPosition(Vector3 position) =>
        Camera.SetCameraPosition(position);

    /// <inheritdoc/>
    public override void InterpolateCameraPosition(Vector3 position) =>
        Camera.InterpolateCameraPosition(position);
    #endregion

    #region Constructor
    /// <summary>
    /// Create the player at the given car starting position.
    /// </summary>
    /// <param name="setCarPosition">Initial car position.</param>
    public Player(Vector3 setCarPosition)
        : base(setCarPosition)
    {
        Camera = new ChaseCamera(this);
    }
    #endregion

    #region Reset
    /// <summary>
    /// Reset all player and camera values for a new game.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        lapTimes.Clear();
        Camera.Reset();
    }

    /// <summary>
    /// Clear physics and camera variables when the game ends.
    /// </summary>
    public override void ClearVariablesForGameOver()
    {
        base.ClearVariablesForGameOver();
        Camera.ClearVariablesForGameOver();
    }
    #endregion

    #region Handle game logic
    /// <summary>
    /// Update game logic, called every frame.
    /// </summary>
    public override void Update()
    {
        // Don't handle any more game logic if game is over.
        if (RacingGameManager.InGame &&
            ZoomInTime <= 0)
        {
            // Game over? Then show end screen!
            if (isGameOver)
            {
                // Manually orbit the camera around the car — bypass ChaseCamera entirely.
                Vector3 gameOverCamPos = CarPosition + new Vector3(0, -5, +20) +
                            Vector3.TransformNormal(new Vector3(30, 0, 0),
                                Matrix.CreateRotationZ(BaseGame.TotalTimeMilliseconds / GameOverCameraRotationPeriodMs));
                BaseGame.ViewMatrix = Matrix.CreateLookAt(
                    gameOverCamPos, CarPosition, CarUpVector);
                this.currentGameTimeMilliseconds = this.BestTimeMilliseconds;

                // Don't continue processing game logic
                return;
            }

            // Check if car is in the air,
            // used to check if the player died.
            if (this.isCarOnGround == false)
            {
                inAirTimeMilliseconds +=
                    BaseGame.ElapsedTimeThisFrameInMilliseconds;
            }
            else
                // Back on ground, reset
            {
                inAirTimeMilliseconds = 0;
            }

            // Game not over yet, check if we lost or won.
            // Check if we have fallen from the track
            float trackDistance = Vector3.Distance(CarPosition, groundPlanePos);
            if (trackDistance > 20 ||
                inAirTimeMilliseconds > InAirTimeoutMilliseconds)
            {
                // Reset player variables (stop car, etc.)
                ClearVariablesForGameOver();

                // And indicate that game is over and we lost!
                isGameOver = true;
                victory = false;
                Sound.Play(Sound.Sounds.CarLose);

                // Also stop engine sound
                Sound.StopGearSound();
            }

            // Finished all laps? Then we won!
            if (CurrentLap >= LapCount)
            {
                // Reset player variables (stop car, etc.)
                ClearVariablesForGameOver();

                // When you win, you start an extra lap we don't want to show
                this.lap--;

                // Then game is over and we won!
                isGameOver = true;
                victory = true;
                Sound.Play(Sound.Sounds.Victory);

                // Also stop engine sound
                Sound.StopGearSound();
            }
        }

        base.Update();
        Camera.Update();
    }
    #endregion

    #region Render
    /// <summary>
    /// Render game-over overlay: victory/defeat message, lap times and rank.
    /// Must be called during the render phase, not during Update.
    /// </summary>
    public void RenderGameOver()
    {
        if (!isGameOver)
            return;

        int rank = Highscores.GetRankFromCurrentTime(
            this.levelNum, (int)this.BestTimeMilliseconds);

        if (victory)
        {
            TextureFont.WriteTextCentered(
                BaseGame.Width / 2, BaseGame.Height / 7,
                "Victory! You won.",
                Color.LightGreen, 1.25f);
        }
        else
        {
            TextureFont.WriteTextCentered(
                BaseGame.Width / 2, BaseGame.Height / 7,
                "Game Over! You lost.",
                Color.Red, 1.25f);
        }

        for (int num = 0; num < lapTimes.Count; num++)
        {
            TextureFont.WriteTextCentered(
                BaseGame.Width / 2,
                BaseGame.Height / 7 + BaseGame.YToRes(35) * (1 + num),
                "Lap " + (num + 1) + " Time: " +
                (((int)lapTimes[num]) / 60).ToString("00") + ":" +
                (((int)lapTimes[num]) % 60).ToString("00") + "." +
                (((int)(lapTimes[num] * 100)) % 100).ToString("00"),
                Color.White, 1.25f);
        }

        TextureFont.WriteTextCentered(
            BaseGame.Width / 2,
            BaseGame.Height / 7 + BaseGame.YToRes(35) * (1 + lapTimes.Count),
            "Rank: " + (1 + rank),
            Color.White, 1.25f);
    }
    #endregion
}