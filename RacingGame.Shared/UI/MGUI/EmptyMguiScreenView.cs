using MGUI.Core.UI;

namespace RacingGame.UI.MGUI;

internal sealed class EmptyMguiScreenView : IMguiScreenView
{
    public EmptyMguiScreenView(MGWindow window, bool blocksGameplayInput = false)
    {
        Window = window;
        BlocksGameplayInput = blocksGameplayInput;
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => null;

    public bool BlocksGameplayInput { get; }

    public void Activate()
    {
        if (InitialFocusElement?.IsFocusable == true)
        {
            InitialFocusElement.Focus();
        }
        else if (Window.Content is MGElement element && element.IsFocusable)
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