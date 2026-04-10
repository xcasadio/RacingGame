using System;
using System.IO;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Game;
using CasaEngine.Framework.Game.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using RacingGameCasaEngine.Persistence;
using RacingGameCasaEngine.Worlds;
using Color = Microsoft.Xna.Framework.Color;
using DirLight = CasaEngine.Framework.Rendering.DirectionalLight;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Bootstrap;

public sealed class RacingGameCasaEngineGame : CasaEngineGame
{
    private static readonly (int Width, int Height)[] MenuResolutions =
    [
        (1280, 720),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    ];

    private readonly RaceFrontEndFlow _frontEndFlow;
    private readonly RuntimeRaceWorldBinder _raceWorldBinder;
    private readonly FrontEndNavigationSmokeValidator? _navigationSmokeValidator;
    private readonly TrackMigrationCaptureValidator? _trackMigrationCaptureValidator;
    private readonly TrackRuntimeSceneExportValidator? _trackRuntimeSceneExportValidator;
    private readonly string _displaySettingsFileName;
    private readonly string _frontEndOptionsFileName;
    private IViewRenderPipeline? _raceSkyViewPipeline;
    private TextureCube? _raceSkyFallbackReflectionCube;
    private TextureCube? _raceSkySharedCube;
    private bool _raceSkySharedCubeLoadAttempted;

    internal RacingGameCasaEngineGame(EngineRuntimeContext runtimeContext, string displaySettingsFileName, string frontEndOptionsFileName, RaceLaunchOptions? launchOptions = null)
        : base(runtimeContext: runtimeContext)
    {
        launchOptions ??= new RaceLaunchOptions();
        _displaySettingsFileName = displaySettingsFileName;
        _frontEndOptionsFileName = frontEndOptionsFileName;

        ExecutionPolicy = new GameplayExecutionPolicy
        {
            IsEditorPreview = GameplayExecutionPolicies.Runtime.IsEditorPreview,
            UseExternalViewManagement = GameplayExecutionPolicies.Runtime.UseExternalViewManagement,
            InitializePlayerControllers = false,
            InitializeGameplayOnLoad = GameplayExecutionPolicies.Runtime.InitializeGameplayOnLoad,
            RunBeginPlay = GameplayExecutionPolicies.Runtime.RunBeginPlay,
            UpdateGameplayScripts = GameplayExecutionPolicies.Runtime.UpdateGameplayScripts,
            UpdateAnimatedSprites = GameplayExecutionPolicies.Runtime.UpdateAnimatedSprites,
            UpdatePhysicsComponents = GameplayExecutionPolicies.Runtime.UpdatePhysicsComponents,
            UpdatePhysicsEngine = GameplayExecutionPolicies.Runtime.UpdatePhysicsEngine,
        };

        _frontEndFlow = new RaceFrontEndFlow(this);
        FrontEndOptionsPersistence.Load(_frontEndOptionsFileName, _frontEndFlow.State);
        _raceWorldBinder = new RuntimeRaceWorldBinder(this);
        RaceSession = new RuntimeRaceSession();

        if (launchOptions.ValidateFrontEndNavigation)
        {
            _navigationSmokeValidator = new FrontEndNavigationSmokeValidator(this, _frontEndFlow);
        }

        if (launchOptions.CaptureTrackAudit)
        {
            _trackMigrationCaptureValidator = new TrackMigrationCaptureValidator(this, _frontEndFlow);
        }

        if (launchOptions.ExportTrackRuntimeScene)
        {
            _trackRuntimeSceneExportValidator = new TrackRuntimeSceneExportValidator(this, _frontEndFlow, launchOptions.RuntimeSceneExportFilePath);
        }

        GameManager.WorldLoaded += OnWorldLoaded;
    }

    public Texture2D? MenuBackgroundTexture { get; private set; }

    public Texture2D? MenuButtonsTexture { get; private set; }

    internal RuntimeRaceSession RaceSession { get; }

    internal void SyncOptionsState(RaceFrontEndState state)
    {
        DisplaySettings displaySettings = GetDisplaySettings();
        state.SelectedResolutionIndex = GetResolutionIndex(displaySettings.Width, displaySettings.Height);
        state.IsFullscreen = displaySettings.IsFullScreen;
        state.EnableVSync = displaySettings.IsVSyncEnabled;
        state.ShowFps = GameManager.ViewManager.Views.Any(static view => view.ShowDebugOverlay);
        state.SoundVolume = (int)Math.Round(Math.Clamp(SoundEffect.MasterVolume, 0f, 1f) * 100f);
        state.MusicVolume = (int)Math.Round(Math.Clamp(MediaPlayer.Volume, 0f, 1f) * 100f);
    }

