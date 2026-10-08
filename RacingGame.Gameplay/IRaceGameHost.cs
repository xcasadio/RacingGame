using CasaEngine.Framework.Input;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.GameFramework;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay;

/// <summary>What the game's components read from the game that runs them: the race session, the gamepad vibration, the
/// window and the input. The components live in this assembly and the game (<c>RacingGameCasaEngineGame</c>) in the
/// executable, so they reach it through this interface instead of its type. It is internal: the executable sees it by
/// <c>InternalsVisibleTo</c>, and the editor, which has no such game, never needs it.</summary>
internal interface IRaceGameHost
{
    RuntimeRaceSession RaceSession { get; }

    RaceGamepadVibration RaceVibration { get; }

    bool IsActive { get; }

    GameWindow Window { get; }

    InputComponent InputComponent { get; }
}
