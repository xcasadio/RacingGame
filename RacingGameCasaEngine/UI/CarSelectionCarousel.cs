using System;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Rendering;
using CasaEngine.Framework.Rendering.Models;
using CasaEngine.Framework.World;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Worlds;
using Color = Microsoft.Xna.Framework.Color;
using RacingGame.Gameplay.Components;

namespace RacingGameCasaEngine.UI;

/// <summary>
/// RacingGame's car selection carousel (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/CarSelection.cs</c>,
/// PostUIRender; ADR-0009): the three cars, each on its CarSelectionPlate, rendered by CasaEngine in a world of their own,
/// through a view into a render target as large as the screen, which the car selection shows over its sprites.
/// <para/>
/// RacingGame's world had Z up; RGCE's has Y up. The conversion (x, y, z) -> (x, z, -y) is a rotation, under which the
/// models keep their own coordinates (RacingGame turned its Y-up models to Z up first), a rotation about Z becomes one
/// about Y, and RacingGame's numbers carry over: the camera at (0, 10.45, 2.75) looking at (0, 0, -1) is here at
/// (0, 2.75, -10.45) looking at (0, -1, 0).
/// <list type="bullet">
/// <item>Car i and its plate: RotZ(t / 3.9) x T(0, 5, 0) x RotZ(-rotation + i x 2pi/3) x T(1.5, 0, 1), so every car spins
/// on itself at 1/3.9 rad/s and the three sit 5 units from the carousel's centre, 120 degrees apart. The author's capture
/// of the original game shows another framing than this code, which wins (ADR-0009): with the same camera, the centre
/// and the radius are fitted to the capture's front plate and rear cars (see <see cref="CarouselCentre"/>);</item>
/// <item>the rotation chases the selected car's angle at 5 rad/s the shortest way, so the selected car comes to the
/// front, nearest the camera;</item>
/// <item>vertical field of view 90 degrees, near 0.5, far 1750.</item>
/// </list>
/// </summary>
internal sealed class CarSelectionCarousel : IDisposable
{
    private const string WorldName = "FrontEnd.CarSelectionCarousel";
    private const int CarCount = 3;
    private const float CarouselRadius = 6.74f;
    private const float SpinPerSecond = 1f / 3.9f;
    private const float TurnPerSecond = 5f;
    private const float FieldOfView = MathHelper.PiOver2;
    private const float NearPlane = 0.5f;
    private const float FarPlane = 1750f;
    private const string PlateModelAssetName = "StaticModel.CarSelectionPlate";
    // Shadow map of the carousel: the engine's box is centred on the camera, about 11 units from the carousel's centre,
    // whose cars reach 7 units from it.
    private const int ShadowMapResolution = 2048;
    private const float ShadowMaxDistance = 16f;
    private const float ShadowDepthBias = 0.001f;
    private const float ShadowNormalBias = 0.05f;

    // Fitted to the author's capture at 4:3 (plan rgce-title-car-selection-xna-look-tasks.md, T4.3): front plate edges
    // and bottom, rear car centres, within 6.7 capture pixels on average. RacingGame's code had (1.5, 1, 0) and 5.
    private static readonly Vector3 CarouselCentre = new(-0.2f, -0.3f, 2.78f);
    private const float PlateRadius = 2.778736f * 1.203175f;
    private const float PlateThickness = 0.6103561f;
    private static readonly Vector3 CameraPosition = new(0f, 2.75f, -10.45f);
    private static readonly Vector3 CameraTarget = new(0f, -1f, 0f);
    // RacingGame's car selection light (CarSelection.PostUIRender: LensFlare.DefaultLightPos with z flipped), towards the
    // light: (-8500, 7250, 15000) normalised, here (x, z, -y). A LightComponent shines along its Forward, away from it.
    private static readonly Vector3 LightDirection = -Vector3.Normalize(new Vector3(-8500f, 15000f, -7250f));

