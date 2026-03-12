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
    private readonly MGButton _backButton;

    public OptionsView(Options screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var band = MguiUiTheme.CreateMenuBand(Window, 118, 500, MguiUiTheme.ScaleThickness(32, 20, 32, 20));
        var root = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(12), 0);
        root.HorizontalAlignment = HorizontalAlignment.Center;
        root.VerticalAlignment = VerticalAlignment.Center;

        root.TryAddChild(MguiUiTheme.CreateHeading(Window, "Options"));

        var scrollViewer = new MGScrollViewer(Window)
        {
            PreferredWidth = MguiUiTheme.ScaleX(1120),
            PreferredHeight = MguiUiTheme.ScaleY(360),
            AllowClickDragScrolling = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var content = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(10), 0);
        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Top;

        var playerRow = CreateFormRow(Window, "Player Name");
        _playerName = new MGTextBox(Window, 24, false, false)
        {
            PreferredWidth = MguiUiTheme.ScaleX(280),
        };
        _playerName.Text = screen.PlayerName;
        playerRow.TryAddChild(_playerName);
        content.TryAddChild(playerRow);

        var resolutionRow = CreateFormRow(Window, "Resolution");
        var resolutions = MguiUiTheme.CreateHorizontalStack(Window, MguiUiTheme.ScaleX(8));
        _resolutionButtons = new MGButton[5];
        for (int i = 0; i < _resolutionButtons.Length; i++)
        {
            int resIndex = i;
            _resolutionButtons[i] = MguiUiTheme.CreateBandButton(Window, screen.GetResolutionLabel(i), () => screen.SelectResolution(resIndex));
            resolutions.TryAddChild(_resolutionButtons[i]);
        }
        resolutionRow.TryAddChild(resolutions);
        content.TryAddChild(resolutionRow);

        _fullscreen = CreateToggleRow(Window, content, "Fullscreen", value => screen.SetFullscreen(value));
        _postFx = CreateToggleRow(Window, content, "Post Screen Effects", value => screen.SetPostScreenShaders(value));
        _shadows = CreateToggleRow(Window, content, "Shadows", value => screen.SetShadowMapping(value));
        _highDetail = CreateToggleRow(Window, content, "High Detail", value => screen.SetHighDetail(value));
        _showFps = CreateToggleRow(Window, content, "Show FPS", value => screen.SetShowFps(value));
        _vibration = CreateToggleRow(Window, content, "Gamepad Vibration", value => screen.SetGamepadVibration(value));

        (_sound, _soundValue) = CreateSliderRow(Window, content, "Sound Volume", screen.CurrentSoundVolume, screen.SetSoundVolume);
        (_music, _musicValue) = CreateSliderRow(Window, content, "Music Volume", screen.CurrentMusicVolume, screen.SetMusicVolume);
        (_sensitivity, _sensitivityValue) = CreateSliderRow(Window, content, "Controller Sensitivity", screen.CurrentSensitivity, screen.SetSensitivity);

        scrollViewer.SetContent(content);
        root.TryAddChild(scrollViewer);

        _backButton = MguiUiTheme.CreateMenuTextButton(Window, "Back", screen.ApplyAndClose, 150);
        root.TryAddChild(_backButton);

        band.SetContent(root);
        Window.SetContent(band);
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
            bool isActive = _screen.CurrentResolution == i || _resolutionButtons[i].VisualState.IsFocused || _resolutionButtons[i].IsHovered;
            MguiUiTheme.ApplyBandButtonState(_resolutionButtons[i], isActive);
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
        MguiUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
    }

    private static MGStackPanel CreateFormRow(MGWindow window, string label)
    {
        var row = MguiUiTheme.CreateHorizontalStack(window, MguiUiTheme.ScaleX(14));
        row.HorizontalAlignment = HorizontalAlignment.Center;
        row.VerticalAlignment = VerticalAlignment.Center;

        var title = MguiUiTheme.CreateBodyText(window, label, MguiUiTheme.AccentColor);
        title.PreferredWidth = MguiUiTheme.ScaleX(220);
        row.TryAddChild(title);
        return row;
    }

    private static MGCheckBox CreateToggleRow(MGWindow window, MGStackPanel root, string label, Action<bool> onChanged)
    {
        var row = CreateFormRow(window, label);
        var checkBox = new MGCheckBox(window, false);
        checkBox.OnCheckStateChanged += (sender, args) => onChanged(checkBox.IsChecked == true);
        row.TryAddChild(checkBox);
        root.TryAddChild(row);
        return checkBox;
    }

    private static (MGSlider Slider, MGTextBlock ValueLabel) CreateSliderRow(MGWindow window, MGStackPanel root, string label, float value, Action<float> onChanged)
    {
        var row = CreateFormRow(window, label);
        var slider = new MGSlider(window, 0f, 1f, value)
        {
            PreferredWidth = MguiUiTheme.ScaleX(240),
        };
        slider.ValueChanged += (sender, args) => onChanged(slider.Value);

        var valueLabel = MguiUiTheme.CreateBodyText(window, value.ToString("0.00"));
        valueLabel.PreferredWidth = MguiUiTheme.ScaleX(56);
        row.TryAddChild(slider);
        row.TryAddChild(valueLabel);
        root.TryAddChild(row);
        return (slider, valueLabel);
    }
}