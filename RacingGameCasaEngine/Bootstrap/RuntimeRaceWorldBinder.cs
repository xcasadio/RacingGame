using System.Reflection;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Worlds;
using RacingGame.Gameplay.Cars;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Entities;
using RacingGame.Gameplay.GameFramework;
using RacingGame.Gameplay.Race;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RuntimeRaceWorldBinder
{
    private static readonly FieldInfo PlayerControllersField = typeof(World).GetField(
        "_playerControllers",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly RacingGameCasaEngineGame _game;

    public RuntimeRaceWorldBinder(RacingGameCasaEngineGame game)
    {
        _game = game;
    }

    public void BindCurrentWorld(RaceFrontEndState state)
    {
        World? world = _game.GameManager.CurrentWorld;
        if (world == null || !RaceWorldFactory.IsRaceWorld(world))
        {
            _game.RaceSession.Clear();
            return;
        }

        Entity? playerCar = world.Entities.FirstOrDefault(entity => entity.GetComponent<RaceCarStateComponent>() != null);
        if (playerCar == null)
        {
            _game.RaceSession.Clear();
            return;
        }

        var raceGameMode = new RaceGameMode();
        raceGameMode.Configure(state);

        if (TryGetPlayerStart(world) is { } playerStart
            && playerCar.RootComponent != null)
        {
            playerCar.RootComponent.CopyLocalTransformFrom(playerStart.LocalTransform);
        }

        var playerControllers = (List<PlayerController>)PlayerControllersField.GetValue(world)!;
        playerControllers.Clear();

        var playerController = new RacingPlayerController();
        playerController.Configure(state);
        playerController.IsInputEnable = false;
        playerController.Player = new LocalPlayer
        {
            ControllerId = PlayerIndex.One,
        };
        CarPerformanceProfile selectedProfile = RaceFrontEndCatalog.ResolveCarProfile(state.SelectedCarIndex);
        var carState = playerCar.GetComponent<RaceCarStateComponent>()!;
        carState.CarProfile = selectedProfile;
        carState.TargetTopSpeedMph = selectedProfile.TargetTopSpeedMph;
        carState.CarLabel = selectedProfile.Name;
        carState.DrivingMode = state.SelectedDrivingMode;
        playerControllers.Add(playerController);
        playerController.Possess(playerCar);

        _game.RaceSession.Bind(raceGameMode, playerController, playerCar);
        _game.GameManager.SyncPlayerViewAssignments();
        world.SetGameplayMode(raceGameMode);
    }

    private static PlayerStartComponent? TryGetPlayerStart(World world)
    {
        Entity? playerStartEntity = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.PlayerStartEntityName);
        return playerStartEntity?.GetComponent<PlayerStartComponent>();
    }
}