// MusicManager.cs
// Handles background music playback and volume control.

using System.Threading;

namespace RacingGame.Sounds;

/// <summary>
/// Manages background music tracks.
/// Owns playback of MenuMusic / GameMusic cues and their category volume.
/// </summary>
internal class MusicManager
{
    #region Fields
    private readonly SoundBank _soundBank;
    private readonly AudioCategory _musicCategory;
    #endregion

    #region Constructor
    internal MusicManager(SoundBank soundBank, AudioCategory musicCategory)
    {
        _soundBank = soundBank;
        _musicCategory = musicCategory;
    }
    #endregion

    #region Play
    /// <summary>
    /// Start playing a music cue (MenuMusic or GameMusic).
    /// </summary>
    internal void Play(string cueName)
    {
        _soundBank?.PlayCue(cueName);
    }
    #endregion

    #region Stop
    /// <summary>
    /// Stop all currently playing music immediately.
    /// Uses the "play + immediate stop" trick required by XACT to silence
    /// whatever is currently on the music bus.
    /// </summary>
    internal void Stop()
    {
        if (_soundBank == null)
            return;

        Cue musicCue = _soundBank.GetCue("MenuMusic");
        musicCue.Play();
        // Wait briefly so XACT kicks in before we stop.
        Thread.Sleep(10);
        musicCue.Stop(AudioStopOptions.Immediate);
    }
    #endregion

    #region Volume
    /// <summary>
    /// Set the music category volume (0–1).
    /// </summary>
    internal void SetVolume(float volume) =>
        _musicCategory.SetVolume(volume);
    #endregion
}
