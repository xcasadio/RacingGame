using RacingGame.GameLogic;
using RacingGame.Graphics;

namespace RacingGame.GameScreens;

/// <summary>
/// Help
/// </summary>
/// <returns>IGame screen</returns>
class Help : IGameScreen
{
	#region Variables
	private bool _isFinished = false;
	#endregion

	#region Update
	/// <summary>
	/// Process input: any dismiss action closes the help screen.
	/// </summary>
	public void Update(GameTime gameTime)
	{
		BaseGame.UI.UpdateBottomButtons(true);
		_isFinished =
			Input.KeyboardEscapeJustPressed ||
			Input.GamePadBJustPressed ||
			Input.GamePadBackJustPressed ||
			Input.MouseLeftButtonJustPressed ||
			BaseGame.UI.backButtonPressed;
	}
	#endregion

	#region Render
	/// <summary>
	/// Render game screen — drawing only.
	/// </summary>
	public bool Render()
	{
		// This starts both menu and in game post screen shader!
		if (BaseGame.UsePostScreenShaders)
		{
			BaseGame.UI.PostScreenMenuShader.Start();
		}

		// Render background and black bar
		BaseGame.UI.RenderMenuBackground();

		// Help header
		BaseGame.UI.Headers.RenderOnScreenRelative1600(
			10, 18, UIRenderer.HeaderHelpGfxRect);

		BaseGame.UI.HelpScreen.RenderOnScreenRelative4To3(
			0, 125, BaseGame.UI.HelpScreen.GfxRectangle);

		BaseGame.UI.RenderBottomButtons(true);

		return _isFinished;
	}
	#endregion
}