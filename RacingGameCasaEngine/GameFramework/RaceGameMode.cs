using CasaEngine.Framework.GameFramework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;
using System.Globalization;

namespace RacingGameCasaEngine.GameFramework;

public sealed class RaceGameMode : GameMode
{
	private readonly List<float> _completedLapTimesSeconds = [];

	public string PlayerName { get; private set; } = "Player One";

	public string SelectedCarName { get; private set; } = "Prototype Car";

	public string SelectedTrackName { get; private set; } = "Prototype Track";

	internal VehicleDrivingMode DrivingMode { get; private set; } = VehicleDrivingMode.Arcade;

	public DateTimeOffset? StartedAtUtc { get; private set; }

	public int TotalLaps { get; private set; } = 2;

	public int CompletedLaps { get; private set; }

	public int TotalCheckpoints { get; private set; }

	public int NextCheckpointIndex { get; private set; }

	public float CountdownSecondsRemaining { get; private set; } = 3.0f;

	public float StartBannerSecondsRemaining { get; private set; }

	public float RaceTimeSeconds { get; private set; }

	public float CurrentLapTimeSeconds { get; private set; }

	public float? LastLapTimeSeconds { get; private set; }

	public float? BestLapTimeSeconds { get; private set; }

	public IReadOnlyList<float> CompletedLapTimesSeconds => _completedLapTimesSeconds;

	public bool IsRaceFinished { get; private set; }

	public bool IsPaused { get; private set; }

	internal void Configure(RaceFrontEndState state)
	{
		PlayerName = state.PlayerName;
		SelectedCarName = RaceFrontEndCatalog.Cars[state.SelectedCarIndex].Name;
		SelectedTrackName = RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex].Name;
		DrivingMode = state.SelectedDrivingMode;
		TotalLaps = ParseLapCount(RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex].Laps);
		CompletedLaps = 0;
		NextCheckpointIndex = 0;
		CountdownSecondsRemaining = 3.0f;
		StartBannerSecondsRemaining = 0.0f;
		RaceTimeSeconds = 0.0f;
		CurrentLapTimeSeconds = 0.0f;
		LastLapTimeSeconds = null;
		BestLapTimeSeconds = null;
		_completedLapTimesSeconds.Clear();
		IsRaceFinished = false;
		IsPaused = false;
	}

	public override void StartMatch()
	{
		base.StartMatch();
		StartedAtUtc ??= DateTimeOffset.UtcNow;
	}

	public void ConfigureCheckpointCount(int checkpointCount)
	{
		TotalCheckpoints = Math.Max(1, checkpointCount);
		NextCheckpointIndex = 0;
	}

	public void UpdateCountdown(float elapsedTime)
	{
		if (StartBannerSecondsRemaining > 0f)
		{
			StartBannerSecondsRemaining = Math.Max(0f, StartBannerSecondsRemaining - elapsedTime);
		}

		if (IsRaceFinished || CountdownSecondsRemaining <= 0f)
		{
			return;
		}

		float previousCountdown = CountdownSecondsRemaining;
		CountdownSecondsRemaining = Math.Max(0f, CountdownSecondsRemaining - elapsedTime);
		if (previousCountdown > 0f && CountdownSecondsRemaining <= 0f)
		{
			StartBannerSecondsRemaining = 0.85f;
		}
	}

	public void UpdateRaceClock(float elapsedTime)
	{
		if (IsRaceFinished || IsPaused || CountdownSecondsRemaining > 0f)
		{
			return;
		}

		RaceTimeSeconds += elapsedTime;
		CurrentLapTimeSeconds += elapsedTime;
	}

	public void TogglePause()
	{
		if (IsRaceFinished || CountdownSecondsRemaining > 0f)
		{
			return;
		}

		IsPaused = !IsPaused;
	}

	public void RegisterCheckpointPass()
	{
		if (IsRaceFinished || TotalCheckpoints <= 0)
		{
			return;
		}

		NextCheckpointIndex++;
		if (NextCheckpointIndex < TotalCheckpoints)
		{
			return;
		}

		NextCheckpointIndex = 0;
		CommitLapTime(CurrentLapTimeSeconds);
		CompletedLaps++;
		if (CompletedLaps >= TotalLaps)
		{
			IsRaceFinished = true;
			EndMatch();
			return;
		}

		CurrentLapTimeSeconds = 0f;
	}

	internal void CompleteRaceForAutomation()
	{
		if (IsRaceFinished)
		{
			return;
		}

		CommitLapTime(CurrentLapTimeSeconds);
		CompletedLaps = TotalLaps;
		NextCheckpointIndex = 0;
		IsRaceFinished = true;
		EndMatch();
	}

	private void CommitLapTime(float lapTimeSeconds)
	{
		if (lapTimeSeconds <= 0f)
		{
			return;
		}

		LastLapTimeSeconds = lapTimeSeconds;
		_completedLapTimesSeconds.Add(lapTimeSeconds);
		if (!BestLapTimeSeconds.HasValue || lapTimeSeconds < BestLapTimeSeconds.Value)
		{
			BestLapTimeSeconds = lapTimeSeconds;
		}
	}

	private static int ParseLapCount(string lapsLabel)
	{
		string[] tokens = lapsLabel.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length > 0
			&& int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int totalLaps))
		{
			return Math.Max(1, totalLaps);
		}

		return 2;
	}
}