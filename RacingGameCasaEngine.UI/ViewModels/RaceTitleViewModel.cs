using MGUI.Core.UI;
using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.Splash</c>, the title screen of the author's capture of the original game (ADR-0009): the
/// menu decoration, a black band across the middle and RacingGame's "Press START to continue." sprite, laid out every
/// frame in screen pixels with RacingGame's 1024x640 formulas (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/
/// SplashScreen.cs</c>, Render; <c>BaseGame.CalcRectangle</c> and <c>CalcRectangleCenteredWithGivenHeight</c>). The band
/// is at the capture's height, y 350, where the repository's code drew it at y 518.
/// </summary>
public sealed class RaceTitleViewModel : RaceViewModelBase
{
    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 640f;
    private const int BandTop = 350;
    private const int BandHeight = 61;
    private const int PressStartReferenceHeight = 26;
    // UIRenderer.PressStartGfxRect (2, 1, 631, 45) of headers.png.
    private const int PressStartArtWidth = 631;
    private const int PressStartArtHeight = 45;
    // The sprite blinks: hidden for one 0.375 s step, shown for the next two (SplashScreen.Render).
    private const float BlinkStepSeconds = 0.375f;

    private int _bandTopPixels;
    private int _bandHeightPixels;
    private int _pressStartLeft;
    private int _pressStartTop;
    private int _pressStartWidth;
    private int _pressStartHeight;
    private bool _isPressStartShown = true;

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

    public int PressStartLeft
    {
        get => _pressStartLeft;
        set
        {
            if (SetProperty(ref _pressStartLeft, value))
            {
                NotifyPropertyChanged(nameof(PressStartMargin));
            }
        }
    }

    public int PressStartTop
    {
        get => _pressStartTop;
        set
        {
            if (SetProperty(ref _pressStartTop, value))
            {
                NotifyPropertyChanged(nameof(PressStartMargin));
            }
        }
    }

    public int PressStartWidth
    {
        get => _pressStartWidth;
        set => SetProperty(ref _pressStartWidth, value);
    }

    public int PressStartHeight
    {
        get => _pressStartHeight;
        set => SetProperty(ref _pressStartHeight, value);
    }

    public bool IsPressStartShown
    {
        get => _isPressStartShown;
        set
        {
            if (SetProperty(ref _isPressStartShown, value))
            {
                NotifyPropertyChanged(nameof(PressStartVisibility));
            }
        }
    }

    /// <summary>The band's top, as the margin of a top-aligned, full-width element.</summary>
    public Thickness BandMargin => new(0, _bandTopPixels, 0, 0);

    /// <summary>The sprite's top-left corner, as the margin of a top-left aligned image.</summary>
    public Thickness PressStartMargin => new(_pressStartLeft, _pressStartTop, 0, 0);

    public Visibility PressStartVisibility => _isPressStartShown ? Visibility.Visible : Visibility.Hidden;

    /// <summary>Lays the band and the sprite out for the viewport, and blinks the sprite.</summary>
    /// <param name="totalSeconds">The game's total time, as RacingGame's <c>BaseGame.TotalTime</c>.</param>
    /// <param name="pinBlink">Keeps the sprite shown, for the capture automation.</param>
    public void Update(int viewportWidth, int viewportHeight, double totalSeconds, bool pinBlink)
    {
        float widthFactor = viewportWidth / ReferenceWidth;
        float heightFactor = viewportHeight / ReferenceHeight;

        BandTopPixels = Round(BandTop * heightFactor);
        BandHeightPixels = Round(BandHeight * heightFactor);

        // CalcRectangleCenteredWithGivenHeight(512, 518 + 61 / 2, 26, PressStartGfxRect), at the capture's band.
        int height = Round(PressStartReferenceHeight * heightFactor);
        int width = Round(PressStartArtWidth * height / (float)PressStartArtHeight);
        PressStartLeft = Math.Max(0, Round(ReferenceWidth / 2 * widthFactor) - width / 2);
        PressStartTop = Math.Max(0, Round((BandTop + BandHeight / 2) * heightFactor) - height / 2);
        PressStartWidth = width;
        PressStartHeight = height;

        IsPressStartShown = pinBlink || (int)(totalSeconds / BlinkStepSeconds) % 3 != 0;
    }

    // RacingGame rounds with (int)Math.Round, half to even.
    private static int Round(float value) => (int)Math.Round(value);
}
