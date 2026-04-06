using RacingGame.GameLogic;
using RacingGame.GameScreens;
using RacingGame.Tracks;
using System.Threading;

namespace RacingGame.Landscapes;

/// <summary>
/// Manages best replay, current replay and lap transitions.
/// </summary>
internal sealed class ReplayManager
{
    RacingGameManager.Level level = RacingGameManager.Level.Beginner;
    Track track = null;
    Replay bestReplay = null;
    Replay newReplay = null;

    public Replay BestReplay
    {
        get
        {
            return bestReplay;
        }
    }

    public Replay NewReplay
    {
        get
        {
            return newReplay;
        }
    }

    public void ResetForTrack(RacingGameManager.Level level, Track track)
    {
        this.level = level;
        this.track = track;
        bestReplay = new Replay((int)level, false, track);
        newReplay = new Replay((int)level, true, track);
    }

    public int CompareCheckpointTime(int checkpointNum)
    {
        if (bestReplay == null ||
            checkpointNum >= bestReplay.CheckpointTimes.Count)
        {
            return 0;
        }

        float differenceMs =
            RacingGameManager.Player.GameTimeMilliseconds -
            bestReplay.CheckpointTimes[checkpointNum] * 1000.0f;

        return (int)differenceMs;
    }

    public void StartNewLap()
    {
        float thisLapTime =
            RacingGameManager.Player.GameTimeMilliseconds / 1000.0f;

        Highscores.SubmitHighscore((int)level,
            (int)RacingGameManager.Player.GameTimeMilliseconds);

        RacingGameManager.Player.AddLapTime(thisLapTime);

        if (thisLapTime < bestReplay.LapTime)
        {
            newReplay.CheckpointTimes.Add(thisLapTime);
            newReplay.LapTime = thisLapTime;
            ThreadPool.QueueUserWorkItem(new WaitCallback(SaveReplay),
                (Replay)newReplay.Clone());
            bestReplay = newReplay;
        }

        newReplay = new Replay((int)level, true, track);
    }

    static void SaveReplay(object replay)
    {
        ((Replay)replay).Save();
    }
}