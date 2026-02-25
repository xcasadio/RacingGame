using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Properties;
using RacingGame.Sounds;
namespace RacingGame.GameScreens;

/// <summary>
/// Options
/// </summary>
/// <returns>IGame screen</returns>
class Options : IGameScreen
{
    #region Constants
    readonly Rectangle
        Line4ArrowGfxRect = new Rectangle(154, 284, 62, 39),
        Line5ArrowGfxRect = new Rectangle(160, 354, 62, 39),
        Line6ArrowGfxRect = new Rectangle(72, 437, 62, 39),
        Resolution640x480GfxRect = new Rectangle(339, 112, 98, 32),
        Resolution800x600GfxRect = new Rectangle(454, 112, 98, 32),
        Resolution1024x768GfxRect = new Rectangle(575, 112, 108, 32),
        Resolution1280x1024GfxRect = new Rectangle(704, 112, 116, 32),
        ResolutionAutoGfxRect = new Rectangle(838, 112, 69, 32),
        FullscreenGfxRect = new Rectangle(339, 182, 105, 36),
        PostScreenEffectsGfxRect = new Rectangle(339, 226, 206, 36),
        ShadowsGfxRect = new Rectangle(616, 226, 90, 36),
        HighDetailGfxRect = new Rectangle(784, 226, 120, 36),
        // ShowFPS and GamepadVibration checkboxes sit on the row between the
        // graphics checkboxes and the audio sliders (texture-space y ≈ 262).
        ShowFpsGfxRect = new Rectangle(339, 262, 110, 32),
        GamepadVibrationGfxRect = new Rectangle(470, 262, 180, 32),
        SoundGfxRect = new Rectangle(384, 281, 448, 39),
        MusicGfxRect = new Rectangle(384, 354, 448, 39),
        SensitivityGfxRect = new Rectangle(384, 428, 448, 39);
    #endregion

    #region Variables
    /// <summary>
    /// Current player name, copied from the settings file.
    /// </summary>
    string currentPlayerName = GameSettings.Default.PlayerName;
    private bool _isFinished = false;

    /// <summary>
    /// Available resolutions built at construction time from
    /// <see cref="GraphicsAdapter.DefaultAdapter.SupportedDisplayModes"/>.
    /// Always contains exactly 4 entries; index 4 (Auto) is handled separately.
    /// </summary>
    private List<(int Width, int Height)> _availableResolutions = new();

    /// <summary>Local copy of <see cref="GameSettings.ShowFPS"/>, applied on exit.</summary>
    bool showFps = false;

    /// <summary>Local copy of <see cref="GameSettings.GamepadVibration"/>, applied on exit.</summary>
    bool useGamepadVibration = true;
    #endregion

    #region Constructor
    int currentOptionsNumber = 0;
    int currentResolution = 4;
    bool fullscreen = true;
    bool usePostScreenShaders = true;
    bool useShadowMapping = true;
    bool useHighDetail = true;
    float currentMusicVolume = 1.0f;
    float currentSoundVolume = 1.0f;
    float currentSensitivity = 1.0f;
    /// <summary>
    /// Create options
    /// </summary>
    public Options()
    {
        // Build the dynamic resolution list before detecting current resolution.
        BuildResolutionList();

        // Detect which slot matches the current window size; default to "Auto" (index 4).
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

        // Get graphics detail settings
        fullscreen = BaseGame.Fullscreen;
        usePostScreenShaders = BaseGame.UsePostScreenShaders;
        useShadowMapping = BaseGame.AllowShadowMapping;
        useHighDetail = BaseGame.HighDetail;
        showFps = GameSettings.Default.ShowFPS;
        useGamepadVibration = GameSettings.Default.GamepadVibration;

        // Get music and sound volume
        currentMusicVolume = GameSettings.Default.MusicVolume;
        currentSoundVolume = GameSettings.Default.SoundVolume;

        // Get sensitivity
        currentSensitivity = GameSettings.Default.ControllerSensitivity;
    }

