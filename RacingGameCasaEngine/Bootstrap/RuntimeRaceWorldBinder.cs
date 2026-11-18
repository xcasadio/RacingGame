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

        Entity playerCar = SpawnPlayerCar(world, state);

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
        playerControllers.Add(playerController);
        playerController.Possess(playerCar);

        _game.RaceSession.Bind(raceGameMode, playerController, playerCar);
        _game.GameManager.SyncPlayerViewAssignments();
        world.SetGameplayMode(raceGameMode);
    }

    /// <summary>
    /// Creates the player's car from its asset (<c>Entity.PlayerCar</c>) and sets its state. The spawned car enters the world's
    /// entity list at the start of the next <c>World.Update</c>, which <c>GameManager.UpdateWorld</c> runs right
    /// after the WorldLoaded event: before the first frame, and after the entities of the world file, as the car built in code
    /// was. Its state is set now, before it enters the world, because its visual reads the car index and colour when it is
    /// initialized.
    /// </summary>
    private static Entity SpawnPlayerCar(World world, RaceFrontEndState state)
    {
        Entity playerCar = world.SpawnEntity<Entity>(RaceWorldFactory.PlayerCarAssetName);
        playerCar.Name = RaceWorldFactory.PlayerCarEntityName;

        CarPerformanceProfile profile = RaceFrontEndCatalog.ResolveCarProfile(state.SelectedCarIndex);
        var carState = playerCar.GetComponent<RaceCarStateComponent>()!;
        carState.CarLabel = profile.Name;
        carState.TrackLabel = RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex].Name;
        carState.SelectedCarIndex = state.SelectedCarIndex;
        carState.SelectedCarColorIndex = state.SelectedCarColorIndex;
        carState.CarProfile = profile;
        carState.DrivingMode = state.SelectedDrivingMode;
        carState.TargetTopSpeedMph = profile.TargetTopSpeedMph;

        return playerCar;
    }

    private static PlayerStartComponent? TryGetPlayerStart(World world)
    {
        Entity? playerStartEntity = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.PlayerStartEntityName);
        return playerStartEntity?.GetComponent<PlayerStartComponent>();
    }
}