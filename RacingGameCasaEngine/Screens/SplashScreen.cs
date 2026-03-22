using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.UI;

namespace RacingGameCasaEngine.Screens;

internal sealed class SplashScreen : RaceFrontEndScreenBase
{
    private readonly Action _continueToMenu;
    private MGButton? _continueButton;

    public SplashScreen(Texture2D? backgroundTexture, Action continueToMenu)
        : base(backgroundTexture)
    {
        _continueToMenu = continueToMenu;
    }

    public override UILayer Layer => UILayer.Menu;

    public override bool IsModal => true;

    protected override void BuildScreen(UIRoot root)
    {
        var window = CreateForegroundWindow(root);
        var panel = RaceUiTheme.CreatePanel(window, preferredWidth: 520);
        var content = RaceUiTheme.CreateVerticalStack(window, spacing: 20);

        content.TryAddChild(RaceUiTheme.CreateTitle(window, "RacingGame"));
        content.TryAddChild(RaceUiTheme.CreateBody(window, "CasaEngine bootstrap splash screen. This replaces the old title gate and hands control to the new menu stack."));

        _continueButton = RaceUiTheme.CreatePrimaryButton(window, "Continue", _continueToMenu);
        content.TryAddChild(_continueButton);

        panel.SetContent(content);
        window.SetContent(panel);
    }

    public override void Show()
    {
        _continueButton?.Focus();
    }
}