    /// <summary>
    /// Populates <see cref="_availableResolutions"/> with up to 4 entries derived
    /// from <see cref="GraphicsAdapter.DefaultAdapter.SupportedDisplayModes"/>.
    /// Preferred modern resolutions are tried first; if fewer than 4 are found the
    /// list is padded with classic fallback entries.
    /// </summary>
    private void BuildResolutionList()
    {
        // Preferred modern resolutions, ordered from smallest to largest.
        var preferred = new List<(int W, int H)>
        {
            (1280, 720),
            (1920, 1080),
            (2560, 1440),
            (3840, 2160),
        };

        // Classic fallbacks used when few modern resolutions are supported.
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
            // If the adapter query fails, fall through to classic fallback below.
        }

        // Pad with classic resolutions until we have exactly 4 slots.
        foreach (var (w, h) in classic)
        {
            if (_availableResolutions.Count >= 4)
                break;
            if (!_availableResolutions.Contains((w, h)))
                _availableResolutions.Add((w, h));
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Process all input: name editing, resolution, graphics options,
    /// sliders, d-pad navigation, exit and settings save.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        Input.HandleKeyboardInput(ref currentPlayerName);

        // Resolution buttons
        Rectangle res0Rect = BaseGame.CalcRectangleKeep4To3(Resolution640x480GfxRect);
        res0Rect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(res0Rect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); currentResolution = 0; }

