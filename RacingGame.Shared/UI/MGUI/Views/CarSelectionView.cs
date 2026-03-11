using MGUI.Core.UI;
using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Brushes.Fill_Brushes;
using MGUI.Core.UI.Containers;
using RacingGame.GameScreens;

namespace RacingGame.UI.MGUI.Views;

internal sealed class CarSelectionView : IMguiScreenView
{
    private readonly CarSelection _screen;
    private readonly MGTextBlock _title;
    private readonly List<MGTextBlock> _stats;
    private readonly List<MGButton> _colorButtons;
    private readonly MGButton _previousButton;

    public CarSelectionView(CarSelection screen, MguiUiHost host)
    {
        _screen = screen;
        Window = MguiUiTheme.CreateRootWindow(host);

        var panel = MguiUiTheme.CreatePanel(Window, 24);
        var stack = MguiUiTheme.CreateVerticalStack(Window, 10, 0);
        stack.HorizontalAlignment = HorizontalAlignment.Right;
        stack.VerticalAlignment = VerticalAlignment.Center;

        _title = MguiUiTheme.CreateHeading(Window, screen.GetSelectedCarTitle());
        stack.TryAddChild(_title);
        stack.TryAddChild(MguiUiTheme.CreateSubheading(Window, "Preview rendering stays legacy; car, color, and validation controls are now MGUI."));

        var nav = MguiUiTheme.CreateHorizontalStack(Window, 10);
        _previousButton = MguiUiTheme.CreateSecondaryButton(Window, "Previous", screen.MoveToPreviousCar);
        nav.TryAddChild(_previousButton);
        nav.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Next", screen.MoveToNextCar));
        stack.TryAddChild(nav);

        stack.TryAddChild(MguiUiTheme.CreateBodyText(Window, "Colors", MguiUiTheme.AccentColor));

        var colors = MguiUiTheme.CreateHorizontalStack(Window, 8);
        _colorButtons = new();
        for (int i = 0; i < screen.AvailableColors.Count; i++)
        {
            int colorIndex = i;
            var button = new MGButton(Window, _ => screen.SelectCarColor(colorIndex))
            {
                BackgroundBrush = new VisualStateFillBrush(screen.AvailableColors[i].AsFillBrush()),
                BorderThickness = new(1),
                BorderBrush = new MGUniformBorderBrush(Color.White * 0.6f),
                PreferredWidth = 28,
                PreferredHeight = 28,
                Padding = new(0),
            };
            button.SetContent(new MGTextBlock(Window, "", Color.White, 10));
            _colorButtons.Add(button);
            colors.TryAddChild(button);
        }
        stack.TryAddChild(colors);

        _stats = new();
        foreach (string stat in screen.GetCurrentCarStats())
        {
            var text = MguiUiTheme.CreateBodyText(Window, stat);
            _stats.Add(text);
            stack.TryAddChild(text);
        }

        stack.TryAddChild(MguiUiTheme.CreatePrimaryButton(Window, "Continue", screen.ConfirmSelection));
        stack.TryAddChild(MguiUiTheme.CreateSecondaryButton(Window, "Back", screen.RequestBack));

        panel.SetContent(stack);
        Window.SetContent(panel);
    }

    public MGWindow Window { get; }

    public MGElement InitialFocusElement => _previousButton;

    public bool BlocksGameplayInput => true;

    public void Activate()
    {
        Refresh();
        InitialFocusElement.Focus();
    }

    public void Deactivate()
    {
    }

    public void Update(GameTime gameTime)
    {
        Refresh();
    }

    private void Refresh()
    {
        _title.Text = _screen.GetSelectedCarTitle();

        var stats = _screen.GetCurrentCarStats();
        for (int i = 0; i < _stats.Count && i < stats.Count; i++)
        {
            _stats[i].Text = stats[i];
        }

        for (int i = 0; i < _colorButtons.Count; i++)
        {
            _colorButtons[i].BorderThickness = _screen.CurrentCarColor == i ? new(3) : new(1);
        }
    }
}