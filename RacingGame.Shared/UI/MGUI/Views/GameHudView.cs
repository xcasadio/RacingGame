using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using MGUI.Core.UI.Responsive;
using MGUI.Shared.Rendering;
using MGUI.Shared.Text;
using MonoGame.Extended;
using RacingGame.GameScreens;
using RacingGame.Graphics;

namespace RacingGame.UI.MGUI.Views;

internal sealed class GameHudView : IMguiScreenView
{
    private const float HudPanelScale = 0.5f;
    private const float TachometerScale = 0.575f;

    private readonly GameScreen _screen;
    private readonly MGBorder _lapsPanel;
    private readonly MGBorder _timesPanel;
    private readonly MGBorder _topTimesPanel;
    private readonly MGBorder _tachometer;
    private readonly MGBorder _gameOverPanel;
    private readonly MGTextBlock _gameOverTitle;
    private readonly MGTextBlock[] _gameOverLines;
    private readonly MGTextBlock _exitHint;
    private readonly string _fontFamily;

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

    public GameHudView(GameScreen screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host, true);
        _fontFamily = Window.Desktop.DefaultFontFamily;

        var root = new MGOverlayPanel(Window)
        {
            UseResponsiveLayout = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _lapsPanel = CreateHudPanel(UIRenderer.LapsGfxRect, DrawLapsPanel, HudPanelScale);
        _lapsPanel.ResponsiveAnchor = ResponsiveAnchor.TopLeft;
        root.TryAddChild(_lapsPanel, new Thickness(10, 10, 0, 0), 5);

        _timesPanel = CreateHudPanel(UIRenderer.CurrentAndBestGfxRect, DrawTimesPanel, HudPanelScale);
        _timesPanel.ResponsiveAnchor = ResponsiveAnchor.BottomLeft;
        root.TryAddChild(_timesPanel, new Thickness(10, 0, 0, 10), 5);

        _topTimesPanel = CreateHudPanel(
            new Rectangle(0, 0, UIRenderer.TrackNameGfxRect.Width, UIRenderer.TrackNameGfxRect.Height + 4 + (UIRenderer.Best5GfxRect.Height * 5) + (4 * 4)),
            DrawTopTimesPanel,
            HudPanelScale);
        _topTimesPanel.ResponsiveAnchor = ResponsiveAnchor.TopRight;
        root.TryAddChild(_topTimesPanel, new Thickness(0, 10, 10, 0), 5);

        _tachometer = new MGBorder(Window)
        {
            BackgroundBrush = MguiUiTheme.TransparentBackground,
            BorderBrush = null,
            BorderThickness = new(0),
            ResponsiveAnchor = ResponsiveAnchor.BottomRight,
            PreferredWidth = Scale(UIRenderer.TachoGfxRect.Width, TachometerScale),
            PreferredHeight = Scale(UIRenderer.TachoGfxRect.Height, TachometerScale),
        };
        _tachometer.OnEndingDraw += DrawTachometer;
        root.TryAddChild(_tachometer, new Thickness(0, 0, 0, 0), 10);

        _gameOverPanel = MguiUiTheme.CreatePanel(Window, 24);
        _gameOverPanel.HorizontalAlignment = HorizontalAlignment.Center;
        _gameOverPanel.VerticalAlignment = VerticalAlignment.Center;
        _gameOverPanel.PreferredWidth = 420;

        var gameOverStack = MguiUiTheme.CreateVerticalStack(Window, 8, 0);
        _gameOverTitle = MguiUiTheme.CreateHeading(Window, string.Empty);
        gameOverStack.TryAddChild(_gameOverTitle);
        _gameOverLines = new MGTextBlock[4];
        for (int i = 0; i < _gameOverLines.Length; i++)
        {
            _gameOverLines[i] = MguiUiTheme.CreateBodyText(Window, string.Empty);
            gameOverStack.TryAddChild(_gameOverLines[i]);
        }
        _exitHint = MguiUiTheme.CreateSubheading(Window, string.Empty);
        gameOverStack.TryAddChild(_exitHint);
        _gameOverPanel.SetContent(gameOverStack);
        _gameOverPanel.ResponsiveAnchor = ResponsiveAnchor.Center;

        root.TryAddChild(_gameOverPanel, new Thickness(0), 20);
        Window.SetContent(root);
    }

    public MGWindow Window { get; }
    public MGElement InitialFocusElement => null;
    public bool BlocksGameplayInput => false;

    public void Activate()
    {
        Refresh();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        Refresh();
    }

    private void Refresh()
    {
        _gameOverPanel.Visibility = _screen.IsGameOver ? Visibility.Visible : Visibility.Collapsed;
        _gameOverTitle.Text = _screen.GameOverTitle;
        var lines = _screen.GetGameOverLines();
        for (int i = 0; i < _gameOverLines.Length; i++)
            _gameOverLines[i].Text = i < lines.Count ? lines[i] : string.Empty;
        _exitHint.Text = _screen.ExitHint;
    }

