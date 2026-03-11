using MGUI.Core.UI;
using MGUI.Shared.Input;
using MGUI.Shared.Rendering;
using RacingGame.UI.MGUI;

namespace RacingGame.Graphics;

public partial class BaseGame : IObservableUpdate
{
    private static MguiUiHost mguiUi;

    public event EventHandler<TimeSpan> PreviewUpdate;
    public event EventHandler<EventArgs> EndUpdate;

    internal static MguiUiHost MguiUi => mguiUi;

    protected void InitializeMgui()
    {
        mguiUi ??= new MguiUiHost(this, new GameRenderHost<BaseGame>(this), new MonoGameRawInputSource());
    }

    protected void RaisePreviewUpdate(TimeSpan totalElapsed)
    {
        PreviewUpdate?.Invoke(this, totalElapsed);
    }

    protected void RaiseEndUpdate()
    {
        EndUpdate?.Invoke(this, EventArgs.Empty);
    }

    protected void UpdateMgui(GameTime gameTime)
    {
        mguiUi?.Update(gameTime);
    }

    protected void DrawMgui()
    {
        mguiUi?.Draw();
    }

    protected void DisposeMgui()
    {
        mguiUi?.Dispose();
        mguiUi = null;
    }
}