using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Background and bouncing logo shared by the menu screens. The logo rectangle is the legacy menu's (362, 36, 601, 218)
/// in its 1024x640 reference layout, scaled to the viewport and to the bounce of the moment; <see cref="Update"/>
/// recomputes it every frame with the formula of the code-built screens.
/// </summary>
public sealed class RaceMenuDecorationViewModel : RaceViewModelBase
{
    private const float ReferenceWidth = 1024.0f;
    private const float ReferenceHeight = 640.0f;
    private const int ReferenceLogoX = 362;
    private const int ReferenceLogoY = 36;
    private const int ReferenceLogoWidth = 601;
    private const int ReferenceLogoHeight = 218;

    private int _logoLeft;
    private int _logoTop;
    private int _logoWidth;
    private int _logoHeight;

    public int LogoLeft
    {
        get => _logoLeft;
        set
        {
            if (SetProperty(ref _logoLeft, value))
            {
                NotifyPropertyChanged(nameof(LogoMargin));
            }
        }
    }

    public int LogoTop
    {
        get => _logoTop;
        set
        {
            if (SetProperty(ref _logoTop, value))
            {
                NotifyPropertyChanged(nameof(LogoMargin));
            }
        }
    }

    /// <summary>The logo's top-left corner, as the margin of a top-left aligned image.</summary>
    public Thickness LogoMargin => new(_logoLeft, _logoTop, 0, 0);

    public int LogoWidth
    {
        get => _logoWidth;
        set => SetProperty(ref _logoWidth, value);
    }

    public int LogoHeight
    {
        get => _logoHeight;
        set => SetProperty(ref _logoHeight, value);
    }

    public void Update(int viewportWidth, int viewportHeight, double totalSeconds)
    {
        float bounceSize = 1.005f + (float)Math.Sin(totalSeconds / 0.46f) * 0.045f * (float)Math.Cos(totalSeconds / 0.285f);
        float widthFactor = viewportWidth / ReferenceWidth;
        float heightFactor = viewportHeight / ReferenceHeight;
        float middleX = (ReferenceLogoX + ReferenceLogoWidth / 2f) * widthFactor;
        float middleY = (ReferenceLogoY + ReferenceLogoHeight / 2f) * heightFactor;
        float scaledWidth = ReferenceLogoWidth * widthFactor * bounceSize;
        float scaledHeight = ReferenceLogoHeight * heightFactor * bounceSize;

        LogoLeft = (int)Math.Round(middleX - scaledWidth / 2f);
        LogoTop = (int)Math.Round(middleY - scaledHeight / 2f);
        LogoWidth = (int)Math.Round(scaledWidth);
        LogoHeight = (int)Math.Round(scaledHeight);
    }
}
