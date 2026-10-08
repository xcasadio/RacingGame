using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>One help section: a title and its two lines, and where each is written.</summary>
public sealed class RaceHelpSectionViewModel : RaceViewModelBase
{
    private string _title = string.Empty;
    private string _line0 = string.Empty;
    private string _line1 = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Line0
    {
        get => _line0;
        set => SetProperty(ref _line0, value);
    }

    public string Line1
    {
        get => _line1;
        set => SetProperty(ref _line1, value);
    }

    public RaceScreenRectViewModel TitleText { get; } = new();

    public RaceScreenRectViewModel Line0Text { get; } = new();

    public RaceScreenRectViewModel Line1Text { get; } = new();
}

/// <summary>
/// Data context of <c>Screen.Help</c> (ADR-0014): the previous arrangement of the help sections in the menus' style. It
/// holds the menu decoration, the black band, the "HELP" header, four sections and the B BACK button, laid out every
/// frame in screen pixels with RacingGame's formulas and 1024x640 units, as the other menus.
/// <para/>
/// Each section has a title, written in GameFont at full size, then two lines at <see cref="LineScale"/>. Every text is
/// written as TextureFont did: at its position minus YToRes1050(5), and scaled by the screen with
/// <see cref="TextScaleX"/> and <see cref="TextScaleY"/>.
/// </summary>
public sealed class RaceHelpViewModel : RaceViewModelBase
{
    public const int SectionCount = 4;

    /// <summary>Draw scale of the section lines relative to the titles, so the longest line fits the band.</summary>
    public const float LineScale = 0.75f;

    private const int BandTop = 150;
    private const int BandHeight = 340;
    private const int TitleLeft = 120;
    private const int LineLeft = 140;
    private const int FirstTitleTop = 170;
    private const int TitlePitch = 27;
    private const int LinePitch = 20;
    private const int SectionGap = 8;
    // TextureFont drew its glyphs YToRes1050(5) above the given position (SubRenderHeight).
    private const int FontRaise = 5;

    private int _bandTopPixels;
    private int _bandHeightPixels;
    private Rectangle _backButtonRect;

    public RaceHelpViewModel()
    {
        Sections = [Section0, Section1, Section2, Section3];
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

    /// <summary>The "HELP" header (headers.png HeaderHelpGfxRect), drawn with RenderOnScreenRelative1600(10, 18).</summary>
    public RaceScreenRectViewModel Header { get; } = new();

    public RaceHelpSectionViewModel Section0 { get; } = new();

    public RaceHelpSectionViewModel Section1 { get; } = new();

    public RaceHelpSectionViewModel Section2 { get; } = new();

    public RaceHelpSectionViewModel Section3 { get; } = new();

    public IReadOnlyList<RaceHelpSectionViewModel> Sections { get; }

    /// <summary>The B BACK button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel BackButton { get; } = new();

    /// <summary>Horizontal draw scale of the GameFont titles (TextureFont's XToRes1400); the lines use <see cref="LineScale"/> of it.</summary>
    public float TextScaleX { get; private set; } = 1f;

    /// <summary>Vertical draw scale of the GameFont titles (TextureFont's YToRes1050); the lines use <see cref="LineScale"/> of it.</summary>
    public float TextScaleY { get; private set; } = 1f;

    /// <summary>Lays everything out for the viewport, growing the B BACK button under the mouse.</summary>
    public void Update(int viewportWidth, int viewportHeight, int mouseX, int mouseY)
    {
        var layout = new LegacyScreenLayout(viewportWidth, viewportHeight);
        Rectangle band = layout.CalcRectangle(0, BandTop, 1024, BandHeight);
        BandTopPixels = band.Y;
        BandHeightPixels = band.Height;
        Set(Header, layout.CalcRectangle1600(10, 18, 512, 100));

        TextScaleX = layout.Width / 1400f;
        TextScaleY = layout.Height / 1050f;
        int raise = layout.YToRes1050(FontRaise);
        int titleLeft = layout.XToRes(TitleLeft);
        int lineLeft = layout.XToRes(LineLeft);
        int top = FirstTitleTop;
        for (int i = 0; i < SectionCount; i++)
        {
            RaceHelpSectionViewModel section = Sections[i];
            section.TitleText.Set(titleLeft, layout.YToRes(top) - raise, 0, 0);
            section.Line0Text.Set(lineLeft, layout.YToRes(top + TitlePitch) - raise, 0, 0);
            section.Line1Text.Set(lineLeft, layout.YToRes(top + TitlePitch + LinePitch) - raise, 0, 0);
            top += TitlePitch + 2 * LinePitch + SectionGap;
        }

        _backButtonRect = layout.BackButton();
        bool hovered = _backButtonRect.Contains(mouseX, mouseY);
        Set(BackButton, hovered ? layout.GrowBottomButton(_backButtonRect) : _backButtonRect);
        BackButton.IsHighlighted = hovered;
    }

    /// <summary>Whether the point is on the B button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverBackButton(int x, int y) => _backButtonRect.Contains(x, y);

    private static void Set(RaceScreenRectViewModel target, Rectangle rect) => target.Set(rect.X, rect.Y, rect.Width, rect.Height);
}
