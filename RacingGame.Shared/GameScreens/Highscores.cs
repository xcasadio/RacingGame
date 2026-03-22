using System.Threading;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Properties;
using RacingGame.Sounds;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;
namespace RacingGame.GameScreens;

/// <summary>
/// Highscores
/// </summary>
/// <returns>IGame screen</returns>
class Highscores : IGameScreen, IMguiScreen
{
    private Point? _mguiViewSize;

    #region Highscore helper class
    /// <summary>
    /// Highscore helper class
    /// </summary>
    private struct HighscoreInLevel
    {
        #region Variables
        /// <summary>
        /// Player name
        /// </summary>
        public string name;
        /// <summary>
        /// Highscore points 
        /// </summary>
        public int timeMilliseconds;
        #endregion

        #region Constructor
        /// <summary>
        /// Create highscore
        /// </summary>
        /// <param name="setName">Set name</param>
        /// <param name="setTimeMs">Set time ms</param>
        public HighscoreInLevel(string setName, int setTimeMs)
        {
            name = setName;
            timeMilliseconds = setTimeMs;
        }
        #endregion

        #region ToString
        /// <summary>
        /// To string
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return name + ":" + timeMilliseconds;
        }
        #endregion
    }

    /// <summary>
    /// Number of highscores displayed in this screen.
    /// </summary>
    private const int NumOfHighscores = 10,
        NumOfHighscoreLevels = 3;

    /// <summary>
    /// List of remembered highscores.
    /// </summary>
    private static HighscoreInLevel[,] highscores = null;

    /// <summary>
    /// Write highscores to string. Used to save to highscores settings.
    /// </summary>
    private static void WriteHighscoresToSettings()
    {
        string saveString = "";
        for (int level = 0; level < NumOfHighscoreLevels; level++)
        {
            for (int num = 0; num < NumOfHighscores; num++)
            {
                saveString += (saveString.Length == 0 ? "" : ",") +
                              highscores[level, num];
            }
        }

        GameSettings.Default.Highscores = saveString;

        ThreadPool.QueueUserWorkItem(new WaitCallback(SaveSettings), null);
    }

    /// <summary>
    /// Callback used for saving the settings from a worker thread
    /// </summary>
    /// <param name="replay">Not used, delegate signature requires it</param>
    private static void SaveSettings(object state)
    {
        GameSettings.Save();
    }

    /// <summary>
    /// Read highscores from settings
    /// </summary>
    /// <returns>True if reading succeeded, false otherwise.</returns>
    private static bool ReadHighscoresFromSettings()
    {
        if (String.IsNullOrEmpty(GameSettings.Default.Highscores))
        {
            return false;
        }

        try
        {
            string highscoreString = GameSettings.Default.Highscores;
            string[] allHighscores = highscoreString.Split(',');
            for (int level = 0; level < NumOfHighscoreLevels; level++)
            {
                for (int num = 0; num < NumOfHighscores &&
                                  level * NumOfHighscores + num < allHighscores.Length; num++)
                {
                    string[] oneHighscore =
                        allHighscores[level * NumOfHighscores + num].
                            Split(new char[] { ':' });
                    highscores[level, num] = new HighscoreInLevel(
                        oneHighscore[0], Convert.ToInt32(oneHighscore[1]));
                }
            }

            return true;
        }
        catch (Exception exc)
        {
            System.Diagnostics.Debug.WriteLine("Failed to parse highscores: " + exc.Message);
            return false;
        }
    }
    #endregion

    #region Static constructor
    /// <summary>
    /// Create Highscores class, will basically try to load highscore list,
    /// if that fails we generate a standard highscore list!
    /// </summary>
    public static void Initialize()
    {
        // Init highscores
        highscores =
            new HighscoreInLevel[NumOfHighscoreLevels, NumOfHighscores];

        if (ReadHighscoresFromSettings() == false)
        {
            // Generate default lists
            for (int level = 0; level < NumOfHighscoreLevels; level++)
            {
                for (int rank = 0; rank < NumOfHighscores; rank++)
                {
                    highscores[level, rank] =
                        new HighscoreInLevel("Player " + (rank + 1).ToString(),
                            (75000 + rank * 5000) * (level + 1));
                }
            }

            WriteHighscoresToSettings();
        }
    }
    #endregion

    #region Get top lap time
    /// <summary>
    /// Get top lap time
    /// </summary>
    /// <param name="level">Level</param>
    /// <returns>Best lap time</returns>
    public static float GetTopLapTime(int level)
    {
        return (float)highscores[level, 0].timeMilliseconds / 1000.0f;
    }
    #endregion

    #region Get top 5 rank lap times
    /// <summary>
    /// Get top 5 rank lap times
    /// </summary>
    /// <param name="level">Current level</param>
    /// <returns>Array of top 5 times</returns>
    public static int[] GetTop5LapTimes(int level)
    {
        return new int[]
        {
            highscores[level, 0].timeMilliseconds,
            highscores[level, 1].timeMilliseconds,
            highscores[level, 2].timeMilliseconds,
            highscores[level, 3].timeMilliseconds,
            highscores[level, 4].timeMilliseconds,
        };
    }
    #endregion

    #region Get rank from current score
    /// <summary>
    /// Get rank from current time.
    /// Used in game to determinate rank while flying around ^^
    /// </summary>
    /// <param name="level">Level</param>
    /// <param name="timeMilisec">Time ms</param>
    /// <returns>Int</returns>
    public static int GetRankFromCurrentTime(int level, int timeMilliseconds)
    {
        // Time must be at least 1 second
        if (timeMilliseconds < 1000)
            // Invalid time, return rank 11 (out of highscore)
        {
            return NumOfHighscores;
        }

        // Just compare with all highscores and return the rank we have reached.
        for (int num = 0; num < NumOfHighscores; num++)
        {
            if (timeMilliseconds <= highscores[level, num].timeMilliseconds)
            {
                return num;
            }
        }

        // No Rank found, use rank 11
        return NumOfHighscores;
    }
    #endregion

    #region Submit highscore after game
    /// <summary>
    /// Submit highscore. Done after each game is over (won or lost).
    /// New highscore will be added to the highscore screen.
    /// In the future: Also send highscores to the online server.
    /// </summary>
    /// <param name="score">Score</param>
    /// <param name="levelName">Level name</param>
    public static void SubmitHighscore(int level, int timeMilliseconds)
    {
        // Search which highscore rank we can replace
        for (int num = 0; num < NumOfHighscores; num++)
        {
            if (timeMilliseconds <= highscores[level, num].timeMilliseconds)
            {
                // Move all highscores up
                for (int moveUpNum = NumOfHighscores - 1; moveUpNum > num;
                     moveUpNum--)
                {
                    highscores[level, moveUpNum] = highscores[level, moveUpNum - 1];
                }

                // Add this highscore into the local highscore table
                highscores[level, num].name = GameSettings.Default.PlayerName;
                highscores[level, num].timeMilliseconds = timeMilliseconds;

                // And save that
                Highscores.WriteHighscoresToSettings();

                break;
            }
        }

        // Else no highscore was reached, we can't replace any rank.
    }
    #endregion

    #region Update
    private bool _isFinished = false;
    private int selectedLevel = 1;
    private IMguiScreenView _mguiView;

    /// <summary>
    /// Process input: level-tab selection, keyboard navigation, exit.
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
    /// Render game screen — drawing only.
    /// </summary>
    /// <returns>Bool</returns>
    public bool Render()
    {
        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();
        return _isFinished;
    }
    #endregion

    public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
    {
        Point viewportSize = new(host.ViewportBounds.Width, host.ViewportBounds.Height);
        if (_mguiView == null || _mguiViewSize != viewportSize)
        {
            _mguiView = new HighscoresView(this, host);
            _mguiViewSize = viewportSize;
        }

        return _mguiView;
    }

    internal int SelectedLevel => selectedLevel;

    internal string GetLevelLabel(int level) => level switch
    {
        0 => "Beginner",
        1 => "Advanced",
        2 => "Expert",
        _ => "Unknown",
    };

    internal void SelectLevel(int level)
    {
        if (level < 0 || level >= NumOfHighscoreLevels || selectedLevel == level)
            return;

        Sound.Play(Sound.Sounds.ButtonClick);
        selectedLevel = level;
    }

    internal void RequestBack()
    {
        _isFinished = true;
    }

    internal IReadOnlyList<(int Rank, string Name, string Time)> GetEntries()
    {
        var result = new List<(int Rank, string Name, string Time)>(NumOfHighscores);
        for (int num = 0; num < NumOfHighscores; num++)
        {
            result.Add((num + 1, highscores[selectedLevel, num].name,
                FormatTime(highscores[selectedLevel, num].timeMilliseconds)));
        }
        return result;
    }

    private static string FormatTime(int timeMilliseconds)
    {
        return
            (timeMilliseconds < 0 ? "-" : "") +
            ((Math.Abs(timeMilliseconds) / 1000) / 60) + ":" +
            ((Math.Abs(timeMilliseconds) / 1000) % 60).ToString("00") + "." +
            ((Math.Abs(timeMilliseconds) / 10) % 100).ToString("00");
    }
}