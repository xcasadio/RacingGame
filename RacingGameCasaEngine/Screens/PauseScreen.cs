using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Screens;

internal sealed class PauseScreen : RaceFrontEndScreenBase
{
    private readonly RacingGameCasaEngineGame _game;
    private readonly Action _resume;
    private readonly Action _returnToMenu;
    private MGTextBlock? _summary;
    private MGButton? _resumeButton;

    public PauseScreen(RacingGameCasaEngineGame game, Action resume, Action returnToMenu)
        : base(backgroundTexture: null)
    {
        _game = game;
        _resume = resume;
        _returnToMenu = returnToMenu;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        MGWindow window = CreateForegroundWindow(root);
        window.BackgroundBrush = new VisualStateFillBrush(new Color(0, 0, 0, 170).AsFillBrush());

        var panel = RaceUiTheme.CreatePanel(window, 560);
        var content = RaceUiTheme.CreateVerticalStack(window, spacing: 14);
        _summary = RaceUiTheme.CreateBody(window, string.Empty);
        _resumeButton = RaceUiTheme.CreatePrimaryButton(window, "Resume", _resume);

        content.TryAddChild(RaceUiTheme.CreateTitle(window, "Paused"));
        content.TryAddChild(_summary);
        content.TryAddChild(RaceUiTheme.CreateBody(window, "Press Escape or GamePad Start to resume instantly, or use the buttons below."));
        content.TryAddChild(_resumeButton);
        content.TryAddChild(RaceUiTheme.CreateSecondaryButton(window, "Back to main menu", _returnToMenu));

        panel.SetContent(content);
        window.SetContent(panel);
        RefreshSummary();
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
            _summary!.Text = "Race session unavailable.";
            return;
        }

        int displayedLap = Math.Min(session.GameMode.CompletedLaps + 1, session.GameMode.TotalLaps);
        _summary!.Text = $"{session.TrackName} | Lap {displayedLap}/{session.GameMode.TotalLaps} | Total {FormatTime(session.GameMode.RaceTimeSeconds)}";
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