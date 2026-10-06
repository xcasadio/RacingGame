using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>One line of the highscore board: rank, player name and time, and where RacingGame wrote each of them.</summary>
public sealed class RaceHighscoreRowViewModel : RaceViewModelBase
{
    private string _rank = string.Empty;
    private string _name = string.Empty;
    private string _time = string.Empty;

    public string Rank
    {
        get => _rank;
        set => SetProperty(ref _rank, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    public RaceScreenRectViewModel RankText { get; } = new();

    public RaceScreenRectViewModel NameText { get; } = new();

    public RaceScreenRectViewModel TimeText { get; } = new();
}

/// <summary>
/// Data context of <c>Screen.Highscores</c>, RacingGame's highscores screen (ADR-0013): the menu decoration, the black
/// band, the "HIGHSCORES" header, the three level tabs, the separator line, the ten board lines and the B BACK button,
/// laid out every frame in screen pixels with RacingGame's formulas (<c>git show
/// 4f840a3^:RacingGame.Shared/GameScreens/Highscores.cs</c>, Update and Render; <c>Graphics/UIRenderer.cs</c>,
/// RenderBlackBar and RenderBottomButtons). The texts are written in GameFont as TextureFont did: at their position minus
/// YToRes1050(5), and scaled by the screen with <see cref="TextScaleX"/> and <see cref="TextScaleY"/>.
/// </summary>
public sealed class RaceHighscoresViewModel : RaceViewModelBase
{
    public const int LevelCount = 3;

    /// <summary>RacingGame's Highscores.NumOfHighscores: the board's lines, and where its clickable area ends.</summary>
    public const int RowCount = 10;

    /// <summary>Colour of the selected tab (Color.Yellow) and of every time.</summary>
    public static readonly Color SelectedColor = new(255, 255, 0);

    /// <summary>Colour of a tab or a line under the mouse (Color.White).</summary>
    public static readonly Color HoveredColor = new(255, 255, 255);

    /// <summary>Colour of the other tabs (Color.LightGray).</summary>
    public static readonly Color TabColor = new(211, 211, 211);

    /// <summary>Colour of the other lines' rank and name.</summary>
    public static readonly Color RowColor = new(200, 200, 200);

    // TextureFont drew its glyphs YToRes1050(5) above the given position (SubRenderHeight).
    private const int FontRaise = 5;

    private readonly Rectangle[] _tabRects = new Rectangle[LevelCount];
    private readonly Rectangle[] _rowRects = new Rectangle[RowCount];
    private int _bandTopPixels;
    private int _bandHeightPixels;
    private int _selectedLevel = 1;
    private int _rowsBottom;
    private Rectangle _backButtonRect;

    public RaceHighscoresViewModel()
    {
        Tabs = [Tab0, Tab1, Tab2];
        Rows = [Row0, Row1, Row2, Row3, Row4, Row5, Row6, Row7, Row8, Row9];
    }

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public int BandTopPixels
    {
        get => _bandTopPixels;
        set
        {
            if (SetProperty(ref _bandTopPixels, value))
            {
                NotifyPropertyChanged(nameof(BandMargin));
            }
        }
    }

    public int BandHeightPixels
    {
        get => _bandHeightPixels;
        set => SetProperty(ref _bandHeightPixels, value);
    }

    public MonoGame.Extended.Thickness BandMargin => new(0, _bandTopPixels, 0, 0);

    /// <summary>The "HIGHSCORES" header (headers.png HeaderHighscoresGfxRect), drawn with RenderOnScreenRelative1600(10, 18).</summary>
    public RaceScreenRectViewModel Header { get; } = new();

    /// <summary>Where the "Beginner" tab text is written.</summary>
    public RaceScreenRectViewModel Tab0 { get; } = new();

    public RaceScreenRectViewModel Tab1 { get; } = new();

    public RaceScreenRectViewModel Tab2 { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> Tabs { get; }

    /// <summary>The two one-pixel lines under the tabs, as one rectangle two pixels high.</summary>
    public RaceScreenRectViewModel Separator { get; } = new();

    public RaceHighscoreRowViewModel Row0 { get; } = new();

    public RaceHighscoreRowViewModel Row1 { get; } = new();

    public RaceHighscoreRowViewModel Row2 { get; } = new();

    public RaceHighscoreRowViewModel Row3 { get; } = new();

    public RaceHighscoreRowViewModel Row4 { get; } = new();

    public RaceHighscoreRowViewModel Row5 { get; } = new();

    public RaceHighscoreRowViewModel Row6 { get; } = new();

    public RaceHighscoreRowViewModel Row7 { get; } = new();

    public RaceHighscoreRowViewModel Row8 { get; } = new();

    public RaceHighscoreRowViewModel Row9 { get; } = new();

    public IReadOnlyList<RaceHighscoreRowViewModel> Rows { get; }

    /// <summary>The B BACK button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel BackButton { get; } = new();

    /// <summary>Horizontal draw scale of the GameFont texts (TextureFont's XToRes1400).</summary>
    public float TextScaleX { get; private set; } = 1f;

    /// <summary>Vertical draw scale of the GameFont texts (TextureFont's YToRes1050).</summary>
    public float TextScaleY { get; private set; } = 1f;

    /// <summary>Index of the selected level tab: 0 Beginner, 1 Advanced (RacingGame's opening tab), 2 Expert.</summary>
    public int SelectedLevel
    {
        get => _selectedLevel;
        set => SetProperty(ref _selectedLevel, Math.Clamp(value, 0, LevelCount - 1));
    }

    /// <summary>The colour of each tab text after the last <see cref="Update"/>.</summary>
    public Color[] TabColors { get; } = new Color[LevelCount];

    /// <summary>The colour of each line's rank and name after the last <see cref="Update"/>.</summary>
    public Color[] RowColors { get; } = new Color[RowCount];

    /// <summary>Lays everything out for the viewport and colours the tabs and lines under the mouse.</summary>
    /// <param name="separatorTextWidth">Width of "5:67:89" in GameFont at its native size, which ends the separator line.</param>
    public void Update(int viewportWidth, int viewportHeight, int mouseX, int mouseY, float separatorTextWidth)
    {
        var layout = new LegacyScreenLayout(viewportWidth, viewportHeight);

        // RenderBlackBar(160, 498 - 160): CalcRectangle(0, 160, 1024, 338).
        Rectangle band = layout.CalcRectangle(0, 160, 1024, 498 - 160);
        BandTopPixels = band.Y;
        BandHeightPixels = band.Height;
        Set(Header, layout.CalcRectangle1600(10, 18, 512, 100));

        TextScaleX = layout.Width / 1400f;
        TextScaleY = layout.Height / 1050f;
        int raise = layout.YToRes1050(FontRaise);

        int yPos = layout.YToRes(182);
        int lineHeight = layout.YToRes(27);
        int xPos = layout.XToRes(512 - 160 * 3 / 2 + 25);
        for (int i = 0; i < LevelCount; i++)
        {
            _tabRects[i] = new Rectangle(xPos, yPos, layout.XToRes(125), lineHeight);
            Tabs[i].Set(xPos, yPos - raise, 0, 0);
            TabColors[i] = i == _selectedLevel ? SelectedColor : _tabRects[i].Contains(mouseX, mouseY) ? HoveredColor : TabColor;
            xPos += i == 0 ? layout.XToRes(160 + 8) : layout.XToRes(160 + 30 - 8);
        }

        int rankLeft = layout.XToRes(300);
        int nameLeft = layout.XToRes(350);
        int timeLeft = layout.XToRes(640);
        int lineEnd = timeLeft + (int)Math.Round(separatorTextWidth * TextScaleX);
        Separator.Set(rankLeft, layout.YToRes(208), lineEnd - rankLeft, 2);

        yPos = layout.YToRes(220);
        for (int i = 0; i < RowCount; i++)
        {
            _rowRects[i] = new Rectangle(0, yPos, layout.Width, lineHeight);
            RaceHighscoreRowViewModel row = Rows[i];
            row.RankText.Set(rankLeft, yPos - raise, 0, 0);
            row.NameText.Set(nameLeft, yPos - raise, 0, 0);
            row.TimeText.Set(timeLeft, yPos - raise, 0, 0);
            RowColors[i] = _rowRects[i].Contains(mouseX, mouseY) ? HoveredColor : RowColor;
            yPos += lineHeight;
        }

        _rowsBottom = yPos;

        _backButtonRect = layout.BackButton();
        bool hovered = _backButtonRect.Contains(mouseX, mouseY);
        Set(BackButton, hovered ? layout.GrowBottomButton(_backButtonRect) : _backButtonRect);
        BackButton.IsHighlighted = hovered;
    }

    /// <summary>The tab under the point, or -1.</summary>
    public int GetTabAt(int x, int y) => Array.FindIndex(_tabRects, rect => rect.Contains(x, y));

    /// <summary>The board line under the point (the full screen width, as RacingGame tested it), or -1.</summary>
    public int GetRowAt(int x, int y) => Array.FindIndex(_rowRects, rect => rect.Contains(x, y));

    /// <summary>Whether the point is below the board's last line, where RacingGame's click left the screen.</summary>
    public bool IsBelowRows(int y) => y > _rowsBottom;

    /// <summary>Whether the point is on the B button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverBackButton(int x, int y) => _backButtonRect.Contains(x, y);

    private static void Set(RaceScreenRectViewModel target, Rectangle rect) => target.Set(rect.X, rect.Y, rect.Width, rect.Height);
}