    // pi: a lit-pbr surface lit head-on is pi times darker than a lit-diffuse one at the same intensity (CasaEngine
    // docs/engine/pbr-rendering.md, Units), so the light of a PBR scene keeps the level the original had at intensity 1; the race
    // world's key light is lit the same way (RaceWorldFactory, KeyLightIntensity). The carousel's cars and plates are lit-pbr
    // since the PBR materials (plan ai-agent/tasks/rgce-pbr-handover-tasks.md, phase 16, ADR-0031).
    private const float LightIntensity = MathF.PI;

    // The ambient colour of the carousel is 0.05, the engine's default (WorldEnvironmentSettings.ResetToDefaults), written out as
    // the race world's is (RaceWorldFactory.AmbientColor, phase 13): lit-pbr multiplies the irradiance of the sky cube by it.
    private static readonly Vector3 AmbientColor = new(0.05f);

    private readonly RacingGameCasaEngineGame _game;
    private readonly World _world;
    private readonly Entity _cameraEntity;
    private readonly CameraLookAtComponent _camera;
    private readonly OpaqueGeometryAlphaViewPipeline _pipeline = new();
    private readonly StaticModelComponent[] _plates = new StaticModelComponent[CarCount];
    private readonly StaticModelComponent[] _cars = new StaticModelComponent[CarCount];
    private readonly LightComponent _light;
    private RenderTargetSurface? _surface;
    private int _surfaceWidth;
    private int _surfaceHeight;
    private RenderView? _view;
    private ViewId _viewId;
    private RenderTarget2D? _boundTarget;
    private MGTextureData? _textureData;
    private float _rotation;
    private int _paintedColor = -1;
    private bool _isRequested;
    private bool _wasShown;
    private int _requestedCar;
    private int _requestedColor;
    private bool _requestedPin;
    private bool _requestedShadows;
    private int _requestedWidth;
    private int _requestedHeight;

    public CarSelectionCarousel(RacingGameCasaEngineGame game)
    {
        _game = game;
        _world = new World { Name = WorldName };
        _world.LoadContent(game);
        _world.EnvironmentSettings.SpecularEnvironmentCubemap = game.GetOrCreateRaceSkyReflectionCube();
        _world.EnvironmentSettings.AmbientColor = AmbientColor;
        _world.EnvironmentSettings.Shadows.Resolution = ShadowMapResolution;
        _world.EnvironmentSettings.Shadows.MaxDistance = ShadowMaxDistance;
        _world.EnvironmentSettings.Shadows.DepthBias = ShadowDepthBias;
        _world.EnvironmentSettings.Shadows.NormalBias = ShadowNormalBias;

        _light = new LightComponent
        {
            Name = nameof(LightComponent),
            Type = LightType.Directional,
            LocalOrientation = RaceWorldFactory.CreateOrientationFromForward(LightDirection),
            Color = Color.White,
            SpecularColor = Color.White,
            Intensity = LightIntensity,
        };
        _world.AddEntity(new Entity { Name = "Carousel.Light", RootComponent = _light });

        // As in RacingGame, only the cars cast shadows; the plates receive them.
        Guid plateAssetId = GetPlateAssetId();
        for (int i = 0; i < CarCount; i++)
        {
            _plates[i] = AddModelEntity($"Carousel.Plate.{i}", null, plateAssetId);
            _plates[i].CastShadows = false;
            _cars[i] = AddModelEntity($"Carousel.Car.{i}", LegacyCarVisualFactory.LoadConfiguredCarModel(game.AssetContentManager, i, 0));
        }

        _cameraEntity = new Entity { Name = "Carousel.Camera" };
        _camera = new CameraLookAtComponent { Name = nameof(CameraLookAtComponent) };
        _cameraEntity.RootComponent = _camera;
        _cameraEntity.Initialize();
        _cameraEntity.InitializeWithWorld(_world);
        _camera.SetPositionAndTarget(CameraPosition, CameraTarget);
    }

    /// <summary>The carousel's image for an MGUI image, once rendered; it changes when the render target is recreated.</summary>
    public MGTextureData? TextureData => _textureData;

    /// <summary>
    /// The left and right edges of the front plate on the screen, in pixels, for the selection arrows; (-1, -1) until the
    /// carousel has been laid out.
    /// </summary>
    public (int Left, int Right) FrontPlateEdges { get; private set; } = (-1, -1);

