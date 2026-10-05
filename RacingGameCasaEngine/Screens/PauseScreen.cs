using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI.ViewModels;

namespace RacingGameCasaEngine.Screens;

/// <summary>Race pause, loaded from the <c>Screen.Pause</c> screen asset (Content/UI/Screens/Pause).</summary>
internal sealed class PauseScreen : RaceXamlScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly Action _resume;
    private readonly Action _returnToMenu;
    private readonly RacePauseViewModel _viewModel = new();
    private MGButton? _resumeButton;

    public PauseScreen(AssetContentManager assetContentManager, RacingGameCasaEngineGame game, Action resume, Action returnToMenu)
        : base(assetContentManager, "Screen.Pause")
    {
        _game = game;
        _resume = resume;
        _returnToMenu = returnToMenu;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        RefreshSummary();
        window.WindowDataContext = _viewModel;

        _resumeButton = FindControl<MGButton>("btnResume");
        _resumeButton.AddCommandHandler((_, _) => _resume());
        FindControl<MGButton>("btnMainMenu").AddCommandHandler((_, _) => _returnToMenu());
    }

    public override void Show()
    {
        RefreshSummary();
        _resumeButton?.Focus();
    }

    public override void Update(GameTime gameTime)
    {
        _ = gameTime;
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        RuntimeRaceSession session = _game.RaceSession;
        if (!session.IsActive || session.GameMode == null)
        {
            _viewModel.Summary = "Race session unavailable.";
            return;
        }

        int displayedLap = Math.Min(session.GameMode.CompletedLaps + 1, session.GameMode.TotalLaps);
        _viewModel.Summary = $"{session.TrackName} | Lap {displayedLap}/{session.GameMode.TotalLaps} | Total {FormatTime(session.GameMode.RaceTimeSeconds)}";
    }

    private static string FormatTime(float seconds)
    {
        if (seconds <= 0f)
        {
            return "00:00.00";
        }

        int minutes = (int)(seconds / 60f);
        float remainingSeconds = seconds - minutes * 60f;
        return $"{minutes:00}:{remainingSeconds:00.00}";
    }
}
