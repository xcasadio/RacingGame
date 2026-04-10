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
    private readonly List<RaceCheckpointTriggerComponent> _checkpointTriggers = [];
    private bool _isInitialized;
    private bool _hasPreviousPlayerPosition;
    private Vector3 _previousPlayerPosition;

    public override EntityComponent Clone()
    {
        return new RaceFlowCoordinatorComponent();
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
            _hasPreviousPlayerPosition = false;
            return;
        }

        session.GameMode.UpdateRaceClock(elapsedTime);

        Vector3 playerPosition = session.PlayerPawn.RootComponent.Position;
        if (!_hasPreviousPlayerPosition)
        {
            _previousPlayerPosition = playerPosition;
            _hasPreviousPlayerPosition = true;
            return;
        }

        UpdateCheckpointProgress(session.GameMode, _previousPlayerPosition, playerPosition);
        _previousPlayerPosition = playerPosition;
    }

    private void InitializeCheckpoints(World world, RaceGameMode gameMode)
    {
        _checkpointTriggers.Clear();
        _hasPreviousPlayerPosition = false;

        foreach (Entity entity in world.Entities
                     .Where(static entity => entity.Name.StartsWith("Checkpoint.", StringComparison.Ordinal))
                     .OrderBy(static entity => entity.Name, StringComparer.Ordinal))
        {
            RaceCheckpointTriggerComponent? checkpointTrigger = entity.GetComponent<RaceCheckpointTriggerComponent>();
            if (checkpointTrigger != null)
            {
                _checkpointTriggers.Add(checkpointTrigger);
            }
        }

        gameMode.ConfigureCheckpointCount(_checkpointTriggers.Count);
    }

    private void HandlePauseToggle(RacingGameCasaEngineGame game, RaceGameMode gameMode, RacingPlayerController playerController)
    {
        bool pressed = game.InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.Escape);
        if (!pressed && playerController.Player is LocalPlayer localPlayer)
        {
            CasaEngine.Engine.Input.GamePad playerGamePad = game.InputComponent.GamePadManager.GetGamePad(localPlayer.ControllerId);
            pressed = playerGamePad.IsConnected && (playerGamePad.StartJustPressed || playerGamePad.BackJustPressed);
        }

        if (!pressed)
        {
            return;
        }

        gameMode.TogglePause();
        playerController.IsInputEnable = !gameMode.IsPaused;
    }

    private void UpdateCheckpointProgress(RaceGameMode gameMode, Vector3 previousPlayerPosition, Vector3 playerPosition)
    {
        if (_checkpointTriggers.Count == 0)
        {
            return;
        }

        RaceCheckpointTriggerComponent checkpointTrigger = _checkpointTriggers[gameMode.NextCheckpointIndex];
        if (checkpointTrigger.IsTriggered(previousPlayerPosition, playerPosition))
        {
            gameMode.RegisterCheckpointPass();
        }
    }
}