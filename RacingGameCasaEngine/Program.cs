using System;
using System.IO;
using System.Linq;
using CasaEngine.Core.Log;
using CasaEngine.Engine;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Game;
using CasaEngine.Framework.GUI;
using RacingGameCasaEngine.Bootstrap;

public static class Program
{
    private const string ApplicationName = "RacingGameCasaEngine";
    private const string DisplaySettingsFileName = "display-settings.json";
    private const string FrontEndOptionsFileName = "front-end-options.json";

    [STAThread]
    private static void Main()
    {
        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        Logs.AddLogger(new DebugLogger());
        string logFileName = Path.Combine(AppContext.BaseDirectory, $"racinggame-casaengine-{Environment.ProcessId}.log");
        Logs.AddLogger(new FileLogger(logFileName));
        Logs.Verbosity = LogVerbosity.Trace;

        string projectPath = Path.Combine(AppContext.BaseDirectory, "Content");
        EngineEnvironment.ProjectPath = projectPath;
        AssetCatalog.Load(Path.Combine(projectPath, "AssetInfos.json"));

        var runtimeContext = GameSettings.CreateRuntimeContext();
        runtimeContext.UIViewRuntimeFactory = new MguiViewRuntimeFactory();
        runtimeContext.ProjectSettings.ProjectName = ApplicationName;
        string userSettingsDirectory = GetUserSettingsDirectory(runtimeContext.ProjectSettings.ProjectName);
        string displaySettingsPath = Path.Combine(userSettingsDirectory, DisplaySettingsFileName);
        string frontEndOptionsPath = Path.Combine(userSettingsDirectory, FrontEndOptionsFileName);

        DisplaySettings persistedDisplaySettings = DisplaySettingsPersistence.Load(
            displaySettingsPath,
            new DisplaySettings(
                runtimeContext.ProjectSettings.DebugWidth,
                runtimeContext.ProjectSettings.DebugHeight,
                runtimeContext.ProjectSettings.DebugIsFullScreen,
                runtimeContext.ProjectSettings.VSyncEnabled));

        runtimeContext.ProjectSettings.DebugWidth = persistedDisplaySettings.Width;
        runtimeContext.ProjectSettings.DebugHeight = persistedDisplaySettings.Height;
        runtimeContext.ProjectSettings.DebugIsFullScreen = persistedDisplaySettings.IsFullScreen;
        runtimeContext.ProjectSettings.VSyncEnabled = persistedDisplaySettings.IsVSyncEnabled;

        var launchOptions = new RaceLaunchOptions
        {
            ValidateFrontEndNavigation = args.Contains("--smoke-frontend", StringComparer.OrdinalIgnoreCase),
        };

        using var game = new RacingGameCasaEngineGame(runtimeContext, displaySettingsPath, frontEndOptionsPath, launchOptions);
        game.Run();
    }

    private static string GetUserSettingsDirectory(string projectName)
    {
        string effectiveProjectName = string.IsNullOrWhiteSpace(projectName)
            || string.Equals(projectName, "Project name undefined", StringComparison.Ordinal)
            ? ApplicationName
            : projectName;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CasaEngine",
            effectiveProjectName);
    }
}