    /// <summary>
    /// Asks for the carousel this frame, for the selection and at the screen's size: the car selection calls it every
    /// update. The carousel renders only on the frames it is asked for.
    /// </summary>
    /// <param name="pinSpin">Holds the cars' spin and the carousel's rotation still, for the capture automation.</param>
    /// <param name="enableShadows">The Shadows option, as RacingGame's car selection followed it.</param>
    public void Request(int selectedCar, int selectedColor, bool pinSpin, bool enableShadows, int width, int height)
    {
        _isRequested = true;
        _requestedShadows = enableShadows;
        _requestedCar = selectedCar;
        _requestedColor = selectedColor;
        _requestedPin = pinSpin;
        _requestedWidth = Math.Max(1, width);
        _requestedHeight = Math.Max(1, height);
    }

    /// <summary>
    /// Shows the carousel if it was asked for this frame, else hides it. The game calls it after its update: a view is
    /// added to or enabled in the view manager only there, never while CasaEngine walks the views to change screens.
    /// </summary>
    public void UpdateView(GameTime gameTime)
    {
        bool requested = _isRequested;
        _isRequested = false;
        if (!requested)
        {
            if (_view != null)
            {
                _view.Enabled = false;
            }

            _wasShown = false;
            return;
        }

        EnsureView();
        _view!.Enabled = true;
        _view.ShowDebugOverlay = false;
        _world.EnvironmentSettings.Shadows.Enabled = _requestedShadows;
        _light.CastShadows = _requestedShadows;

        if (_requestedColor != _paintedColor)
        {
            _paintedColor = _requestedColor;
            for (int i = 0; i < CarCount; i++)
            {
                LegacyCarVisualFactory.LoadConfiguredCarModel(_game.AssetContentManager, i, _requestedColor);
            }
        }

        // RacingGame's screen started with car 1 in front and turned to the selected car.
        float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float target = _requestedCar * MathHelper.TwoPi / CarCount;
        _rotation = _requestedPin ? target : InterpolateRotation(_wasShown ? _rotation : 0f, target, elapsedSeconds * TurnPerSecond);
        _wasShown = true;
        float spin = _requestedPin ? 0f : (float)(gameTime.TotalGameTime.TotalSeconds * SpinPerSecond);

        for (int i = 0; i < CarCount; i++)
        {
            float angle = -_rotation + i * MathHelper.TwoPi / CarCount;
            Quaternion orientation = Quaternion.CreateFromAxisAngle(Vector3.Up, spin + angle);
            Vector3 position = Vector3.Transform(new Vector3(0f, 0f, -CarouselRadius), Matrix.CreateRotationY(angle)) + CarouselCentre;
            Place(_plates[i], position, orientation);
            Place(_cars[i], position, orientation);
        }

        if (_requestedWidth != _surfaceWidth || _requestedHeight != _surfaceHeight)
        {
            _surfaceWidth = _requestedWidth;
            _surfaceHeight = _requestedHeight;
            _surface!.EnsureSize(_surfaceWidth, _surfaceHeight);
            ConfigureCamera();
        }

        if (!ReferenceEquals(_boundTarget, _surface!.RenderTarget))
        {
            _boundTarget = _surface.RenderTarget;
            _textureData = new MGTextureData(UiImageResources.AsRenderTarget(_boundTarget));
        }

        _world.Update(elapsedSeconds);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _game.GameManager.ViewManager.Remove(_view);
            _view = null;
        }

