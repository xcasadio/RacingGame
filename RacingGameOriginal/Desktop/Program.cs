using System;

namespace RacingGame
{
	static class Program
	{
		[STAThread]
		static void Main (string [] args)
		{
			// --capture-track-audit: render the reference views of every track and quit
			RacingGame.Helpers.CaptureOptions.Parse (args);

			using (var game = new RacingGameManager ()) {
				game.Run ();
			}
		}
	}
}
