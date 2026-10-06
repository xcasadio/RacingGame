namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// One property row of the car selection (RacingGame's <c>CarSelection.ShowCarPropertyBar</c>): a label, and under it a
/// bar cut from OptionsScreenWindows.png whose width shows the value.
/// </summary>
public sealed class RaceCarStatViewModel : RaceViewModelBase
{
    private string _label = string.Empty;

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    /// <summary>The label's top-left corner (the text block's).</summary>
    public RaceScreenRectViewModel Text { get; } = new();

    public RaceScreenRectViewModel Bar { get; } = new();
}