    internal void ApplyFrontEndOptions(RaceFrontEndState state)
    {
        state.SoundVolume = Math.Clamp(state.SoundVolume, 0, 100);
        state.MusicVolume = Math.Clamp(state.MusicVolume, 0, 100);
        state.ControllerSensitivity = Math.Clamp(state.ControllerSensitivity, 0, 100);

        DisplaySettings currentDisplaySettings = GetDisplaySettings();
        int width = currentDisplaySettings.Width;
        int height = currentDisplaySettings.Height;
        if (TryGetResolution(state.SelectedResolutionIndex, out int selectedWidth, out int selectedHeight))
        {
            width = selectedWidth;
            height = selectedHeight;
        }

        ApplyDisplaySettings(new DisplaySettings(width, height, state.IsFullscreen, state.EnableVSync));
        SaveDisplaySettings(_displaySettingsFileName);
        FrontEndOptionsPersistence.Save(_frontEndOptionsFileName, state);

        SoundEffect.MasterVolume = state.SoundVolume / 100f;
        MediaPlayer.Volume = state.MusicVolume / 100f;

        foreach (var view in GameManager.ViewManager.Views)
        {
            view.ShowDebugOverlay = state.ShowFps;
            view.Invalidate();
        }
    }

    protected override void Initialize()
    {
        GameSettings.ProjectSettings.WindowTitle = "RacingGameCasaEngine";
        GameSettings.ProjectSettings.AllowUserResizing = true;
        GameSettings.ProjectSettings.IsMouseVisible = true;
        GameSettings.ProjectSettings.IsFixedTimeStep = true;

        base.Initialize();
    }

    protected override void LoadContentPrivate()
    {
        LoadBootstrapAssets();

        World world = RaceWorldFactory.CreateFrontEndWorld();
        GameManager.SetWorldToLoad(world);
    }

    private void LoadBootstrapAssets()
    {
        AssetInfo? backgroundAsset = AssetCatalog.Get(RaceBootstrapAssets.MenuBackground);
        if (backgroundAsset != null)
        {
            MenuBackgroundTexture = AssetContentManager.Load<Texture2D>(backgroundAsset.Id);
        }

        AssetInfo? buttonsAsset = AssetCatalog.Get(RaceBootstrapAssets.MenuButtons);
        if (buttonsAsset != null)
        {
            MenuButtonsTexture = AssetContentManager.Load<Texture2D>(buttonsAsset.Id);
        }
    }

    private void OnWorldLoaded(object? sender, EventArgs e)
    {
        _raceWorldBinder.BindCurrentWorld(_frontEndFlow.State);
        ApplyFrontEndOptions(_frontEndFlow.State);
        ConfigureCurrentWorldLighting();
        ConfigureCurrentWorldSky();
        ApplyDebugMouseCursorState();
        ApplyRaceWorldVisibilityState();
        _frontEndFlow.InitializeForCurrentWorld();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        HandleRuntimeDebugHotkeys();
    }

    private void ConfigureCurrentWorldLighting()
    {
        StaticMeshRendererComponent? renderer = this.GetGameComponent<StaticMeshRendererComponent>();
        if (renderer == null)
        {
            return;
        }

        if (GameManager.CurrentWorld is { } world && RaceWorldFactory.IsRaceWorld(world))
        {
            renderer.DefaultLighting.ActiveDirectionalLightCount = 3;
            renderer.DefaultLighting.AmbientColor = new Vector3(0.16f, 0.17f, 0.19f);
            renderer.DefaultLighting.DirectionalLights[0] = new DirLight(
                new Vector3(-0.42f, -0.86f, -0.29f),
                new Vector3(1.00f, 0.94f, 0.83f),
                new Vector3(0.95f, 0.90f, 0.84f),
                1.10f);
            renderer.DefaultLighting.DirectionalLights[1] = new DirLight(
                new Vector3(0.58f, -0.28f, 0.76f),
                new Vector3(0.30f, 0.36f, 0.46f),
                Vector3.Zero,
                0.85f);
            renderer.DefaultLighting.DirectionalLights[2] = new DirLight(
                new Vector3(0.18f, -0.35f, -0.92f),
                new Vector3(0.20f, 0.19f, 0.18f),
                new Vector3(0.18f, 0.18f, 0.18f),
                0.60f);
            return;
        }

        renderer.DefaultLighting.ActiveDirectionalLightCount = 3;
        renderer.DefaultLighting.AmbientColor = new Vector3(0.05f, 0.05f, 0.05f);
        renderer.DefaultLighting.DirectionalLights[0] = new DirLight(
            new Vector3(-0.5265408f, -0.5735765f, -0.6275069f),
            new Vector3(0.92f, 0.92f, 0.92f),
            new Vector3(0.92f, 0.92f, 0.92f));
        renderer.DefaultLighting.DirectionalLights[1] = new DirLight(
            new Vector3(0.7198464f, 0.3420201f, 0.6040227f),
            new Vector3(0.71f, 0.71f, 0.71f),
            Vector3.Zero);
        renderer.DefaultLighting.DirectionalLights[2] = new DirLight(
            new Vector3(0.4545195f, -0.7660444f, 0.4545195f),
            new Vector3(0.36f, 0.36f, 0.36f),
            new Vector3(0.36f, 0.36f, 0.36f));
    }

