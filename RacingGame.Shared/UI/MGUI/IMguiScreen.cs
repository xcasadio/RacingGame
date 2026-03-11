using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI;

internal interface IMguiScreen
{
    IMguiScreenView GetOrCreateMguiView(MguiUiHost host);
}