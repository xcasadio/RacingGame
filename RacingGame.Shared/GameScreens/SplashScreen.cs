using RacingGame.Graphics;
using RacingGame.GameLogic;
using RacingGame.Shaders;
namespace RacingGame.GameScreens;

/// <summary>
/// Splash screen
/// </summary>
class SplashScreen : IGameScreen
{
	#region Variables
	private bool _isFinished = false;
	#endregion

	#region Update
	/// <summary>
	/// Process input: any button or click advances past the splash screen.
	/// </summary>
	public void Update(GameTime gameTime)
	{
		_isFinished =
			Input.MouseLeftButtonJustPressed ||
			Input.KeyboardSpaceJustPressed ||
			Input.KeyboardEscapeJustPressed ||
			Input.GamePadStartPressed;
	}
	#endregion

	#region RenderSplashScreen
	/// <summary>
	/// Render splash screen — drawing only.
	/// </summary>
	public bool Render()
	{
		BaseGame.UI.UpdateCarInMenu();

		ShadowMapShader.PrepareGameShadows();

		// Render background and black bar
		BaseGame.UI.RenderGameBackground();
		BaseGame.UI.RenderMenuTrackBackground();
		BaseGame.UI.RenderBlackBar(518, 61);

		// Show shadows we calculated above
		if (BaseGame.AllowShadowMapping)
		{
			ShaderEffect.shadowMapping.ShowShadows();
		}

		// Show Press Start to continue.
		if ((int)(BaseGame.TotalTime / 0.375f) % 3 != 0)
		{
			BaseGame.UI.Headers.RenderOnScreen(
				BaseGame.CalcRectangleCenteredWithGivenHeight(
					512, 518 + 61 / 2, 26, UIRenderer.PressStartGfxRect),
				UIRenderer.PressStartGfxRect);
		}

		return _isFinished;
	}
	#endregion
}