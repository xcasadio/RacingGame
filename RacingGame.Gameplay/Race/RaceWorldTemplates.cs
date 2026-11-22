using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Rendering.Environment;
using CasaEngine.Framework.Rendering.Shadows;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Race;

/// <summary>
/// The entities and the environment settings of the game's worlds (the front end, a race without its car, the fixed part of
/// the car selection carousel), built in the gameplay assembly so that the game and the tool that writes the worlds as
/// assets build the same ones. Nothing here reads the player's options: the callers pass them.
/// </summary>
internal static class RaceWorldTemplates
{
    /// <summary>The number of plates of the car selection carousel, one per car.</summary>
    internal const int CarSelectionPlateCount = 3;

    // RacingGame lights the world with ONE white directional light. The direction towards the light is
    // normalize(8500, -7250, 15000) in the original's Z-up space (Core/Graphics/LensFlare.cs:45-47, DefaultLightPos). The
    // port's axes are (x, y, z) -> (x, z, -y), so the direction towards the light is (8500, 15000, 7250) and the light
    // travels along its opposite.
    private const string KeyLightName = RaceWorldNames.KeyLightEntityName;
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

    // Shadow of the race world (ADR-0045): cast in perspective from a virtual light, as the original does
    // (RacingGameOriginal Core/Shaders/ShadowMapShader.cs:208-266, engine ADR-0147), and no longer from an orthographic square
    // centred on the camera.
    // - The virtual light is 129.25 m (5.5 x 23.5) from the aimed point, against the direction of the key light; the map
    //   covers atan2(129.25, 171.6) x 129.25 = 83.44 m by 129.25 m at that point, between the planes 1 and 218.75. The aimed
    //   point is 129.25 / 6 m ahead of the player's car: RaceShadowFocusComponent sets it every frame.
    // - The original multiplies the whole image by a shadow colour of (0.25, 0.26, 0.27) (Core/Content/Shaders/ShadowMap.fx:36,
    //   240): MultiplyLitColor with that hue (level: see ShadowColor).
    // - 2048 texels: a texel of 0.04 m at the aimed point. 4096 measured the same within 0.011 of mean difference.
    // - Fade over the last 8.34 m of the half-width (a fifth of it), the band of the original's fade texture
    //   (ShadowDistanceFadeoutMap: full up to 80 % of the half-width). Without the fade a probe of the bench is lost.
    // - Depth bias: a fraction of the interval between the planes, so 0.002 is 0.44 m. The original's two biases of 0.00065
    //   are worth 0.00118 here (engine docs/engine/shadow-settings.md); the value is the one measured, with the filter radius
    //   and the normal bias, against the original's views and the author's window for the tunnels (ADR-0045).
    // - No normal bias: the original has none. With the 0.1 of the orthographic map the tunnels came out darker (mean of the
    //   median luminances of the three tunnel views 24.98 instead of 27.89, for a window of 28.88 to 31.88), and darker
    //   still with a smaller depth bias; the walls of a tunnel are one layer thick and almost parallel to the rays.
    // - Filter radius 3 texels, as before: 1.5 and 6 measured within 0.08 of mean difference on the views.
    // - ShadowMaxDistance and ShadowDepthRange are not used by the perspective projection; ShadowMaxDistance only bounds the
    //   fade distance. They keep the values of the orthographic map.
    // The video memory of the map is a quarter of the 4096 map's. Not timed here.
    private const float ShadowLightDistance = 129.25f;
    private const float ShadowViewWidth = 83.4372f;
    private const float ShadowViewHeight = 129.25f;
    private const float ShadowNearPlane = 1f;
    private const float ShadowFarPlane = 218.75f;
    private const int ShadowMapResolution = 2048;
    private const float ShadowMaxDistance = 100f;
    private const float ShadowFadeDistance = 8.34f;
    private const float ShadowDepthRange = 150f;
    private const float ShadowFilterRadius = 3f;
    // 64 reads instead of the engine's 12 (ADR-0035, engine ADR-0127): with 12 a shadow edge seen at a grazing angle, on a
    // tunnel wall, showed as a fan of bands; with 64 it is a gradient, as the original's blurred shadow. 32 measured the same
    // within 0.002 of mean difference on the views (ADR-0045); 64 is kept. Not timed here.
    private const int ShadowFilterTaps = 64;
    // ADR-0036: no longer the original's (0.25, 0.26, 0.27). The engine applies the shadow colour in linear light, before the tone
    // curve, where 0.25 left the shadowed ground about half as bright, relative to the lit ground, as the original's display-space
    // multiply. The level 0.38 is measured against the original; the three channels keep the original's 25 : 26 : 27 ratio.
    // Measured again with the perspective shadow (ADR-0045): 0.30 and 0.46 both lose the shadow to lit ratios of the bench.
    private static readonly Vector3 ShadowColor = new(0.38f, 0.3952f, 0.4104f);
    private const float ShadowDepthBias = 0.002f;
    private const float ShadowNormalBias = 0f;

