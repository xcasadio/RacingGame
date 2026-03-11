using System.Threading;
using RacingGame.Graphics;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;
namespace RacingGame.GameScreens;

/// <summary>
/// Loading screen
/// </summary>
class LoadingScreen : IGameScreen, IMguiScreen
{
	#region Variables
	private const string loadingText = "Loading...";
	private string loadingStatus = "";
	private bool _isFinished = false;
	private IMguiScreenView _mguiView;
	private bool _isSubscribed = true;
	#endregion

	#region Constructor
	public LoadingScreen()
	{
		//Setup the handler before we start the thread
		RacingGameManager.LoadEvent += OnLoadStatusChanged;
	}

	public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
	{
		_mguiView ??= new LoadingScreenView(this, host);
		return _mguiView;
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
		if (_isFinished && _isSubscribed)
		{
			RacingGameManager.LoadEvent -= OnLoadStatusChanged;
			_isSubscribed = false;
		}
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
		return _isFinished;
	}
	#endregion

	internal string LoadingTitle => loadingText;
	internal string LoadingStatus => string.IsNullOrWhiteSpace(loadingStatus) ? "Bootstrapping..." : loadingStatus;
	internal float LoadProgress => Math.Clamp(RacingGameManager.LoadProgress, 0f, 1f);
}