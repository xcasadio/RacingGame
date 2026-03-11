using RacingGame.Graphics;
using RacingGame.GameLogic;
using RacingGame.Shaders;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;
namespace RacingGame.GameScreens;

/// <summary>
/// Splash screen
/// </summary>
class SplashScreen : IGameScreen, IMguiScreen
{
	#region Variables
	private bool _isFinished = false;
	private IMguiScreenView _mguiView;
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

		return _isFinished;
	}
	#endregion

	public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
	{
		_mguiView ??= new SplashScreenView(this, host);
		return _mguiView;
	}

	internal bool ShouldShowPrompt => (int)(BaseGame.TotalTime / 0.375f) % 3 != 0;
}