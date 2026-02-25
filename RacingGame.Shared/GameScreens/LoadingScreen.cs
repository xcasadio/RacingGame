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

		int    fontH    = TextureFont.Height;
		float  centerX  = BaseGame.Width / 2f;
		// Anchor the whole block around the vertical centre of the screen.
		float  baseY    = (BaseGame.Height / 2f) - fontH * 1.5f;
		float  sineArg  = -BaseGame.TotalTime * 3f;
		// Row gap = font height + a small margin, fully resolution-independent.
		int    rowGap   = fontH + fontH / 2;

		// Row 1 – "Loading..." animated wave (each character independent).
		WriteBouncingText(loadingText, centerX, (int)baseY, Color.Red, sineArg);

		// Row 2 – Status label ("Models...", "Textures...", …) with the same wave.
		WriteBouncingText(loadingStatus, centerX, (int)(baseY + rowGap), Color.White, sineArg + 1f);

		// Row 3 – Progress bar, positioned below row 2 and shifted by the same bounce
		// so the whole block moves as one unit.
		float bounce = 7f * Math.Abs((float)Math.Sin(sineArg));
		RenderProgressBar((int)(baseY + rowGap * 2 + bounce), sineArg);

		return _isFinished;
	}

	/// <summary>
	/// Writes <paramref name="text"/> character-by-character, applying a sine-wave
	/// vertical offset to each character so the text bounces like "Loading...".
	/// The text is horizontally centred around <paramref name="centerX"/>.
	/// </summary>
	private static void WriteBouncingText(string text, float centerX, int baseY, Color color, float sineArg)
	{
		if (string.IsNullOrEmpty(text)) return;
		int totalWidth = TextureFont.GetTextWidth(text);
		float posX = centerX - totalWidth / 2f;
		for (int i = 0; i < text.Length; i++)
		{
			string ch = new string(text[i], 1);
			int charY = (int)(baseY + 7 * Math.Abs(Math.Sin((i / 4f) + sineArg)));
			TextureFont.WriteText((int)posX, charY, ch, color);
			posX += TextureFont.GetTextWidth(ch);
		}
	}

	/// <summary>
	/// Draws a progress bar centred horizontally at the given <paramref name="yTop"/> coordinate.
	/// The percentage label below the bar uses the same wave as the rest of the screen.
	/// </summary>
	private void RenderProgressBar(int yTop, float sineArg)
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

		// Percentage label below the bar — same wave effect as the rest.
		string pctText = $"{(int)(progress * 100f)}%";
		WriteBouncingText(pctText, BaseGame.Width / 2f,
			yTop + barHeight + TextureFont.Height / 2 + 4, Color.White, sineArg + 2f);
	}
	#endregion
}