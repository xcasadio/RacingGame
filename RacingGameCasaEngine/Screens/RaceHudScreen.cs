using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GameFramework;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI.ViewModels;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

/// <summary>
/// Race HUD, loaded from the <c>Screen.RaceHud</c> screen asset (Content/UI/Screens/RaceHud): panels and digits are
/// sprites, times and names are text blocks, the tachometer needle is turned by a bound render transform. The values come
/// from <see cref="RaceHudViewModel"/>, refreshed every frame from the race session; the input that closes the
/// race-finished panel stays in code.
/// </summary>
internal sealed class RaceHudScreen : RaceXamlScreenBase
{
    private const int TopTimeCount = 5;

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _returnToMenu;
    private readonly RaceHudViewModel _viewModel = new();
    private bool _returnRequested;

    public RaceHudScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, RaceFrontEndState state, Action returnToMenu)
        : base(assetContentManager, "Screen.RaceHud")
    {
        _game = game;
        _state = state;
        _returnToMenu = returnToMenu;
    }

    public override UILayer Layer => UILayer.HUD;

    protected override void OnWindowLoaded(MGWindow window)
    {
        Refresh();
        window.WindowDataContext = _viewModel;
    }

    public override void Show()
    {
        _returnRequested = false;
        Refresh();
    }

    public override void Update(GameTime gameTime)
    {
        _ = gameTime;
        Refresh();

        if (!_returnRequested && IsGameOver && IsDismissPressed())
        {
            _returnRequested = true;
            _returnToMenu();
        }
    }

    private void Refresh()
    {
        _viewModel.UpdateTextSizes(Root.Desktop.ResponsiveMetrics.UIScaleFactor);
        _viewModel.Lap.Value = GetCurrentLapDisplay();
        _viewModel.CurrentLapTime = FormatMilliseconds(GetCurrentLapTimeMilliseconds());
        _viewModel.BestLapTime = FormatMilliseconds(GetBestLapTimeMilliseconds());
        _viewModel.TrackName = GetTrackName();

        IReadOnlyList<int> topTimes = GetTopLapTimesMilliseconds();
        for (int i = 0; i < TopTimeCount; i++)
        {
            _viewModel.SetTopTime(i, i < topTimes.Count && topTimes[i] > 0
                ? FormatMilliseconds(topTimes[i])
                : "--:--.--");
        }

        // Same angle as the code-built needle (-2.33 + acceleration * 2.5 radians), in degrees for the render transform.
        float acceleration = Math.Clamp(GetTachometerNeedleValue(), 0f, 1f);
        _viewModel.NeedleRotation = MathHelper.ToDegrees(-2.33f + acceleration * 2.5f);
        _viewModel.Speed.Value = GetHudSpeedDisplay();
        _viewModel.Gear.Value = GetHudGearDisplay();

        _viewModel.GameOverVisibility = IsGameOver ? Visibility.Visible : Visibility.Collapsed;
        _viewModel.GameOverTitle = IsGameOver ? "Victory! You won." : string.Empty;
        _viewModel.SetGameOverLines(GetGameOverLines());
        _viewModel.ExitHint = IsGameOver
            ? "Press Space, Enter, A, B, X, click, Start, or Back to return to menu."
            : string.Empty;
    }

    private bool IsGameOver => _game.RaceSession.GameMode?.IsRaceFinished == true;

    private bool IsDismissPressed()
    {
        if (_game.InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.Enter)
            || _game.InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.Space)
            || _game.InputComponent.KeyboardManager.IsKeyJustPressed(XnaKeys.Escape)
            || _game.InputComponent.MouseManager.LeftButtonJustPressed)
        {
            return true;
        }

        if (_game.RaceSession.PlayerController?.Player is not LocalPlayer localPlayer)
        {
            return false;
        }

        var gamePad = _game.InputComponent.GamePadManager.GetGamePad(localPlayer.ControllerId);
        return gamePad.IsConnected
            && (gamePad.AJustPressed || gamePad.BJustPressed || gamePad.XJustPressed || gamePad.BackJustPressed || gamePad.StartJustPressed);
    }

    private bool TryGetActiveRace(out RuntimeRaceSession session, out GameFramework.RaceGameMode gameMode, out Entities.RacingCarPawn playerPawn)
    {
        session = _game.RaceSession;
        gameMode = null!;
        playerPawn = null!;

        if (!session.IsActive || session.GameMode == null || session.PlayerPawn == null)
        {
            return false;
        }

        gameMode = session.GameMode;
        playerPawn = session.PlayerPawn;
        return true;
    }

    private int GetCurrentLapDisplay()
    {
        if (!TryGetActiveRace(out _, out GameFramework.RaceGameMode gameMode, out _))
        {
            return 1;
        }

        return Math.Min(gameMode.CompletedLaps + 1, gameMode.TotalLaps);
    }

    private int GetCurrentLapTimeMilliseconds()
    {
        if (!TryGetActiveRace(out _, out GameFramework.RaceGameMode gameMode, out _))
        {
            return 0;
        }

        if (gameMode.IsRaceFinished && gameMode.BestLapTimeSeconds.HasValue)
        {
            return ToMilliseconds(gameMode.BestLapTimeSeconds.Value);
        }

        return ToMilliseconds(gameMode.CurrentLapTimeSeconds);
    }

    private int GetBestLapTimeMilliseconds()
    {
        return TryGetActiveRace(out _, out GameFramework.RaceGameMode gameMode, out _)
            && gameMode.BestLapTimeSeconds.HasValue
            ? ToMilliseconds(gameMode.BestLapTimeSeconds.Value)
            : 0;
    }

    private string GetTrackName()
    {
        return _game.RaceSession.TrackName.Length > 0
            ? _game.RaceSession.TrackName
            : RaceFrontEndCatalog.Tracks[_state.SelectedTrackIndex].Name;
    }

    private IReadOnlyList<int> GetTopLapTimesMilliseconds()
    {
        RuntimeRaceSession session = _game.RaceSession;
        if (session.ReferenceLapTimesMilliseconds.Count > 0)
        {
            return session.ReferenceLapTimesMilliseconds;
        }

        string trackName = RaceFrontEndCatalog.Tracks[_state.SelectedTrackIndex].Name;
        if (!RaceFrontEndCatalog.Highscores.TryGetValue(trackName, out IReadOnlyList<HighscoreEntry>? entries)
            || entries.Count == 0)
        {
            return Array.Empty<int>();
        }

        int[] times = new int[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            times[i] = ParseLapTimeMilliseconds(entries[i].Time);
        }

        return times;
    }

    private int GetHudSpeedDisplay()
    {
        return TryGetActiveRace(out _, out GameFramework.RaceGameMode gameMode, out Entities.RacingCarPawn playerPawn) && !gameMode.IsRaceFinished
            ? (int)Math.Round(playerPawn.CurrentSpeedMph)
            : 0;
    }

    private int GetHudGearDisplay()
    {
        return TryGetActiveRace(out _, out GameFramework.RaceGameMode gameMode, out Entities.RacingCarPawn playerPawn) && !gameMode.IsRaceFinished
            ? playerPawn.CurrentGear
            : 1;
    }

    private float GetTachometerNeedleValue()
    {
        if (!TryGetActiveRace(out _, out GameFramework.RaceGameMode gameMode, out Entities.RacingCarPawn playerPawn) || gameMode.IsRaceFinished)
        {
            return 0f;
        }

        float speedRatio = playerPawn.TargetTopSpeedMph <= 0.01f
            ? 0f
            : Math.Clamp(playerPawn.CurrentSpeedMph / playerPawn.TargetTopSpeedMph, 0f, 1f);
        return (0.5f * speedRatio) + (0.5f * playerPawn.TachometerAcceleration);
    }

    private IReadOnlyList<string> GetGameOverLines()
    {
        if (!TryGetActiveRace(out RuntimeRaceSession session, out GameFramework.RaceGameMode gameMode, out _))
        {
            return Array.Empty<string>();
        }

        var lines = new List<string>();
        for (int i = 0; i < gameMode.CompletedLapTimesSeconds.Count; i++)
        {
            lines.Add($"Lap {i + 1} Time: {FormatMilliseconds(ToMilliseconds(gameMode.CompletedLapTimesSeconds[i]))}");
        }

        int bestLapMilliseconds = GetBestLapTimeMilliseconds();
        if (bestLapMilliseconds > 0)
        {
            lines.Add($"Rank: {GetRank(bestLapMilliseconds, session.ReferenceLapTimesMilliseconds)}");
        }

        return lines;
    }

    private static int GetRank(int bestLapMilliseconds, IReadOnlyList<int> topLapTimes)
    {
        int rank = 1;
        for (int i = 0; i < topLapTimes.Count; i++)
        {
            if (topLapTimes[i] > 0 && bestLapMilliseconds > topLapTimes[i])
            {
                rank++;
            }
        }

        return rank;
    }

    private static int ToMilliseconds(float seconds)
    {
        return (int)Math.Round(Math.Max(0f, seconds) * 1000f);
    }

    private static int ParseLapTimeMilliseconds(string timeText)
    {
        if (string.IsNullOrWhiteSpace(timeText))
        {
            return 0;
        }

        string[] minuteAndSeconds = timeText.Split(':', StringSplitOptions.TrimEntries);
        if (minuteAndSeconds.Length != 2 || !int.TryParse(minuteAndSeconds[0], out int minutes))
        {
            return 0;
        }

        string[] secondsAndCentiseconds = minuteAndSeconds[1].Split('.', StringSplitOptions.TrimEntries);
        if (secondsAndCentiseconds.Length != 2
            || !int.TryParse(secondsAndCentiseconds[0], out int seconds)
            || !int.TryParse(secondsAndCentiseconds[1], out int centiseconds))
        {
            return 0;
        }

        return (((minutes * 60) + seconds) * 1000) + (centiseconds * 10);
    }

    private static string FormatMilliseconds(int timeMilliseconds)
    {
        return
            (timeMilliseconds < 0 ? "-" : string.Empty) +
            ((Math.Abs(timeMilliseconds) / 1000) / 60) + ":" +
            ((Math.Abs(timeMilliseconds) / 1000) % 60).ToString("00") + "." +
            ((Math.Abs(timeMilliseconds) / 10) % 100).ToString("00");
    }
}
