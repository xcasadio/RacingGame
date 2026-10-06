using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.TrackSelection</c>, RacingGame's track selection (ADR-0010): the menu decoration, the black
/// band, the "SELECT TRACK" header, the three track cards and the A and B buttons, laid out every frame in screen pixels
/// with RacingGame's formulas (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/TrackSelection.cs</c>, Update and
/// Render; <c>Graphics/UIRenderer.cs</c>, RenderBlackBar and RenderBottomButtons; <c>Graphics/BaseGame.cs</c>, XToRes,
/// YToRes, CalcRectangle, CalcRectangle1600 and CalcRectangleCenteredWithGivenHeight; <c>MainMenu.InterpolateRect</c>).
/// <para/>
/// A card is as high as 132 (selected) or 108 (other) units of width would make the 212x352 sprite, and as wide as that
/// height gives at the sprite's aspect. Its size moves towards the selected or the other size at 2 per second. The row
/// starts where its resting width would be centred, so the cards slide while they grow and shrink. Each opening starts
/// from card 1 grown, as RacingGame's (<see cref="Reset"/>).
/// </summary>
public sealed class RaceTrackSelectionViewModel : RaceViewModelBase
{
    public const int CardCount = 3;

    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 640f;
    private const int BandTop = 220;
    private const int BandHeight = 280;
    // UIRenderer.HeaderSelectTrackGfxRect (0, 312, 512, 100) of headers.png, drawn with RenderOnScreenRelative1600(10, 18).
    private const int HeaderLeft = 10;
    private const int HeaderTop = 18;
    private const int HeaderArtWidth = 512;
    private const int HeaderArtHeight = 100;
    // UIRenderer.TrackButton*GfxRect (212x352) and TrackText*GfxRect (212x24) of buttons.png.
    private const int CardArtWidth = 212;
    private const int CardArtHeight = 352;
    private const int LabelArtHeight = 24;
    private const int ActiveButtonWidth = 132;
    private const int InactiveButtonWidth = 108;
    private const int DistanceBetweenButtons = 32;
    private const int RowTop = 258;
    private const int LabelGap = 5;
    // Size steps per second of the grow/shrink animation (RacingGame: MoveFactorPerSecond * 2).
    private const float AnimationSpeed = 2f;
    // UIRenderer.BottomButton*GfxRect (212x92) of buttons.png, 48 units high, centred on y 587.
    private const int BottomButtonArtWidth = 212;
    private const int BottomButtonArtHeight = 92;
    private const int BottomButtonHeight = 48;
    private const int BottomButtonCentreY = 587;

    private readonly float[] _sizeFactors = new float[CardCount];
    private int _viewportWidth = (int)ReferenceWidth;
    private int _viewportHeight = (int)ReferenceHeight;
    private int _bandTopPixels;
    private int _bandHeightPixels;
    private int _selectedIndex;
    private Rectangle _selectButtonRect;
    private Rectangle _backButtonRect;

    public RaceTrackSelectionViewModel()
    {
        Cards = [Card0, Card1, Card2];
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

    public RaceTrackCardViewModel Card0 { get; } = new();

    public RaceTrackCardViewModel Card1 { get; } = new();

    public RaceTrackCardViewModel Card2 { get; } = new();

    /// <summary>The three cards: Beginner, Advanced, Expert.</summary>
    public IReadOnlyList<RaceTrackCardViewModel> Cards { get; }

    /// <summary>The A SELECT button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel SelectButton { get; } = new();

    /// <summary>The B BACK button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel BackButton { get; } = new();

    /// <summary>Index of the selected card in <see cref="Cards"/>.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, Math.Clamp(value, 0, CardCount - 1));
    }

    /// <summary>Selects <paramref name="selectedIndex"/> with RacingGame's opening sizes: card 1 grown, the others small.</summary>
    public void Reset(int selectedIndex)
    {
        SelectedIndex = selectedIndex;
        for (int i = 0; i < CardCount; i++)
        {
            _sizeFactors[i] = i == 0 ? 1f : 0f;
        }
    }

