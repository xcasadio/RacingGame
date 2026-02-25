#region File Description
//-----------------------------------------------------------------------------
// Options.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives

using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Properties;
using RacingGame.Sounds;
#endregion

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
        // Current resolution:
        // 0=640x480, 1=800x600, 2=1024x768, 3=1280x1024, 4=auto (default)
        if (BaseGame.Width == 640 && BaseGame.Height == 480)
        {
            currentResolution = 0;
        }

        if (BaseGame.Width == 800 && BaseGame.Height == 600)
        {
            currentResolution = 1;
        }

        if (BaseGame.Width == 1024 && BaseGame.Height == 768)
        {
            currentResolution = 2;
        }

        if (BaseGame.Width == 1280 && BaseGame.Height == 1024)
        {
            currentResolution = 3;
        }

        // Get graphics detail settings
        fullscreen = BaseGame.Fullscreen;
        usePostScreenShaders = BaseGame.UsePostScreenShaders;
        useShadowMapping = BaseGame.AllowShadowMapping;
        useHighDetail = BaseGame.HighDetail;

        // Get music and sound volume
        currentMusicVolume = GameSettings.Default.MusicVolume;
        currentSoundVolume = GameSettings.Default.SoundVolume;

        // Get sensitivity
        currentSensitivity = GameSettings.Default.ControllerSensitivity;
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
            switch (currentResolution)
            {
                case 0:
                    GameSettings.Default.ResolutionWidth = 640;
                    GameSettings.Default.ResolutionHeight = 480;
                    break;
                case 1:
                    GameSettings.Default.ResolutionWidth = 800;
                    GameSettings.Default.ResolutionHeight = 600;
                    break;
                case 2:
                    GameSettings.Default.ResolutionWidth = 1024;
                    GameSettings.Default.ResolutionHeight = 768;
                    break;
                case 3:
                    GameSettings.Default.ResolutionWidth = 1280;
                    GameSettings.Default.ResolutionHeight = 1024;
                    break;
                case 4:
                    GameSettings.Default.ResolutionWidth = 0;
                    GameSettings.Default.ResolutionHeight = 0;
                    break;
            }
            GameSettings.Default.Fullscreen = fullscreen;
            GameSettings.Default.PostScreenEffects = usePostScreenShaders;
            GameSettings.Default.ShadowMapping = useShadowMapping;
            GameSettings.Default.HighDetail = useHighDetail;
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
        #region Background
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();
        BaseGame.UI.Headers.RenderOnScreenRelative1600(
            10, 18, UIRenderer.HeaderOptionsGfxRect);
        BaseGame.UI.OptionsScreen.RenderOnScreenRelative4To3(
            0, 125, BaseGame.UI.OptionsScreen.GfxRectangle);
        #endregion

        #region Display player name
        int xPos = BaseGame.XToRes(352);
        int yPos = BaseGame.YToRes768(125 + 65 - 20);
        TextureFont.WriteText(xPos, yPos,
            currentPlayerName +
            ((int)(BaseGame.TotalTime / 0.35f) % 2 == 0 ? "|" : ""));
        #endregion

        #region Resolution selection highlight
        Color selColor = new Color(255, 156, 0, 160);

        Rectangle res0Rect = BaseGame.CalcRectangleKeep4To3(Resolution640x480GfxRect);
        res0Rect.Y += BaseGame.YToRes768(125);
        if (currentResolution == 0)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                res0Rect, Resolution640x480GfxRect, selColor, BlendState.AlphaBlend);

        Rectangle res1Rect = BaseGame.CalcRectangleKeep4To3(Resolution800x600GfxRect);
        res1Rect.Y += BaseGame.YToRes768(125);
        if (currentResolution == 1)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                res1Rect, Resolution800x600GfxRect, selColor, BlendState.AlphaBlend);

        Rectangle res2Rect = BaseGame.CalcRectangleKeep4To3(Resolution1024x768GfxRect);
        res2Rect.Y += BaseGame.YToRes768(125);
        if (currentResolution == 2)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                res2Rect, Resolution1024x768GfxRect, selColor, BlendState.AlphaBlend);

        Rectangle res3Rect = BaseGame.CalcRectangleKeep4To3(Resolution1280x1024GfxRect);
        res3Rect.Y += BaseGame.YToRes768(125);
        if (currentResolution == 3)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                res3Rect, Resolution1280x1024GfxRect, selColor, BlendState.AlphaBlend);

        Rectangle res4Rect = BaseGame.CalcRectangleKeep4To3(ResolutionAutoGfxRect);
        res4Rect.Y += BaseGame.YToRes768(125);
        if (currentResolution == 4)
            BaseGame.UI.OptionsScreen.RenderOnScreen(
                res4Rect, ResolutionAutoGfxRect, selColor, BlendState.AlphaBlend);
        #endregion

        #region Graphics checkboxes highlight
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
        #endregion

        #region Sound slider
        Rectangle soundRect = BaseGame.CalcRectangleKeep4To3(SoundGfxRect);
        soundRect.Y += BaseGame.YToRes768(125);
        Rectangle gfxRect = UIRenderer.SelectionRadioButtonGfxRect;
        BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                soundRect.X + (int)(soundRect.Width * currentSoundVolume) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                soundRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
            gfxRect);
        #endregion

        #region Music slider
        Rectangle musicRect = BaseGame.CalcRectangleKeep4To3(MusicGfxRect);
        musicRect.Y += BaseGame.YToRes768(125);
        BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                musicRect.X + (int)(musicRect.Width * currentMusicVolume) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                musicRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
            gfxRect);
        #endregion

        #region Sensitivity slider
        Rectangle sensitivityRect = BaseGame.CalcRectangleKeep4To3(SensitivityGfxRect);
        sensitivityRect.Y += BaseGame.YToRes768(125);
        BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                sensitivityRect.X +
                (int)(sensitivityRect.Width * currentSensitivity) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                sensitivityRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
            gfxRect);
        #endregion

        #region Show selected line arrow
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
        #endregion

        #region Bottom buttons
        BaseGame.UI.RenderBottomButtons(true);
        #endregion

        return _isFinished;
    }
    #endregion
}