using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Entities;

namespace RacingGameCasaEngine.Worlds;

public static class RaceWorldFactory
{
    internal const string FrontEndWorldName = "RacingGameCasaEngine.FrontEndWorld";
    internal const string RaceWorldNamePrefix = "RacingGameCasaEngine.RaceWorld";
    internal const string CameraEntityName = "BootstrapCamera";
    internal const string PlayerStartEntityName = "PlayerStart";
    internal const string PlayerCarEntityName = "PlayerCar";

    internal static bool IsRaceWorld(World world)
    {
        return world.Name?.StartsWith(RaceWorldNamePrefix, StringComparison.Ordinal) == true;
    }

    internal static World CreateFrontEndWorld()
    {
        var world = new World
        {
            Name = FrontEndWorldName,
        };

        world.AddEntity(CreateCameraEntity(enableChaseCamera: false));
        world.AddEntity(CreatePlayerStartEntity());

        return world;
    }

    internal static World CreateRaceWorld(RaceFrontEndState state)
    {
        TrackDefinition track = RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex];
        CarDefinition car = RaceFrontEndCatalog.Cars[state.SelectedCarIndex];
        RaceTrackScene trackScene = LegacyTrackSceneFactory.Create(track.Name);

        var world = new World
        {
            Name = $"{RaceWorldNamePrefix}.{track.Name}",
        };

        world.AddEntity(CreateCameraEntity(enableChaseCamera: true));
        world.AddEntity(CreateRaceRootEntity());
        foreach (Entity entity in trackScene.Entities)
        {
            world.AddEntity(entity);
        }

        for (int index = 0; index < trackScene.CheckpointPositions.Count; index++)
        {
            world.AddEntity(CreateCheckpointEntity($"Checkpoint.{index + 1:00}", trackScene.CheckpointPositions[index]));
        }

        world.AddEntity(CreatePlayerStartEntity(trackScene.PlayerStartPosition));
        world.AddEntity(CreatePlayerCarEntity(car, track));

        return world;
    }

    private static Entity CreateCameraEntity(bool enableChaseCamera)
    {
        var cameraEntity = new Entity
        {
            Name = CameraEntityName,
        };

        var cameraComponent = new CameraLookAtComponent();
        cameraComponent.SetPositionAndTarget(new Vector3(0f, 6f, -18f), Vector3.Zero);
        cameraEntity.RootComponent = cameraComponent;

        if (enableChaseCamera)
        {
            cameraEntity.AddComponent(new ChaseCameraRigComponent());
        }

        return cameraEntity;
    }

    private static Entity CreatePlayerStartEntity()
    {
        return CreatePlayerStartEntity(new Vector3(0f, 0f, -4f));
    }

    private static Entity CreatePlayerStartEntity(Vector3 position)
    {
        var entity = new Entity
        {
            Name = PlayerStartEntityName,
            RootComponent = new PlayerStartComponent(),
        };

        entity.RootComponent!.LocalPosition = position;
        return entity;
    }

    private static Entity CreateNamedEntity(string name)
    {
        return new Entity
        {
            Name = name,
        };
    }

    private static Entity CreateRaceRootEntity()
    {
        var entity = new Entity
        {
            Name = "RaceRoot",
        };

        entity.AddComponent(new RaceFlowCoordinatorComponent());
        entity.AddComponent(new RaceCourseDebugComponent());
        return entity;
    }

    private static Entity CreateCheckpointEntity(string name, Vector3 position)
    {
        var entity = new Entity
        {
            Name = name,
            RootComponent = new PlayerStartComponent(),
        };

        entity.RootComponent!.LocalPosition = position;
        return entity;
    }

    private static RacingCarPawn CreatePlayerCarEntity(CarDefinition car, TrackDefinition track)
    {
        var pawn = new RacingCarPawn
        {
            Name = PlayerCarEntityName,
            CarLabel = car.Name,
            TrackLabel = track.Name,
        };

        return pawn;
    }
}