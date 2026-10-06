using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Entities;
using Color = Microsoft.Xna.Framework.Color;

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

    // The implicit lighting the old CasaEngine applied to every view (CasaEngine 295db0c6,
    // EnvironmentLightingResolver.CreateLegacyLighting). CasaEngine now lights a world only through its
    // LightComponents (CasaEngine 604c2fbea). Each old color is a gray level: it becomes the intensity of a white
    // light, so the shader receives the same values (ForwardLightBinder multiplies both colors by the intensity).
    private const string KeyLightName = "Light.Key";

    // Shadow map of the race world, tuned on the track audit views (ai-agent/tasks/rgce-hud-shadows-start-tasks.md, T3.1):
    // - 4096 texels over 2 x 150 units: a texel of 0.07 unit instead of 0.2 with the engine's 1024 over 2 x 100, so road and
    //   car shadow edges step far less, and shadows reach about as far as RacingGame's visible range of 129 units;
    // - normal bias 0.4 unit: removes the acne of the terrain lit at grazing angles, and shrinks cast shadows little;
    // - depth bias: the engine's 0.001.
    // Measured uncapped in 1920x1080: 1.6 ms per frame, as with the engine's settings (1.4 ms without shadows). The map
    // takes about 128 MB of video memory instead of 8.
    private const int ShadowMapResolution = 4096;
    private const float ShadowMaxDistance = 150f;
    private const float ShadowDepthBias = 0.001f;
    private const float ShadowNormalBias = 0.4f;

    private static readonly (string Name, Vector3 Direction, float Intensity, bool HasSpecular)[] LegacyDirectionalLights =
    [
        (KeyLightName, new Vector3(-0.5265408f, -0.5735765f, -0.6275069f), 0.92f, true),
        ("Light.Fill", new Vector3(0.7198464f, 0.3420201f, 0.6040227f), 0.71f, false),
        ("Light.Back", new Vector3(0.4545195f, -0.7660444f, 0.4545195f), 0.36f, true),
    ];

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

        // The Shadows option: the race world renders shadow maps, cast by its key light.
        world.EnvironmentSettings.Shadows.Enabled = state.EnableShadows;
        world.EnvironmentSettings.Shadows.Resolution = ShadowMapResolution;
        world.EnvironmentSettings.Shadows.MaxDistance = ShadowMaxDistance;
        world.EnvironmentSettings.Shadows.DepthBias = ShadowDepthBias;
        world.EnvironmentSettings.Shadows.NormalBias = ShadowNormalBias;
        foreach (Entity entity in CreateLegacyLightEntities(keyLightCastsShadows: state.EnableShadows))
        {
            world.AddEntity(entity);
        }

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
        cameraEntity.ApplyExplicitPolicies(EntityPolicySet.DynamicDefault);

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

    private static IEnumerable<Entity> CreateLegacyLightEntities(bool keyLightCastsShadows)
    {
        foreach (var light in LegacyDirectionalLights)
        {
            var entity = new Entity
            {
                Name = light.Name,
                RootComponent = new LightComponent
                {
                    Type = LightType.Directional,
                    LocalOrientation = CreateOrientationFromForward(light.Direction),
                    Color = Color.White,
                    SpecularColor = light.HasSpecular ? Color.White : Color.Black,
                    Intensity = light.Intensity,
                    CastShadows = keyLightCastsShadows && light.Name == KeyLightName,
                },
            };
            entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);
            yield return entity;
        }
    }

    // A LightComponent shines along its Forward. Same as CasaEngine.Demos DemoSceneLightRig.CreateOrientationFromForward.
    internal static Quaternion CreateOrientationFromForward(Vector3 forward)
    {
        forward = Vector3.Normalize(forward);
        float dot = Math.Clamp(Vector3.Dot(Vector3.Forward, forward), -1.0f, 1.0f);
        if (dot >= 0.9999f)
        {
            return Quaternion.Identity;
        }

        if (dot <= -0.9999f)
        {
            return Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi);
        }

        Vector3 axis = Vector3.Normalize(Vector3.Cross(Vector3.Forward, forward));
        return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(dot));
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
        entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);

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
        entity.ApplyExplicitPolicies(new EntityPolicySet(
            Mobility.Static,
            TickPolicy.EveryFrame,
            SpatialPolicy.StaticIndex,
            RenderDynamicPolicy.Static));

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
        entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);

        entity.RootComponent!.LocalPosition = checkpointTrigger.Position;
        entity.RootComponent.LocalOrientation = checkpointTrigger.Orientation;
        return entity;
    }

    private static RacingCarPawn CreatePlayerCarEntity(RaceFrontEndState state, CarDefinition car, TrackDefinition track)
    {
        var profile = car.PerformanceProfile;
        var pawn = new RacingCarPawn
        {
            Name = PlayerCarEntityName,
            CarLabel = car.Name,
            TrackLabel = track.Name,
            SelectedCarIndex = state.SelectedCarIndex,
            SelectedCarColorIndex = state.SelectedCarColorIndex,
            CarProfile = profile,
            DrivingMode = state.SelectedDrivingMode,
            TargetTopSpeedMph = profile.TargetTopSpeedMph,
        };

        return pawn;
    }
}