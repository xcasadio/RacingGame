using MGUI.Core.UI;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class MainMenuView : IMguiScreenView
{
    private readonly MGButton _playButton;

    public MainMenuView(MainMenu screen, MguiUiHost host)
    {
        Window = MguiUiTheme.CreateRootWindow(host);

        var panel = MguiUiTheme.CreatePanel(Window, 28);
        var stack = MguiUiTheme.CreateVerticalStack(Window, 12, 0);
        stack.HorizontalAlignment = HorizontalAlignment.Center;
        stack.VerticalAlignment = VerticalAlignment.Center;

        stack.TryAddChild(MguiUiTheme.CreateHeading(Window, "Racing Game"));
        stack.TryAddChild(MguiUiTheme.CreateSubheading(Window, "Main menu controls are now backed by MGUI while the scene rendering remains unchanged."));

        _playButton = MguiUiTheme.CreatePrimaryButton(Window, "Play", screen.StartGame);
        stack.TryAddChild(_playButton);
        stack.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Highscores", screen.OpenHighscores));
        stack.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Options", screen.OpenOptions));
        stack.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Help", screen.OpenHelp));
        stack.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Quit", screen.RequestExit));

        panel.SetContent(stack);
        Window.SetContent(panel);
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => _playButton;

    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        InitialFocusElement.Focus();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
    }
}