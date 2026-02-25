namespace RacingGame.GameScreens;

/// <summary>
/// Game screen helper interface for all game screens of our game.
/// Helps us to put them all into one list and manage them in our RaceGame.
/// </summary>
public interface IGameScreen
{
	/// <summary>
	/// Draw this screen. Called each frame after Update.
	/// Must be pure rendering — no input detection or state mutation.
	/// Returns true when the screen should be popped from the stack.
	/// </summary>
	bool Render();

	/// <summary>
	/// Process input and update game state for this screen.
	/// Called every frame before Render. Sets the internal IsFinished state
	/// that Render will return, kicks off screen transitions, etc.
	/// </summary>
	void Update(GameTime gameTime);
}