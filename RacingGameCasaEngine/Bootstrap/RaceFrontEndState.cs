using RacingGameCasaEngine.Components;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RaceFrontEndState
{
    public int SelectedCarIndex { get; set; }

    /// <summary>The selected track, Advanced at first as in RacingGame (TrackSelection.selectedButton; ADR-0010). Not saved.</summary>
    public int SelectedTrackIndex { get; set; } = 1;

    /// <summary>The main menu's selected button, kept while the game runs (not saved), so the menu reopens on it.</summary>
    public int SelectedMainMenuButton { get; set; }

    /// <summary>
    /// Set by the UI capture automation (--capture-ui-screens): the screens' time-driven animations that would make two
    /// runs differ (the title's blink, the car carousel's spin, the selection arrows' swing) hold a fixed pose (ADR-0009).
    /// Not saved.
    /// </summary>
    public bool PinScreenAnimations { get; set; }

    public string PlayerName { get; set; } = "Player One";

    public int SelectedResolutionIndex { get; set; } = 1;

    public int SelectedCarColorIndex { get; set; }

    public VehicleDrivingMode SelectedDrivingMode { get; set; } = VehicleDrivingMode.Arcade;

    public bool IsFullscreen { get; set; } = false;

    public bool EnableVSync { get; set; } = true;

    public bool EnablePostEffects { get; set; } = true;

    public bool EnableShadows { get; set; } = true;

    public bool ShowFps { get; set; } = false;

    public bool EnableVibration { get; set; } = true;

    public int SoundVolume { get; set; } = 80;

    public int MusicVolume { get; set; } = 70;

    public int ControllerSensitivity { get; set; } = 60;
}