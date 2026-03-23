using CasaEngine.Framework.GameFramework;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;

namespace RacingGameCasaEngine.GameFramework;

public sealed class RacingPlayerController : PlayerController
{
	public string PlayerName { get; private set; } = "Player One";

	public string SelectedCarName { get; private set; } = "Prototype Car";

	public float SteeringSensitivityScale { get; private set; } = 1.0f;

	internal void Configure(RaceFrontEndState state)
	{
		PlayerName = state.PlayerName;
		SelectedCarName = RaceFrontEndCatalog.Cars[state.SelectedCarIndex].Name;
		SteeringSensitivityScale = MathHelper.Lerp(0.45f, 1.65f, Math.Clamp(state.ControllerSensitivity / 100f, 0f, 1f));
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