using CasaEngine.Engine.Plugins;

namespace RacingGame.Gameplay;

/// <summary>Entry point that CasaEngine runs when it loads this assembly as the project's gameplay DLL (<c>GameplayDllName</c>
/// of <c>RacingGameCasaEngine/Content/RacingGameCasaEngine.json</c>). Nothing needs registering: the engine and the editor
/// find the game's types by name. The editor runs it again on every project open and script reload, so it must stay
/// repeatable.</summary>
public sealed class Plugin : IPlugin
{
    public void Initialize()
    {
    }
}
