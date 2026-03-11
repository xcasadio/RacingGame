using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class OptionsView : IMguiScreenView
{
    private readonly Options _screen;
    private readonly MGTextBox _playerName;
    private readonly MGButton[] _resolutionButtons;
    private readonly MGCheckBox _fullscreen;
    private readonly MGCheckBox _postFx;
    private readonly MGCheckBox _shadows;
    private readonly MGCheckBox _highDetail;
    private readonly MGCheckBox _showFps;
    private readonly MGCheckBox _vibration;
    private readonly MGSlider _sound;
    private readonly MGSlider _music;
    private readonly MGSlider _sensitivity;
    private readonly MGTextBlock _soundValue;
    private readonly MGTextBlock _musicValue;
    private readonly MGTextBlock _sensitivityValue;

    public OptionsView(Options screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var panel = MguiUiTheme.CreatePanel(Window, 20);
        var root = MguiUiTheme.CreateVerticalStack(Window, 10, 0);

        root.TryAddChild(MguiUiTheme.CreateHeading(Window, "Options"));
        root.TryAddChild(MguiUiTheme.CreateSubheading(Window, "Player profile, rendering flags, and analog settings are now configured through MGUI widgets."));

        root.TryAddChild(MguiUiTheme.CreateBodyText(Window, "Player Name", MguiUiTheme.AccentColor));
        _playerName = new MGTextBox(Window, 24, false, false)
        {
            PreferredWidth = 260,
        };
        _playerName.Text = screen.PlayerName;
        root.TryAddChild(_playerName);

        root.TryAddChild(MguiUiTheme.CreateBodyText(Window, "Resolution", MguiUiTheme.AccentColor));
        var resolutions = MguiUiTheme.CreateHorizontalStack(Window, 6);
        _resolutionButtons = new MGButton[5];
        for (int i = 0; i < _resolutionButtons.Length; i++)
        {
            int resIndex = i;
            _resolutionButtons[i] = MguiUiTheme.CreateSecondaryButton(Window, screen.GetResolutionLabel(i), () => screen.SelectResolution(resIndex));
            resolutions.TryAddChild(_resolutionButtons[i]);
        }
        root.TryAddChild(resolutions);

        _fullscreen = CreateCheckBox(Window, "Fullscreen", value => screen.SetFullscreen(value));
        _postFx = CreateCheckBox(Window, "Post Screen Effects", value => screen.SetPostScreenShaders(value));
        _shadows = CreateCheckBox(Window, "Shadows", value => screen.SetShadowMapping(value));
        _highDetail = CreateCheckBox(Window, "High Detail", value => screen.SetHighDetail(value));
        _showFps = CreateCheckBox(Window, "Show FPS", value => screen.SetShowFps(value));
        _vibration = CreateCheckBox(Window, "Gamepad Vibration", value => screen.SetGamepadVibration(value));

        root.TryAddChild(_fullscreen);
        root.TryAddChild(_postFx);
        root.TryAddChild(_shadows);
        root.TryAddChild(_highDetail);
        root.TryAddChild(_showFps);
        root.TryAddChild(_vibration);

        (_sound, _soundValue) = CreateSliderRow(Window, root, "Sound Volume", screen.CurrentSoundVolume, screen.SetSoundVolume);
        (_music, _musicValue) = CreateSliderRow(Window, root, "Music Volume", screen.CurrentMusicVolume, screen.SetMusicVolume);
        (_sensitivity, _sensitivityValue) = CreateSliderRow(Window, root, "Controller Sensitivity", screen.CurrentSensitivity, screen.SetSensitivity);

        root.TryAddChild(MguiUiTheme.CreatePrimaryButton(Window, "Save and Back", screen.ApplyAndClose));

        panel.SetContent(root);
        Window.SetContent(panel);
    }

    public MGWindow Window { get; }
    public MGElement InitialFocusElement => _playerName;
    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        Refresh();
        _playerName.Focus();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        if (_screen.PlayerName != _playerName.Text)
            _screen.PlayerName = _playerName.Text;

        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < _resolutionButtons.Length; i++)
        {
            _resolutionButtons[i].BorderThickness = _screen.CurrentResolution == i ? new(3) : new(1);
        }

        _fullscreen.IsChecked = _screen.Fullscreen;
        _postFx.IsChecked = _screen.UsePostScreenShaders;
        _shadows.IsChecked = _screen.UseShadowMapping;
        _highDetail.IsChecked = _screen.UseHighDetail;
        _showFps.IsChecked = _screen.ShowFps;
        _vibration.IsChecked = _screen.UseGamepadVibration;

        _sound.Value = _screen.CurrentSoundVolume;
        _music.Value = _screen.CurrentMusicVolume;
        _sensitivity.Value = _screen.CurrentSensitivity;

        _soundValue.Text = $"{_screen.CurrentSoundVolume:0.00}";
        _musicValue.Text = $"{_screen.CurrentMusicVolume:0.00}";
        _sensitivityValue.Text = $"{_screen.CurrentSensitivity:0.00}";
    }

    private static MGCheckBox CreateCheckBox(MGWindow window, string label, Action<bool> onChanged)
    {
        var checkBox = new MGCheckBox(window, false);
        checkBox.SetContent(new MGTextBlock(window, label, MguiUiTheme.PrimaryTextColor, 14));
        checkBox.OnCheckStateChanged += (sender, args) => onChanged(checkBox.IsChecked == true);
        return checkBox;
    }

    private static (MGSlider Slider, MGTextBlock ValueLabel) CreateSliderRow(MGWindow window, MGStackPanel root, string label, float value, Action<float> onChanged)
    {
        root.TryAddChild(MguiUiTheme.CreateBodyText(window, label, MguiUiTheme.AccentColor));

        var row = MguiUiTheme.CreateHorizontalStack(window, 8);
        var slider = new MGSlider(window, 0f, 1f, value)
        {
            PreferredWidth = 180,
        };
        slider.ValueChanged += (sender, args) => onChanged(slider.Value);

        var valueLabel = MguiUiTheme.CreateBodyText(window, value.ToString("0.00"));
        row.TryAddChild(slider);
        row.TryAddChild(valueLabel);
        root.TryAddChild(row);
        return (slider, valueLabel);
    }
}