using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.MainMenu</c>: the menu decoration, and the band and the five buttons laid out every frame,
/// in screen pixels, with the formulas of RacingGame's main menu (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/
/// MainMenu.cs</c>, Render and Update; <c>BaseGame.CalcRectangle</c> and <c>CalcRectangleCenteredWithGivenHeight</c>).
/// Everything is placed in a 1024x640 reference layout scaled to the viewport: the band and the button sizes follow the
/// height, the gaps follow the width. The selected button grows to the active size in 0.5 s while the others shrink.
/// </summary>
public sealed class RaceMainMenuViewModel : RaceViewModelBase
{
    public const int ButtonCount = 5;

    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 640f;
    private const int BandTop = 280;
    private const int BandHeight = 192;
    private const int ActiveButtonSize = 132;
    private const int InactiveButtonSize = 108;
    private const int ButtonGap = 14;
    private const int InactiveButtonTop = 316;
    private const int LabelGap = 5;
    // Size steps per second of the grow/shrink animation (RacingGame: MoveFactorPerSecond * 2).
    private const float AnimationSpeed = 2f;

    // 1 = active size, 0 = inactive size; the menu opens with Play already grown, as RacingGame's did.
    private readonly float[] _sizeFactors = [1f, 0f, 0f, 0f, 0f];
    private int _bandTopPixels;
    private int _bandHeightPixels;
    private int _rowLeft;
    private int _rowTop;
    private int _buttonSpacing;
    private int _selectedIndex;

    public RaceMainMenuViewModel()
    {
        Buttons = [Play, Highscores, Options, Help, Quit];
    }

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public RaceMainMenuButtonViewModel Play { get; } = new();

    public RaceMainMenuButtonViewModel Highscores { get; } = new();

    public RaceMainMenuButtonViewModel Options { get; } = new();

    public RaceMainMenuButtonViewModel Help { get; } = new();

    public RaceMainMenuButtonViewModel Quit { get; } = new();

    /// <summary>The five buttons in menu order: Play, Highscores, Options, Help, Quit.</summary>
    public IReadOnlyList<RaceMainMenuButtonViewModel> Buttons { get; }

    /// <summary>Index of the selected button in <see cref="Buttons"/>.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, Math.Clamp(value, 0, ButtonCount - 1));
    }

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

    /// <summary>The left of the row: RacingGame's <c>XToRes(512) - totalWidth / 2</c>, with one active button.</summary>
    public int RowLeft
    {
        get => _rowLeft;
        set
        {
            if (SetProperty(ref _rowLeft, value))
            {
                NotifyPropertyChanged(nameof(RowMargin));
            }
        }
    }

    /// <summary>The top of the row of button slots: the active button's top.</summary>
    public int RowTop
    {
        get => _rowTop;
        set
        {
            if (SetProperty(ref _rowTop, value))
            {
                NotifyPropertyChanged(nameof(RowMargin));
            }
        }
    }

    /// <summary>The band's top, as the margin of a top-aligned, full-width element.</summary>
    public Thickness BandMargin => new(0, _bandTopPixels, 0, 0);

    /// <summary>The row's top-left corner, as the margin of a top-left aligned row.</summary>
    public Thickness RowMargin => new(_rowLeft, _rowTop, 0, 0);

    public int ButtonSpacing
    {
        get => _buttonSpacing;
        set => SetProperty(ref _buttonSpacing, value);
    }

    /// <summary>Lays the band and the buttons out for the viewport, and advances the size animation.</summary>
    public void Update(int viewportWidth, int viewportHeight, double elapsedSeconds)
    {
        float widthFactor = viewportWidth / ReferenceWidth;
        float heightFactor = viewportHeight / ReferenceHeight;

        BandTopPixels = Round(BandTop * heightFactor);
        BandHeightPixels = Round(BandHeight * heightFactor);

        int activeSize = Round(ActiveButtonSize * heightFactor);
        int inactiveSize = Round(InactiveButtonSize * heightFactor);
        int inactiveTop = Round(InactiveButtonTop * heightFactor);
        int slotTop = inactiveTop - (activeSize - inactiveSize) / 2;
        int labelGap = Round(LabelGap * heightFactor);
        int slotHeight = activeSize + labelGap + activeSize * 24 / 212;

        ButtonSpacing = Round(ButtonGap * widthFactor);
        RowLeft = Round(ReferenceWidth / 2 * widthFactor) - (activeSize + (ButtonCount - 1) * (inactiveSize + ButtonSpacing)) / 2;
        RowTop = slotTop;

        float step = (float)elapsedSeconds * AnimationSpeed;
        for (int i = 0; i < ButtonCount; i++)
        {
            bool selected = i == _selectedIndex;
            _sizeFactors[i] = Math.Clamp(_sizeFactors[i] + (selected ? step : -step), 0f, 1f);
            int size = Round(activeSize * _sizeFactors[i] + inactiveSize * (1 - _sizeFactors[i]));
            int top = inactiveTop - (size - inactiveSize) / 2 - slotTop;
            Buttons[i].Layout(size, top, labelGap, slotHeight, selected);
        }
    }

    // RacingGame rounds with (int)Math.Round, half to even.
    private static int Round(float value) => (int)Math.Round(value);
}
