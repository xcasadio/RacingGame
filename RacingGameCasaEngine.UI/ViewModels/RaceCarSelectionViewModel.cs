using MGUI.Core.UI;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.CarSelection</c>, the car selection of the author's capture of the original game (ADR-0008):
/// the menu decoration, the black band, the "CHOOSE YOUR CAR" header, the six property rows, the eleven colour squares,
/// the two selection arrows and the A and B buttons, laid out every frame in screen pixels with RacingGame's formulas
/// (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/CarSelection.cs</c>, Render; <c>Graphics/UIRenderer.cs</c>,
/// RenderBlackBar and RenderBottomButtons; <c>Graphics/BaseGame.cs</c>, XToRes, YToRes, CalcRectangle,
/// CalcRectangle1600 and CalcRectangleCenteredWithGivenHeight). The arrows' x comes from the capture, where it differs
/// from the repository's code. Texts are drawn with RacingGame's GameFont at its native size and scaled at draw time by
/// <see cref="TextScaleX"/> and <see cref="TextScaleY"/>, as <c>TextureFont</c> scaled them.
/// </summary>
public sealed class RaceCarSelectionViewModel : RaceViewModelBase
{
    public const int StatCount = 6;
    public const int SwatchCount = 11;

    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 640f;
    private const int BandTop = 170;
    private const int BandHeight = 390;
    // UIRenderer.HeaderChooseCarGfxRect (0, 212, 512, 100) of headers.png, drawn with RenderOnScreenRelative1600(10, 18).
    private const int HeaderLeft = 10;
    private const int HeaderTop = 18;
    private const int HeaderArtWidth = 512;
    private const int HeaderArtHeight = 100;
    private const int StatLeft = 1024 - 258;
    private static readonly int[] StatTops = [190, 235, 280, 335, 390, 445];
    private const int StatBarOffset = 29;
    private const int StatBarHeight = 6;
    private const int StatBarUnit = 192;
    private const int ColorLabelLeft = 85;
    private const int ColorLabelTop = 512;
    private const int SwatchLeft = 250;
    private const int SwatchStep = 50;
    private const int SwatchTop = 500;
    private const int SwatchSize = 46;
    private const int SwatchGrowth = 6;
    // UIRenderer.SelectionArrowGfxRect (874, 426, 53, 39) of buttons.png. RacingGame's code put the arrows at x 35 and
    // 1024 - 335 - 53; the capture shows them centred on the front plate's left and right edges (at x 279 and 691 of the
    // 1024 layout there), so they follow the plate's projected edges, whatever the screen's aspect.
    private const int ArrowWidth = 53;
    private const int ArrowHeight = 39;
    private const int LeftArrowLeft = 279;
    private const int RightArrowLeft = 691;
    private const int ArrowSwing = 12;
    // UIRenderer.BottomButton*GfxRect (212x92) of buttons.png, 48 units high, centred on y 587.
    private const int BottomButtonArtWidth = 212;
    private const int BottomButtonArtHeight = 92;
    private const int BottomButtonHeight = 48;
    private const int BottomButtonCentreY = 587;
    // TextureFont: glyphs scaled from a 1400x1050 layout and raised by SubRenderHeight.
    private const float FontReferenceWidth = 1400f;
    private const float FontReferenceHeight = 1050f;
    private const int FontRaise = 5;

    // Mouse zones that turn the carousel (CarSelection.Update, Input.MouseInBoxRelative), in 1024x640 units.
    private static readonly Rectangle TurnLeftZone = new(512 + 50, 170, 512 - 150, 135);
    private static readonly Rectangle TurnRightZone = new(100, 170, 512 - 200, 135);

    private readonly float[] _statValues = new float[StatCount];
    private int _viewportWidth = (int)ReferenceWidth;
    private int _viewportHeight = (int)ReferenceHeight;
    private int _bandTopPixels;
    private int _bandHeightPixels;
    private int _selectedColorIndex;
    private Rectangle _selectButtonRect;
    private Rectangle _backButtonRect;
    private MGTextureData? _carouselImage;

