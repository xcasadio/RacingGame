using CasaEngine.Framework.GameFramework;
using RacingGameCasaEngine.Bootstrap;

namespace RacingGameCasaEngine.GameFramework;

public sealed class RacingPlayerController : PlayerController
{
	public string PlayerName { get; private set; } = "Player One";

	public string SelectedCarName { get; private set; } = "Prototype Car";

	internal void Configure(RaceFrontEndState state)
	{
		PlayerName = state.PlayerName;
		SelectedCarName = RaceFrontEndCatalog.Cars[state.SelectedCarIndex].Name;
		IsInputEnable = true;
	}

	public override void ShowPauseMenu()
	{
		IsInputEnable = false;
	}

	public override void HidePauseMenu()
	{
		IsInputEnable = true;
	}
}