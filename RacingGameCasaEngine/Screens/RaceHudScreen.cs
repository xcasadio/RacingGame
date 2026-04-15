using CasaEngine.Framework.GameFramework;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Responsive;
using MGUI.Shared.Rendering;
using MGUI.Shared.Text;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using Color = Microsoft.Xna.Framework.Color;
using HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Thickness = MonoGame.Extended.Thickness;
using VerticalAlignment = MGUI.Core.UI.VerticalAlignment;
using Visibility = MGUI.Core.UI.Visibility;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Screens;

internal sealed class RaceHudScreen : RaceFrontEndScreenBase
{
    private const float HudPanelScale = 0.5f;
    private const float TachometerScale = 0.575f;

    private static readonly Rectangle LapsGfxRect = new(381, 132, 222, 160);
    private static readonly Rectangle TachoGfxRect = new(0, 0, 343, 341);
    private static readonly Rectangle TachoArrowGfxRect = new(347, 0, 28, 186);
    private static readonly Rectangle TachoMphGfxRect = new(184, 256, 148, 72);
    private static readonly Rectangle TachoGearGfxRect = new(286, 149, 52, 72);
    private static readonly Rectangle CurrentAndBestGfxRect = new(381, 2, 342, 128);
    private static readonly Rectangle TrackNameGfxRect = new(726, 2, 282, 62);
    private static readonly Rectangle Best5GfxRect = new(726, 66, 282, 62);
    private static readonly Rectangle[] BigNumberRects =
    {
        new(2, 342, 80, 133),
        new(84, 342, 80, 133),
        new(167, 342, 80, 133),
        new(247, 342, 78, 133),
        new(330, 342, 80, 133),
        new(411, 342, 80, 133),
        new(495, 342, 80, 133),
        new(578, 342, 80, 133),
        new(659, 342, 80, 133),
        new(749, 342, 80, 133),
    };

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndState _state;
    private readonly Action _returnToMenu;
    private MGBorder? _lapsPanel;
    private MGBorder? _timesPanel;
    private MGBorder? _topTimesPanel;
    private MGBorder? _tachometer;
    private MGBorder? _gameOverPanel;
    private MGTextBlock? _gameOverTitle;
    private MGTextBlock? _exitHint;
    private MGTextBlock[]? _gameOverLines;
    private string? _fontFamily;
    private bool _returnRequested;

    public RaceHudScreen(RacingGameCasaEngineGame game, RaceFrontEndState state, Action returnToMenu)
        : base(backgroundTexture: null)
    {
        _game = game;
        _state = state;
        _returnToMenu = returnToMenu;
    }

    public override UILayer Layer => UILayer.HUD;

    protected override void BuildScreen(UIRoot root)
    {
        MGWindow window = CreateForegroundWindow(root);
        window.AllowsClickThrough = true;
        _fontFamily = window.Desktop.DefaultFontFamily;

        var overlay = new MGOverlayPanel(window)
        {
            UseResponsiveLayout = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _lapsPanel = CreateHudPanel(window, LapsGfxRect, DrawLapsPanel, HudPanelScale);
        _lapsPanel.ResponsiveAnchor = ResponsiveAnchor.TopLeft;
        overlay.TryAddChild(_lapsPanel, new Thickness(10, 10, 0, 0), 5);

        _timesPanel = CreateHudPanel(window, CurrentAndBestGfxRect, DrawTimesPanel, HudPanelScale);
        _timesPanel.ResponsiveAnchor = ResponsiveAnchor.BottomLeft;
        overlay.TryAddChild(_timesPanel, new Thickness(10, 0, 0, 10), 5);

        _topTimesPanel = CreateHudPanel(
            window,
            new Rectangle(0, 0, TrackNameGfxRect.Width, TrackNameGfxRect.Height + 4 + (Best5GfxRect.Height * 5) + (4 * 4)),
            DrawTopTimesPanel,
            HudPanelScale);
        _topTimesPanel.ResponsiveAnchor = ResponsiveAnchor.TopRight;
        overlay.TryAddChild(_topTimesPanel, new Thickness(0, 10, 10, 0), 5);

        _tachometer = CreateHudPanel(window, TachoGfxRect, DrawTachometer, TachometerScale);
        _tachometer.ResponsiveAnchor = ResponsiveAnchor.BottomRight;
        overlay.TryAddChild(_tachometer, new Thickness(0, 0, 0, 0), 10);

        _gameOverPanel = RaceUiTheme.CreatePanel(window, 420);
        _gameOverPanel.Padding = new Thickness(24);
        _gameOverPanel.Visibility = Visibility.Collapsed;

        var gameOverStack = new MGStackPanel(window, MGUI.Core.UI.Orientation.Vertical)
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _gameOverTitle = new MGTextBlock(window, string.Empty, RaceUiTheme.AccentColor, 26)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            UseResponsiveTextScale = true,
        };
        gameOverStack.TryAddChild(_gameOverTitle);

        _gameOverLines = new MGTextBlock[4];
        for (int i = 0; i < _gameOverLines.Length; i++)
        {
            _gameOverLines[i] = new MGTextBlock(window, string.Empty, Color.White, 14)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = HorizontalAlignment.Left,
                WrapText = true,
                UseResponsiveTextScale = true,
            };
            gameOverStack.TryAddChild(_gameOverLines[i]);
        }

