using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;

namespace RacingGame.GameScreens;

/// <summary>
/// Help
/// </summary>
/// <returns>IGame screen</returns>
class Help : IGameScreen, IMguiScreen
{
	#region Variables
	private bool _isFinished = false;
	private IMguiScreenView _mguiView;
	#endregion

	#region Update
	/// <summary>
	/// Process input: any dismiss action closes the help screen.
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
		_mguiView ??= new HelpView(this, host);
		return _mguiView;
	}

	internal void RequestBack()
	{
		_isFinished = true;
	}

	internal IReadOnlyList<string> GetSections()
	{
		return new[]
		{
			"Race: Accelerate with Up, W, left mouse, or GamePad A/right trigger. Brake or reverse with Down, S, right mouse, GamePad B, left trigger, or D-pad down.",
			"Steer: Use Left and Right, A and D, mouse X movement, the left stick, or the D-pad. Controller sensitivity from Options affects turning response.",
			"Camera: Change chase distance with Page Up and Page Down, GamePad X and Y, or the mouse wheel.",
			"Menus: Use mouse, arrow keys, Enter, Space, GamePad A, and GamePad B or Escape to confirm or go back.",
			"Session flow: In race, Escape or Back returns to the menu. After game over, Space or controller face buttons dismiss the results overlay."
		};
	}
}