        _world.Clear();
        _surface?.Dispose();
        _pipeline.Dispose();
    }

    // Adds the view, again after a world load: CasaEngine then removes every view (GameManager), this one included.
    private void EnsureView()
    {
        ViewManager viewManager = _game.GameManager.ViewManager;
        if (_view != null && viewManager.TryGetView(_viewId, out RenderView? current) && ReferenceEquals(current, _view))
        {
            return;
        }

        if (_surface == null)
        {
            _surfaceWidth = _requestedWidth;
            _surfaceHeight = _requestedHeight;
            _surface = new RenderTargetSurface(_game.GraphicsDevice, _surfaceWidth, _surfaceHeight);
            ConfigureCamera();
        }

        _viewId = viewManager.CreateView(new ViewDefinition
        {
            Name = "Car Selection Carousel",
            World = _world,
            Camera = _camera,
            Surface = _surface,
            ClearColor = Color.Transparent,
            Pipeline = _pipeline,
        });
        viewManager.TryGetView(_viewId, out _view);

        // CasaEngine gives every view an MGUI runtime, and GameScreenManager pushes each screen into every view's: this
        // view shows no UI, so it takes none.
        _view!.UIView?.Dispose();
        _view.UIView = null;
    }

    // RacingGame's CarSelection.InterpolateRotation: a constant step towards the target the shortest way, snapping once
    // within a step, the result kept within (-pi, pi].
    private static float InterpolateRotation(float current, float target, float step)
    {
        while (target - current > MathHelper.Pi)
        {
            target -= MathHelper.TwoPi;
        }

        while (target - current < -MathHelper.Pi)
        {
            target += MathHelper.TwoPi;
        }

        float difference = target - current;
        current = Math.Abs(difference) <= step ? target : current + Math.Sign(difference) * step;
        return MathHelper.WrapAngle(current);
    }

    private static void Place(StaticModelComponent component, Vector3 position, Quaternion orientation)
    {
        component.LocalPosition = position;
        component.LocalOrientation = orientation;
    }

    private void ConfigureCamera()
    {
        // Camera3dComponent.OnScreenResized sets its own field of view: RacingGame's is set after it.
        _camera.OnScreenResized(_surfaceWidth, _surfaceHeight);
        _camera.FieldOfView = FieldOfView;
        _camera.NearPlane = NearPlane;
        _camera.FarPlane = FarPlane;
        FrontPlateEdges = ProjectFrontPlateEdges();
    }

    // The widest screen extent of the front plate's rim (top and bottom circles), with the camera of the view.
    private (int Left, int Right) ProjectFrontPlateEdges()
    {
        Matrix viewProjection = _camera.ViewMatrix * _camera.ProjectionMatrix;
        Vector3 centre = new Vector3(0f, 0f, -CarouselRadius) + CarouselCentre;
        float left = float.MaxValue;
        float right = float.MinValue;
        for (int i = 0; i < 72; i++)
        {
            float angle = i * MathHelper.TwoPi / 72;
            for (int level = 0; level < 2; level++)
            {
                Vector3 point = centre + new Vector3(PlateRadius * MathF.Cos(angle), -level * PlateThickness, PlateRadius * MathF.Sin(angle));
                Vector4 clip = Vector4.Transform(new Vector4(point, 1f), viewProjection);
                float x = (clip.X / clip.W + 1f) * 0.5f * _surfaceWidth;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
        }

        return ((int)Math.Round(left), (int)Math.Round(right));
    }

    private StaticModelComponent AddModelEntity(string name, StaticModel? model, Guid staticModelAssetId = default)
    {
        var component = new StaticModelComponent { Name = nameof(StaticModelComponent), StaticModel = model };
        if (staticModelAssetId != Guid.Empty)
        {
            // The cooked plate, shared by the engine between the three plates: no paint, nothing to own.
            component.StaticModelAssetId = staticModelAssetId;
        }

        var entity = new Entity { Name = name, RootComponent = component };
        entity.ApplyExplicitPolicies(EntityPolicySet.DynamicDefault);
        _world.AddEntity(entity);
        return component;
    }

    // The cooked CarSelectionPlate (Y up and in the car's units already, so no root correction, unlike the track scenery).
    private static Guid GetPlateAssetId()
    {
        AssetInfo? assetInfo = AssetCatalog.Get(PlateModelAssetName);
        if (assetInfo == null)
        {
            Logs.WriteWarning($"Car selection: the catalog has no '{PlateModelAssetName}' asset.");
            return Guid.Empty;
        }

        return assetInfo.Id;
    }
}
