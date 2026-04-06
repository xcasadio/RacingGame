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

        bool validateFrontEndNavigation = args.Contains("--smoke-frontend", StringComparer.OrdinalIgnoreCase);
        bool captureTrackAudit = args.Contains("--capture-track-audit", StringComparer.OrdinalIgnoreCase);
        bool exportTrackRuntimeScene = args.Contains("--export-track-runtime-scene", StringComparer.OrdinalIgnoreCase);
        EnsureSingleAutomationMode(validateFrontEndNavigation, captureTrackAudit, exportTrackRuntimeScene);

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
            ValidateFrontEndNavigation = validateFrontEndNavigation,
            CaptureTrackAudit = captureTrackAudit,
            ExportTrackRuntimeScene = exportTrackRuntimeScene,
            RuntimeSceneExportFilePath = GetOptionValue(args, "--track-runtime-export-file"),
        };

        using var game = new RacingGameCasaEngineGame(runtimeContext, displaySettingsPath, frontEndOptionsPath, launchOptions)
        {
            Arguments = args,
            ContentPath = projectPath,
        };
        game.Run();
    }

    private static void EnsureSingleAutomationMode(bool validateFrontEndNavigation, bool captureTrackAudit, bool exportTrackRuntimeScene)
    {
        int enabledModes = 0;
        if (validateFrontEndNavigation)
        {
            enabledModes++;
        }

        if (captureTrackAudit)
        {
            enabledModes++;
        }

        if (exportTrackRuntimeScene)
        {
            enabledModes++;
        }

        if (enabledModes > 1)
        {
            throw new InvalidOperationException("Choose only one automation mode at a time.");
        }
    }

    private static string? GetOptionValue(string[] args, string optionName)
    {
        for (int index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {optionName}.");
            }

            return args[index + 1];
        }

        return null;
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