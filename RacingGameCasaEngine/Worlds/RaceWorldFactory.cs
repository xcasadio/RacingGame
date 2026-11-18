using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Rendering.Environment;
using CasaEngine.Framework.Rendering.Shadows;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using Color = Microsoft.Xna.Framework.Color;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Entities;
using RacingGame.Gameplay.Race;
using RacingGame.Gameplay.Track;

namespace RacingGameCasaEngine.Worlds;

public static class RaceWorldFactory
{
    internal const string FrontEndWorldName = "RacingGameCasaEngine.FrontEndWorld";
    internal const string RaceWorldNamePrefix = "RacingGameCasaEngine.RaceWorld";
    internal const string CameraEntityName = RaceWorldNames.CameraEntityName;
    internal const string PlayerStartEntityName = RaceWorldNames.PlayerStartEntityName;
    internal const string PlayerCarEntityName = RaceWorldNames.PlayerCarEntityName;
    internal const string CheckpointEntityNamePrefix = RaceWorldNames.CheckpointEntityNamePrefix;
    internal const string TrackRoadEntityNamePrefix = RaceWorldNames.TrackRoadEntityNamePrefix;
    internal const string TrackGroundEntityNamePrefix = RaceWorldNames.TrackGroundEntityNamePrefix;
    internal const string TrackSceneryEntityNamePrefix = RaceWorldNames.TrackSceneryEntityNamePrefix;
    internal const string TrackGuardRailEntityNamePrefix = RaceWorldNames.TrackGuardRailEntityNamePrefix;
    internal const string TrackGuardRailHolderEntityNamePrefix = RaceWorldNames.TrackGuardRailHolderEntityNamePrefix;
    internal const string TrackColumnsEntityNamePrefix = RaceWorldNames.TrackColumnsEntityNamePrefix;
    internal const string TrackColumnSegmentEntityNamePrefix = RaceWorldNames.TrackColumnSegmentEntityNamePrefix;

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

        foreach (Entity entity in RaceWorldTemplates.CreateFrontEndEntities())
        {
            world.AddEntity(entity);
        }

        return world;
    }

    internal static World CreateRaceWorld(RacingGameCasaEngineGame game, RaceFrontEndState state)
    {
        TrackDefinition track = RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex];
        CarDefinition car = RaceFrontEndCatalog.Cars[state.SelectedCarIndex];

        var world = new World
        {
            Name = $"{RaceWorldNamePrefix}.{track.Name}",
        };

        // The environment first, then the entities in the order the template gives; the player's car comes last.
        RaceWorldTemplates.ApplyRaceEnvironment(world.EnvironmentSettings, state.EnableShadows, game.UseLinearColorPipeline);

        foreach (Entity entity in RaceWorldTemplates.CreateRaceEntities(track.Name, castShadows: state.EnableShadows))
        {
            world.AddEntity(entity);
        }

        world.AddEntity(CreatePlayerCarEntity(state, car, track));

        return world;
    }

    private static Entity CreatePlayerCarEntity(RaceFrontEndState state, CarDefinition car, TrackDefinition track)
    {
        var profile = car.PerformanceProfile;
        Entity playerCar = RaceCarEntityFactory.Create();
        playerCar.Name = PlayerCarEntityName;

        var carState = playerCar.GetComponent<RaceCarStateComponent>()!;
        carState.CarLabel = car.Name;
        carState.TrackLabel = track.Name;
        carState.SelectedCarIndex = state.SelectedCarIndex;
        carState.SelectedCarColorIndex = state.SelectedCarColorIndex;
        carState.CarProfile = profile;
        carState.DrivingMode = state.SelectedDrivingMode;
        carState.TargetTopSpeedMph = profile.TargetTopSpeedMph;

        return playerCar;
    }
}