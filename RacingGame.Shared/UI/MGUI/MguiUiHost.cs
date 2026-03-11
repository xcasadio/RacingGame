using MGUI.Core.UI;
using MGUI.Shared.Input;
using MGUI.Shared.Rendering;
using RacingGame.Graphics;

namespace RacingGame.UI.MGUI;

internal sealed class MguiUiHost : IDisposable
{
    private readonly BaseGame _game;

    public MguiUiHost(BaseGame game, IRenderHost renderHost, IRawInputSource rawInputSource)
    {
        _game = game;
        Renderer = new MainRenderer(renderHost, rawInputSource);
        Desktop = new MGDesktop(Renderer);
        Desktop.LoadDefaultResources();
        ScreenBridge = new MguiScreenBridge(this);
    }

    public MainRenderer Renderer { get; }

    public MGDesktop Desktop { get; }

    public MguiScreenBridge ScreenBridge { get; }

    public bool BlocksGameplayInput => ScreenBridge.ActiveView?.BlocksGameplayInput == true;

    public Rectangle ViewportBounds => new(0, 0, _game.Window.ClientBounds.Width, _game.Window.ClientBounds.Height);

    public MGWindow CreateFullscreenWindow(bool allowsClickThrough)
    {
        var window = new MGWindow(Desktop, 0, 0, ViewportBounds.Width, ViewportBounds.Height)
        {
            IsTitleBarVisible = false,
            IsUserResizable = false,
            BorderThickness = new(0),
            Padding = new(0),
            AllowsClickThrough = allowsClickThrough,
            BackgroundBrush = null,
        };
        window.WindowWidth = ViewportBounds.Width;
        window.WindowHeight = ViewportBounds.Height;
        return window;
    }

    public void ShowView(IMguiScreenView view)
    {
        SyncWindowBounds(view.Window);
        if (!Desktop.Windows.Contains(view.Window))
        {
            Desktop.Windows.Add(view.Window);
        }

        if (view.InitialFocusElement?.IsFocusable == true)
        {
            view.InitialFocusElement.Focus();
        }
    }

    public void HideView(IMguiScreenView view)
    {
        Desktop.Windows.Remove(view.Window);
    }

    public void Update(GameTime gameTime)
    {
        ScreenBridge.Update(gameTime);

        foreach (var window in Desktop.Windows)
        {
            SyncWindowBounds(window);
        }

        Desktop.Update();
    }

    public void Draw()
    {
        if (Desktop.Windows.Count > 0)
        {
            Desktop.Draw();
        }
    }

    private void SyncWindowBounds(MGWindow window)
    {
        window.Left = 0;
        window.Top = 0;
        window.WindowWidth = ViewportBounds.Width;
        window.WindowHeight = ViewportBounds.Height;
    }

    public void Dispose()
    {
        Desktop.Windows.Clear();
    }
}