    private void ConfigureCurrentWorldSky()
    {
        World? currentWorld = GameManager.CurrentWorld;
        bool isRaceWorld = currentWorld is { } world && RaceWorldFactory.IsRaceWorld(world);

        if (currentWorld != null)
        {
            currentWorld.EnvironmentSettings.SpecularEnvironmentCubemapAssetId = Guid.Empty;
            currentWorld.EnvironmentSettings.SpecularEnvironmentCubemap = isRaceWorld
                ? GetOrCreateRaceSkyReflectionCube()
                : null;
            currentWorld.EnvironmentSettings.MarkDirty();
        }

        IViewRenderPipeline? pipeline = isRaceWorld ? GetOrCreateRaceSkyViewPipeline() : null;
        Color clearColor = isRaceWorld ? RaceSkySystem.Settings.HorizonColor : Color.CornflowerBlue;

        foreach (RenderView view in GameManager.ViewManager.Views)
        {
            if (GameManager.CurrentWorld != null && !ReferenceEquals(view.World, GameManager.CurrentWorld))
            {
                continue;
            }

            view.Pipeline = pipeline;
            view.ClearColor = clearColor;
            view.Invalidate();
        }
    }

    private IViewRenderPipeline GetOrCreateRaceSkyViewPipeline()
    {
        if (_raceSkyViewPipeline != null)
        {
            return _raceSkyViewPipeline;
        }

        if (TryGetOrCreateRaceSkySharedCube() is { } legacySkyCube)
        {
            Effect effect = Content.Load<Effect>("Shaders\\LegacySkyCube").Clone();
            _raceSkyViewPipeline = new LegacySkyCubeViewPipeline(effect, legacySkyCube, RaceSkySystem.LegacySkyCubeTintColor);
            return _raceSkyViewPipeline;
        }

        _raceSkyViewPipeline = new SkyBackgroundViewPipeline(RaceSkySystem.Settings);
        return _raceSkyViewPipeline;
    }

    private TextureCube GetOrCreateRaceSkyReflectionCube()
        => TryGetOrCreateRaceSkySharedCube()
            ?? (_raceSkyFallbackReflectionCube ??= ProceduralSkyCubeFactory.CreateReflectionCube(GraphicsDevice, RaceSkySystem.Settings));

    private TextureCube? TryGetOrCreateRaceSkySharedCube()
    {
        if (_raceSkySharedCubeLoadAttempted)
        {
            return _raceSkySharedCube;
        }

        _raceSkySharedCubeLoadAttempted = true;

        string skyCubePath = RaceSkySystem.ResolveLegacySharedSkyCubePath(Content.RootDirectory);
        if (!File.Exists(skyCubePath))
        {
            Logs.WriteWarning($"Race sky cubemap '{skyCubePath}' was not found. Falling back to the procedural race sky.");
            return null;
        }

        try
        {
            _raceSkySharedCube = TextureCubeLoader.LoadTextureCube(skyCubePath, GraphicsDevice);
            return _raceSkySharedCube;
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            Logs.WriteWarning($"Race sky cubemap '{skyCubePath}' could not be loaded. Falling back to the procedural race sky.");
            return null;
        }
    }

