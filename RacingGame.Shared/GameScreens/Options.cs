using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Properties;
using RacingGame.Sounds;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;

namespace RacingGame.GameScreens;

class Options : IGameScreen, IMguiScreen
{
    private string currentPlayerName = GameSettings.Default.PlayerName;
    private bool _isFinished = false;
    private readonly List<(int Width, int Height)> _availableResolutions = new();
    private bool showFps = false;
    private bool useGamepadVibration = true;
    private IMguiScreenView _mguiView;
    private Point? _mguiViewSize;

    private int currentResolution = 4;
    private bool fullscreen = true;
    private bool usePostScreenShaders = true;
    private bool useShadowMapping = true;
    private bool useHighDetail = true;
    private float currentMusicVolume = 1.0f;
    private float currentSoundVolume = 1.0f;
    private float currentSensitivity = 1.0f;

    public Options()
    {
        BuildResolutionList();

        currentResolution = 4;
        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            if (BaseGame.Width == _availableResolutions[i].Width &&
                BaseGame.Height == _availableResolutions[i].Height)
            {
                currentResolution = i;
                break;
            }
        }

        fullscreen = BaseGame.Fullscreen;
        usePostScreenShaders = BaseGame.UsePostScreenShaders;
        useShadowMapping = BaseGame.AllowShadowMapping;
        useHighDetail = BaseGame.HighDetail;
        showFps = GameSettings.Default.ShowFPS;
        useGamepadVibration = GameSettings.Default.GamepadVibration;
        currentMusicVolume = GameSettings.Default.MusicVolume;
        currentSoundVolume = GameSettings.Default.SoundVolume;
        currentSensitivity = GameSettings.Default.ControllerSensitivity;
    }

    private void BuildResolutionList()
    {
        var preferred = new List<(int W, int H)>
        {
            (1280, 720),
            (1920, 1080),
            (2560, 1440),
            (3840, 2160),
        };

        var classic = new List<(int W, int H)>
        {
            (800, 600),
            (1024, 768),
            (1280, 1024),
            (1600, 900),
        };

        try
        {
            var supported = new HashSet<(int, int)>();
            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                supported.Add((mode.Width, mode.Height));

            foreach (var (w, h) in preferred)
                if (supported.Contains((w, h)))
                    _availableResolutions.Add((w, h));
        }
        catch (Exception)
        {
        }

        foreach (var (w, h) in classic)
        {
            if (_availableResolutions.Count >= 4)
                break;
            if (!_availableResolutions.Contains((w, h)))
                _availableResolutions.Add((w, h));
        }
    }

    public void Update(GameTime gameTime)
    {
        Sound.SetVolumes(currentSoundVolume, currentMusicVolume);

        if (Input.KeyboardEscapeJustPressed ||
            Input.GamePadBJustPressed ||
            Input.GamePadBackJustPressed)
        {
            ApplyAndClose();
        }
    }

    public bool Render()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();

        return _isFinished;
    }

    public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
    {
        Point viewportSize = new(host.ViewportBounds.Width, host.ViewportBounds.Height);
        if (_mguiView == null || _mguiViewSize != viewportSize)
        {
            _mguiView = new OptionsView(this, host);
            _mguiViewSize = viewportSize;
        }

        return _mguiView;
    }

    internal string PlayerName
    {
        get => currentPlayerName;
        set => currentPlayerName = value;
    }

    internal IReadOnlyList<(int Width, int Height)> AvailableResolutions => _availableResolutions;
    internal int CurrentResolution => currentResolution;
    internal bool Fullscreen => fullscreen;
    internal bool UsePostScreenShaders => usePostScreenShaders;
    internal bool UseShadowMapping => useShadowMapping;
    internal bool UseHighDetail => useHighDetail;
    internal bool ShowFps => showFps;
    internal bool UseGamepadVibration => useGamepadVibration;
    internal float CurrentMusicVolume => currentMusicVolume;
    internal float CurrentSoundVolume => currentSoundVolume;
    internal float CurrentSensitivity => currentSensitivity;

    internal string GetResolutionLabel(int index)
    {
        if (index >= 0 && index < _availableResolutions.Count)
            return $"{_availableResolutions[index].Width}x{_availableResolutions[index].Height}";

        return "Auto";
    }

    internal void SelectResolution(int index) => currentResolution = index;
    internal void SetFullscreen(bool value) => fullscreen = value;
    internal void SetPostScreenShaders(bool value) => usePostScreenShaders = value;
    internal void SetShadowMapping(bool value) => useShadowMapping = value;
    internal void SetHighDetail(bool value) => useHighDetail = value;
    internal void SetShowFps(bool value) => showFps = value;
    internal void SetGamepadVibration(bool value) => useGamepadVibration = value;
    internal void SetMusicVolume(float value) => currentMusicVolume = Math.Clamp(value, 0f, 1f);
    internal void SetSoundVolume(float value) => currentSoundVolume = Math.Clamp(value, 0f, 1f);
    internal void SetSensitivity(float value) => currentSensitivity = Math.Clamp(value, 0f, 1f);

    internal void ApplyAndClose()
    {
        GameSettings.Default.PlayerName = currentPlayerName;
        if (currentResolution >= 0 && currentResolution < _availableResolutions.Count)
        {
            GameSettings.Default.ResolutionWidth = _availableResolutions[currentResolution].Width;
            GameSettings.Default.ResolutionHeight = _availableResolutions[currentResolution].Height;
        }
        else
        {
            GameSettings.Default.ResolutionWidth = 0;
            GameSettings.Default.ResolutionHeight = 0;
        }

        GameSettings.Default.Fullscreen = fullscreen;
        GameSettings.Default.PostScreenEffects = usePostScreenShaders;
        GameSettings.Default.ShadowMapping = useShadowMapping;
        GameSettings.Default.HighDetail = useHighDetail;
        GameSettings.Default.ShowFPS = showFps;
        GameSettings.Default.GamepadVibration = useGamepadVibration;
        GameSettings.Default.MusicVolume = currentMusicVolume;
        GameSettings.Default.SoundVolume = currentSoundVolume;
        GameSettings.Default.ControllerSensitivity = currentSensitivity;
        GameSettings.Save();
        BaseGame.CheckOptionsAndPSVersion();
        BaseGame.ApplyResolutionChange();
        _isFinished = true;
    }
}