using System.Reflection;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Gameplay;
using RacingGameCasaEngine.Worlds;

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

        RacingCarPawn? playerPawn = world.Entities.OfType<RacingCarPawn>().FirstOrDefault();
        if (playerPawn == null)
        {
            _game.RaceSession.Clear();
            return;
        }

        var raceGameMode = new RaceGameMode();
        raceGameMode.Configure(state);

        if (TryGetPlayerStart(world) is { } playerStart
            && playerPawn.RootComponent != null)
        {
            playerPawn.RootComponent.Coordinates.CopyFrom(playerStart.Coordinates);
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
        playerPawn.CarProfile = selectedProfile;
        playerPawn.TargetTopSpeedMph = selectedProfile.TargetTopSpeedMph;
        playerPawn.CarLabel = selectedProfile.Name;
        playerPawn.DrivingMode = state.SelectedDrivingMode;
        playerControllers.Add(playerController);
        playerController.Possess(playerPawn);

        _game.RaceSession.Bind(raceGameMode, playerController, playerPawn);
        _game.GameManager.SyncPlayerViewAssignments();
        world.SetGameplayMode(raceGameMode);
    }

    private static PlayerStartComponent? TryGetPlayerStart(World world)
    {
        Entity? playerStartEntity = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.PlayerStartEntityName);
        return playerStartEntity?.GetComponent<PlayerStartComponent>();
    }
}