    public RaceCarSelectionViewModel()
    {
        Stats = [Stat0, Stat1, Stat2, Stat3, Stat4, Stat5];
        Swatches = [Swatch0, Swatch1, Swatch2, Swatch3, Swatch4, Swatch5, Swatch6, Swatch7, Swatch8, Swatch9, Swatch10];
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

    /// <summary>The band's top, as the margin of a top-aligned, full-width element.</summary>
    public MonoGame.Extended.Thickness BandMargin => new(0, _bandTopPixels, 0, 0);

    public RaceScreenRectViewModel Header { get; } = new();

    /// <summary>The "Car Color:" label's top-left corner.</summary>
    public RaceScreenRectViewModel ColorLabel { get; } = new();

    public RaceCarStatViewModel Stat0 { get; } = new();

    public RaceCarStatViewModel Stat1 { get; } = new();

    public RaceCarStatViewModel Stat2 { get; } = new();

    public RaceCarStatViewModel Stat3 { get; } = new();

    public RaceCarStatViewModel Stat4 { get; } = new();

    public RaceCarStatViewModel Stat5 { get; } = new();

    /// <summary>The six property rows, top to bottom: max speed, acceleration, car mass, braking, friction, engine.</summary>
    public IReadOnlyList<RaceCarStatViewModel> Stats { get; }

    public RaceCarSwatchViewModel Swatch0 { get; } = new();

    public RaceCarSwatchViewModel Swatch1 { get; } = new();

    public RaceCarSwatchViewModel Swatch2 { get; } = new();

    public RaceCarSwatchViewModel Swatch3 { get; } = new();

    public RaceCarSwatchViewModel Swatch4 { get; } = new();

    public RaceCarSwatchViewModel Swatch5 { get; } = new();

    public RaceCarSwatchViewModel Swatch6 { get; } = new();

    public RaceCarSwatchViewModel Swatch7 { get; } = new();

    public RaceCarSwatchViewModel Swatch8 { get; } = new();

    public RaceCarSwatchViewModel Swatch9 { get; } = new();

    public RaceCarSwatchViewModel Swatch10 { get; } = new();

    public IReadOnlyList<RaceCarSwatchViewModel> Swatches { get; }

    public RaceScreenRectViewModel LeftArrow { get; } = new();

    public RaceScreenRectViewModel RightArrow { get; } = new();

    /// <summary>The A SELECT button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel SelectButton { get; } = new();

    /// <summary>The B BACK button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel BackButton { get; } = new();

    /// <summary>The render target of the 3D car carousel, as large as the screen; set by the screen, absent at design time.</summary>
    public MGTextureData? CarouselImage
    {
        get => _carouselImage;
        set => SetProperty(ref _carouselImage, value);
    }

    /// <summary>Horizontal draw scale of the GameFont texts (TextureFont's XToRes1400).</summary>
    public float TextScaleX { get; private set; } = 1f;

    /// <summary>Vertical draw scale of the GameFont texts (TextureFont's YToRes1050).</summary>
    public float TextScaleY { get; private set; } = 1f;

    /// <summary>The selected colour, whose square is drawn larger.</summary>
    public int SelectedColorIndex
    {
        get => _selectedColorIndex;
        set => SetProperty(ref _selectedColorIndex, Math.Clamp(value, 0, SwatchCount - 1));
    }

    /// <summary>Sets a property row: its label and its value (1 = a bar of 192 units; RacingGame's go past 1).</summary>
    public void SetStat(int index, string label, float value)
    {
        Stats[index].Label = label;
        _statValues[index] = value;
    }

    /// <summary>Lays everything out for the viewport and the time.</summary>
    /// <param name="totalSeconds">The game's total time, as RacingGame's <c>BaseGame.TotalTime</c>, for the arrows' swing.</param>
    /// <param name="pinAnimations">Holds the arrows still, for the capture automation.</param>
    /// <param name="mouseX">The mouse position in screen pixels, to grow the A or B button under it.</param>
    /// <param name="plateLeft">The front plate's left edge on the screen, or -1 before the carousel is laid out.</param>
    /// <param name="plateRight">The front plate's right edge on the screen, or -1 before the carousel is laid out.</param>
    public void Update(int viewportWidth, int viewportHeight, double totalSeconds, bool pinAnimations, int mouseX, int mouseY, int plateLeft = -1, int plateRight = -1)
    {
        _viewportWidth = Math.Max(1, viewportWidth);
        _viewportHeight = Math.Max(1, viewportHeight);

        BandTopPixels = Round(BandTop * HeightFactor);
        BandHeightPixels = Round(BandHeight * HeightFactor);

        // RenderOnScreenRelative1600: CalcRectangle1600.
        float width1600 = _viewportWidth / 1600f;
        float height1200 = _viewportHeight / 1200f;
        Header.Set(Round(HeaderLeft * width1600), Round(HeaderTop * height1200), Round(HeaderArtWidth * width1600), Round(HeaderArtHeight * height1200));

        TextScaleX = _viewportWidth / FontReferenceWidth;
        TextScaleY = _viewportHeight / FontReferenceHeight;
        int fontRaise = YToRes1050(FontRaise);

        ColorLabel.Set(XToRes(ColorLabelLeft), YToRes(ColorLabelTop) - fontRaise, 0, 0);
        for (int i = 0; i < SwatchCount; i++)
        {
            int growth = i == _selectedColorIndex ? SwatchGrowth : 0;
            Rectangle rect = CalcRectangle(SwatchLeft + i * SwatchStep - growth, SwatchTop - growth, SwatchSize + 2 * growth, SwatchSize + 2 * growth);
            Swatches[i].Rect.Set(rect.X, rect.Y, rect.Width, rect.Height);
        }

        int statLeft = XToRes(StatLeft);
        for (int i = 0; i < StatCount; i++)
        {
            int top = YToRes(StatTops[i]);
            int barWidth = XToRes((int)(StatBarUnit * _statValues[i]));
            Stats[i].Text.Set(statLeft, top - fontRaise, 0, 0);
            Stats[i].Bar.Set(statLeft, top + YToRes(StatBarOffset), barWidth, YToRes(StatBarHeight));
            Stats[i].Bar.IsShown = barWidth > 0;
        }

        float wave = pinAnimations ? 0f : (float)Math.Sin(totalSeconds / 0.46f) * (float)Math.Cos(totalSeconds / 0.285f);
        int swing = (int)Math.Round(XToRes(ArrowSwing) * wave);
        int arrowTop = YToRes(300 + 60) + YToRes(120) / 3;
        Rectangle leftArrow = CalcRectangle(LeftArrowLeft, 250, ArrowWidth, ArrowHeight);
        Rectangle rightArrow = CalcRectangle(RightArrowLeft, 250, ArrowWidth, ArrowHeight);
        if (plateLeft >= 0 && plateRight > plateLeft)
        {
            leftArrow.X = plateLeft - leftArrow.Width / 2;
            rightArrow.X = plateRight - rightArrow.Width / 2;
        }

        LeftArrow.Set(leftArrow.X + swing, arrowTop, leftArrow.Width, leftArrow.Height);
        RightArrow.Set(rightArrow.X - swing, arrowTop, rightArrow.Width, rightArrow.Height);

        int buttonHeight = Round(BottomButtonHeight * HeightFactor);
        int buttonWidth = Round(BottomButtonArtWidth * buttonHeight / (float)BottomButtonArtHeight);
        int buttonTop = Math.Max(0, Round(BottomButtonCentreY * HeightFactor) - buttonHeight / 2);
        _backButtonRect = new Rectangle(_viewportWidth - buttonWidth - XToRes(25 + 25), buttonTop, buttonWidth, buttonHeight);
        _selectButtonRect = new Rectangle(_viewportWidth - buttonWidth * 2 - XToRes(55 + 25), buttonTop, buttonWidth, buttonHeight);
        LayOutBottomButton(BackButton, _backButtonRect, mouseX, mouseY);
        LayOutBottomButton(SelectButton, _selectButtonRect, mouseX, mouseY);
    }

    /// <summary>Whether the point is on the A button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverSelectButton(int x, int y) => _selectButtonRect.Contains(x, y);

    /// <summary>Whether the point is on the B button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverBackButton(int x, int y) => _backButtonRect.Contains(x, y);

    /// <summary>The colour square under the point (the selected one at its larger size), or -1.</summary>
    public int GetSwatchAt(int x, int y)
    {
        for (int i = 0; i < SwatchCount; i++)
        {
            if (Swatches[i].Rect.Contains(x, y))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Whether a click at the point turns the carousel as the Left key does (the right-hand zone).</summary>
    public bool IsInTurnLeftZone(int x, int y) => ToScreen(TurnLeftZone).Contains(x, y);

    /// <summary>Whether a click at the point turns the carousel as the Right key does (the left-hand zone).</summary>
    public bool IsInTurnRightZone(int x, int y) => ToScreen(TurnRightZone).Contains(x, y);

    private void LayOutBottomButton(RaceScreenRectViewModel button, Rectangle rect, int mouseX, int mouseY)
    {
        bool hovered = rect.Contains(mouseX, mouseY);
        if (hovered)
        {
            int xAdd = XToRes(16);
            int yAdd = YToRes(9);
            rect = new Rectangle(rect.X - xAdd / 2, rect.Y - yAdd / 2, rect.Width + xAdd, rect.Height + yAdd);
        }

        button.Set(rect.X, rect.Y, rect.Width, rect.Height);
        button.IsHighlighted = hovered;
    }

    private float WidthFactor => _viewportWidth / ReferenceWidth;

    private float HeightFactor => _viewportHeight / ReferenceHeight;

    // MouseInBoxRelative.
    private Rectangle ToScreen(Rectangle reference) =>
        new(Round(reference.X * WidthFactor), Round(reference.Y * HeightFactor), Round(reference.Width * WidthFactor), Round(reference.Height * HeightFactor));

    private Rectangle CalcRectangle(int x, int y, int width, int height) =>
        new(Round(x * WidthFactor), Round(y * HeightFactor), Round(width * WidthFactor), Round(height * HeightFactor));

    private int XToRes(int x) => (int)Math.Round(x * _viewportWidth / ReferenceWidth);

    private int YToRes(int y) => (int)Math.Round(y * _viewportHeight / ReferenceHeight);

    private int YToRes1050(int y) => (int)Math.Round(y * _viewportHeight / FontReferenceHeight);

    // RacingGame rounds with (int)Math.Round, half to even.
    private static int Round(float value) => (int)Math.Round(value);
}
