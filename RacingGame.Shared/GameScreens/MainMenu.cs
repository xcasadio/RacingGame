using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Sounds;
namespace RacingGame.GameScreens;

/// <summary>
/// Main menu
/// </summary>
class MainMenu : IGameScreen
{
    #region Constants
    static readonly Rectangle[] ButtonRects = new Rectangle[]
    {
        UIRenderer.MenuButtonPlayGfxRect,
        UIRenderer.MenuButtonHighscoresGfxRect,
        UIRenderer.MenuButtonOptionsGfxRect,
        UIRenderer.MenuButtonHelpGfxRect,
        UIRenderer.MenuButtonQuitGfxRect,
    };
    static readonly Rectangle[] TextRects = new Rectangle[]
    {
        UIRenderer.MenuTextPlayGfxRect,
        UIRenderer.MenuTextHighscoresGfxRect,
        UIRenderer.MenuTextOptionsGfxRect,
        UIRenderer.MenuTextHelpGfxRect,
        UIRenderer.MenuTextQuitGfxRect,
    };
    const int NumberOfButtons = 5,
        ActiveButtonWidth = 132,
        InactiveButtonWidth = 108,
        DistanceBetweenButtons = 14;

    /// <summary>
    /// The amount of time idle at the menu before returning to the splash screen
    /// </summary>
    const float TimeOutMenu = 60000.0f;
    #endregion

    #region Variables
    /// <summary>
    /// Start with button 0 being selected (play game)
    /// </summary>
    int selectedButton = 0;

    private int SelectedButton
    {
        get
        {
            return selectedButton;
        }

        set
        {
            selectedButton = value;
            idleTime = 0.0f;
        }
    }

    /// <summary>
    /// Current button sizes for scaling up/down smooth effect.
    /// </summary>
    float[] currentButtonSizes =
        new float[NumberOfButtons] { 1, 0, 0, 0, 0 };

    /// <summary>
    /// Ignore the mouse unless it moves;
    /// this is so the mouse does not disrupt game pads and keyboard
    /// </summary>
    bool ignoreMouse = true;

    float idleTime = 0.0f;
    bool musicHasStarted = false;
    private bool _isFinished = false;
    #endregion

    #region Constructor
    public MainMenu()
    {

    }
    #endregion

    #region Update
    float pressedLeftMs = 0;
    float pressedRightMs = 0;

    /// <summary>
    /// Handle music start, button animation, input and idle timeout.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Start menu music once
        if (!musicHasStarted)
        {
            Sound.Play(Sound.Sounds.MenuMusic);
            musicHasStarted = true;
        }

        // If the user manipulated the mouse, stop ignoring the mouse
        if (Input.HasMouseMoved || Input.MouseLeftButtonJustPressed)
            ignoreMouse = false;

