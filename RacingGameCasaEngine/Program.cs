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

        var launchOptions = new RaceLaunchOptions
        {
            ValidateFrontEndNavigation = args.Contains("--smoke-frontend", StringComparer.OrdinalIgnoreCase),
        };

        using var game = new RacingGameCasaEngineGame(runtimeContext, launchOptions);
        game.Run();
    }
}