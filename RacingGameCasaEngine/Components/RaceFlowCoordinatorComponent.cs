using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Worlds;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Components;

public sealed class RaceFlowCoordinatorComponent : EntityComponent
{
    private readonly List<Vector3> _checkpointPositions = [];
    private bool _isInitialized;
    private bool _pauseLatch;
    private double _lastCheckpointDistance = double.MaxValue;

    public float CheckpointRadius { get; set; } = 3.6f;

    public override EntityComponent Clone()
    {
        return new RaceFlowCoordinatorComponent
        {
            CheckpointRadius = CheckpointRadius,
        };
    }

    public override void Update(float elapsedTime)
    {
        if (Owner?.World?.Game is not RacingGameCasaEngineGame game)
        {
            return;
        }

        RuntimeRaceSession session = game.RaceSession;
        if (!session.IsActive || session.GameMode == null || session.PlayerController == null || session.PlayerPawn?.RootComponent == null)
        {
            return;
        }

        if (!_isInitialized)
        {
            InitializeCheckpoints(Owner.World, session.GameMode);
            _isInitialized = true;
        }

        HandlePauseToggle(game, session.GameMode, session.PlayerController);

        session.GameMode.UpdateCountdown(elapsedTime);
        bool canDrive = !session.IsDebugCameraEnabled
            && session.GameMode.CountdownSecondsRemaining <= 0f
            && !session.GameMode.IsPaused
            && !session.GameMode.IsRaceFinished;
        session.PlayerController.IsInputEnable = canDrive;
        session.PlayerPawn.InputEnabled = canDrive;

        if (!canDrive)
        {
            return;
        }

        session.GameMode.UpdateRaceClock(elapsedTime);
        UpdateCheckpointProgress(session.GameMode, session.PlayerPawn.RootComponent.Position);
    }

    private void InitializeCheckpoints(CasaEngine.Framework.World.World world, RaceGameMode gameMode)
    {
        _checkpointPositions.Clear();

        foreach (Entity entity in world.Entities
                     .Where(static entity => entity.Name.StartsWith("Checkpoint.", StringComparison.Ordinal))
                     .OrderBy(static entity => entity.Name, StringComparer.Ordinal))
        {
            if (entity.RootComponent != null)
            {
                _checkpointPositions.Add(entity.RootComponent.Position);
            }
        }

        gameMode.ConfigureCheckpointCount(_checkpointPositions.Count);
    }

    private void HandlePauseToggle(RacingGameCasaEngineGame game, RaceGameMode gameMode, RacingPlayerController playerController)
    {
        bool pressed = game.InputComponent.KeyboardManager.IsKeyPressed(XnaKeys.Escape);
        if (pressed && !_pauseLatch)
        {
            gameMode.TogglePause();
            playerController.IsInputEnable = !gameMode.IsPaused;
        }

        _pauseLatch = pressed;
    }

    private void UpdateCheckpointProgress(RaceGameMode gameMode, Vector3 playerPosition)
    {
        if (_checkpointPositions.Count == 0)
        {
            return;
        }

        Vector3 checkpointPosition = _checkpointPositions[gameMode.NextCheckpointIndex];
        double checkpointDistance = Vector3.Distance(playerPosition, checkpointPosition);

        if (checkpointDistance <= CheckpointRadius && _lastCheckpointDistance > CheckpointRadius)
        {
            gameMode.RegisterCheckpointPass();
            _lastCheckpointDistance = double.MaxValue;
            return;
        }

        _lastCheckpointDistance = checkpointDistance;
    }
}