using MGUI.Core.UI;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class TrackSelectionView : IMguiScreenView
{
    private readonly TrackSelection _screen;
    private readonly MGButton[] _trackButtons;
    private readonly MGTextBlock[] _trackLabels;

    public TrackSelectionView(TrackSelection screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var panel = MguiUiTheme.CreatePanel(Window, 28);
        var stack = MguiUiTheme.CreateVerticalStack(Window, 10, 0);
        stack.HorizontalAlignment = HorizontalAlignment.Center;
        stack.VerticalAlignment = VerticalAlignment.Center;

        stack.TryAddChild(MguiUiTheme.CreateHeading(Window, "Select Track"));
        stack.TryAddChild(MguiUiTheme.CreateSubheading(Window, "Choose a circuit, then start the race."));

        _trackButtons = new MGButton[3];
        _trackLabels = new MGTextBlock[3];
        string[] names = ["Beginner", "Advanced", "Expert"];

        for (int i = 0; i < names.Length; i++)
        {
            int trackIndex = i;
            _trackButtons[i] = MguiUiTheme.CreateSecondaryButton(Window, names[i], () => _screen.SelectTrack(trackIndex));
            _trackLabels[i] = (MGTextBlock)_trackButtons[i].Content;
            stack.TryAddChild(_trackButtons[i]);
        }

        stack.TryAddChild(MguiUiTheme.CreatePrimaryButton(Window, "Start Race", _screen.ConfirmSelection));
        stack.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Back", _screen.RequestBack));

        panel.SetContent(stack);
        Window.SetContent(panel);
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => _trackButtons[TrackSelection.SelectedTrackNumber];

    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        UpdateButtonLabels();
        InitialFocusElement.Focus();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        UpdateButtonLabels();
    }

    private void UpdateButtonLabels()
    {
        string[] names = ["Beginner", "Advanced", "Expert"];
        for (int i = 0; i < names.Length; i++)
        {
            bool selected = TrackSelection.SelectedTrackNumber == i;
            _trackLabels[i].Text = selected ? $"> {names[i]} <" : names[i];
            _trackLabels[i].DefaultTextForeground = selected
                ? new VisualStateSetting<Color?>(MguiUiTheme.AccentColor, MguiUiTheme.AccentColor, MguiUiTheme.AccentColor)
                : new VisualStateSetting<Color?>(MguiUiTheme.PrimaryTextColor, MguiUiTheme.PrimaryTextColor, MguiUiTheme.PrimaryTextColor);
        }
    }
}