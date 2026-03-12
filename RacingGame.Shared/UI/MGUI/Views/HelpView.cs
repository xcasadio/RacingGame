using MGUI.Core.UI;
using MGUI.Core.UI.Containers;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class HelpView : IMguiScreenView
{
    private readonly Help _screen;
    private readonly MGButton _backButton;

    public HelpView(Help screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var band = MguiUiTheme.CreateMenuBand(Window, 120, 480, MguiUiTheme.ScaleThickness(32, 22, 32, 18));
        band.UseResponsiveLayout = true;
        var root = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(10), 0);
        root.HorizontalAlignment = HorizontalAlignment.Center;
        root.VerticalAlignment = VerticalAlignment.Center;

        root.TryAddChild(MguiUiTheme.CreateHeading(Window, "Help"));

        var scrollViewer = new MGScrollViewer(Window)
        {
            PreferredWidth = MguiUiTheme.ScaleX(900),
            PreferredHeight = MguiUiTheme.ScaleY(290),
            AllowClickDragScrolling = false,
        };

        var content = MguiUiTheme.CreateVerticalStack(Window, MguiUiTheme.ScaleY(12), MguiUiTheme.ScaleY(4));
        foreach (string section in _screen.GetSections())
            content.TryAddChild(MguiUiTheme.CreateBodyText(Window, section));

        scrollViewer.SetContent(content);
        root.TryAddChild(scrollViewer);
        _backButton = MguiUiTheme.CreateMenuTextButton(Window, "Back", _screen.RequestBack, 150);
        root.TryAddChild(_backButton);

        band.SetContent(root);
        Window.SetContent(band);
    }

    public MGWindow Window { get; }
    public MGElement InitialFocusElement => _backButton;
    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        _backButton.Focus();
        MguiUiTheme.ApplyMenuTextButtonState(_backButton, true);
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        MguiUiTheme.ApplyMenuTextButtonState(_backButton, _backButton.VisualState.IsFocused || _backButton.IsHovered);
    }
}