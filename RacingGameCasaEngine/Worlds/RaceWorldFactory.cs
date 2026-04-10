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
    internal const string CheckpointEntityNamePrefix = "Checkpoint.";
    internal const string TrackRoadEntityNamePrefix = "Track.Road.";
    internal const string TrackGroundEntityNamePrefix = "Track.Ground.";
    internal const string TrackSceneryEntityNamePrefix = "Track.Scenery.";
    internal const string TrackGuardRailEntityNamePrefix = "Track.GuardRail.";
    internal const string TrackGuardRailHolderEntityNamePrefix = "Track.GuardRailHolder.";
    internal const string TrackColumnsEntityNamePrefix = "Track.Columns.";
    internal const string TrackColumnSegmentEntityNamePrefix = "Track.ColumnSegment.";

    internal static bool IsRaceWorld(World world)
    {
        return world.Name?.StartsWith(RaceWorldNamePrefix, StringComparison.Ordinal) == true;
    }

    internal static bool IsRaceRenderableEntity(Entity entity)
    {
        string? entityName = entity.Name;
        return entityName?.StartsWith(TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackGroundEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackSceneryEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackGuardRailEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackGuardRailHolderEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackColumnsEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackColumnSegmentEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(CheckpointEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName == PlayerStartEntityName
            || entityName == PlayerCarEntityName;
    }

    internal static bool IsVisibleInCircuitOnlyView(Entity entity)
    {
        return entity.Name?.StartsWith(TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true;
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

    internal static World CreateRaceWorld(RacingGameCasaEngineGame game, RaceFrontEndState state)
    {
        TrackDefinition track = RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex];
        CarDefinition car = RaceFrontEndCatalog.Cars[state.SelectedCarIndex];
        RaceTrackScene trackScene = LegacyTrackSceneFactory.Create(track.Name, game.AssetContentManager);

        var world = new World
        {
            Name = $"{RaceWorldNamePrefix}.{track.Name}",
        };

        world.AddEntity(CreateCameraEntity(enableChaseCamera: true));
        world.AddEntity(CreateRaceRootEntity(trackScene.PhysicsProfile));

        foreach (Entity entity in trackScene.TrackEntities)
        {
            world.AddEntity(entity);
        }

        foreach (Entity entity in trackScene.SceneryEntities)
        {
            world.AddEntity(entity);
        }

        for (int index = 0; index < trackScene.CheckpointTriggers.Count; index++)
        {
            world.AddEntity(CreateCheckpointEntity($"Checkpoint.{index + 1:00}", trackScene.CheckpointTriggers[index]));
        }

        world.AddEntity(CreatePlayerStartEntity(trackScene.PlayerStartPose));
        world.AddEntity(CreatePlayerCarEntity(state, car, track));

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
            cameraEntity.AddComponent(new DebugFreeCameraComponent());
        }

        return cameraEntity;
    }

    private static Entity CreatePlayerStartEntity()
    {
        return CreatePlayerStartEntity(new Vector3(0f, 0f, -4f));
    }

    private static Entity CreatePlayerStartEntity(RaceTrackStartPose startPose)
    {
        Entity entity = CreatePlayerStartEntity(startPose.Position);
        entity.RootComponent!.LocalOrientation = startPose.Orientation;
        return entity;
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

    private static Entity CreateRaceRootEntity(RaceTrackPhysicsProfile physicsProfile)
    {
        var entity = new Entity
        {
            Name = "RaceRoot",
        };

        entity.AddComponent(new RaceTrackPhysicsComponent(physicsProfile));
        entity.AddComponent(new RaceFlowCoordinatorComponent());
        entity.AddComponent(new RaceCourseDebugComponent());
        return entity;
    }

    private static Entity CreateCheckpointEntity(string name, RaceCheckpointTriggerDefinition checkpointTrigger)
    {
        var entity = new Entity
        {
            Name = name,
            RootComponent = new RaceCheckpointTriggerComponent
            {
                HalfWidth = checkpointTrigger.HalfWidth,
                HalfHeight = checkpointTrigger.HalfHeight,
                HalfDepth = checkpointTrigger.HalfDepth,
            },
        };

        entity.RootComponent!.LocalPosition = checkpointTrigger.Position;
        entity.RootComponent.LocalOrientation = checkpointTrigger.Orientation;
        return entity;
    }

    private static RacingCarPawn CreatePlayerCarEntity(RaceFrontEndState state, CarDefinition car, TrackDefinition track)
    {
        var pawn = new RacingCarPawn
        {
            Name = PlayerCarEntityName,
            CarLabel = car.Name,
            TrackLabel = track.Name,
            SelectedCarIndex = state.SelectedCarIndex,
            SelectedCarColorIndex = state.SelectedCarColorIndex,
        };

        return pawn;
    }
}