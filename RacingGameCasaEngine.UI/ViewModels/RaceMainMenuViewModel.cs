namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>Data context of <c>Screen.MainMenu</c>: the menu decoration only, the five entries being fixed XAML.</summary>
public sealed class RaceMainMenuViewModel : RaceViewModelBase
{
    public RaceMenuDecorationViewModel Decoration { get; } = new();
}
