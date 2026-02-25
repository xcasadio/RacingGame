using System.Threading;
using RacingGame.Graphics;
namespace RacingGame.GameScreens;

/// <summary>
/// Loading screen
/// </summary>
class LoadingScreen : IGameScreen
{
	#region Variables
	private const string loadingText = "Loading...";
	private int loadingTextWidth = TextureFont.GetTextWidth(loadingText);
	private string loadingStatus = "";
	private bool _isFinished = false;
	#endregion

	#region Constructor
	public LoadingScreen()
	{
		//Setup the handler before we start the thread
		RacingGameManager.LoadEvent += OnLoadStatusChanged;
	}
	#endregion

	#region Update LoadingScreen
	/// <summary>
	/// Start the loading thread and track loading completion.
	/// </summary>
	public void Update(GameTime gameTime)
	{
		if (RacingGameManager.LoadingThread.ThreadState == ThreadState.Unstarted)
		{
			RacingGameManager.LoadingThread.Start();
		}
		_isFinished = RacingGameManager.ContentLoaded;
	}

	public void OnLoadStatusChanged(string status)
	{
		loadingStatus = status;
	}
	#endregion

	#region RenderLoadingScreen
	/// <summary>
	/// Render loading screen — drawing only.
	/// </summary>
	public bool Render()
	{
		Vector2 position = new Vector2((BaseGame.Width / 2) - 50, (BaseGame.Height / 2) - 20);

		for (int i = 0; i < loadingText.Length; i++)
		{
			string charStr = new string(loadingText[i], 1);
			int charHeight = (int)(position.Y + 7 * Math.Abs(Math.Sin((i / 4f) + (-BaseGame.TotalTime * 3))));
			TextureFont.WriteText((int)position.X, charHeight, charStr, Color.Red);

			position.X += TextureFont.GetTextWidth(charStr);
		}

		TextureFont.WriteTextCentered(BaseGame.Width / 2, (int)position.Y + 40, loadingStatus);

		return _isFinished;
	}
	#endregion
}