        Rectangle res1Rect = BaseGame.CalcRectangleKeep4To3(Resolution800x600GfxRect);
        res1Rect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(res1Rect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); currentResolution = 1; }

        Rectangle res2Rect = BaseGame.CalcRectangleKeep4To3(Resolution1024x768GfxRect);
        res2Rect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(res2Rect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); currentResolution = 2; }

        Rectangle res3Rect = BaseGame.CalcRectangleKeep4To3(Resolution1280x1024GfxRect);
        res3Rect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(res3Rect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); currentResolution = 3; }

        Rectangle res4Rect = BaseGame.CalcRectangleKeep4To3(ResolutionAutoGfxRect);
        res4Rect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(res4Rect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); currentResolution = 4; }

        // Graphics checkboxes
        Rectangle fsRect = BaseGame.CalcRectangleKeep4To3(FullscreenGfxRect);
        fsRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(fsRect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); fullscreen = !fullscreen; }

        Rectangle pseRect = BaseGame.CalcRectangleKeep4To3(PostScreenEffectsGfxRect);
        pseRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(pseRect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); usePostScreenShaders = !usePostScreenShaders; }

        Rectangle smRect = BaseGame.CalcRectangleKeep4To3(ShadowsGfxRect);
        smRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(smRect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); useShadowMapping = !useShadowMapping; }

        Rectangle hdRect = BaseGame.CalcRectangleKeep4To3(HighDetailGfxRect);
        hdRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(hdRect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); useHighDetail = !useHighDetail; }

        Rectangle showFpsRect = BaseGame.CalcRectangleKeep4To3(ShowFpsGfxRect);
        showFpsRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(showFpsRect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); showFps = !showFps; }

        Rectangle vibrationRect = BaseGame.CalcRectangleKeep4To3(GamepadVibrationGfxRect);
        vibrationRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(vibrationRect) && Input.MouseLeftButtonJustPressed)
        { Sound.Play(Sound.Sounds.ButtonClick); useGamepadVibration = !useGamepadVibration; }

        // Sound slider
        Rectangle soundRect = BaseGame.CalcRectangleKeep4To3(SoundGfxRect);
        soundRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(soundRect) && Input.MouseLeftButtonJustPressed)
        {
            currentSoundVolume =
                (Input.MousePos.X - soundRect.X) / (float)soundRect.Width;
            Sound.Play(Sound.Sounds.Highlight);
        }
        if (currentOptionsNumber == 0)
        {
            if (Input.GamePadLeftJustPressed || Input.KeyboardLeftJustPressed)
            { currentSoundVolume -= 0.1f; Sound.Play(Sound.Sounds.Highlight); }
            if (Input.GamePadRightJustPressed || Input.KeyboardRightJustPressed)
            { currentSoundVolume += 0.1f; Sound.Play(Sound.Sounds.Highlight); }
            currentSoundVolume = Math.Clamp(currentSoundVolume, 0f, 1f);
        }

        // Music slider
        Rectangle musicRect = BaseGame.CalcRectangleKeep4To3(MusicGfxRect);
        musicRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(musicRect) && Input.MouseLeftButtonJustPressed)
        {
            currentMusicVolume =
                (Input.MousePos.X - musicRect.X) / (float)musicRect.Width;
            Sound.Play(Sound.Sounds.Highlight);
        }
        if (currentOptionsNumber == 1)
        {
            if (Input.GamePadLeftJustPressed || Input.KeyboardLeftJustPressed)
            { currentMusicVolume -= 0.1f; Sound.Play(Sound.Sounds.Highlight); }
            if (Input.GamePadRightJustPressed || Input.KeyboardRightJustPressed)
            { currentMusicVolume += 0.1f; Sound.Play(Sound.Sounds.Highlight); }
            currentMusicVolume = Math.Clamp(currentMusicVolume, 0f, 1f);
        }

        Sound.SetVolumes(currentSoundVolume, currentMusicVolume);

        // Sensitivity slider
        Rectangle sensitivityRect = BaseGame.CalcRectangleKeep4To3(SensitivityGfxRect);
        sensitivityRect.Y += BaseGame.YToRes768(125);
        if (Input.MouseInBox(sensitivityRect) && Input.MouseLeftButtonJustPressed)
        {
            currentSensitivity =
                (Input.MousePos.X - sensitivityRect.X) / (float)sensitivityRect.Width;
            Sound.Play(Sound.Sounds.Highlight);
        }
        if (currentOptionsNumber == 2)
        {
            if (Input.GamePadLeftJustPressed || Input.KeyboardLeftJustPressed)
            { currentSensitivity -= 0.1f; Sound.Play(Sound.Sounds.Highlight); }
            if (Input.GamePadRightJustPressed || Input.KeyboardRightJustPressed)
            { currentSensitivity += 0.1f; Sound.Play(Sound.Sounds.Highlight); }
            currentSensitivity = Math.Clamp(currentSensitivity, 0f, 1f);
        }

        // D-pad up/down selects active slider row
        const int numOptions = 3;
        if (Input.GamePadUpJustPressed || Input.KeyboardUpJustPressed)
        {
            Sound.Play(Sound.Sounds.Highlight);
            currentOptionsNumber =
                (numOptions + currentOptionsNumber - 1) % numOptions;
        }
        else if (Input.GamePadDownJustPressed || Input.KeyboardDownJustPressed)
        {
            Sound.Play(Sound.Sounds.Highlight);
            currentOptionsNumber = (currentOptionsNumber + 1) % numOptions;
        }

        BaseGame.UI.UpdateBottomButtons(true);

        // Exit: apply and save settings
        if (Input.KeyboardEscapeJustPressed ||
            Input.GamePadBJustPressed ||
            Input.GamePadBackJustPressed ||
            BaseGame.UI.backButtonPressed)
        {
            GameSettings.Default.PlayerName = currentPlayerName;
            // Persist width/height from the dynamic resolution list; index 4 = Auto (0×0).
            if (currentResolution >= 0 && currentResolution < _availableResolutions.Count)
            {
                GameSettings.Default.ResolutionWidth  = _availableResolutions[currentResolution].Width;
                GameSettings.Default.ResolutionHeight = _availableResolutions[currentResolution].Height;
            }
            else
            {
                GameSettings.Default.ResolutionWidth  = 0;
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
            _isFinished = true;
        }
    }
    #endregion

    #region Run
    /// <summary>
    /// Render game screen — drawing only.
    /// </summary>
    public bool Render()
    {
        RenderMenuBackground();

        Color selColor = new Color(255, 156, 0, 160);
        RenderResolutionOptions(selColor);
        RenderGraphicsOptions(selColor);
        RenderAudioSliders();
        RenderSelectionArrow();

        BaseGame.UI.RenderBottomButtons(true);

        return _isFinished;
    }

    /// <summary>Draws the menu background, header image, options panel and player name.</summary>
    private void RenderMenuBackground()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();
        BaseGame.UI.Headers.RenderOnScreenRelative1600(
            10, 18, UIRenderer.HeaderOptionsGfxRect);
        BaseGame.UI.OptionsScreen.RenderOnScreenRelative4To3(
            0, 125, BaseGame.UI.OptionsScreen.GfxRectangle);

        int xPos = BaseGame.XToRes(352);
        int yPos = BaseGame.YToRes768(125 + 65 - 20);
        TextureFont.WriteText(xPos, yPos,
            currentPlayerName +
            ((int)(BaseGame.TotalTime / 0.35f) % 2 == 0 ? "|" : ""));
    }

    /// <summary>Draws the selection highlight over the active resolution button and
    /// overlays each button with its dynamic resolution label.</summary>
    private void RenderResolutionOptions(Color selColor)
    {
        // Source rectangles for the five button slots in the texture sheet.
        Rectangle[] slotSrcRects = new Rectangle[]
        {
            Resolution640x480GfxRect,
            Resolution800x600GfxRect,
            Resolution1024x768GfxRect,
            Resolution1280x1024GfxRect,
            ResolutionAutoGfxRect,
        };

        // Labels: slots 0-3 come from the dynamic list; slot 4 is always "Auto".
        string[] labels = new string[5];
        for (int i = 0; i < 4; i++)
            labels[i] = i < _availableResolutions.Count
                ? $"{_availableResolutions[i].Width}x{_availableResolutions[i].Height}"
                : "";
        labels[4] = "Auto";

        int yOffset = BaseGame.YToRes768(125);

        for (int i = 0; i < slotSrcRects.Length; i++)
        {
            Rectangle destRect = BaseGame.CalcRectangleKeep4To3(slotSrcRects[i]);
            destRect.Y += yOffset;

            // Highlight selected slot.
            if (currentResolution == i)
                BaseGame.UI.OptionsScreen.RenderOnScreen(
                    destRect, slotSrcRects[i], selColor, BlendState.AlphaBlend);

            // Overlay dynamic label text, centred vertically within the button.
            if (!string.IsNullOrEmpty(labels[i]))
            {
                int textWidth = TextureFont.GetTextWidth(labels[i]);
                int textX = destRect.X + (destRect.Width - textWidth) / 2;
                int textY = destRect.Y + (destRect.Height - TextureFont.Height) / 2;
                TextureFont.WriteText(textX, textY, labels[i], Color.White);
            }
        }
    }

    /// <summary>Draws selection highlights over the active graphics-option checkboxes.</summary>
    private void RenderGraphicsOptions(Color selColor)
    {
        Rectangle fsRect = BaseGame.CalcRectangleKeep4To3(FullscreenGfxRect);
        fsRect.Y += BaseGame.YToRes768(125);
        if (fullscreen)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                fsRect, FullscreenGfxRect, selColor, BlendState.AlphaBlend);

        Rectangle pseRect = BaseGame.CalcRectangleKeep4To3(PostScreenEffectsGfxRect);
        pseRect.Y += BaseGame.YToRes768(125);
        if (usePostScreenShaders)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                pseRect, PostScreenEffectsGfxRect, selColor, BlendState.AlphaBlend);

        Rectangle smRect = BaseGame.CalcRectangleKeep4To3(ShadowsGfxRect);
        smRect.Y += BaseGame.YToRes768(125);
        if (useShadowMapping)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                smRect, ShadowsGfxRect, selColor, BlendState.AlphaBlend);

        Rectangle hdRect = BaseGame.CalcRectangleKeep4To3(HighDetailGfxRect);
        hdRect.Y += BaseGame.YToRes768(125);
        if (useHighDetail)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                hdRect, HighDetailGfxRect, selColor, BlendState.AlphaBlend);

        // Show FPS toggle (text-based button, no dedicated texture label).
        Rectangle showFpsRect = BaseGame.CalcRectangleKeep4To3(ShowFpsGfxRect);
        showFpsRect.Y += BaseGame.YToRes768(125);
        Color fpsColor = showFps ? selColor : new Color(180, 180, 180, 120);

        BaseGame.UI.Buttons.RenderOnScreen(
            new Rectangle(showFpsRect.X, showFpsRect.Y,
                BaseGame.XToRes(UIRenderer.SelectionRadioButtonGfxRect.Width),
                BaseGame.YToRes768(UIRenderer.SelectionRadioButtonGfxRect.Height)),
            UIRenderer.SelectionRadioButtonGfxRect, fpsColor);

        TextureFont.WriteText(
            showFpsRect.X + BaseGame.XToRes(UIRenderer.SelectionRadioButtonGfxRect.Width + 4),
            showFpsRect.Y + (showFpsRect.Height - TextureFont.Height) / 2,
            "Show FPS", Color.White);

        // Gamepad vibration toggle.
        Rectangle vibrationRect = BaseGame.CalcRectangleKeep4To3(GamepadVibrationGfxRect);
        vibrationRect.Y += BaseGame.YToRes768(125);
        Color vibColor = useGamepadVibration ? selColor : new Color(180, 180, 180, 120);

        BaseGame.UI.Buttons.RenderOnScreen(
            new Rectangle(vibrationRect.X, vibrationRect.Y,
                BaseGame.XToRes(UIRenderer.SelectionRadioButtonGfxRect.Width),
                BaseGame.YToRes768(UIRenderer.SelectionRadioButtonGfxRect.Height)),
            UIRenderer.SelectionRadioButtonGfxRect, vibColor);

        TextureFont.WriteText(
            vibrationRect.X + BaseGame.XToRes(UIRenderer.SelectionRadioButtonGfxRect.Width + 4),
            vibrationRect.Y + (vibrationRect.Height - TextureFont.Height) / 2,
            "Gamepad Vibration", Color.White);
    }

    /// <summary>Draws the sound-volume, music-volume and sensitivity slider knobs.</summary>
    private void RenderAudioSliders()
    {
        Rectangle gfxRect = UIRenderer.SelectionRadioButtonGfxRect;

        Rectangle soundRect = BaseGame.CalcRectangleKeep4To3(SoundGfxRect);
        soundRect.Y += BaseGame.YToRes768(125);
        BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                soundRect.X + (int)(soundRect.Width * currentSoundVolume) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                soundRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
            gfxRect);

        Rectangle musicRect = BaseGame.CalcRectangleKeep4To3(MusicGfxRect);
        musicRect.Y += BaseGame.YToRes768(125);
        BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                musicRect.X + (int)(musicRect.Width * currentMusicVolume) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                musicRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
            gfxRect);

        Rectangle sensitivityRect = BaseGame.CalcRectangleKeep4To3(SensitivityGfxRect);
        sensitivityRect.Y += BaseGame.YToRes768(125);
        BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                sensitivityRect.X +
                (int)(sensitivityRect.Width * currentSensitivity) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                sensitivityRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
            gfxRect);
    }

    /// <summary>Draws the animated selection arrow next to the currently highlighted slider row.</summary>
    private void RenderSelectionArrow()
    {
        Rectangle[] lineArrowGfxRects = new Rectangle[]
        {
            Line4ArrowGfxRect,
            Line5ArrowGfxRect,
            Line6ArrowGfxRect,
        };
        for (int num = 0; num < lineArrowGfxRects.Length; num++)
        {
            Rectangle lineRect = BaseGame.CalcRectangleKeep4To3(
                lineArrowGfxRects[num]);
            lineRect.Y += BaseGame.YToRes768(125);
            lineRect.X -= BaseGame.XToRes(8 + (int)Math.Round(8 *
                Math.Sin(BaseGame.TotalTime / 0.21212f)));
            if (currentOptionsNumber == num)
                BaseGame.UI.Buttons.RenderOnScreen(
                    lineRect, UIRenderer.SelectionArrowGfxRect, Color.White);
        }
    }
    #endregion
}