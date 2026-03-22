using CasaEngine.Framework.GameFramework;
using RacingGameCasaEngine.Bootstrap;
using System.Globalization;

namespace RacingGameCasaEngine.GameFramework;

public sealed class RaceGameMode : GameMode
{
	public string PlayerName { get; private set; } = "Player One";

	public string SelectedCarName { get; private set; } = "Prototype Car";

	public string SelectedTrackName { get; private set; } = "Prototype Track";

	public DateTimeOffset? StartedAtUtc { get; private set; }

	public int TotalLaps { get; private set; } = 2;

	public int CompletedLaps { get; private set; }

	public int TotalCheckpoints { get; private set; }

	public int NextCheckpointIndex { get; private set; }

	public float CountdownSecondsRemaining { get; private set; } = 3.0f;

	public float RaceTimeSeconds { get; private set; }

	public bool IsRaceFinished { get; private set; }

	public bool IsPaused { get; private set; }

	internal void Configure(RaceFrontEndState state)
	{
		PlayerName = state.PlayerName;
		SelectedCarName = RaceFrontEndCatalog.Cars[state.SelectedCarIndex].Name;
		SelectedTrackName = RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex].Name;
		TotalLaps = ParseLapCount(RaceFrontEndCatalog.Tracks[state.SelectedTrackIndex].Laps);
		CompletedLaps = 0;
		NextCheckpointIndex = 0;
		CountdownSecondsRemaining = 3.0f;
		RaceTimeSeconds = 0.0f;
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
		if (IsRaceFinished || CountdownSecondsRemaining <= 0f)
		{
			return;
		}

		CountdownSecondsRemaining = Math.Max(0f, CountdownSecondsRemaining - elapsedTime);
	}

	public void UpdateRaceClock(float elapsedTime)
	{
		if (IsRaceFinished || IsPaused || CountdownSecondsRemaining > 0f)
		{
			return;
		}

		RaceTimeSeconds += elapsedTime;
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
		CompletedLaps++;
		if (CompletedLaps >= TotalLaps)
		{
			IsRaceFinished = true;
			EndMatch();
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