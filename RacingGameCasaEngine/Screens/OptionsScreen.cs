using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.UI;
using Microsoft.Xna.Framework.Graphics;

namespace RacingGameCasaEngine.Screens;

internal sealed class OptionsScreen : RaceFrontEndScreenBase
{
    private readonly RaceFrontEndState _state;
    private readonly Action _back;
    private MGTextBox? _playerName;
    private MGButton[] _resolutionButtons = [];
    private MGCheckBox? _fullscreen;
    private MGCheckBox? _postFx;
    private MGCheckBox? _shadows;
    private MGCheckBox? _highDetail;
    private MGCheckBox? _showFps;
    private MGCheckBox? _vibration;
    private MGSlider? _sound;
    private MGSlider? _music;
    private MGSlider? _sensitivity;
    private MGTextBlock? _soundValue;
    private MGTextBlock? _musicValue;
    private MGTextBlock? _sensitivityValue;
    private MGButton? _backButton;

    private static readonly string[] ResolutionLabels = ["1280x720", "1920x1080", "2560x1440", "3840x2160", "Auto"];

    public OptionsScreen(Texture2D? backgroundTexture, Texture2D? buttonsTexture, RaceFrontEndState state, Action back)
        : base(backgroundTexture, buttonsTexture)
    {
        _state = state;
        _back = back;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        var window = CreateForegroundWindow(root);
        var band = LegacyMenuUiTheme.CreateMenuBand(window, 118, 500, new MonoGame.Extended.Thickness(32, 20, 32, 20));
        var layout = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 12);
        layout.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        layout.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;
        layout.TryAddChild(LegacyMenuUiTheme.CreateHeading(window, "Options"));

        var scrollViewer = new MGScrollViewer(window)
        {
            PreferredWidth = 1120,
            PreferredHeight = 360,
            AllowClickDragScrolling = false,
            HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center,
            VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center,
        };

        var content = LegacyMenuUiTheme.CreateVerticalStack(window, spacing: 10);
        content.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        content.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Top;

        var playerRow = CreateFormRow(window, "Player Name");
        _playerName = new MGTextBox(window, 24, false, false)
        {
            PreferredWidth = 280,
            Text = _state.PlayerName,
        };
        playerRow.TryAddChild(_playerName);
        content.TryAddChild(playerRow);

        var resolutionRow = CreateFormRow(window, "Resolution");
        var resolutionButtonsPanel = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 8);
        _resolutionButtons = new MGButton[ResolutionLabels.Length];
        for (int i = 0; i < ResolutionLabels.Length; i++)
        {
            int capturedIndex = i;
            _resolutionButtons[i] = LegacyMenuUiTheme.CreateBandButton(window, ResolutionLabels[i], () => _state.SelectedResolutionIndex = capturedIndex);
            resolutionButtonsPanel.TryAddChild(_resolutionButtons[i]);
        }
        resolutionRow.TryAddChild(resolutionButtonsPanel);
        content.TryAddChild(resolutionRow);

        _fullscreen = CreateToggleRow(window, content, "Fullscreen", value => _state.IsFullscreen = value);
        _postFx = CreateToggleRow(window, content, "Post Screen Effects", value => _state.EnablePostEffects = value);
        _shadows = CreateToggleRow(window, content, "Shadows", value => _state.EnableShadows = value);
        _highDetail = CreateToggleRow(window, content, "High Detail", value => _state.EnableHighDetail = value);
        _showFps = CreateToggleRow(window, content, "Show FPS", value => _state.ShowFps = value);
        _vibration = CreateToggleRow(window, content, "Gamepad Vibration", value => _state.EnableVibration = value);
        (_sound, _soundValue) = CreateSliderRow(window, content, "Sound Volume", _state.SoundVolume, value => _state.SoundVolume = (int)Math.Round(value));
        (_music, _musicValue) = CreateSliderRow(window, content, "Music Volume", _state.MusicVolume, value => _state.MusicVolume = (int)Math.Round(value));
        (_sensitivity, _sensitivityValue) = CreateSliderRow(window, content, "Controller Sensitivity", _state.ControllerSensitivity, value => _state.ControllerSensitivity = (int)Math.Round(value));

        scrollViewer.SetContent(content);
        layout.TryAddChild(scrollViewer);

        _backButton = LegacyMenuUiTheme.CreateMenuTextButton(window, "Back", ApplyAndClose);
        layout.TryAddChild(_backButton);

        band.SetContent(layout);
        window.SetContent(band);
        Refresh();
    }

    public override void Show()
    {
        _playerName?.Focus();
    }

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        UpdateMenuDecoration(gameTime.TotalGameTime.TotalSeconds);

        if (_playerName != null && _state.PlayerName != _playerName.Text)
        {
            _state.PlayerName = _playerName.Text;
        }

        Refresh();
    }

    private void ApplyAndClose()
    {
        _state.SoundVolume = Math.Clamp(_state.SoundVolume, 0, 100);
        _state.MusicVolume = Math.Clamp(_state.MusicVolume, 0, 100);
        _state.ControllerSensitivity = Math.Clamp(_state.ControllerSensitivity, 0, 100);
        _back();
    }

    private void Refresh()
    {
        for (int i = 0; i < _resolutionButtons.Length; i++)
        {
            bool isActive = _state.SelectedResolutionIndex == i || _resolutionButtons[i].VisualState.IsFocused || _resolutionButtons[i].IsHovered;
            LegacyMenuUiTheme.ApplyBandButtonState(_resolutionButtons[i], isActive);
        }

        if (_fullscreen != null) _fullscreen.IsChecked = _state.IsFullscreen;
        if (_postFx != null) _postFx.IsChecked = _state.EnablePostEffects;
        if (_shadows != null) _shadows.IsChecked = _state.EnableShadows;
        if (_highDetail != null) _highDetail.IsChecked = _state.EnableHighDetail;
        if (_showFps != null) _showFps.IsChecked = _state.ShowFps;
        if (_vibration != null) _vibration.IsChecked = _state.EnableVibration;
        if (_sound != null) _sound.Value = _state.SoundVolume;
        if (_music != null) _music.Value = _state.MusicVolume;
        if (_sensitivity != null) _sensitivity.Value = _state.ControllerSensitivity;
        if (_soundValue != null) _soundValue.Text = $"{_state.SoundVolume:0}";
        if (_musicValue != null) _musicValue.Text = $"{_state.MusicVolume:0}";
        if (_sensitivityValue != null) _sensitivityValue.Text = $"{_state.ControllerSensitivity:0}";
        if (_backButton != null) LegacyMenuUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
    }

    private static MGStackPanel CreateFormRow(MGWindow window, string label)
    {
        var row = LegacyMenuUiTheme.CreateHorizontalStack(window, spacing: 14);
        row.HorizontalAlignment = MGUI.Core.UI.HorizontalAlignment.Center;
        row.VerticalAlignment = MGUI.Core.UI.VerticalAlignment.Center;

        var title = LegacyMenuUiTheme.CreateBodyText(window, label, LegacyMenuUiTheme.AccentColor);
        title.PreferredWidth = 220;
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
        var slider = new MGSlider(window, 0f, 100f, value)
        {
            PreferredWidth = 240,
        };
        slider.ValueChanged += (sender, args) => onChanged(slider.Value);
        var valueLabel = LegacyMenuUiTheme.CreateBodyText(window, value.ToString("0"));
        valueLabel.PreferredWidth = 56;
        row.TryAddChild(slider);
        row.TryAddChild(valueLabel);
        root.TryAddChild(row);
        return (slider, valueLabel);
    }
}