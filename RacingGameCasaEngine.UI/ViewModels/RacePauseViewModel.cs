namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>Data context of <c>Screen.Pause</c>: the race summary line (track, lap, total time), refreshed by the screen.</summary>
public sealed class RacePauseViewModel : RaceViewModelBase
{
    private string _summary = string.Empty;

    public string Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }
}