    // The car selection carousel's fixed part (the cars, the camera and the placement stay with the carousel).
    private const string CarSelectionPlateModelAssetName = "StaticModel.CarSelectionPlate";
    // Shadow map of the carousel: the engine's box is centred on the camera, about 11 units from the carousel's centre,
    // whose cars reach 7 units from it.
    private const int CarSelectionShadowMapResolution = 2048;
    private const float CarSelectionShadowMaxDistance = 16f;
    private const float CarSelectionShadowDepthBias = 0.001f;
    private const float CarSelectionShadowNormalBias = 0.05f;
    // RacingGame's car selection light (CarSelection.PostUIRender: LensFlare.DefaultLightPos with z flipped), towards the
    // light: (-8500, 7250, 15000) normalised, here (x, z, -y). A LightComponent shines along its Forward, away from it.
    private static readonly Vector3 CarSelectionLightDirection = -Vector3.Normalize(new Vector3(-8500f, 15000f, -7250f));

    // pi: a lit-pbr surface lit head-on is pi times darker than a lit-diffuse one at the same intensity (CasaEngine
    // docs/engine/pbr-rendering.md, Units), so the light of a PBR scene keeps the level the original had at intensity 1; the race
    // world's key light is lit the same way (KeyLightIntensity). The carousel's cars and plates are lit-pbr
    // since the PBR materials (plan ai-agent/tasks/rgce-pbr-handover-tasks.md, phase 16, ADR-0031).
    private const float CarSelectionLightIntensity = MathF.PI;

    // The ambient colour of the carousel is 0.05, the engine's default (WorldEnvironmentSettings.ResetToDefaults), written out as
    // the race world's is (AmbientColor, phase 13): lit-pbr multiplies the irradiance of the sky cube by it.
    private static readonly Vector3 CarSelectionAmbientColor = new(0.05f);

    /// <summary>The front end's entities, in the order the world adds them: the camera, then the player start.</summary>
    internal static List<Entity> CreateFrontEndEntities()
    {
        return
        [
            CreateCameraEntity(enableChaseCamera: false),
            CreatePlayerStartEntity(new Vector3(0f, 0f, -4f)),
        ];
    }

    /// <summary>
    /// The entities of a race world, in the order the world adds them: the camera, <c>RaceRoot</c>, the key light, the
    /// track's entities, the scenery's, the checkpoints, then the player start. The player's car is not one of them.
    /// </summary>
    internal static List<Entity> CreateRaceEntities(string trackName, bool castShadows)
    {
        RaceTrackScene trackScene = LegacyTrackSceneFactory.Create(trackName);

        var entities = new List<Entity>
        {
            CreateCameraEntity(enableChaseCamera: true),
            CreateRaceRootEntity(trackScene.TrackAssetId, trackScene.HeightmapAssetId),
            CreateKeyLightEntity(castShadows),
        };

        foreach (Entity entity in trackScene.TrackEntities)
        {
            entities.Add(entity);
        }

        foreach (Entity entity in trackScene.SceneryEntities)
        {
            entities.Add(entity);
        }

        for (int index = 0; index < trackScene.CheckpointTriggers.Count; index++)
        {
            entities.Add(CreateCheckpointEntity($"Checkpoint.{index + 1:00}", trackScene.CheckpointTriggers[index]));
        }

        entities.Add(CreatePlayerStartEntity(trackScene.PlayerStartPose));

        return entities;
    }