    private MGBorder CreateHudPanel(Rectangle designBounds, EventHandler<MGElement.MGElementDrawEventArgs> drawHandler, float scale)
    {
        var panel = new MGBorder(Window)
        {
            BackgroundBrush = MguiUiTheme.TransparentBackground,
            BorderBrush = null,
            BorderThickness = new(0),
            PreferredWidth = Scale(designBounds.Width, scale),
            PreferredHeight = Scale(designBounds.Height, scale),
        };
        panel.OnEndingDraw += drawHandler;
        return panel;
    }

    private void DrawLapsPanel(object sender, MGElement.MGElementDrawEventArgs e)
    {
        Rectangle bounds = TranslateBounds(_lapsPanel.LayoutBounds, e.DA.Offset);
        dynamic dt = e.DA.DT;
        dt.DrawTextureTo(UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture), UIRenderer.LapsGfxRect, bounds, Color.White * e.DA.Opacity);

        float scaleX = bounds.Width / (float)UIRenderer.LapsGfxRect.Width;
        float scaleY = bounds.Height / (float)UIRenderer.LapsGfxRect.Height;

        Rectangle numberBounds = new(
            bounds.X + Scale(15, scaleX),
            bounds.Y + Scale(12, scaleY),
            Scale(80, scaleX),
            Scale(133, scaleY));
        DrawBigNumber(dt, numberBounds, _screen.CurrentLapDisplay, e.DA.Opacity, horizontalAlignment: 0f);
    }

    private void DrawTimesPanel(object sender, MGElement.MGElementDrawEventArgs e)
    {
        Rectangle bounds = TranslateBounds(_timesPanel.LayoutBounds, e.DA.Offset);
        dynamic dt = e.DA.DT;
        dt.DrawTextureTo(UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture), UIRenderer.CurrentAndBestGfxRect, bounds, Color.White * e.DA.Opacity);

        float scaleX = bounds.Width / (float)UIRenderer.CurrentAndBestGfxRect.Width;
        float scaleY = bounds.Height / (float)UIRenderer.CurrentAndBestGfxRect.Height;

        DrawShadowedText(dt, FormatMilliseconds(_screen.CurrentGameTime), bounds.X + Scale(154, scaleX), bounds.Y + Scale(14, scaleY), new Color(255, 185, 80), 38, CustomFontStyles.Bold, e.DA.Opacity, scaleY);
        DrawShadowedText(dt, FormatMilliseconds(_screen.BestLapTime), bounds.X + Scale(154, scaleX), bounds.Y + Scale(78, scaleY), Color.White, 38, CustomFontStyles.Bold, e.DA.Opacity, scaleY);
    }

    private void DrawTopTimesPanel(object sender, MGElement.MGElementDrawEventArgs e)
    {
        Rectangle bounds = TranslateBounds(_topTimesPanel.LayoutBounds, e.DA.Offset);
        dynamic dt = e.DA.DT;

        float scaleX = bounds.Width / (float)UIRenderer.TrackNameGfxRect.Width;
        float scaleY = scaleX;

        Rectangle trackBounds = new(bounds.X, bounds.Y, bounds.Width, Scale(UIRenderer.TrackNameGfxRect.Height, scaleY));
        dt.DrawTextureTo(UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture), UIRenderer.TrackNameGfxRect, trackBounds, Color.White * e.DA.Opacity);

        Vector2 trackSize = dt.MeasureText(_fontFamily, CustomFontStyles.Bold, _screen.TrackName, Scale(26, scaleY));
        DrawShadowedText(dt, _screen.TrackName,
            trackBounds.X + (int)Math.Round((trackBounds.Width - trackSize.X) / 2f),
            trackBounds.Y + Scale(10, scaleY),
            Color.White,
            26,
            CustomFontStyles.Bold,
            e.DA.Opacity,
            scaleY);

        int rowHeight = Scale(UIRenderer.Best5GfxRect.Height, scaleY);
        int gap = Scale(4, scaleY);
        var topTimes = _screen.TopLapTimes;
        for (int i = 0; i < 5; i++)
        {
            Rectangle rowBounds = new(bounds.X, trackBounds.Bottom + gap + i * (rowHeight + gap), bounds.Width, rowHeight);
            dt.DrawTextureTo(UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture), UIRenderer.Best5GfxRect, rowBounds, Color.White * e.DA.Opacity);

            DrawShadowedText(dt, $"{i + 1}.", rowBounds.X + Scale(20, scaleX), rowBounds.Y + Scale(11, scaleY), Color.White, 30, CustomFontStyles.Bold, e.DA.Opacity, scaleY);

            string timeText = i < topTimes.Count ? FormatMilliseconds(topTimes[i]) : "--:--.--";
            DrawShadowedText(dt, timeText, rowBounds.X + Scale(82, scaleX), rowBounds.Y + Scale(11, scaleY), Color.White, 30, CustomFontStyles.Bold, e.DA.Opacity, scaleY);
        }
    }

    private void DrawTachometer(object sender, MGElement.MGElementDrawEventArgs e)
    {
        if (!BaseGame.UI.Ingame.Valid)
        {
            return;
        }

        Rectangle bounds = new(
            _tachometer.LayoutBounds.X + (int)e.DA.Offset.X,
            _tachometer.LayoutBounds.Y + (int)e.DA.Offset.Y,
            _tachometer.LayoutBounds.Width,
            _tachometer.LayoutBounds.Height);
        dynamic drawTransaction = e.DA.DT;
        drawTransaction.DrawTextureTo(UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture), UIRenderer.TachoGfxRect, bounds, Color.White * e.DA.Opacity);

        float scaleX = bounds.Width / (float)UIRenderer.TachoGfxRect.Width;
        float scaleY = bounds.Height / (float)UIRenderer.TachoGfxRect.Height;

        float acceleration = Math.Clamp(_screen.TachometerNeedleValue, 0f, 1f);
        float rotation = -2.33f + acceleration * 2.5f;

        Rectangle arrowBounds = new(
            bounds.X + Scale(194, scaleX),
            bounds.Y + Scale(194, scaleY),
            Scale(UIRenderer.TachoArrowGfxRect.Width, scaleX),
            Scale(UIRenderer.TachoArrowGfxRect.Height, scaleY));

        Vector2 rotationOrigin = new(
            UIRenderer.TachoArrowGfxRect.Width / 2f,
            UIRenderer.TachoArrowGfxRect.Height - 13f);
        drawTransaction.DrawTextureTo(
            UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture),
            UIRenderer.TachoArrowGfxRect,
            arrowBounds,
            Color.White * e.DA.Opacity,
            rotationOrigin,
            rotation,
            0,
            UIDrawFlip.None);

        Rectangle mphBounds = new(
            bounds.X + Scale(UIRenderer.TachoMphGfxRect.X, scaleX),
            bounds.Y + Scale(UIRenderer.TachoMphGfxRect.Y, scaleY),
            Scale(UIRenderer.TachoMphGfxRect.Width, scaleX),
            Scale(UIRenderer.TachoMphGfxRect.Height, scaleY));
        DrawBigNumber(drawTransaction, mphBounds, _screen.HudSpeedDisplay, e.DA.Opacity);

        Rectangle gearBounds = new(
            bounds.X + Scale(UIRenderer.TachoGearGfxRect.X, scaleX),
            bounds.Y + Scale(UIRenderer.TachoGearGfxRect.Y, scaleY),
            Scale(UIRenderer.TachoGearGfxRect.Width, scaleX),
            Scale(UIRenderer.TachoGearGfxRect.Height, scaleY));
        DrawBigNumber(drawTransaction, gearBounds, _screen.HudGearDisplay, e.DA.Opacity);
    }

    private void DrawShadowedText(object drawTransaction, string text, int x, int y, Color color, int fontSizeAtDesign, CustomFontStyles style, float opacity, float scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int fontSize = (int)Math.Round(fontSizeAtDesign * scale);
        fontSize = Math.Max(10, fontSize);
        dynamic transaction = drawTransaction;
        transaction.DrawShadowedText(_fontFamily, style, text, new Vector2(x, y), color * opacity, Color.Black * 0.75f * opacity, fontSize);
    }

    private static void DrawBigNumber(object drawTransaction, Rectangle targetBounds, int number, float opacity, float horizontalAlignment = 0.5f)
    {
        string text = Math.Max(0, number).ToString();
        float scale = targetBounds.Height / (float)BigNumberRects[0].Height;
        int totalWidth = text.Sum(c => (int)Math.Round(BigNumberRects[c - '0'].Width * scale));

        if (totalWidth > targetBounds.Width && totalWidth > 0)
        {
            scale *= targetBounds.Width / (float)totalWidth;
            totalWidth = text.Sum(c => (int)Math.Round(BigNumberRects[c - '0'].Width * scale));
        }

        int x = targetBounds.X + Math.Max(0, (int)Math.Round((targetBounds.Width - totalWidth) * horizontalAlignment));
        dynamic transaction = drawTransaction;
        foreach (char c in text)
        {
            Rectangle source = BigNumberRects[c - '0'];
            int width = (int)Math.Round(source.Width * scale);
            int height = (int)Math.Round(source.Height * scale);
            Rectangle destination = new(x, targetBounds.Y + Math.Max(0, (targetBounds.Height - height) / 2), width, height);
            transaction.DrawTextureTo(UiImageResources.AsImage(BaseGame.UI.Ingame.XnaTexture), source, destination, Color.White * opacity);
            x += width;
        }
    }

    private static Rectangle TranslateBounds(Rectangle bounds, Point offset)
        => new(bounds.X + (int)offset.X, bounds.Y + (int)offset.Y, bounds.Width, bounds.Height);

    private static int Scale(int value, float scale) => (int)Math.Round(value * scale);

    private static string FormatMilliseconds(int timeMilliseconds)
    {
        return
            (timeMilliseconds < 0 ? "-" : "") +
            ((Math.Abs(timeMilliseconds) / 1000) / 60) + ":" +
            ((Math.Abs(timeMilliseconds) / 1000) % 60).ToString("00") + "." +
            ((Math.Abs(timeMilliseconds) / 10) % 100).ToString("00");
    }
}