    private void HandleRuntimeDebugHotkeys()
    {
        if (InputComponent == null)
        {
            return;
        }

        if (InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.F1)
            && GameManager.CurrentWorld is { } raceWorldForDebugCamera
            && RaceWorldFactory.IsRaceWorld(raceWorldForDebugCamera)
            && RaceSession.IsActive)
        {
            bool debugCameraEnabled = RaceSession.ToggleDebugCamera();
            ApplyDebugMouseCursorState();
            Logs.WriteInfo(debugCameraEnabled ? "Debug camera enabled" : "Debug camera disabled");
        }

        if (InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.F2))
        {
            CaptureScreenshot();
        }

        if (InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.F3)
            && GameManager.CurrentWorld is { } raceWorldForCircuitOnlyView
            && RaceWorldFactory.IsRaceWorld(raceWorldForCircuitOnlyView)
            && RaceSession.IsActive)
        {
            bool circuitOnlyViewEnabled = RaceSession.ToggleCircuitOnlyView();
            ApplyRaceWorldVisibilityState();
            Logs.WriteInfo(circuitOnlyViewEnabled ? "Circuit-only view enabled" : "Circuit-only view disabled");
        }
    }

    private void ApplyDebugMouseCursorState()
    {
        bool debugCameraActive = GameManager.CurrentWorld is { } world
            && RaceWorldFactory.IsRaceWorld(world)
            && RaceSession.IsDebugCameraEnabled;
        IsMouseVisible = !debugCameraActive && RuntimeContext.ProjectSettings.IsMouseVisible;
    }

    internal void SetDebugCameraEnabled(bool enabled)
    {
        RaceSession.SetDebugCameraEnabled(enabled);
        ApplyDebugMouseCursorState();
    }

    internal void SetCircuitOnlyViewEnabled(bool enabled)
    {
        RaceSession.SetCircuitOnlyViewEnabled(enabled);
        ApplyRaceWorldVisibilityState();
    }

    internal void ApplyRaceWorldVisibilityState()
    {
        if (GameManager.CurrentWorld is not { } world || !RaceWorldFactory.IsRaceWorld(world))
        {
            return;
        }

        bool circuitOnlyViewEnabled = RaceSession.IsCircuitOnlyViewEnabled;
        foreach (Entity entity in world.Entities)
        {
            if (!RaceWorldFactory.IsRaceRenderableEntity(entity))
            {
                continue;
            }

            entity.IsVisible = !circuitOnlyViewEnabled || RaceWorldFactory.IsVisibleInCircuitOnlyView(entity);
        }
    }

    private void CaptureScreenshot()
    {
        CaptureScreenshotWithStem(null);
    }

    internal string CaptureScreenshotWithStem(string? fileStem)
    {
        try
        {
            string screenshotDirectory = Path.Combine(GetUserDataDirectory(), "Screenshots");
            Directory.CreateDirectory(screenshotDirectory);

            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            byte[] backBuffer = new byte[width * height * 4];
            GraphicsDevice.GetBackBufferData(backBuffer);

            string effectiveStem = string.IsNullOrWhiteSpace(fileStem)
                ? "screenshot"
                : fileStem;
            string filePath = Path.Combine(
                screenshotDirectory,
                $"{effectiveStem}-{DateTime.Now:yyyyMMdd-HHmmssfff}.png");

            using var screenshot = new Texture2D(
                GraphicsDevice,
                width,
                height,
                false,
                GraphicsDevice.PresentationParameters.BackBufferFormat);
            screenshot.SetData(backBuffer);

            using FileStream stream = File.Create(filePath);
            screenshot.SaveAsPng(stream, width, height);
            Logs.WriteInfo($"Screenshot saved: {filePath}");
            return filePath;
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            return string.Empty;
        }
    }

    internal string GetUserDataDirectory()
    {
        string projectName = RuntimeContext.ProjectSettings.ProjectName;
        string effectiveProjectName = string.IsNullOrWhiteSpace(projectName)
            || string.Equals(projectName, "Project name undefined", StringComparison.Ordinal)
            ? "RacingGameCasaEngine"
            : projectName;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CasaEngine",
            effectiveProjectName);
    }

    private static int GetResolutionIndex(int width, int height)
    {
        for (int i = 0; i < MenuResolutions.Length; i++)
        {
            if (MenuResolutions[i].Width == width && MenuResolutions[i].Height == height)
            {
                return i;
            }
        }

        return MenuResolutions.Length;
    }

    private static bool TryGetResolution(int resolutionIndex, out int width, out int height)
    {
        if (resolutionIndex >= 0 && resolutionIndex < MenuResolutions.Length)
        {
            width = MenuResolutions[resolutionIndex].Width;
            height = MenuResolutions[resolutionIndex].Height;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }
}