using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Size of a menu text button scaled with the UI (the "Back" buttons of the menu screens), computed once when the screen
/// is built, as the code-built screens did. The initial values are those of a UI scale of 1.
/// </summary>
public sealed class RaceMenuTextButtonViewModel : RaceViewModelBase
{
    private const int DefaultMinWidth = 150;

    private Thickness _padding = new(20, 10, 20, 11);
    private int _minWidth = DefaultMinWidth;
    private int _minHeight = 48;
    private int _height = 48;

    public Thickness Padding
    {
        get => _padding;
        set => SetProperty(ref _padding, value);
    }

    public int MinWidth
    {
        get => _minWidth;
        set => SetProperty(ref _minWidth, value);
    }

    public int MinHeight
    {
        get => _minHeight;
        set => SetProperty(ref _minHeight, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public void Update(float uiScale, int minWidth = DefaultMinWidth)
    {
        float scale = Math.Clamp(uiScale, 1.0f, 1.75f);
        int verticalPadding = (int)MathF.Round(10f * scale);
        int horizontalPadding = (int)MathF.Round(20f * scale);
        int minHeight = (int)MathF.Round(48f * scale);

        Padding = new Thickness(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding + 1);
        MinWidth = Math.Max(minWidth, (int)MathF.Round(minWidth * scale));
        MinHeight = minHeight;
        Height = minHeight;
    }
}
