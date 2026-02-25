using System.Threading;
using RacingGame.Graphics;
using XnaTexture = RacingGame.Graphics.Texture;
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

	/// <summary>1×1 white texture used to draw the progress bar rectangles.</summary>
	private static Texture2D _pixelTex;
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
		// Lazily create the white pixel used for bar rendering.
		if (_pixelTex == null)
		{
			_pixelTex = new Texture2D(BaseGame.Device, 1, 1);
			_pixelTex.SetData(new[] { Color.White });
		}

		// Vertical anchor: everything is laid out relative to this point.
		float baseY    = (BaseGame.Height / 2f) - 50f;
		float sineArg  = -BaseGame.TotalTime * 3f;

		// The bar bounces in sync with the first letter of "Loading...".
		float barBounce = 7f * Math.Abs((float)Math.Sin(sineArg));

		// Layout (all Y values relative to baseY):
		//   [0]        Animated "Loading..." letters
		//   [+52]      Status text (centred)
		//   [+90+bounce] Progress bar top
		int statusY = (int)(baseY + 52);
		int barYTop  = (int)(baseY + 90 + barBounce);

		// Draw the progress bar FIRST so it is submitted to the sprite batch
		// before the text characters — text is rendered in a later pass and
		// therefore appears on top of the bar.
		RenderProgressBar(barYTop);

		// Animated "Loading..." letters — each character bounces independently.
		float posX = (BaseGame.Width / 2f) - 50f;
		for (int i = 0; i < loadingText.Length; i++)
		{
			string charStr = new string(loadingText[i], 1);
			int charY = (int)(baseY + 7 * Math.Abs(Math.Sin((i / 4f) + sineArg)));
			TextureFont.WriteText((int)posX, charY, charStr, Color.Red);
			posX += TextureFont.GetTextWidth(charStr);
		}

		// Status label (e.g. "Models...", "Textures...").
		TextureFont.WriteTextCentered(BaseGame.Width / 2, statusY, loadingStatus);

		return _isFinished;
	}

	/// <summary>
	/// Draws a progress bar centred horizontally at the given <paramref name="yTop"/> coordinate.
	/// </summary>
	private void RenderProgressBar(int yTop)
	{
		const int barWidth  = 300;
		const int barHeight = 14;
		const int borderPx  = 2;

		int barX = BaseGame.Width / 2 - barWidth / 2;

		// Outer border (dark grey)
		XnaTexture.alphaSprite.Draw(_pixelTex,
			new Rectangle(barX - borderPx, yTop - borderPx,
				barWidth + borderPx * 2, barHeight + borderPx * 2),
			new Color(60, 60, 60, 200));

		// Background (black)
		XnaTexture.alphaSprite.Draw(_pixelTex,
			new Rectangle(barX, yTop, barWidth, barHeight),
			new Color(0, 0, 0, 180));

		// Filled portion
		float progress = Math.Clamp(RacingGameManager.LoadProgress, 0f, 1f);
		int fillWidth = (int)(barWidth * progress);
		if (fillWidth > 0)
		{
			XnaTexture.alphaSprite.Draw(_pixelTex,
				new Rectangle(barX, yTop, fillWidth, barHeight),
				new Color(220, 80, 20, 230));
		}

		// Percentage text
		int pct = (int)(progress * 100f);
		string pctText = $"{pct}%";
		TextureFont.WriteTextCentered(BaseGame.Width / 2, yTop + barHeight + TextureFont.Height / 2 + 4, pctText);
	}
	#endregion
}