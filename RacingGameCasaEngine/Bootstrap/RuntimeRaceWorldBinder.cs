using System.Reflection;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RuntimeRaceWorldBinder
{
    private static readonly PropertyInfo GameModeProperty = typeof(World).GetProperty(
        nameof(World.GameMode),
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

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
        raceGameMode.InitGame(world);
        GameModeProperty.SetValue(world, raceGameMode);

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
        playerController.Pawn = playerPawn;
        playerPawn.Controller = playerController;
        playerPawn.InputEnabled = false;
        playerControllers.Add(playerController);

        _game.RaceSession.Bind(raceGameMode, playerController, playerPawn);
        _game.GameManager.SyncPlayerViewAssignments();
        raceGameMode.StartMatch();
    }

    private static PlayerStartComponent? TryGetPlayerStart(World world)
    {
        Entity? playerStartEntity = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.PlayerStartEntityName);
        return playerStartEntity?.GetComponent<PlayerStartComponent>();
    }
}