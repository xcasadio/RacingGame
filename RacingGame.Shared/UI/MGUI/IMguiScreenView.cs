using MGUI.Core.UI;

namespace RacingGame.UI.MGUI;

internal interface IMguiScreenView
{
    MGWindow Window { get; }

    MGElement InitialFocusElement { get; }

    bool BlocksGameplayInput { get; }

    void Activate();

    void Deactivate();

    void Update(GameTime gameTime);
}