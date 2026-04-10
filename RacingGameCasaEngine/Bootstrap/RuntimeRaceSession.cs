using System.Text;
using System.Globalization;
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

    public string TrackSummary { get; private set; } = string.Empty;

    public string TrackSurface { get; private set; } = string.Empty;

    public string ReferenceBestLapTimeText { get; private set; } = "--:--.--";

    public IReadOnlyList<string> ReferenceLapTimes { get; private set; } = Array.Empty<string>();

    public IReadOnlyList<int> ReferenceLapTimesMilliseconds { get; private set; } = Array.Empty<int>();

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
        RefreshTrackMetadata(TrackName);
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
        TrackSummary = string.Empty;
        TrackSurface = string.Empty;
        ReferenceBestLapTimeText = "--:--.--";
        ReferenceLapTimes = Array.Empty<string>();
        ReferenceLapTimesMilliseconds = Array.Empty<int>();
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

    private void RefreshTrackMetadata(string trackName)
    {
        TrackSummary = string.Empty;
        TrackSurface = string.Empty;
        ReferenceBestLapTimeText = "--:--.--";
        ReferenceLapTimes = Array.Empty<string>();
        ReferenceLapTimesMilliseconds = Array.Empty<int>();

        foreach (TrackDefinition track in RaceFrontEndCatalog.Tracks)
        {
            if (!string.Equals(track.Name, trackName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TrackSummary = track.Summary;
            TrackSurface = track.Surface;
            break;
        }

        if (!RaceFrontEndCatalog.Highscores.TryGetValue(trackName, out IReadOnlyList<HighscoreEntry>? entries)
            || entries.Count == 0)
        {
            return;
        }

        string[] lapTimes = new string[entries.Count];
        int[] lapTimesMilliseconds = new int[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            lapTimes[i] = entries[i].Time;
            lapTimesMilliseconds[i] = ParseLapTimeMilliseconds(entries[i].Time);
        }

        ReferenceBestLapTimeText = lapTimes[0];
        ReferenceLapTimes = lapTimes;
        ReferenceLapTimesMilliseconds = lapTimesMilliseconds;
    }

    private static int ParseLapTimeMilliseconds(string timeText)
    {
        if (string.IsNullOrWhiteSpace(timeText))
        {
            return 0;
        }

        string[] minuteAndSeconds = timeText.Split(':', StringSplitOptions.TrimEntries);
        if (minuteAndSeconds.Length != 2
            || !int.TryParse(minuteAndSeconds[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
        {
            return 0;
        }

        string[] secondsAndCentiseconds = minuteAndSeconds[1].Split('.', StringSplitOptions.TrimEntries);
        if (secondsAndCentiseconds.Length != 2
            || !int.TryParse(secondsAndCentiseconds[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds)
            || !int.TryParse(secondsAndCentiseconds[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int centiseconds))
        {
            return 0;
        }

        return Math.Max(0, (((minutes * 60) + seconds) * 1000) + (centiseconds * 10));
    }

    private static string FormatVector(Vector3 vector)
    {
        return $"({vector.X:0.000}, {vector.Y:0.000}, {vector.Z:0.000})";
    }
}