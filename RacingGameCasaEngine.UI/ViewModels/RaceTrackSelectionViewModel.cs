namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>Data context of <c>Screen.TrackSelection</c>: the names of the three fixed track slots, from the game's catalogue.</summary>
public sealed class RaceTrackSelectionViewModel : RaceViewModelBase
{
    private string _track0Name = string.Empty;
    private string _track1Name = string.Empty;
    private string _track2Name = string.Empty;

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public string Track0Name
    {
        get => _track0Name;
        set => SetProperty(ref _track0Name, value);
    }

    public string Track1Name
    {
        get => _track1Name;
        set => SetProperty(ref _track1Name, value);
    }

    public string Track2Name
    {
        get => _track2Name;
        set => SetProperty(ref _track2Name, value);
    }
}
