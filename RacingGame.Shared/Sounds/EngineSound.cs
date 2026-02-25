// EngineSound.cs
// Manages engine / gear sounds with pitch and volume interpolation.

using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Properties;

namespace RacingGame.Sounds;

/// <summary>
/// Simulates a multi-gear engine sound by selecting and blending
/// gear cues and smoothly interpolating their pitch and volume.
/// Must have <see cref="Update"/> called every frame while the player
/// is racing.
/// </summary>
internal class EngineSound
{
    #region Constants
    private const int NumberOfGears = 5;
    private const int GearChangeSoundLengthInMs = 1200;
    private const float StayingVol = 0.5f;

    private static readonly float[] Vol =
        new float[NumberOfGears] { 1, 1, 1, 1, 1 };

    private static readonly float[] MinPitch =
        new float[NumberOfGears] { -0.375f, -0.375f, -0.345f, -0.25f, -0.205f };

    private static readonly float[] MaxPitch =
        new float[NumberOfGears] { 0.24f, 0.17f, 0.17f, 0.145f, 0.10f };
    #endregion

    #region Fields
    private readonly SoundBank _soundBank;
    private readonly AudioCategory _gearsCategory;

    private int _currentGear = 0;
    private Cue _currentGearCue = null;
    private Cue _currentGearChangeCue = null;
    private float _gearChangeSoundInitiatedMs = 0;
    private float _lastGearVolume = StayingVol;
    private float _lastGearPitch = 0;
    #endregion

    #region Constructor
    internal EngineSound(SoundBank soundBank, AudioCategory gearsCategory)
    {
        _soundBank = soundBank;
        _gearsCategory = gearsCategory;
    }
    #endregion

    #region Start / Stop
    /// <summary>
    /// Start engine sound at gear 1 (called when the race begins).
    /// </summary>
    internal void Start()
    {
        _currentGear = 0;
        PlayGearCue("Gear1");
        UpdateVolumeAndPitch("Gear1", StayingVol, MinPitch[0]);
    }

    /// <summary>
    /// Stop all engine sounds immediately (called when returning to menu).
    /// </summary>
    internal void Stop()
    {
        _currentGear = 0;

        _currentGearChangeCue?.Stop(AudioStopOptions.Immediate);
        _currentGearChangeCue = null;

        _currentGearCue?.Stop(AudioStopOptions.Immediate);
        _currentGearCue = null;
    }
    #endregion

    #region Private helpers
    /// <summary>
    /// Fire and forget: play a gear or gear-transition cue.
    /// </summary>
    private void PlayGearCue(string soundName)
    {
        if (_soundBank == null)
            return;

        if (soundName.Contains("To"))
        {
            _currentGearChangeCue = _soundBank.GetCue(soundName);
            _currentGearChangeCue.Play();
            _gearChangeSoundInitiatedMs = GearChangeSoundLengthInMs;
            _currentGearCue = null;
        }
        else
        {
            _currentGearCue = _soundBank.GetCue(soundName);
            _currentGearCue.Play();
            _currentGearChangeCue = null;
        }
    }

    /// <summary>
    /// Apply volume and pitch to the XACT categories / cues.
    /// Also handles gear-change countdown and auto-transition to the
    /// steady-state gear cue when the transition sound finishes.
    /// </summary>
    private void UpdateVolumeAndPitch(string gearSound, float volume, float pitch)
    {
        if (_soundBank == null)
            return;

        if (_gearChangeSoundInitiatedMs > 0)
        {
            _gearChangeSoundInitiatedMs -=
                BaseGame.ElapsedTimeThisFrameInMilliseconds;

            if (_gearChangeSoundInitiatedMs <= 0)
            {
                _gearChangeSoundInitiatedMs = 0;
                PlayGearCue(gearSound);
                volume = _lastGearVolume = 1.0f;
                pitch = _lastGearPitch = -0.3f;
            }
        }

        _gearsCategory.SetVolume(
            MathHelper.Clamp(volume, 0, 1) *
            GameSettings.Default.SoundVolume);

        _currentGearCue?.SetVariable("Pitch",
            55 * MathHelper.Clamp(pitch, -1, 1));
    }
    #endregion

    #region Update
    /// <summary>
    /// Update engine sound every frame based on current speed and acceleration.
    /// </summary>
    internal void Update(float speed, float acceleration)
    {
        int newGear = (int)(NumberOfGears * speed / Player.MaxPossibleSpeed);
        newGear = Math.Clamp(newGear, 0, NumberOfGears - 1);

        if (_gearChangeSoundInitiatedMs <= 0)
        {
            if (newGear > _currentGear)
            {
                PlayGearCue("Gear" + newGear + "ToGear" + (newGear + 1));
                _lastGearVolume = 1.0f;
                _lastGearPitch  = 0.0f;
            }
            else if (newGear < _currentGear)
            {
                PlayGearCue("Gear" + (newGear + 1));
                _lastGearVolume = 1.0f;
                _lastGearPitch  = MaxPitch[newGear];
            }
            _currentGear = newGear;
        }

        if (speed < 0)
            speed = MathHelper.Clamp(
                Math.Abs(speed), 0, Player.MaxPossibleSpeed / 5);

        float gearPercentage = (float)
            ((int)(speed / Player.MaxPossibleSpeed * 499) %
             (int)(500f / NumberOfGears)) / 100.0f;
        gearPercentage = MathHelper.Clamp(gearPercentage, 0, 1);

        float minVolume = _currentGear > 0 ? Vol[_currentGear - 1] : StayingVol;
        float volume = MathHelper.Lerp(minVolume, Vol[_currentGear], gearPercentage);
        float pitch  = MathHelper.Lerp(
            MinPitch[_currentGear], MaxPitch[_currentGear], gearPercentage);

        if (_gearChangeSoundInitiatedMs > 0)
            pitch = 0;

        if (acceleration > 0.25f)
        {
            volume = 1.0f;
        }
        else
        {
            volume /= 1.75f;
            pitch = Math.Min(-0.025f, pitch / 1.25f);
            if (_lastGearPitch > pitch)
                _lastGearPitch = _lastGearPitch * 0.9f + pitch * 0.1f;
        }

        _lastGearVolume = MathHelper.Lerp(
            _lastGearVolume, volume, 5.0f * BaseGame.MoveFactorPerSecond);
        _lastGearPitch = MathHelper.Lerp(
            _lastGearPitch, pitch, 5.0f * BaseGame.MoveFactorPerSecond);

        UpdateVolumeAndPitch(
            "Gear" + (_currentGear + 1), _lastGearVolume, _lastGearPitch);
    }
    #endregion
}
