using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RuntimeRaceSession
{
    public RaceGameMode? GameMode { get; private set; }

    public RacingPlayerController? PlayerController { get; private set; }

    public RacingCarPawn? PlayerPawn { get; private set; }

    public string TrackName { get; private set; } = string.Empty;

    public string CarName { get; private set; } = string.Empty;

    public bool IsDebugCameraEnabled { get; private set; }

    public bool IsCircuitOnlyViewEnabled { get; private set; }

    public bool IsActive => GameMode != null && PlayerController != null && PlayerPawn != null;

    public void Bind(RaceGameMode gameMode, RacingPlayerController playerController, RacingCarPawn playerPawn)
    {
        GameMode = gameMode;
        PlayerController = playerController;
        PlayerPawn = playerPawn;
        TrackName = gameMode.SelectedTrackName;
        CarName = gameMode.SelectedCarName;
        IsDebugCameraEnabled = false;
        IsCircuitOnlyViewEnabled = false;
    }

    public bool ToggleDebugCamera()
    {
        IsDebugCameraEnabled = !IsDebugCameraEnabled;
        return IsDebugCameraEnabled;
    }

    public void SetDebugCameraEnabled(bool enabled)
    {
        IsDebugCameraEnabled = enabled;
    }

    public bool ToggleCircuitOnlyView()
    {
        IsCircuitOnlyViewEnabled = !IsCircuitOnlyViewEnabled;
        return IsCircuitOnlyViewEnabled;
    }

    public void SetCircuitOnlyViewEnabled(bool enabled)
    {
        IsCircuitOnlyViewEnabled = enabled;
    }

    public void Clear()
    {
        GameMode = null;
        PlayerController = null;
        PlayerPawn = null;
        TrackName = string.Empty;
        CarName = string.Empty;
        IsDebugCameraEnabled = false;
        IsCircuitOnlyViewEnabled = false;
    }
}