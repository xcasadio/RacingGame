using MGUI.Core.UI;

namespace RacingGame.UI.MGUI;

internal sealed class EmptyMguiScreenView : IMguiScreenView
{
    public EmptyMguiScreenView(MGWindow window)
    {
        Window = window;
    }

    public MGWindow Window { get; }

    public void Activate()
    {
        if (Window.Content is MGElement element && element.IsFocusable)
        {
            element.Focus();
        }
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
    }
}