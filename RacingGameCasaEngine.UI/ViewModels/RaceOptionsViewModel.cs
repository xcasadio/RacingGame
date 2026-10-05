using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.Options</c>. The form fields are bound two ways: the screen writes every change made in the
/// form to the game's front-end state, and refreshes the fields from that state every frame. The band and scroll sizes
/// follow the safe area and the UI scale, computed once when the screen is built, as the code-built screen did.
/// </summary>
public sealed class RaceOptionsViewModel : RaceViewModelBase
{
    private int _bandTop;
    private int _bandHeight;
    private int _scrollWidth;
    private int _scrollHeight;
    private string _playerName = string.Empty;
    private bool _isFullscreen;
    private bool _enableVSync;
    private bool _enablePostEffects;
    private bool _enableShadows;
    private bool _showFps;
    private bool _enableVibration;
    private float _soundVolume;
    private float _musicVolume;
    private float _controllerSensitivity;
    private string _soundVolumeText = string.Empty;
    private string _musicVolumeText = string.Empty;
    private string _controllerSensitivityText = string.Empty;

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public RaceMenuTextButtonViewModel BackButton { get; } = new();

    public int BandTop
    {
        get => _bandTop;
        set
        {
            if (SetProperty(ref _bandTop, value))
            {
                NotifyPropertyChanged(nameof(BandMargin));
            }
        }
    }

    /// <summary>The band's top offset, as the margin of a top-aligned border.</summary>
    public Thickness BandMargin => new(0, _bandTop, 0, 0);

    public int BandHeight
    {
        get => _bandHeight;
        set => SetProperty(ref _bandHeight, value);
    }

    public int ScrollWidth
    {
        get => _scrollWidth;
        set => SetProperty(ref _scrollWidth, value);
    }

    public int ScrollHeight
    {
        get => _scrollHeight;
        set => SetProperty(ref _scrollHeight, value);
    }

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }

    public bool IsFullscreen
    {
        get => _isFullscreen;
        set => SetProperty(ref _isFullscreen, value);
    }

    public bool EnableVSync
    {
        get => _enableVSync;
        set => SetProperty(ref _enableVSync, value);
    }

    public bool EnablePostEffects
    {
        get => _enablePostEffects;
        set => SetProperty(ref _enablePostEffects, value);
    }

    public bool EnableShadows
    {
        get => _enableShadows;
        set => SetProperty(ref _enableShadows, value);
    }

    public bool ShowFps
    {
        get => _showFps;
        set => SetProperty(ref _showFps, value);
    }

    public bool EnableVibration
    {
        get => _enableVibration;
        set => SetProperty(ref _enableVibration, value);
    }

    public float SoundVolume
    {
        get => _soundVolume;
        set => SetProperty(ref _soundVolume, value);
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set => SetProperty(ref _musicVolume, value);
    }

    public float ControllerSensitivity
    {
        get => _controllerSensitivity;
        set => SetProperty(ref _controllerSensitivity, value);
    }

    public string SoundVolumeText
    {
        get => _soundVolumeText;
        set => SetProperty(ref _soundVolumeText, value);
    }

    public string MusicVolumeText
    {
        get => _musicVolumeText;
        set => SetProperty(ref _musicVolumeText, value);
    }

    public string ControllerSensitivityText
    {
        get => _controllerSensitivityText;
        set => SetProperty(ref _controllerSensitivityText, value);
    }

    public void UpdateLayout(int safeAreaTop, int safeAreaWidth, int safeAreaHeight, float uiScale)
    {
        float scale = Math.Clamp(uiScale, 1.0f, 1.75f);
        int bandHeight = Math.Max(500, safeAreaHeight - 48);
        int footerHeight = (int)MathF.Round(64f * scale);
        int headerHeight = (int)MathF.Round(82f * scale);

        BandTop = safeAreaTop + 24;
        BandHeight = bandHeight;
        ScrollWidth = Math.Max(1120, safeAreaWidth - 160);
        ScrollHeight = Math.Max(280, bandHeight - headerHeight - footerHeight);
    }
}
