// SfxManager.cs
// Manages one-shot sound effects with per-category cooldown protection.

using RacingGame.GameLogic;
using RacingGame.Graphics;

namespace RacingGame.Sounds;

/// <summary>
/// Manages one-shot sound effects.
/// Handles brake and crash cooldown timers so the same sounds do not stack.
/// </summary>
internal class SfxManager
{
    #region Fields
    private readonly SoundBank _soundBank;
    private readonly AudioCategory _defaultCategory;

    /// <summary>Cooldown remaining for the current brake sound (ms).</summary>
    private float _brakeSoundStillPlayingMs = 1000;

    /// <summary>Cooldown remaining for the current crash sound (ms).</summary>
    private float _crashSoundStillPlayingMs = 2000;
    #endregion

    #region Constructor
    internal SfxManager(SoundBank soundBank, AudioCategory defaultCategory)
    {
        _soundBank = soundBank;
        _defaultCategory = defaultCategory;
    }
    #endregion

    #region Play
    /// <summary>
    /// Play a sound cue by name.
    /// </summary>
    internal void Play(string soundName)
    {
        _soundBank?.PlayCue(soundName);
    }
    #endregion

    #region Brake sounds
    /// <summary>
    /// Play a brake sound, respecting the cooldown so sounds never stack.
    /// Ignored while the game is in menu mode.
    /// </summary>
    internal void PlayBrakeSound(Sound.Sounds soundBrakeType)
    {
        if (_brakeSoundStillPlayingMs > 0 || RacingGameManager.InMenu)
            return;

        Play(soundBrakeType.ToString());

        _brakeSoundStillPlayingMs = soundBrakeType switch
        {
            Sound.Sounds.BrakeMinor      => 750,
            Sound.Sounds.BrakeMajor      => 2500,
            Sound.Sounds.BrakeCurveMinor => 1250,
            Sound.Sounds.BrakeCurveMajor => 3500,
            _                            => 750,
        };
    }

    /// <summary>
    /// Determine the most appropriate brake sound for the current
    /// physical state of the car.
    /// </summary>
    internal static Sound.Sounds GetBreakSoundType(
        float speed, float speedChange, float rotationChange)
    {
        bool inRotation = rotationChange >
            0.25f * Player.MaxRotationPerSec * BaseGame.MoveFactorPerSecond;

        Sound.Sounds soundBrakeType = inRotation
            ? Sound.Sounds.BrakeCurveMinor
            : Sound.Sounds.BrakeMinor;

        if (speed > 1.5f &&
            Math.Abs(speedChange) > 5 * BaseGame.MoveFactorPerSecond)
        {
            soundBrakeType = inRotation
                ? Sound.Sounds.BrakeCurveMajor
                : Sound.Sounds.BrakeMajor;
        }

        return soundBrakeType;
    }
    #endregion

    #region Crash sounds
    /// <summary>
    /// Play a crash sound, respecting the cooldown so sounds never stack.
    /// Ignored while in menu mode.
    /// </summary>
    internal void PlayCrashSound(bool totalCrash)
    {
        if (_crashSoundStillPlayingMs > 0 || RacingGameManager.InMenu)
            return;

        Play(totalCrash
            ? Sound.Sounds.CarCrashTotal.ToString()
            : Sound.Sounds.CarCrashMinor.ToString());

        _crashSoundStillPlayingMs = totalCrash ? 3456 : 2345;
    }
    #endregion

    #region Volume
    /// <summary>
    /// Set the default (SFX) category volume (0–1).
    /// </summary>
    internal void SetVolume(float volume) =>
        _defaultCategory.SetVolume(volume);
    #endregion

    #region Update
    /// <summary>
    /// Tick cooldown timers. Must be called once per frame.
    /// </summary>
    internal void Update()
    {
        if (_brakeSoundStillPlayingMs > 0)
            _brakeSoundStillPlayingMs -= BaseGame.ElapsedTimeThisFrameInMilliseconds;

        if (_crashSoundStillPlayingMs > 0)
            _crashSoundStillPlayingMs -= BaseGame.ElapsedTimeThisFrameInMilliseconds;
    }
    #endregion
}
