namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>One help section: a title and its two lines.</summary>
public sealed class RaceHelpSectionViewModel : RaceViewModelBase
{
    private string _title = string.Empty;
    private string _line0 = string.Empty;
    private string _line1 = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Line0
    {
        get => _line0;
        set => SetProperty(ref _line0, value);
    }

    public string Line1
    {
        get => _line1;
        set => SetProperty(ref _line1, value);
    }
}

/// <summary>Data context of <c>Screen.Help</c>: the six fixed section slots of Help.xaml, filled from the game's catalogue.</summary>
public sealed class RaceHelpViewModel : RaceViewModelBase
{
    public RaceHelpViewModel()
    {
        Sections = [Section0, Section1, Section2, Section3, Section4, Section5];
    }

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public RaceMenuTextButtonViewModel BackButton { get; } = new();

    public RaceHelpSectionViewModel Section0 { get; } = new();

    public RaceHelpSectionViewModel Section1 { get; } = new();

    public RaceHelpSectionViewModel Section2 { get; } = new();

    public RaceHelpSectionViewModel Section3 { get; } = new();

    public RaceHelpSectionViewModel Section4 { get; } = new();

    public RaceHelpSectionViewModel Section5 { get; } = new();

    /// <summary>The slots in order, for the screen to fill; Help.xaml binds the named slots.</summary>
    public IReadOnlyList<RaceHelpSectionViewModel> Sections { get; }
}
