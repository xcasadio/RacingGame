namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RaceFrontEndState
{
    public int SelectedCarIndex { get; set; }

    public int SelectedTrackIndex { get; set; }

    public string PlayerName { get; set; } = "Player One";

    public int SelectedResolutionIndex { get; set; } = 1;

    public int SelectedCarColorIndex { get; set; }

    public bool IsFullscreen { get; set; } = false;

    public bool EnablePostEffects { get; set; } = true;

    public bool EnableShadows { get; set; } = true;

    public bool EnableHighDetail { get; set; } = true;

    public bool ShowFps { get; set; } = false;

    public bool EnableVibration { get; set; } = true;

    public int SoundVolume { get; set; } = 80;

    public int MusicVolume { get; set; } = 70;

    public int ControllerSensitivity { get; set; } = 60;
}