    /// <summary>Lays everything out for the viewport, and advances the cards' size animation.</summary>
    /// <param name="pinAnimations">Shows the cards at rest, for the capture automation.</param>
    /// <param name="mouseX">The mouse position in screen pixels, to grow the A or B button under it.</param>
    public void Update(int viewportWidth, int viewportHeight, double elapsedSeconds, bool pinAnimations, int mouseX, int mouseY)
    {
        _viewportWidth = Math.Max(1, viewportWidth);
        _viewportHeight = Math.Max(1, viewportHeight);

        BandTopPixels = Round(BandTop * HeightFactor);
        BandHeightPixels = Round(BandHeight * HeightFactor);

        // RenderOnScreenRelative1600: CalcRectangle1600.
        float width1600 = _viewportWidth / 1600f;
        float height1200 = _viewportHeight / 1200f;
        Header.Set(Round(HeaderLeft * width1600), Round(HeaderTop * height1200), Round(HeaderArtWidth * width1600), Round(HeaderArtHeight * height1200));

        // CalcRectangleCenteredWithGivenHeight(0, 0, width x 352 / 212, card art): the height from the units, the width
        // from the height at the art's aspect.
        int activeHeight = Round(ActiveButtonWidth * CardArtHeight / CardArtWidth * HeightFactor);
        int activeWidth = Round(CardArtWidth * activeHeight / (float)CardArtHeight);
        int inactiveHeight = Round(InactiveButtonWidth * CardArtHeight / CardArtWidth * HeightFactor);
        int inactiveWidth = Round(CardArtWidth * inactiveHeight / (float)CardArtHeight);
        int gap = XToRes(DistanceBetweenButtons);
        int totalWidth = activeWidth + 2 * inactiveWidth + 2 * gap;
        int x = XToRes(512) - totalWidth / 2;
        int rowTop = YToRes(RowTop);

        float step = (float)elapsedSeconds * AnimationSpeed;
        for (int i = 0; i < CardCount; i++)
        {
            bool selected = i == _selectedIndex;
            _sizeFactors[i] = pinAnimations
                ? selected ? 1f : 0f
                : Math.Clamp(_sizeFactors[i] + (selected ? step : -step), 0f, 1f);

            // MainMenu.InterpolateRect.
            float t = _sizeFactors[i];
            int width = (int)Math.Round(activeWidth * t + inactiveWidth * (1 - t));
            int height = (int)Math.Round(activeHeight * t + inactiveHeight * (1 - t));
            int top = rowTop - (height - inactiveHeight) / 2;

            RaceTrackCardViewModel card = Cards[i];
            card.Rect.Set(x, top, width, height);
            card.Label.Set(x, top + height + YToRes(LabelGap), width, height * LabelArtHeight / CardArtHeight);
            card.IsSelected = selected;
            x += width + gap;
        }

        int buttonHeight = Round(BottomButtonHeight * HeightFactor);
        int buttonWidth = Round(BottomButtonArtWidth * buttonHeight / (float)BottomButtonArtHeight);
        int buttonTop = Math.Max(0, Round(BottomButtonCentreY * HeightFactor) - buttonHeight / 2);
        _backButtonRect = new Rectangle(_viewportWidth - buttonWidth - XToRes(25 + 25), buttonTop, buttonWidth, buttonHeight);
        _selectButtonRect = new Rectangle(_viewportWidth - buttonWidth * 2 - XToRes(55 + 25), buttonTop, buttonWidth, buttonHeight);
        LayOutBottomButton(BackButton, _backButtonRect, mouseX, mouseY);
        LayOutBottomButton(SelectButton, _selectButtonRect, mouseX, mouseY);
    }

    /// <summary>The card under the point (at its current, animated rectangle), or -1.</summary>
    public int GetCardAt(int x, int y)
    {
        for (int i = 0; i < CardCount; i++)
        {
            if (Cards[i].Rect.Contains(x, y))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Whether the point is on the A button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverSelectButton(int x, int y) => _selectButtonRect.Contains(x, y);

    /// <summary>Whether the point is on the B button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverBackButton(int x, int y) => _backButtonRect.Contains(x, y);

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

    private float HeightFactor => _viewportHeight / ReferenceHeight;

    private int XToRes(int x) => (int)Math.Round(x * _viewportWidth / ReferenceWidth);

    private int YToRes(int y) => (int)Math.Round(y * _viewportHeight / ReferenceHeight);

    // RacingGame rounds with (int)Math.Round, half to even.
    private static int Round(float value) => (int)Math.Round(value);
}
