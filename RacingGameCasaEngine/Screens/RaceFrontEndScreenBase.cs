using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.UI;

namespace RacingGameCasaEngine.Screens;

internal abstract class RaceFrontEndScreenBase : UIScreenBase
{
    private readonly Texture2D? _backgroundTexture;
    private readonly Texture2D? _buttonsTexture;
    private readonly List<MGWindow> _windows = [];

    protected RaceFrontEndScreenBase(Texture2D? backgroundTexture, Texture2D? buttonsTexture = null)
    {
        _backgroundTexture = backgroundTexture;
        _buttonsTexture = buttonsTexture;
    }

    protected sealed override void OnInitialize(UIRoot root)
    {
        if (_backgroundTexture != null)
        {
            _windows.Add(RaceUiTheme.CreateBackgroundWindow(root, _backgroundTexture));
        }

        if (_buttonsTexture != null)
        {
            _windows.Add(LegacyMenuUiTheme.CreateLogoWindow(root, _buttonsTexture));
        }

        BuildScreen(root);
    }

    protected abstract void BuildScreen(UIRoot root);

    protected MGWindow CreateForegroundWindow(UIRoot root)
    {
        var window = RaceUiTheme.CreateFullscreenWindow(root);
        _windows.Add(window);
        return window;
    }

    protected Texture2D? ButtonsTexture => _buttonsTexture;

    public override IEnumerable<MGWindow> GetWindows()
    {
        return _windows;
    }
}