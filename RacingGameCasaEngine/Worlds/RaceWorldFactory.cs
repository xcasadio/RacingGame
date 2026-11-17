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

    // RacingGame lights the world with ONE white directional light. The direction towards the light is
    // normalize(8500, -7250, 15000) in the original's Z-up space (Core/Graphics/LensFlare.cs:45-47, DefaultLightPos). The
    // port's axes are (x, y, z) -> (x, z, -y), so the direction towards the light is (8500, 15000, 7250) and the light
    // travels along its opposite.
    private const string KeyLightName = "Light.Key";
    private static readonly Vector3 KeyLightDirection = Vector3.Normalize(new Vector3(-8500f, -15000f, -7250f));

    // The specular colour of the key light, as it was when this light was one of three (white specular colour x intensity
    // 0.92). The engine's lit-pbr shader ignores the specular colour of a light (docs/engine/pbr-rendering.md, Units): it is
    // kept for the legacy shaders that read it.
    private static readonly Color KeyLightSpecularColor = new(235, 235, 235);

    // pi: a lit-pbr surface lit head-on is pi times darker than a lit-diffuse one at the same intensity (CasaEngine
    // docs/engine/pbr-rendering.md, Units), so the key light of a PBR world keeps the level the original had at intensity 1.
    private const float KeyLightIntensity = MathF.PI;

    // The ambient colour of the race world is 0.05, the engine's default (WorldEnvironmentSettings.ResetToDefaults), written out
    // here. In LinearHdr the engine decodes the ambient colour before it multiplies the irradiance of the sky cube in lit-pbr
    // (CasaEngine ResolvedEnvironmentSettings.cs:106-111), so 0.05 displays as about 0.0014 linear; it is 0.05 only in the legacy
    // mode. The white of the other machine took the ambient to 100 % of that irradiance and made the world brighter than the
    // original. The author chose 0.05 on 2026-11-17, after the measurement of open point O13
    // (ai-agent/tasks/rgce-pbr-handover-tasks.md, phase 13); a Flat ambient of 0.25 to 0.35 was measured again in R3 and not
    // retained (docs/decisions/0032-*.md).
    private static readonly Vector3 AmbientColor = new(0.05f);

    // Exposure of the linear pipeline with the ACES approximation. 0.9 was measured against the original's 15 reference views
    // (ai-agent/tasks/rgce-render-gaps-tasks.md, R3; docs/decisions/0032-*.md): at 1.0 the mean luminance was 104.0 for 96.3 and
    // the mean difference per channel 19.41; at 0.9 they are 98.7 and 17.95, and the lit and away probes that pass go from 16 to
    // 17 of 34.
    private const float LinearExposure = 0.9f;

    // Shadow map of the race world, tuned on the track audit views (ai-agent/tasks/rgce-hud-shadows-start-tasks.md, T3.1):
    // - the original multiplies the whole image by a shadow colour of (0.25, 0.26, 0.27) (Core/Content/Shaders/ShadowMap.fx:36,
    //   240) and softens the shadow in screen space: MultiplyLitColor with that colour, and a 3 texel filter radius for the
    //   soft edge (2 left the edge sharper than the original, 5 showed the 12-tap pattern as grain on the boards: the other
    //   machine's measurement, not repeated here);
    // - 4096 texels over 2 x ShadowMaxDistance units, centred on the camera. The original's map is centred on a point
    //   virtualVisibleRange / 6 = 21.5 m ahead of the car and its visible range is 5.5 x 23.5 = 129.25 m
    //   (Core/Shaders/ShadowMapShader.cs:215, 250-261), which covers about +-42 m across the light and +-80 m along it
    //   (scripts/TrackCaptureCompare/README.md); the camera of a bench view is about 40 m from that point, so 100 units
    //   leaves the probed shadows inside the full-strength zone, and a texel of 0.05 unit steps road and car shadow edges
    //   far less than 0.2;
    // - fade over the last 15 units, so the end of the map does not show as an edge;
    // - depth range 150, apart from the lateral extent: the depth bias in metres is 2 x DepthBias x (2 x DepthRange - 0.1)
    //   on DesktopGL (CasaEngine docs/engine/shadow-settings.md), 0.6 m with 150 and 0.001, as when both were 150;
    // - normal bias 0.1 unit: the terrain only receives shadows, as in RacingGame, so it has no acne at grazing angles, and
    //   a small bias keeps cast shadows at their full width
    //   (ai-agent/tasks/archive/rgce-start-beeps-shadow-shimmer-tasks.md, T2.2);
    // - depth bias: the engine's 0.001.
    // Measured uncapped in 1920x1080 with a 150 unit map and no filter (ai-agent/tasks/rgce-hud-shadows-start-tasks.md, T3.1):
    // 1.6 ms per frame, as with the engine's settings (1.4 ms without shadows). The map takes about 128 MB of video memory
    // instead of 8. The 12-tap filter of FilterRadius has not been timed here.
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
        world.AddEntity(CreateRaceRootEntity(trackScene.TrackAssetId, trackScene.HeightmapAssetId));

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

        // Image-based lighting from the sky cube (Bootstrap/RacingGameCasaEngineGame.cs binds it to the race world as its
        // specular environment cube): the cube gives both the ambient diffuse (irradiance) and the reflections of the PBR
        // materials, as CasaEngine.Demos PbrMaterialDemo.ApplyIbl. Same in both colour pipeline modes. The values are written
        // out so that the world does not depend on the engine's defaults; the ambient colour is AmbientColor.
        world.EnvironmentSettings.AmbientSource = AmbientSource.Auto;
        world.EnvironmentSettings.AmbientColor = AmbientColor;
        world.EnvironmentSettings.AmbientIntensity = 1f;
        world.EnvironmentSettings.SpecularIntensity = 1f;

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
    // (the original's global 7 % darkening is not reproduced); bloom and SSAO stay off.
    private static void ConfigureLinearColorPipeline(ColorPipelineSettings colorPipeline)
    {
        colorPipeline.Mode = ColorPipelineMode.LinearHdr;
        colorPipeline.Tonemapper = TonemapOperator.AcesApprox;
        colorPipeline.Exposure = LinearExposure;
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

        var cameraComponent = new CameraLookAtComponent { Name = nameof(CameraLookAtComponent) };
        cameraComponent.SetPositionAndTarget(new Vector3(0f, 6f, -18f), Vector3.Zero);
        cameraEntity.RootComponent = cameraComponent;

        if (enableChaseCamera)
        {
            cameraEntity.AddComponent(new ChaseCameraRigComponent { Name = nameof(ChaseCameraRigComponent) });
            cameraEntity.AddComponent(new DebugFreeCameraComponent { Name = nameof(DebugFreeCameraComponent) });
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
                Name = nameof(LightComponent),
                Type = LightType.Directional,
                LocalOrientation = CreateOrientationFromForward(KeyLightDirection),
                Color = Color.White,
                SpecularColor = KeyLightSpecularColor,
                Intensity = KeyLightIntensity,
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
            RootComponent = new PlayerStartComponent { Name = nameof(PlayerStartComponent) },
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

    private static Entity CreateRaceRootEntity(Guid trackAssetId, Guid heightmapAssetId)
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

        entity.AddComponent(new RaceTrackPhysicsComponent
        {
            Name = nameof(RaceTrackPhysicsComponent),
            TrackAssetId = trackAssetId,
            HeightmapAssetId = heightmapAssetId,
        });
        entity.AddComponent(new RaceFlowCoordinatorComponent { Name = nameof(RaceFlowCoordinatorComponent) });
        entity.AddComponent(new RaceCourseDebugComponent { Name = nameof(RaceCourseDebugComponent) });
        return entity;
    }

    private static Entity CreateCheckpointEntity(string name, RaceCheckpointTriggerDefinition checkpointTrigger)
    {
        var entity = new Entity
        {
            Name = name,
            RootComponent = new RaceCheckpointTriggerComponent
            {
                Name = nameof(RaceCheckpointTriggerComponent),
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