        // Compute button rects (same formula as Render)
        Rectangle activeRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0, ActiveButtonWidth, ButtonRects[0]);
        Rectangle inactiveRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0, InactiveButtonWidth, ButtonRects[0]);
        int totalWidth = activeRect.Width +
                         (NumberOfButtons - 1) * inactiveRect.Width +
                         (NumberOfButtons - 1) * BaseGame.XToRes(DistanceBetweenButtons);
        int xPos = BaseGame.XToRes(512) - totalWidth / 2;
        int yPos = BaseGame.YToRes(316);

        int mouseIsOverButton = -1;

        for (int num = 0; num < NumberOfButtons; num++)
        {
            bool selected = num == SelectedButton;

            // Animate button size
            currentButtonSizes[num] +=
                (selected ? 1 : -1) * BaseGame.MoveFactorPerSecond * 2;
            currentButtonSizes[num] = Math.Clamp(currentButtonSizes[num], 0f, 1f);

            Rectangle thisRect =
                InterpolateRect(activeRect, inactiveRect, currentButtonSizes[num]);
            Rectangle renderRect = new Rectangle(
                xPos, yPos - (thisRect.Height - inactiveRect.Height) / 2,
                thisRect.Width, thisRect.Height);

            if (Input.MouseInBox(renderRect))
                mouseIsOverButton = num;

            xPos += thisRect.Width + BaseGame.XToRes(DistanceBetweenButtons);
        }

        if (!ignoreMouse && mouseIsOverButton >= 0)
            SelectedButton = mouseIsOverButton;

        // Hold-repeat accumulation
        if (Input.KeyboardLeftPressed || Input.GamePadLeftPressed)
            pressedLeftMs += BaseGame.ElapsedTimeThisFrameInMilliseconds;
        else
            pressedLeftMs = 0;

        if (Input.KeyboardRightPressed || Input.GamePadRightPressed)
            pressedRightMs += BaseGame.ElapsedTimeThisFrameInMilliseconds;
        else
            pressedRightMs = 0;

        // Navigate left
        if (Input.GamePadLeftJustPressed ||
            Input.KeyboardLeftJustPressed ||
            (pressedLeftMs > 250 &&
             (Input.KeyboardLeftPressed || Input.GamePadLeftPressed)))
        {
            pressedLeftMs -= 250;
            Sound.Play(Sound.Sounds.Highlight);
            SelectedButton =
                (SelectedButton + NumberOfButtons - 1) % NumberOfButtons;
            ignoreMouse = true;
        }
        else if (Input.GamePadRightJustPressed ||
                 Input.KeyboardRightJustPressed ||
                 (pressedRightMs > 250 &&
                  (Input.KeyboardRightPressed || Input.GamePadRightPressed)))
        {
            pressedRightMs -= 250;
            Sound.Play(Sound.Sounds.Highlight);
            SelectedButton = (SelectedButton + 1) % NumberOfButtons;
            ignoreMouse = true;
        }

        // Button click dispatch
        if ((mouseIsOverButton >= 0 && Input.MouseLeftButtonJustPressed) ||
            Input.GamePadAJustPressed ||
            Input.KeyboardSpaceJustPressed)
        {
            idleTime = 0.0f;

            switch (SelectedButton)
            {
                case 0:
                    RacingGameManager.AddGameScreen(new CarSelection());
                    break;
                case 1:
                    RacingGameManager.AddGameScreen(new Highscores());
                    break;
                case 2:
                    RacingGameManager.AddGameScreen(new Options());
                    break;
                case 3:
                    RacingGameManager.AddGameScreen(new Help());
                    break;
                case 4:
                    _isFinished = true;
                    break;
            }
        }

        // Escape / back button
        if (Input.KeyboardEscapeJustPressed || Input.GamePadBackJustPressed)
            _isFinished = true;

        // Idle timeout → return to splash screen
        idleTime += BaseGame.ElapsedTimeThisFrameInMilliseconds;
        if (idleTime > TimeOutMenu)
        {
            idleTime = 0.0f;
            RacingGameManager.AddGameScreen(new SplashScreen());
        }
    }
    #endregion

    #region Render
    /// <summary>
    /// Interpolate rectangle
    /// </summary>
    /// <param name="rect1">Rectangle 1</param>
    /// <param name="rect2">Rectangle 2</param>
    /// <param name="interpolation">Interpolation</param>
    /// <returns>Rectangle</returns>
    internal static Rectangle InterpolateRect(
        Rectangle rect1, Rectangle rect2,
        float interpolation)
    {
        return new Rectangle(
            (int)Math.Round(
                rect1.X * interpolation + rect2.X * (1 - interpolation)),
            (int)Math.Round(
                rect1.Y * interpolation + rect2.Y * (1 - interpolation)),
            (int)Math.Round(
                rect1.Width * interpolation + rect2.Width * (1 - interpolation)),
            (int)Math.Round(
                rect1.Height * interpolation + rect2.Height * (1 - interpolation)));
    }

    /// <summary>
    /// Render — drawing only.
    /// </summary>
    /// <returns>Bool</returns>
    public bool Render()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();
        BaseGame.UI.RenderBlackBar(280, 192);

        // Draw buttons using currentButtonSizes animated in Update
        Rectangle activeRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0, ActiveButtonWidth, ButtonRects[0]);
        Rectangle inactiveRect = BaseGame.CalcRectangleCenteredWithGivenHeight(
            0, 0, InactiveButtonWidth, ButtonRects[0]);
        int totalWidth = activeRect.Width +
                         (NumberOfButtons - 1) * inactiveRect.Width +
                         (NumberOfButtons - 1) * BaseGame.XToRes(DistanceBetweenButtons);
        int xPos = BaseGame.XToRes(512) - totalWidth / 2;
        int yPos = BaseGame.YToRes(316);

        for (int num = 0; num < NumberOfButtons; num++)
        {
            bool selected = num == SelectedButton;

            Rectangle thisRect =
                InterpolateRect(activeRect, inactiveRect, currentButtonSizes[num]);
            Rectangle renderRect = new Rectangle(
                xPos, yPos - (thisRect.Height - inactiveRect.Height) / 2,
                thisRect.Width, thisRect.Height);

            BaseGame.UI.Buttons.RenderOnScreen(renderRect, ButtonRects[num],
                selected ? Color.White : new Color(192, 192, 192, 192));

            if (selected)
                BaseGame.UI.Buttons.RenderOnScreen(renderRect,
                    UIRenderer.MenuButtonSelectionGfxRect);

            Rectangle textRenderRect = new Rectangle(
                xPos, renderRect.Bottom + BaseGame.YToRes(5),
                renderRect.Width,
                renderRect.Height * TextRects[0].Height / ButtonRects[0].Height);
            if (selected)
                BaseGame.UI.Buttons.RenderOnScreen(textRenderRect, TextRects[num],
                    Color.White);

            xPos += thisRect.Width + BaseGame.XToRes(DistanceBetweenButtons);
        }

        return _isFinished;
    }
    #endregion
}