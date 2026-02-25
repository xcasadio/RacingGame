// Sound.cs
// Thin static façade — all implementation lives in MusicManager, SfxManager,
// and EngineSound.  Callers use the same API as before.

using RacingGame.Helpers;
using RacingGame.Properties;

namespace RacingGame.Sounds;

/// <summary>
/// Static façade for all game audio.
/// Initialises the XACT audio engine and delegates every operation to one
/// of three focused managers:
/// <list type="bullet">
///   <item><see cref="MusicManager"/> — background music tracks</item>
///   <item><see cref="SfxManager"/> — one-shot SFX with cooldown guards</item>
///   <item><see cref="EngineSound"/> — gear-shifting engine audio</item>
/// </list>
/// </summary>
class Sound
{
    #region Sound name enumeration
    /// <summary>All named sounds used by the game (SFX and music).</summary>
    public enum Sounds
    {
        // Menu sounds
        ButtonClick,
        ScreenClick,
        ScreenBack,
        Highlight,
        // Game sounds
        Beep,
        Bleep,
        BrakeCurveMajor,
        BrakeCurveMinor,
        BrakeMajor,
        BrakeMinor,
        CarCrashMinor,
        CarCrashTotal,
        // Result sounds
        CheckpointBetter,
        CheckpointWorse,
        Victory,
        CarLose,
        // Music
        MenuMusic,
        GameMusic,
    }
    #endregion

    #region Private XACT state
    private static AudioEngine _audioEngine;
    private static WaveBank _waveBank;
    private static SoundBank _soundBank;

    private static MusicManager _music;
    private static SfxManager _sfx;
    private static EngineSound _engine;
    #endregion

    #region Constructor (prevent instantiation)
    private Sound() { }
    #endregion

    #region Initialize
    /// <summary>
    /// Initialise the XACT audio engine and create the three audio managers.
    /// Call once at startup before any audio is needed.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            string dir = Directories.SoundsDirectory;
            _audioEngine = new AudioEngine(
                Path.Combine(dir, "RacingGameManager.xgs"));
            _waveBank = new WaveBank(
                _audioEngine, Path.Combine(dir, "Wave Bank.xwb"));

            if (_waveBank != null)
            {
                _soundBank = new SoundBank(
                    _audioEngine, Path.Combine(dir, "Sound Bank.xsb"));
            }

            var defaultCategory = _audioEngine.GetCategory("Default");
            var gearsCategory   = _audioEngine.GetCategory("Gears");
            var musicCategory   = _audioEngine.GetCategory("Music");

            _music  = new MusicManager(_soundBank, musicCategory);
            _sfx    = new SfxManager(_soundBank, defaultCategory);
            _engine = new EngineSound(_soundBank, gearsCategory);

            SetVolumes(GameSettings.Default.SoundVolume,
                       GameSettings.Default.MusicVolume);
        }
        catch (NoAudioHardwareException ex)
        {
            Log.Write("Failed to initialise audio: " + ex.ToString());
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Tick the audio engine and SFX cooldown timers.
    /// Must be called once per game-loop frame.
    /// </summary>
    public static void Update()
    {
        _sfx?.Update();
        _audioEngine?.Update();
    }
    #endregion

    #region Volume
    /// <summary>
    /// Set master SFX and music volumes (0–1).
    /// </summary>
    public static void SetVolumes(float soundVolume, float musicVolume)
    {
        _sfx?.SetVolume(soundVolume);
        _music?.SetVolume(musicVolume);
    }
    #endregion

    #region SFX playback
    /// <summary>Play any sound or music cue by its XACT name.</summary>
    public static void Play(string soundName) =>
        _sfx?.Play(soundName);

    /// <summary>Play any sound or music cue by enum value.</summary>
    public static void Play(Sounds sound)
    {
        // Music cues are routed to the music manager so volume is correct.
        if (sound == Sounds.MenuMusic || sound == Sounds.GameMusic)
            _music?.Play(sound.ToString());
        else
            _sfx?.Play(sound.ToString());
    }

    /// <summary>Play a brake sound, honouring the cooldown timer.</summary>
    public static void PlayBrakeSound(Sounds soundBrakeType) =>
        _sfx?.PlayBrakeSound(soundBrakeType);

    /// <summary>Derive the most appropriate brake sound for the given state.</summary>
    public static Sounds GetBreakSoundType(
        float speed, float speedChange, float rotationChange) =>
        SfxManager.GetBreakSoundType(speed, speedChange, rotationChange);

    /// <summary>Play a crash sound, honouring the cooldown timer.</summary>
    public static void PlayCrashSound(bool totalCrash) =>
        _sfx?.PlayCrashSound(totalCrash);
    #endregion

    #region Music control
    /// <summary>Stop all currently playing music tracks.</summary>
    public static void StopMusic() =>
        _music?.Stop();
    #endregion

    #region Engine / gear sounds
    /// <summary>Start engine sounds at gear 1 (call when race starts).</summary>
    public static void StartGearSound() =>
        _engine?.Start();

    /// <summary>Stop all engine sounds (call when returning to menu).</summary>
    public static void StopGearSound() =>
        _engine?.Stop();

    /// <summary>
    /// Update engine pitch and volume based on current speed and acceleration.
    /// Must be called every frame while the game screen is active.
    /// </summary>
    public static void UpdateGearSound(float speed, float acceleration) =>
        _engine?.Update(speed, acceleration);
    #endregion
}
