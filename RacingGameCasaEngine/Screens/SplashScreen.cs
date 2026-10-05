using CasaEngine.Framework.Assets;
using CasaEngine.Framework.GUI;
using MGUI.Core.UI;

namespace RacingGameCasaEngine.Screens;

/// <summary>Bootstrap title gate, loaded from the <c>Screen.Splash</c> screen asset (Content/UI/Screens/Splash).</summary>
internal sealed class SplashScreen : RaceXamlScreenBase
{
    private readonly Action _continueToMenu;
    private MGButton? _continueButton;

    public SplashScreen(AssetContentManager assetContentManager, Action continueToMenu)
        : base(assetContentManager, "Screen.Splash")
    {
        _continueToMenu = continueToMenu;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void OnWindowLoaded(MGWindow window)
    {
        _continueButton = FindControl<MGButton>("btnContinue");
        _continueButton.AddCommandHandler((_, _) => _continueToMenu());
    }

    public override void Show()
    {
        _continueButton?.Focus();
    }
}
