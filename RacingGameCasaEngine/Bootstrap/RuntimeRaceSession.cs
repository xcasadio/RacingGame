using System.Text;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RuntimeRaceSession
{
    private const int MaxMovementDebugEntries = 320;
    private readonly List<string> _movementDebugEntries = [];

    public RaceGameMode? GameMode { get; private set; }

    public RacingPlayerController? PlayerController { get; private set; }

    public RacingCarPawn? PlayerPawn { get; private set; }

    public string TrackName { get; private set; } = string.Empty;

    public string CarName { get; private set; } = string.Empty;

    public bool IsDebugCameraEnabled { get; private set; }

    public bool IsCircuitOnlyViewEnabled { get; private set; }

    public bool IsActive => GameMode != null && PlayerController != null && PlayerPawn != null;

    public int MovementDebugEntryCount => _movementDebugEntries.Count;

    public string LatestMovementDebugEntry => _movementDebugEntries.Count == 0
        ? "No movement events captured yet."
        : _movementDebugEntries[^1];

    public void Bind(RaceGameMode gameMode, RacingPlayerController playerController, RacingCarPawn playerPawn)
    {
        GameMode = gameMode;
        PlayerController = playerController;
        PlayerPawn = playerPawn;
        TrackName = gameMode.SelectedTrackName;
        CarName = gameMode.SelectedCarName;
        IsDebugCameraEnabled = false;
        IsCircuitOnlyViewEnabled = false;

        ResetMovementDebugEntries();
        AppendMovementDebug("session", $"bound track='{TrackName}' car='{CarName}'");
        if (playerPawn.RootComponent != null)
        {
            AppendMovementDebug(
                "spawn",
                $"position={FormatVector(playerPawn.RootComponent.Position)} forward={FormatVector(playerPawn.RootComponent.Forward)}");
        }
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

    public void AppendMovementDebug(string category, string message)
    {
        string raceTimeText = GameMode == null
            ? "n/a"
            : $"{GameMode.RaceTimeSeconds:0.000}s";

        string entry = $"{DateTimeOffset.Now:HH:mm:ss.fff} | race={raceTimeText} | {category} | {message}";
        if (_movementDebugEntries.Count == MaxMovementDebugEntries)
        {
            _movementDebugEntries.RemoveAt(0);
        }

        _movementDebugEntries.Add(entry);
    }

    public string BuildMovementDebugReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine("RacingGameCasaEngine movement debug report");
        builder.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        builder.AppendLine($"Track: {TrackName}");
        builder.AppendLine($"Car: {CarName}");
        builder.AppendLine($"Active: {IsActive}");
        builder.AppendLine($"DebugCamera: {IsDebugCameraEnabled}");
        builder.AppendLine($"CircuitOnlyView: {IsCircuitOnlyViewEnabled}");

        if (PlayerPawn?.RootComponent != null)
        {
            builder.AppendLine($"PlayerPosition: {FormatVector(PlayerPawn.RootComponent.Position)}");
            builder.AppendLine($"PlayerForward: {FormatVector(PlayerPawn.RootComponent.Forward)}");
            builder.AppendLine($"SpeedMph: {PlayerPawn.CurrentSpeedMph:0.0}");
            builder.AppendLine($"SteeringInput: {PlayerPawn.SteeringInput:0.000}");
        }

        builder.AppendLine("Entries:");
        if (_movementDebugEntries.Count == 0)
        {
            builder.AppendLine("(none)");
        }
        else
        {
            foreach (string entry in _movementDebugEntries)
            {
                builder.AppendLine(entry);
            }
        }

        return builder.ToString();
    }

    private void ResetMovementDebugEntries()
    {
        _movementDebugEntries.Clear();
    }

    private static string FormatVector(Vector3 vector)
    {
        return $"({vector.X:0.000}, {vector.Y:0.000}, {vector.Z:0.000})";
    }
}