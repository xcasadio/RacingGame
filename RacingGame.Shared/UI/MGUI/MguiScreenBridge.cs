using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI;

internal sealed class MguiScreenBridge
{
    private readonly MguiUiHost _host;
    private IGameScreen _activeScreen;
    private IMguiScreenView _activeView;

    public MguiScreenBridge(MguiUiHost host)
    {
        _host = host;
    }

    public IMguiScreenView ActiveView => _activeView;

    public void SyncTopScreen(IGameScreen screen)
    {
        if (ReferenceEquals(_activeScreen, screen))
        {
            return;
        }

        if (_activeView != null)
        {
            _host.HideView(_activeView);
            _activeView.Deactivate();
        }

        _activeScreen = screen;
        _activeView = (screen as IMguiScreen)?.GetOrCreateMguiView(_host);

        if (_activeView != null)
        {
            _host.ShowView(_activeView);
            _activeView.Activate();
        }
    }

    public void Update(GameTime gameTime)
    {
        _activeView?.Update(gameTime);
    }
}