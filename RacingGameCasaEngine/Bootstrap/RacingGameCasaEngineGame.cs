using System;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Game;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

public sealed class RacingGameCasaEngineGame : CasaEngineGame
{
    private readonly RaceFrontEndFlow _frontEndFlow;
    private readonly RuntimeRaceWorldBinder _raceWorldBinder;
    private readonly FrontEndNavigationSmokeValidator? _navigationSmokeValidator;

    internal RacingGameCasaEngineGame(EngineRuntimeContext runtimeContext, RaceLaunchOptions? launchOptions = null)
        : base(runtimeContext: runtimeContext)
    {
        launchOptions ??= new RaceLaunchOptions();

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
        _frontEndFlow.InitializeForCurrentWorld();
    }
}