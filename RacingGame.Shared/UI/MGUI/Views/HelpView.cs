using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class HelpView : IMguiScreenView
{
    private readonly Help _screen;

    public HelpView(Help screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var panel = MguiUiTheme.CreatePanel(Window, 24);
        var root = MguiUiTheme.CreateVerticalStack(Window, 10, 0);

        root.TryAddChild(MguiUiTheme.CreateHeading(Window, "Help"));
        root.TryAddChild(MguiUiTheme.CreateSubheading(Window, "Core controls and flow are summarized here so the legacy help texture is no longer required for this screen."));

        var scrollViewer = new MGScrollViewer(Window)
        {
            PreferredWidth = 760,
            PreferredHeight = 340,
            AllowClickDragScrolling = true,
        };

        var content = MguiUiTheme.CreateVerticalStack(Window, 12, 4);
        foreach (string section in _screen.GetSections())
            content.TryAddChild(MguiUiTheme.CreateBodyText(Window, section));

        scrollViewer.SetContent(content);
        root.TryAddChild(scrollViewer);
        root.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Back", _screen.RequestBack));

        panel.SetContent(root);
        Window.SetContent(panel);
    }

    public MGWindow Window { get; }
    public MGElement InitialFocusElement => Window.Content;
    public bool BlocksGameplayInput => true;

    public void Activate()
    {
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
    }
}