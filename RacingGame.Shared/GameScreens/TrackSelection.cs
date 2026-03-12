using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Sounds;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;

namespace RacingGame.GameScreens;

/// <summary>
/// Track selection screen
/// </summary>
/// <returns>IGame screen</returns>
class TrackSelection : IGameScreen, IMguiScreen
{
    #region Constants
    const int NumberOfButtons = 3,
        ActiveButtonWidth = 132,
        InactiveButtonWidth = 108,
        DistanceBetweenButtons = 32;
    #endregion

    #region Update
    private bool _isFinished = false;
    private IMguiScreenView _mguiView;

    /// <summary>
    /// Process input: mouse, keyboard/gamepad navigation, track selection.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (Input.KeyboardEscapeJustPressed ||
            Input.GamePadBJustPressed ||
            Input.GamePadBackJustPressed)
        {
            _isFinished = true;
        }
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
    public bool Render()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();

        return _isFinished;
    }

    public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
    {
        _mguiView ??= new TrackSelectionView(this, host);
        return _mguiView;
    }

    internal void SelectTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= NumberOfButtons)
            return;

        if (selectedButton != trackIndex)
            Sound.Play(Sound.Sounds.ButtonClick);

        selectedButton = trackIndex;
    }

    internal void ConfirmSelection()
    {
        RacingGameManager.AddGameScreen(new GameScreen());
    }

    internal void RequestBack()
    {
        _isFinished = true;
    }
    #endregion
}