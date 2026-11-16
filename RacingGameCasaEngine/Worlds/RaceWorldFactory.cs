using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Rendering.Environment;
using CasaEngine.Framework.Rendering.Shadows;
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

    // RacingGame lights a pixel as texture * (A + D * N.L) with ONE directional light and the ambient (A) and diffuse (D)
    // constants of each material. The direction towards the light is normalize(8500, -7250, 15000) in the original's Z-up
    // space (Core/Graphics/LensFlare.cs:45-47, DefaultLightPos). The port's axes are (x, y, z) -> (x, z, -y), so the
    // direction towards the light is (8500, 15000, 7250) and the light travels along its opposite.
    //
    // The engine renders texture * DiffuseColor * (N.L * light + AmbientColor * MaterialAmbientColor) + emissive
    // (docs/engine/lit-ambient-and-ambient-source.md). With a white light of intensity 1 (ForwardLightBinder sends
    // Color * Intensity, no other factor) and a flat world ambient of 1, a material with DiffuseColor = D and
    // MaterialAmbientColor = A / D renders texture * (D * N.L + A), as the original does.
    private const string KeyLightName = "Light.Key";
    private static readonly Vector3 KeyLightDirection = Vector3.Normalize(new Vector3(-8500f, -15000f, -7250f));

    // The key light's specular was 0.92 (white specular colour x intensity 0.92 before this light became the only one,
    // with intensity 1); the specular term is out of the lighting identity and stays as it was.
    private static readonly Color KeyLightSpecularColor = new(235, 235, 235);

    // Shadow map of the race world, tuned on the track audit views (ai-agent/tasks/rgce-hud-shadows-start-tasks.md, T3.1;
    // ai-agent/tasks/rgce-3d-lighting-shadows-tasks.md, S2b T3):
    // - the original multiplies the whole image by a shadow colour of (0.25, 0.26, 0.27) (ShadowMap.fx:36, 240) and softens
    //   the shadow in screen space: MultiplyLitColor with that colour, and a 3 texel filter radius for the soft edge (2 left the
    //   edge sharper than the original, 5 showed the 12-tap pattern as grain on the boards);
    // - 4096 texels over 2 x ShadowMaxDistance units, centred on the camera. The original's map covers about +-42 x +-65 m
    //   in the light's plane around a point 21.5 m ahead of the car (ShadowMapShader.cs:212-261), and the camera of a view
    //   is about 40 m from it: 100 units leaves the probed shadows of the bench views inside the full-strength zone
    //   (docs in the plan, T3 result), and a texel of 0.05 unit steps road and car shadow edges far less than 0.2;
    // - fade over the last 15 units, so the end of the map does not show as an edge;
    // - depth range 150, apart from the lateral extent: the depth bias in metres is 2 x DepthBias x (2 x DepthRange - 0.1)
    //   on DesktopGL (docs/engine/shadow-settings.md), 0.6 m with 150 and 0.001, as when both were 150;
    // - normal bias 0.1 unit: the terrain only receives shadows, as in RacingGame, so it has no acne at grazing angles, and
    //   a small bias keeps cast shadows at their full width
    //   (ai-agent/tasks/archive/rgce-start-beeps-shadow-shimmer-tasks.md, T2.2);
    // - depth bias: the engine's 0.001.
    // Measured uncapped in 1920x1080 with the 4-tap filter (before the 12-tap filter of FilterRadius, not measured): 1.6 ms
    // per frame, as with the engine's settings (1.4 ms without shadows). The map
    // takes about 128 MB of video memory instead of 8.
    private const int ShadowMapResolution = 4096;
    private const float ShadowMaxDistance = 100f;
    private const float ShadowFadeDistance = 15f;
    private const float ShadowDepthRange = 150f;
    private const float ShadowFilterRadius = 3f;
    private static readonly Vector3 ShadowColor = new(0.25f, 0.26f, 0.27f);
    private const float ShadowDepthBias = 0.001f;
    private const float ShadowNormalBias = 0.1f;

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
        world.EnvironmentSettings.Shadows.FadeDistance = ShadowFadeDistance;
        world.EnvironmentSettings.Shadows.DepthRange = ShadowDepthRange;
        world.EnvironmentSettings.Shadows.FilterRadius = ShadowFilterRadius;
        world.EnvironmentSettings.Shadows.Mode = ShadowMode.MultiplyLitColor;
        world.EnvironmentSettings.Shadows.ShadowColor = ShadowColor;
        world.EnvironmentSettings.Shadows.DepthBias = ShadowDepthBias;
        world.EnvironmentSettings.Shadows.NormalBias = ShadowNormalBias;
        // Flat ambient of 1, whatever the reflection cube of the world (Bootstrap/RacingGameCasaEngineGame.cs binds it to
        // the race world): the cube keeps driving the reflections only.
        world.EnvironmentSettings.AmbientSource = AmbientSource.Flat;
        world.EnvironmentSettings.AmbientColor = Vector3.One;
        world.EnvironmentSettings.AmbientIntensity = 1f;
        if (game.UseLinearColorPipeline)
        {
            ConfigureLinearColorPipeline(world.EnvironmentSettings.ColorPipeline);
            world.EnvironmentSettings.MarkDirty();
        }

        world.AddEntity(CreateKeyLightEntity(castShadows: state.EnableShadows));

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

    // Linear HDR with an ACES tonemapper and FXAA, as CasaEngine.Demos PbrMaterialDemo. The brightness is set by the exposure
    // (the original's global 7 % darkening is not reproduced, A11); bloom and SSAO stay off.
    private static void ConfigureLinearColorPipeline(ColorPipelineSettings colorPipeline)
    {
        colorPipeline.Mode = ColorPipelineMode.LinearHdr;
        colorPipeline.Tonemapper = TonemapOperator.AcesApprox;
        colorPipeline.Exposure = 1.0f;
        colorPipeline.BloomEnabled = false;
        colorPipeline.SsaoEnabled = false;
        colorPipeline.AntiAliasing = AntiAliasingMode.Fxaa;
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

    private static Entity CreateKeyLightEntity(bool castShadows)
    {
        var entity = new Entity
        {
            Name = KeyLightName,
            RootComponent = new LightComponent
            {
                Type = LightType.Directional,
                LocalOrientation = CreateOrientationFromForward(KeyLightDirection),
                Color = Color.White,
                SpecularColor = KeyLightSpecularColor,
                Intensity = 1f,
                CastShadows = castShadows,
            },
        };
        entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);
        return entity;
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