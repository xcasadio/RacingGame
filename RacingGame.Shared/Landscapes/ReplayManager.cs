using RacingGame.GameLogic;
using RacingGame.Tracks;

namespace RacingGame.Landscapes;

/// <summary>
/// Placeholder for replay responsibilities while the facade is frozen.
/// </summary>
internal sealed class ReplayManager
{
    public Replay BestReplay
    {
        get
        {
            return null;
        }
    }

    public Replay NewReplay
    {
        get
        {
            return null;
        }
    }

    public void ResetForTrack(RacingGameManager.Level level, Track track)
    {
    }

    public int CompareCheckpointTime(int checkpointNum)
    {
        return 0;
    }

    public void StartNewLap()
    {
    }
}