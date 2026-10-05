namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>
/// Data context of <c>Screen.Highscores</c>: the ten fixed board lines of Highscores.xaml (<c>Entry0..Entry9</c>), as
/// formatted by the screen for the selected level; a line past the level's entries is empty.
/// </summary>
public sealed class RaceHighscoresViewModel : RaceViewModelBase
{
    public const int EntryCount = 10;

    private readonly string[] _entries = Enumerable.Repeat(string.Empty, EntryCount).ToArray();

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public RaceMenuTextButtonViewModel BackButton { get; } = new();

    public string Entry0 { get => _entries[0]; set => SetEntry(0, value); }

    public string Entry1 { get => _entries[1]; set => SetEntry(1, value); }

    public string Entry2 { get => _entries[2]; set => SetEntry(2, value); }

    public string Entry3 { get => _entries[3]; set => SetEntry(3, value); }

    public string Entry4 { get => _entries[4]; set => SetEntry(4, value); }

    public string Entry5 { get => _entries[5]; set => SetEntry(5, value); }

    public string Entry6 { get => _entries[6]; set => SetEntry(6, value); }

    public string Entry7 { get => _entries[7]; set => SetEntry(7, value); }

    public string Entry8 { get => _entries[8]; set => SetEntry(8, value); }

    public string Entry9 { get => _entries[9]; set => SetEntry(9, value); }

    public void SetEntry(int index, string text)
    {
        SetProperty(ref _entries[index], text, "Entry" + index);
    }
}