    /// <summary>
    /// The race world's environment: the shadows (<paramref name="shadowsEnabled"/> is the player's Shadows option), the
    /// ambient light, and, in the linear colour pipeline, that pipeline.
    /// </summary>
    internal static void ApplyRaceEnvironment(WorldEnvironmentSettings environment, bool shadowsEnabled, bool linearColorPipeline)
    {
        // The Shadows option: the race world renders shadow maps, cast by its key light.
        environment.Shadows.Enabled = shadowsEnabled;
        environment.Shadows.Projection = ShadowProjection.Perspective;
        environment.Shadows.LightDistance = ShadowLightDistance;
        environment.Shadows.ViewWidth = ShadowViewWidth;
        environment.Shadows.ViewHeight = ShadowViewHeight;
        environment.Shadows.NearPlane = ShadowNearPlane;
        environment.Shadows.FarPlane = ShadowFarPlane;
        environment.Shadows.Resolution = ShadowMapResolution;
        environment.Shadows.MaxDistance = ShadowMaxDistance;
        environment.Shadows.FadeDistance = ShadowFadeDistance;
        environment.Shadows.DepthRange = ShadowDepthRange;
        environment.Shadows.FilterRadius = ShadowFilterRadius;
        environment.Shadows.FilterTaps = ShadowFilterTaps;
        environment.Shadows.Mode = ShadowMode.MultiplyLitColor;
        environment.Shadows.ShadowColor = ShadowColor;
        environment.Shadows.DepthBias = ShadowDepthBias;
        environment.Shadows.NormalBias = ShadowNormalBias;

        // Image-based lighting from the sky cube (the game binds it to the race world as its specular environment cube):
        // the cube gives both the ambient diffuse (irradiance) and the reflections of the PBR materials, as CasaEngine.Demos
        // PbrMaterialDemo.ApplyIbl. Same in both colour pipeline modes. The values are written out so that the world does not
        // depend on the engine's defaults; the ambient colour is AmbientColor.
        environment.AmbientSource = AmbientSource.Auto;
        environment.AmbientColor = AmbientColor;
        environment.AmbientIntensity = 1f;
        environment.SpecularIntensity = 1f;

        if (linearColorPipeline)
        {
            ConfigureLinearColorPipeline(environment.ColorPipeline);
            environment.MarkDirty();
        }
    }

    /// <summary>The carousel world's environment: the ambient colour and the shadow map's size, reach and biases.</summary>
    internal static void ApplyCarSelectionEnvironment(WorldEnvironmentSettings environment)
    {
        environment.AmbientColor = CarSelectionAmbientColor;
        environment.Shadows.Resolution = CarSelectionShadowMapResolution;
        environment.Shadows.MaxDistance = CarSelectionShadowMaxDistance;
        environment.Shadows.DepthBias = CarSelectionShadowDepthBias;
        environment.Shadows.NormalBias = CarSelectionShadowNormalBias;
    }

    /// <summary>The carousel's light: <c>Carousel.Light</c>, whose root is the <see cref="LightComponent"/>.</summary>
    internal static Entity CreateCarSelectionLightEntity()
    {
        var light = new LightComponent
        {
            Name = nameof(LightComponent),
            Type = LightType.Directional,
            LocalOrientation = CreateOrientationFromForward(CarSelectionLightDirection),
            Color = Color.White,
            SpecularColor = Color.White,
            Intensity = CarSelectionLightIntensity,
        };
        return new Entity { Name = "Carousel.Light", RootComponent = light };
    }

    /// <summary>
    /// The carousel's plates, <c>Carousel.Plate.0</c> to <c>.2</c>, in that order; the root of each is its
    /// <see cref="StaticModelComponent"/>. As in RacingGame, only the cars cast shadows; the plates receive them.
    /// </summary>
    internal static List<Entity> CreateCarSelectionPlateEntities()
    {
        Guid plateAssetId = GetCarSelectionPlateAssetId();
        var plates = new List<Entity>(CarSelectionPlateCount);
        for (int i = 0; i < CarSelectionPlateCount; i++)
        {
            var component = new StaticModelComponent { Name = nameof(StaticModelComponent), StaticModel = null };
            if (plateAssetId != Guid.Empty)
            {
                // The cooked plate, shared by the engine between the three plates: no paint, nothing to own.
                component.StaticModelAssetId = plateAssetId;
            }

            var entity = new Entity { Name = $"Carousel.Plate.{i}", RootComponent = component };
            entity.ApplyExplicitPolicies(EntityPolicySet.DynamicDefault);
            component.CastShadows = false;
            plates.Add(entity);
        }

        return plates;
    }

    // The cooked CarSelectionPlate (Y up and in the car's units already, so no root correction, unlike the track scenery).
    private static Guid GetCarSelectionPlateAssetId()
    {
        AssetInfo? assetInfo = AssetCatalog.Get(CarSelectionPlateModelAssetName);
        if (assetInfo == null)
        {
            Logs.WriteWarning($"Car selection: the catalog has no '{CarSelectionPlateModelAssetName}' asset.");
            return Guid.Empty;
        }

        return assetInfo.Id;
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
            Name = RaceWorldNames.CameraEntityName,
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
            Name = RaceWorldNames.PlayerStartEntityName,
            RootComponent = new PlayerStartComponent { Name = nameof(PlayerStartComponent) },
        };
        entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);

        entity.RootComponent!.LocalPosition = position;
        return entity;
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
        entity.AddComponent(new RaceShadowFocusComponent { Name = nameof(RaceShadowFocusComponent) });
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
}
