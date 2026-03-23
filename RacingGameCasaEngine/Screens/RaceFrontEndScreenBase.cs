using CasaEngine.Framework.GUI;
using MGUI.Core.UI;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.UI;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace RacingGameCasaEngine.Screens;

internal abstract class RaceFrontEndScreenBase : UIScreenBase
{
    private readonly Texture2D? _backgroundTexture;
    private readonly Texture2D? _buttonsTexture;
    private readonly List<MGWindow> _windows = [];
    private UIRoot? _root;
    private MGImage? _menuLogoImage;

    protected RaceFrontEndScreenBase(Texture2D? backgroundTexture, Texture2D? buttonsTexture = null)
    {
        _backgroundTexture = backgroundTexture;
        _buttonsTexture = buttonsTexture;
    }

    protected sealed override void OnInitialize(UIRoot root)
    {
        _root = root;

        if (_backgroundTexture != null)
        {
            if (_buttonsTexture != null)
            {
                _windows.Add(LegacyMenuUiTheme.CreateMenuBackgroundWindow(root, _backgroundTexture));
                MGWindow logoWindow = LegacyMenuUiTheme.CreateLogoWindow(root, _backgroundTexture, out MGImage logoImage);
                _menuLogoImage = logoImage;
                _windows.Add(logoWindow);
                UpdateMenuDecoration(0.0);
            }
            else
            {
                _windows.Add(RaceUiTheme.CreateBackgroundWindow(root, _backgroundTexture));
            }
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

    protected void UpdateMenuDecoration(double totalSeconds)
    {
        if (_root == null || _menuLogoImage == null)
        {
            return;
        }

        float bounceSize = 1.005f + (float)Math.Sin(totalSeconds / 0.46f) * 0.045f * (float)Math.Cos(totalSeconds / 0.285f);
        Rectangle targetRect = LegacyMenuUiTheme.CalculateLegacyMenuBounceRectangle(_root.Metrics.ViewportSize, 362, 36, 601, 218, bounceSize);
        _menuLogoImage.PreferredWidth = targetRect.Width;
        _menuLogoImage.PreferredHeight = targetRect.Height;
        _menuLogoImage.Margin = new MonoGame.Extended.Thickness(targetRect.X, targetRect.Y, 0, 0);
    }

    public override IEnumerable<MGWindow> GetWindows()
    {
        return _windows;
    }
}