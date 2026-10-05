using CasaEngine.Core.Log;
using CasaEngine.Engine.Input;
using CasaEngine.Framework.GameFramework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;

namespace RacingGameCasaEngine.GameFramework;

/// <summary>
/// Gamepad vibration on guard-rail hits, ported from RacingGame (<c>RacingGame.Shared/GameLogic/CarPhysics.cs</c>,
/// <c>ApplyVibration</c> and its countdown): a glancing hit vibrates at 0.35 for 0.25 s, a frontal hit at 0.85 for 0.40 s.
/// RacingGame calls a hit glancing when the car's right and the rail normal are less than 45 degrees apart; with the
/// impact strength |forward . normal| of the RacingGameCasaEngine solvers, that is a strength under sqrt(2)/2.
/// <para/>
/// As in RacingGame, every frame in contact applies its intensity and keeps the longer of the running and the new
/// durations, and nothing happens when the Gamepad Vibration option is off. The vibration stops when it runs out, and when
/// the race is paused, finished or left. The local player's gamepad is driven even when it reports itself disconnected,
/// as RacingGame did: setting the vibration of an absent pad is harmless.
/// </summary>
internal sealed class RaceGamepadVibration
{
    private const float GlancingIntensity = 0.35f;
    private const float GlancingDurationSeconds = 0.25f;
    private const float FrontalIntensity = 0.85f;
    private const float FrontalDurationSeconds = 0.40f;
    private static readonly float GlancingImpactLimit = MathF.Sqrt(2f) / 2f;

    private readonly RacingGameCasaEngineGame _game;
    private readonly Func<bool> _isEnabled;
    private float _remainingSeconds;
    private float _intensity;
    // The pad set vibrating, kept so a stop reaches it even once the race session no longer has a player.
    private GamePad? _vibratingGamePad;

    public RaceGamepadVibration(RacingGameCasaEngineGame game, Func<bool> isEnabled)
    {
        _game = game;
        _isEnabled = isEnabled;
    }

    /// <summary>Reports a guard-rail contact of <paramref name="pawn"/> when it is the race's player car.</summary>
    public static void ReportPlayerBarrierContact(RacingCarPawn pawn, RuntimeRaceSession? session, float impactStrength)
    {
        if (session?.PlayerPawn is { } playerPawn
            && ReferenceEquals(playerPawn, pawn)
            && pawn.World?.Game is RacingGameCasaEngineGame game)
        {
            game.RaceVibration.ReportBarrierContact(impactStrength);
        }
    }

    /// <summary>A guard-rail contact of the player's car this frame, with its impact strength |forward . normal| in [0, 1].</summary>
    public void ReportBarrierContact(float impactStrength)
    {
        if (!_isEnabled())
        {
            return;
        }

        bool glancing = impactStrength < GlancingImpactLimit;
        float intensity = glancing ? GlancingIntensity : FrontalIntensity;
        float durationSeconds = glancing ? GlancingDurationSeconds : FrontalDurationSeconds;

        if (_remainingSeconds <= 0f || intensity != _intensity)
        {
            Logs.WriteTrace($"Gamepad vibration: {(glancing ? "glancing" : "frontal")} hit, impact={impactStrength:0.00}, intensity={intensity:0.00}, duration={durationSeconds:0.00}s");
        }

        SetMotors(intensity);
        _intensity = intensity;
        if (durationSeconds > _remainingSeconds)
        {
            _remainingSeconds = durationSeconds;
        }
    }

    /// <summary>Counts the running vibration down while the race runs, and stops it when it runs out.</summary>
    public void Update(float elapsedSeconds)
    {
        if (_remainingSeconds <= 0f)
        {
            return;
        }

        _remainingSeconds -= elapsedSeconds;
        if (_remainingSeconds <= 0f)
        {
            Stop("expired");
        }
    }

    /// <summary>Stops a running vibration at once (pause, end of race, leaving the race).</summary>
    public void Stop(string reason)
    {
        if (_vibratingGamePad == null && _remainingSeconds <= 0f)
        {
            return;
        }

        _vibratingGamePad?.SetVibration(0f, 0f);
        Logs.WriteTrace($"Gamepad vibration: stopped ({reason})");
        _vibratingGamePad = null;
        _remainingSeconds = 0f;
        _intensity = 0f;
    }

    private void SetMotors(float intensity)
    {
        GamePad? gamePad = _game.RaceSession.PlayerController?.Player is LocalPlayer localPlayer
            ? _game.InputComponent.GamePadManager.GetGamePad(localPlayer.ControllerId)
            : _vibratingGamePad;
        gamePad?.SetVibration(intensity, intensity);
        _vibratingGamePad = gamePad;
    }
}
