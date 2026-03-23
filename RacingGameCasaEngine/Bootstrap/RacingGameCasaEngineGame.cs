using System;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Game;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using RacingGameCasaEngine.Worlds;

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
    private readonly string _displaySettingsFileName;

    internal RacingGameCasaEngineGame(EngineRuntimeContext runtimeContext, string displaySettingsFileName, RaceLaunchOptions? launchOptions = null)
        : base(runtimeContext: runtimeContext)
    {
        launchOptions ??= new RaceLaunchOptions();
        _displaySettingsFileName = displaySettingsFileName;

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
        _raceWorldBinder = new RuntimeRaceWorldBinder(this);
        RaceSession = new RuntimeRaceSession();

        if (launchOptions.ValidateFrontEndNavigation)
        {
            _navigationSmokeValidator = new FrontEndNavigationSmokeValidator(this, _frontEndFlow);
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
        _frontEndFlow.InitializeForCurrentWorld();
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