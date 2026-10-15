using CasaEngine.Engine.Input.Providers;
using CasaEngine.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Keeps the player out of an automation run (ADR-0021): the validators drive the screens and the car themselves, so a
/// real click, key or gamepad button must not reach the game. MonoGame DesktopGL reads the mouse buttons of the whole
/// desktop, so without this a click in another application closes the race-finished panel and the run times out.
/// The Options screen reads the typed text from the window itself and checks
/// <see cref="RacingGameCasaEngineGame.IgnoresPlayerInput"/>.
/// </summary>
internal static class AutomationPlayerInput
{
    /// <summary>
    /// Gives the engine an input source that never reports any input, and a UI factory whose MGUI desktops do not read
    /// the gamepad. Call before the game is created: <c>CasaEngineGame</c> keeps a source already set and reads the factory
    /// in its constructor.
    /// </summary>
    public static void Ignore(EngineRuntimeContext runtimeContext)
    {
        runtimeContext.WindowInputSource = NoPlayerInput.Instance;
        runtimeContext.UIViewRuntimeFactory = new NoGamePadUIViewRuntimeFactory(runtimeContext.UIViewRuntimeFactory);
        Logs.WriteInfo("Automation mode: the player input is ignored.");
    }

    /// <summary>Replaces the input component providers, gamepad included, by ones that never report any input.</summary>
    public static void Ignore(InputComponent inputComponent)
    {
        inputComponent.SetProviders(NoPlayerInput.Instance, NoPlayerInput.Instance, NoPlayerInput.Instance);
    }

    private sealed class NoPlayerInput : IWindowInputSource, IKeyboardStateProvider, IMouseStateProvider, IGamePadStateProvider
    {
        public static readonly NoPlayerInput Instance = new();

        public WindowInputSnapshot GetSnapshot() => WindowInputSnapshot.Empty;

        KeyboardState IKeyboardStateProvider.GetState() => WindowInputSnapshot.Empty.KeyboardState;

        MouseState IMouseStateProvider.GetState() => WindowInputSnapshot.Empty.MouseState;

        // The default state is a disconnected gamepad.
        public GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone gamePadDeadZone) => default;
    }

    /// <summary>MGUI reads the gamepad from MonoGame itself, not from the window input source: its tracker is turned off.</summary>
    private sealed class NoGamePadUIViewRuntimeFactory : IUIViewRuntimeFactory
    {
        private readonly IUIViewRuntimeFactory _inner;

        public NoGamePadUIViewRuntimeFactory(IUIViewRuntimeFactory inner)
        {
            _inner = inner;
        }

        public IUIViewRuntime Create(CasaEngineGame game, IRenderSurface surface, EngineRuntimeContext runtimeContext)
        {
            IUIViewRuntime runtime = _inner.Create(game, surface, runtimeContext);
            if (runtime is UIRoot root)
            {
                root.Desktop.InputTracker.GamePad.Enabled = false;
            }

            return runtime;
        }
    }
}