        _exitHint = new MGTextBlock(window, string.Empty, RaceUiTheme.SecondaryTextColor, 14)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = HorizontalAlignment.Center,
            WrapText = true,
            UseResponsiveTextScale = true,
        };
        gameOverStack.TryAddChild(_exitHint);

        _gameOverPanel.SetContent(gameOverStack);
        _gameOverPanel.ResponsiveAnchor = ResponsiveAnchor.Center;
        overlay.TryAddChild(_gameOverPanel, new Thickness(0), 20);

        window.SetContent(overlay);
        Refresh();
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
        if (_gameOverPanel == null || _gameOverTitle == null || _gameOverLines == null || _exitHint == null)
        {
            return;
        }

        _gameOverPanel.Visibility = IsGameOver ? Visibility.Visible : Visibility.Collapsed;
        _gameOverTitle.Text = IsGameOver ? "Victory! You won." : string.Empty;

        IReadOnlyList<string> lines = GetGameOverLines();
        for (int i = 0; i < _gameOverLines.Length; i++)
        {
            _gameOverLines[i].Text = i < lines.Count ? lines[i] : string.Empty;
        }

        _exitHint.Text = IsGameOver
            ? "Press Space, Enter, A, B, X, click, Start, or Back to return to menu."
            : string.Empty;
    }

    private MGBorder CreateHudPanel(MGWindow window, Rectangle designBounds, EventHandler<MGElement.MGElementDrawEventArgs> drawHandler, float scale)
    {
        var panel = new MGBorder(window)
        {
            BackgroundBrush = new VisualStateFillBrush(Color.Transparent.AsFillBrush()),
            BorderBrush = null,
            BorderThickness = new Thickness(0),
            PreferredWidth = Scale(designBounds.Width, scale),
            PreferredHeight = Scale(designBounds.Height, scale),
        };
        panel.OnEndingDraw += drawHandler;
        return panel;
    }

    private void DrawLapsPanel(object? sender, MGElement.MGElementDrawEventArgs e)
    {
        if (_lapsPanel == null || !TryGetHudTexture(out Texture2D hudTexture))
        {
            return;
        }

        Rectangle bounds = TranslateBounds(_lapsPanel.LayoutBounds, e.DA.Offset);
        dynamic drawTransaction = e.DA.DT;
        drawTransaction.DrawTextureTo(UiImageResources.AsImage(hudTexture), LapsGfxRect, bounds, Color.White * e.DA.Opacity);

        float scaleX = bounds.Width / (float)LapsGfxRect.Width;
        float scaleY = bounds.Height / (float)LapsGfxRect.Height;

        Rectangle numberBounds = new(
            bounds.X + Scale(15, scaleX),
            bounds.Y + Scale(12, scaleY),
            Scale(80, scaleX),
            Scale(133, scaleY));
        DrawBigNumber(drawTransaction, hudTexture, numberBounds, GetCurrentLapDisplay(), e.DA.Opacity, 0f);
    }

    private void DrawTimesPanel(object? sender, MGElement.MGElementDrawEventArgs e)
    {
        if (_timesPanel == null || !TryGetHudTexture(out Texture2D hudTexture))
        {
            return;
        }

        Rectangle bounds = TranslateBounds(_timesPanel.LayoutBounds, e.DA.Offset);
        dynamic drawTransaction = e.DA.DT;
        drawTransaction.DrawTextureTo(UiImageResources.AsImage(hudTexture), CurrentAndBestGfxRect, bounds, Color.White * e.DA.Opacity);

        float scaleX = bounds.Width / (float)CurrentAndBestGfxRect.Width;
        float scaleY = bounds.Height / (float)CurrentAndBestGfxRect.Height;

        DrawShadowedText(drawTransaction, FormatMilliseconds(GetCurrentLapTimeMilliseconds()), bounds.X + Scale(154, scaleX), bounds.Y + Scale(14, scaleY), new Color(255, 185, 80), 38, CustomFontStyles.Bold, e.DA.Opacity, scaleY);
        DrawShadowedText(drawTransaction, FormatMilliseconds(GetBestLapTimeMilliseconds()), bounds.X + Scale(154, scaleX), bounds.Y + Scale(78, scaleY), Color.White, 38, CustomFontStyles.Bold, e.DA.Opacity, scaleY);
    }

    private void DrawTopTimesPanel(object? sender, MGElement.MGElementDrawEventArgs e)
    {
        if (_topTimesPanel == null || !TryGetHudTexture(out Texture2D hudTexture) || string.IsNullOrEmpty(_fontFamily))
        {
            return;
        }

        Rectangle bounds = TranslateBounds(_topTimesPanel.LayoutBounds, e.DA.Offset);
        dynamic drawTransaction = e.DA.DT;

        float scaleX = bounds.Width / (float)TrackNameGfxRect.Width;
        float scaleY = scaleX;

        Rectangle trackBounds = new(bounds.X, bounds.Y, bounds.Width, Scale(TrackNameGfxRect.Height, scaleY));
        drawTransaction.DrawTextureTo(UiImageResources.AsImage(hudTexture), TrackNameGfxRect, trackBounds, Color.White * e.DA.Opacity);

        string trackName = GetTrackName();
        Vector2 trackSize = drawTransaction.MeasureText(_fontFamily, CustomFontStyles.Bold, trackName, Scale(26, scaleY));
        DrawShadowedText(
            drawTransaction,
            trackName,
            trackBounds.X + (int)Math.Round((trackBounds.Width - trackSize.X) / 2f),
            trackBounds.Y + Scale(10, scaleY),
            Color.White,
            26,
            CustomFontStyles.Bold,
            e.DA.Opacity,
            scaleY);

        int rowHeight = Scale(Best5GfxRect.Height, scaleY);
        int gap = Scale(4, scaleY);
        IReadOnlyList<int> topTimes = GetTopLapTimesMilliseconds();

        for (int i = 0; i < 5; i++)
        {
            Rectangle rowBounds = new(bounds.X, trackBounds.Bottom + gap + (i * (rowHeight + gap)), bounds.Width, rowHeight);
            drawTransaction.DrawTextureTo(UiImageResources.AsImage(hudTexture), Best5GfxRect, rowBounds, Color.White * e.DA.Opacity);
            DrawShadowedText(drawTransaction, $"{i + 1}.", rowBounds.X + Scale(20, scaleX), rowBounds.Y + Scale(11, scaleY), Color.White, 30, CustomFontStyles.Bold, e.DA.Opacity, scaleY);

            string timeText = i < topTimes.Count && topTimes[i] > 0
                ? FormatMilliseconds(topTimes[i])
                : "--:--.--";
            DrawShadowedText(drawTransaction, timeText, rowBounds.X + Scale(82, scaleX), rowBounds.Y + Scale(11, scaleY), Color.White, 30, CustomFontStyles.Bold, e.DA.Opacity, scaleY);
        }
    }

    private void DrawTachometer(object? sender, MGElement.MGElementDrawEventArgs e)
    {
        if (_tachometer == null || !TryGetHudTexture(out Texture2D hudTexture))
        {
            return;
        }

        Rectangle bounds = TranslateBounds(_tachometer.LayoutBounds, e.DA.Offset);
        dynamic drawTransaction = e.DA.DT;
        drawTransaction.DrawTextureTo(UiImageResources.AsImage(hudTexture), TachoGfxRect, bounds, Color.White * e.DA.Opacity);

        float scaleX = bounds.Width / (float)TachoGfxRect.Width;
        float scaleY = bounds.Height / (float)TachoGfxRect.Height;

        float acceleration = Math.Clamp(GetTachometerNeedleValue(), 0f, 1f);
        float rotation = -2.33f + acceleration * 2.5f;

        Rectangle arrowBounds = new(
            bounds.X + Scale(194, scaleX),
            bounds.Y + Scale(194, scaleY),
            Scale(TachoArrowGfxRect.Width, scaleX),
            Scale(TachoArrowGfxRect.Height, scaleY));

        Vector2 rotationOrigin = new(TachoArrowGfxRect.Width / 2f, TachoArrowGfxRect.Height - 13f);
        drawTransaction.DrawTextureTo(
            UiImageResources.AsImage(hudTexture),
            TachoArrowGfxRect,
            arrowBounds,
            Color.White * e.DA.Opacity,
            rotationOrigin,
            rotation,
            0,
            SpriteEffects.None);

        Rectangle mphBounds = new(
            bounds.X + Scale(TachoMphGfxRect.X, scaleX),
            bounds.Y + Scale(TachoMphGfxRect.Y, scaleY),
            Scale(TachoMphGfxRect.Width, scaleX),
            Scale(TachoMphGfxRect.Height, scaleY));
        DrawBigNumber(drawTransaction, hudTexture, mphBounds, GetHudSpeedDisplay(), e.DA.Opacity);

        Rectangle gearBounds = new(
            bounds.X + Scale(TachoGearGfxRect.X, scaleX),
            bounds.Y + Scale(TachoGearGfxRect.Y, scaleY),
            Scale(TachoGearGfxRect.Width, scaleX),
            Scale(TachoGearGfxRect.Height, scaleY));
        DrawBigNumber(drawTransaction, hudTexture, gearBounds, GetHudGearDisplay(), e.DA.Opacity);
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

    private bool TryGetHudTexture(out Texture2D hudTexture)
    {
        hudTexture = _game.RaceHudTexture!;
        return hudTexture != null;
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

    private void DrawShadowedText(object drawTransaction, string text, int x, int y, Color color, int fontSizeAtDesign, CustomFontStyles style, float opacity, float scale)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(_fontFamily))
        {
            return;
        }

        int fontSize = Math.Max(10, (int)Math.Round(fontSizeAtDesign * scale));
        dynamic transaction = drawTransaction;
        transaction.DrawShadowedText(_fontFamily, style, text, new Vector2(x, y), color * opacity, Color.Black * 0.75f * opacity, fontSize);
    }

    private static void DrawBigNumber(object drawTransaction, Texture2D hudTexture, Rectangle targetBounds, int number, float opacity, float horizontalAlignment = 0.5f)
    {
        string text = Math.Max(0, number).ToString();
        float scale = targetBounds.Height / (float)BigNumberRects[0].Height;
        int totalWidth = text.Sum(character => (int)Math.Round(BigNumberRects[character - '0'].Width * scale));

        if (totalWidth > targetBounds.Width && totalWidth > 0)
        {
            scale *= targetBounds.Width / (float)totalWidth;
            totalWidth = text.Sum(character => (int)Math.Round(BigNumberRects[character - '0'].Width * scale));
        }

        int x = targetBounds.X + Math.Max(0, (int)Math.Round((targetBounds.Width - totalWidth) * horizontalAlignment));
        dynamic transaction = drawTransaction;
        foreach (char character in text)
        {
            Rectangle source = BigNumberRects[character - '0'];
            int width = (int)Math.Round(source.Width * scale);
            int height = (int)Math.Round(source.Height * scale);
            Rectangle destination = new(x, targetBounds.Y + Math.Max(0, (targetBounds.Height - height) / 2), width, height);
            transaction.DrawTextureTo(UiImageResources.AsImage(hudTexture), source, destination, Color.White * opacity);
            x += width;
        }
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

    private static Rectangle TranslateBounds(Rectangle bounds, Point offset)
    {
        return new Rectangle(bounds.X + offset.X, bounds.Y + offset.Y, bounds.Width, bounds.Height);
    }

    private static int Scale(int value, float scale)
    {
        return (int)Math.Round(value * scale);
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