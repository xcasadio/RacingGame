using System;
using System.IO;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Game;
using CasaEngine.Framework.Game.Components;
using CasaEngine.Framework.World;
using MGUI.Core.UI.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Persistence;
using RacingGameCasaEngine.UI;
using RacingGameCasaEngine.Worlds;
using Color = Microsoft.Xna.Framework.Color;
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
    private readonly UiScreenCaptureValidator? _uiScreenCaptureValidator;
    private readonly CarProfileAuditValidator? _carProfileAuditValidator;
    private readonly CarTopSpeedAuditValidator? _carTopSpeedAuditValidator;
    private readonly TrackRuntimeSceneExportValidator? _trackRuntimeSceneExportValidator;
    private readonly string _displaySettingsFileName;
    private readonly string _frontEndOptionsFileName;
    private IViewRenderPipeline? _raceSkyViewPipeline;
    private TextureCube? _raceSkyFallbackReflectionCube;
    private TextureCube? _raceSkySharedCube;
    private bool _raceSkySharedCubeLoadAttempted;
    private IDisposable? _gameFontHold;
    private AssetHandle<CasaEngine.Framework.Assets.Fonts.BitmapFont>? _gameFontMetrics;
    private CarSelectionCarousel? _carSelectionCarousel;

    /// <summary>The menu sounds, held for the game's life (ADR-0009).</summary>
    internal MenuSounds? MenuSounds { get; private set; }

    /// <summary>
    /// The car selection's 3D carousel, built when the car selection first asks for it and kept for the game's life; it
    /// renders only while the car selection asks for it every frame (ADR-0009).
    /// </summary>
    internal CarSelectionCarousel CarSelectionCarousel => _carSelectionCarousel ??= new CarSelectionCarousel(this);

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
        RaceVibration = new RaceGamepadVibration(this, () => _frontEndFlow.State.EnableVibration);

        if (launchOptions.ValidateFrontEndNavigation)
        {
            _navigationSmokeValidator = new FrontEndNavigationSmokeValidator(this, _frontEndFlow);
        }

        if (launchOptions.CaptureTrackAudit)
        {
            _trackMigrationCaptureValidator = new TrackMigrationCaptureValidator(this, _frontEndFlow);
        }

        if (launchOptions.CaptureUiScreens)
        {
            _uiScreenCaptureValidator = new UiScreenCaptureValidator(this, _frontEndFlow);
        }

        if (launchOptions.CaptureCarProfileAudit)
        {
            _carProfileAuditValidator = new CarProfileAuditValidator(this, _frontEndFlow, launchOptions.CarProfileAuditFilePath);
        }

        if (launchOptions.CaptureCarTopSpeedAudit)
        {
            _carTopSpeedAuditValidator = new CarTopSpeedAuditValidator(this, _frontEndFlow, launchOptions.CarTopSpeedAuditFilePath, launchOptions.CarTopSpeedAuditTrackName);
        }

        if (launchOptions.ExportTrackRuntimeScene)
        {
            _trackRuntimeSceneExportValidator = new TrackRuntimeSceneExportValidator(this, _frontEndFlow, launchOptions.RuntimeSceneExportFilePath);
        }

        GameManager.WorldLoaded += OnWorldLoaded;
    }

    internal RuntimeRaceSession RaceSession { get; }

    /// <summary>Gamepad vibration on guard-rail hits (Gamepad Vibration option).</summary>
    internal RaceGamepadVibration RaceVibration { get; }

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

        SoundEffect.MasterVolume = state.SoundVolume / 100f;
        MediaPlayer.Volume = state.MusicVolume / 100f;

        foreach (var view in GameManager.ViewManager.Views)
        {
            view.ShowDebugOverlay = state.ShowFps;
            view.Invalidate();
        }
    }

    /// <summary>
    /// Saves the applied display settings and the front-end options to the user settings files. Only the Options screen
    /// calls it, when the player leaves it (ADR-0004): world loads and the automation modes apply the options without saving them.
    /// </summary>
    internal void SaveFrontEndOptions(RaceFrontEndState state)
    {
        SaveDisplaySettings(_displaySettingsFileName);
        FrontEndOptionsPersistence.Save(_frontEndOptionsFileName, state);
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
        HoldGameFont();
        MenuSounds = new MenuSounds(this);
        World world = RaceWorldFactory.CreateFrontEndWorld();
        GameManager.SetWorldToLoad(world);
    }

    /// <summary>
    /// Holds RacingGame's bitmap font (TextureFont, GameFont.png) for the game's life, so that XAML can name it as
    /// <c>FontFamily="GameFont"</c> (CasaEngine ADR-0036; RGCE ADR-0009).
    /// </summary>
    private void HoldGameFont()
    {
        const string gameFontAssetName = "Font.GameFont";
        if (AssetCatalog.Get(gameFontAssetName) is not { } assetInfo)
        {
            Logs.WriteWarning($"Bitmap font '{gameFontAssetName}' is not in AssetInfos.json.");
            return;
        }

        try
        {
            _gameFontHold = UIFonts.Acquire(assetInfo.Id);
            _gameFontMetrics = AssetContentManager.Acquire<CasaEngine.Framework.Assets.Fonts.BitmapFont>(assetInfo.Id);
        }
        catch (Exception exception)
        {
            Logs.WriteWarning($"Bitmap font '{gameFontAssetName}' cannot be loaded. {exception.Message}");
        }
    }

    /// <summary>
    /// Width of <paramref name="text"/> in GameFont at its native size, as TextureFont.GetTextWidth before its XToRes1400;
    /// 0 when the font is missing. The screens scale it as they scale their GameFont texts.
    /// </summary>
    internal float MeasureGameFontText(string text) => _gameFontMetrics?.Asset?.Font.MeasureString(text).X ?? 0f;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameFontHold?.Dispose();
            _gameFontHold = null;
            _gameFontMetrics?.Dispose();
            _gameFontMetrics = null;
            MenuSounds?.Dispose();
            MenuSounds = null;
            _carSelectionCarousel?.Dispose();
            _carSelectionCarousel = null;
        }

        base.Dispose(disposing);
    }

    private void OnWorldLoaded(object? sender, EventArgs e)
    {
        _raceWorldBinder.BindCurrentWorld(_frontEndFlow.State);
        ApplyFrontEndOptions(_frontEndFlow.State);
        ConfigureCurrentWorldSky();
        ApplyDebugMouseCursorState();
        ApplyRaceWorldVisibilityState();
        _frontEndFlow.InitializeForCurrentWorld();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _carSelectionCarousel?.UpdateView(gameTime);
        HandleRuntimeDebugHotkeys();
        _frontEndFlow.UpdateRuntimeRaceUi(gameTime);
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
        // The front end clears to black, as RacingGame did (BaseGame.BackgroundColor): the menu background is drawn at
        // 0.85 opacity over it (ADR-0009).
        Color clearColor = isRaceWorld ? RaceSkySystem.Settings.HorizonColor : Color.Black;

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

    internal TextureCube GetOrCreateRaceSkyReflectionCube()
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

        if (InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.F5))
        {
            CaptureMovementDebugReport();
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

    private void CaptureMovementDebugReport()
    {
        if (!RaceSession.IsActive)
        {
            Logs.WriteInfo("Movement debug report unavailable because no race session is active.");
            return;
        }

        try
        {
            string report = RaceSession.BuildMovementDebugReport();
            string diagnosticsDirectory = Path.Combine(GetUserDataDirectory(), "Diagnostics");
            Directory.CreateDirectory(diagnosticsDirectory);

            string filePath = Path.Combine(
                diagnosticsDirectory,
                $"movement-debug-{DateTime.Now:yyyyMMdd-HHmmssfff}.txt");
            File.WriteAllText(filePath, report);
            new StringClipboard().Text = report;
            Logs.WriteInfo($"Movement debug report copied to clipboard and saved: {filePath}");
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            Logs.WriteWarning("Failed to capture the movement debug report.");
        }
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

    internal static int GetResolutionIndex(int width, int height)
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