#region File Description
//-----------------------------------------------------------------------------
// TrackSelection.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives

using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Sounds;

#endregion

namespace RacingGame.GameScreens;

/// <summary>
/// Track selection screen
/// </summary>
/// <returns>IGame screen</returns>
class TrackSelection : IGameScreen
{
    #region Constants
    static readonly Rectangle[] ButtonRects = new Rectangle[]
    {
        UIRenderer.TrackButtonBeginnerGfxRect,
        UIRenderer.TrackButtonAdvancedGfxRect,
        UIRenderer.TrackButtonExpertGfxRect,
    };
    static readonly Rectangle[] TextRects = new Rectangle[]
    {
        UIRenderer.TrackTextBeginnerGfxRect,
        UIRenderer.TrackTextAdvancedGfxRect,
        UIRenderer.TrackTextExpertGfxRect,
    };
    const int NumberOfButtons = 3,
        ActiveButtonWidth = 132,
        InactiveButtonWidth = 108,
        DistanceBetweenButtons = 32;
    #endregion

    #region Update
    private bool _isFinished = false;

    /// <summary>
    /// Process input: mouse, keyboard/gamepad navigation, track selection.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // If the user moved the mouse, stop ignoring it
        if (Input.HasMouseMoved || Input.MouseLeftButtonJustPressed)
            ignoreMouse = false;

        // Compute button rects (same formula as Render)
        Rectangle activeRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0,
            ActiveButtonWidth * ButtonRects[0].Height / ButtonRects[0].Width,
            ButtonRects[0]);
        Rectangle inactiveRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0,
            InactiveButtonWidth * ButtonRects[0].Height / ButtonRects[0].Width,
            ButtonRects[0]);
        int totalWidth = activeRect.Width +
                         2 * inactiveRect.Width +
                         2 * BaseGame.XToRes(DistanceBetweenButtons);
        int xPos = BaseGame.XToRes(512) - totalWidth / 2;
        int yPos = BaseGame.YToRes(258);

        int mouseIsOverButton = -1;

        for (int num = 0; num < NumberOfButtons; num++)
        {
            bool selected = num == selectedButton;

            // Animate button size
            currentButtonSizes[num] +=
                (selected ? 1 : -1) * BaseGame.MoveFactorPerSecond * 2;
            currentButtonSizes[num] = Math.Clamp(currentButtonSizes[num], 0f, 1f);

            Rectangle thisRect = MainMenu.InterpolateRect(
                activeRect, inactiveRect, currentButtonSizes[num]);
            Rectangle renderRect = new Rectangle(
                xPos, yPos - (thisRect.Height - inactiveRect.Height) / 2,
                thisRect.Width, thisRect.Height);

            if (Input.MouseInBox(renderRect))
                mouseIsOverButton = num;

            xPos += thisRect.Width + BaseGame.XToRes(DistanceBetweenButtons);
        }

        if (!ignoreMouse && mouseIsOverButton >= 0)
            selectedButton = mouseIsOverButton;

        // Keyboard / gamepad navigation
        if (Input.GamePadLeftJustPressed || Input.KeyboardLeftJustPressed)
        {
            Sound.Play(Sound.Sounds.ButtonClick);
            selectedButton = (selectedButton + NumberOfButtons - 1) % NumberOfButtons;
            ignoreMouse = true;
        }
        else if (Input.GamePadRightJustPressed || Input.KeyboardRightJustPressed)
        {
            Sound.Play(Sound.Sounds.ButtonClick);
            selectedButton = (selectedButton + 1) % NumberOfButtons;
            ignoreMouse = true;
        }

        bool aButtonPressed = BaseGame.UI.UpdateBottomButtons(false);

        // Start the game when confirmed
        if ((mouseIsOverButton >= 0 && Input.MouseLeftButtonJustPressed) ||
            aButtonPressed ||
            Input.GamePadAJustPressed ||
            Input.KeyboardSpaceJustPressed)
        {
            RacingGameManager.AddGameScreen(new GameScreen());
        }

        _isFinished =
            Input.KeyboardEscapeJustPressed ||
            Input.GamePadBJustPressed ||
            Input.GamePadBackJustPressed ||
            BaseGame.UI.backButtonPressed;
    }
    #endregion

    #region Render
    /// <summary>
    /// Start with button 0 being selected (beginner track)
    /// Update: Now use advanced track as default, looks better in replays.
    /// </summary>
    static int selectedButton = 1;

    /// <summary>
    /// Selected track number
    /// </summary>
    /// <returns>Int</returns>
    static public int SelectedTrackNumber
    {
        get
        {
            return selectedButton;
        }
    }

    /// <summary>
    /// Selected track
    /// </summary>
    /// <returns>Track level</returns>
    static public RacingGameManager.Level SelectedTrack
    {
        get
        {
            return (RacingGameManager.Level)selectedButton;
        }
    }

    /// <summary>
    /// Current button sizes for scaling up/down smooth effect.
    /// </summary>
    float[] currentButtonSizes =
        new float[NumberOfButtons] { 1, 0, 0 };

    /// <summary>
    /// Ignore the mouse unless it moves;
    /// this is so the mouse does not disrupt game pads and keyboard
    /// </summary>
    bool ignoreMouse = true;

    /// <summary>
    /// Render game screen — drawing only.
    /// </summary>
    public bool Render()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();
        BaseGame.UI.RenderBlackBar(220, 280);

        BaseGame.UI.Headers.RenderOnScreenRelative1600(
            10, 18, UIRenderer.HeaderSelectTrackGfxRect);

        // Recompute button rects using updated currentButtonSizes (set in Update)
        Rectangle activeRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0,
            ActiveButtonWidth * ButtonRects[0].Height / ButtonRects[0].Width,
            ButtonRects[0]);
        Rectangle inactiveRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0,
            InactiveButtonWidth * ButtonRects[0].Height / ButtonRects[0].Width,
            ButtonRects[0]);
        int totalWidth = activeRect.Width +
                         2 * inactiveRect.Width +
                         2 * BaseGame.XToRes(DistanceBetweenButtons);
        int xPos = BaseGame.XToRes(512) - totalWidth / 2;
        int yPos = BaseGame.YToRes(258);

        for (int num = 0; num < NumberOfButtons; num++)
        {
            bool selected = num == selectedButton;

            Rectangle thisRect = MainMenu.InterpolateRect(
                activeRect, inactiveRect, currentButtonSizes[num]);
            Rectangle renderRect = new Rectangle(
                xPos, yPos - (thisRect.Height - inactiveRect.Height) / 2,
                thisRect.Width, thisRect.Height);

            BaseGame.UI.Buttons.RenderOnScreen(renderRect, ButtonRects[num],
                selected ? Color.White : new Color(192, 192, 192, 192));

            if (selected)
                BaseGame.UI.Buttons.RenderOnScreen(renderRect,
                    UIRenderer.TrackButtonSelectionGfxRect);

            Rectangle textRenderRect = new Rectangle(
                xPos, renderRect.Bottom + BaseGame.YToRes(5),
                renderRect.Width,
                renderRect.Height * TextRects[0].Height / ButtonRects[0].Height);
            if (selected)
            {
                BaseGame.UI.Buttons.RenderOnScreen(textRenderRect, TextRects[num],
                    Color.White);
            }

            xPos += thisRect.Width + BaseGame.XToRes(DistanceBetweenButtons);
        }

        BaseGame.UI.RenderBottomButtons(false);

        return _isFinished;
    }
    #endregion
}