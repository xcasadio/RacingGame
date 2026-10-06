using MGUI.Core.UI;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// One track card of the track selection: the card sprite at its animated rectangle, drawn in full when selected and at
/// RacingGame's 192/255 tint otherwise (a premultiplied tint, so the opacity of the whole sprite), and, for the selected
/// card only, the orange outline over it and the track's name sprite under it.
/// </summary>
public sealed class RaceTrackCardViewModel : RaceViewModelBase
{
    // RacingGame drew an unselected card with Color(192, 192, 192, 192) on a premultiplied texture.
    private const float UnselectedOpacity = 192f / 255f;

    private bool _isSelected;

    public RaceScreenRectViewModel Rect { get; } = new();

    /// <summary>The name sprite's rectangle, under the card and as wide as it.</summary>
    public RaceScreenRectViewModel Label { get; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                NotifyPropertyChanged(nameof(Opacity));
                NotifyPropertyChanged(nameof(SelectedVisibility));
            }
        }
    }

    public float Opacity => _isSelected ? 1f : UnselectedOpacity;

    /// <summary>Visible for the selected card only: its outline and its name.</summary>
    public Visibility SelectedVisibility => _isSelected ? Visibility.Visible : Visibility.Collapsed;
}
