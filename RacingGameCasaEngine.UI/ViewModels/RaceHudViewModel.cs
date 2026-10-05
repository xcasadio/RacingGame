using MGUI.Core.UI;
using MGUI.Core.UI.Responsive;
using MonoGame.Extended;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>One digit slot of a HUD number: the <c>Ui.Hud.Digit*</c> sprite and its place in the panel.</summary>
public sealed class RaceHudDigitViewModel : RaceViewModelBase
{
    // A valid sprite name even while collapsed: an image with an empty name still asks the catalogue for it.
    private string _sourceName = "Ui.Hud.Digit0";
    private Visibility _visibility = Visibility.Collapsed;
    private Thickness _margin;
    private int _width;
    private int _height;

    public string SourceName
    {
        get => _sourceName;
        set => SetProperty(ref _sourceName, value);
    }

    public Visibility Visibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }

    /// <summary>The digit's top-left corner in its panel, as the margin of a top-left aligned image.</summary>
    public Thickness Margin
    {
        get => _margin;
        set => SetProperty(ref _margin, value);
    }

    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }
}

/// <summary>
/// A number drawn with the HUD digit sprites in a box of its panel. Setting <see cref="Value"/> lays the digits out as the
/// code-built HUD did (<c>DrawBigNumber</c>): digits scaled to the box height, shrunk to its width when too wide,
/// aligned horizontally, centred vertically. Slots past the number's digits are collapsed.
/// </summary>
public sealed class RaceHudNumberViewModel : RaceViewModelBase
{
    // Width of each digit sprite (Ui.Hud.Digit0..9, scripts/generate_rgce_ui_assets.py); every digit is 133 high.
    private static readonly int[] DigitWidths = [80, 80, 80, 78, 80, 80, 80, 80, 80, 80];
    private const int DigitHeight = 133;

    private readonly int _boxX;
    private readonly int _boxY;
    private readonly int _boxWidth;
    private readonly int _boxHeight;
    private readonly float _horizontalAlignment;
    private readonly RaceHudDigitViewModel[] _digits;
    private int? _value;

    public RaceHudNumberViewModel(int boxX, int boxY, int boxWidth, int boxHeight, float horizontalAlignment, int slotCount)
    {
        _boxX = boxX;
        _boxY = boxY;
        _boxWidth = boxWidth;
        _boxHeight = boxHeight;
        _horizontalAlignment = horizontalAlignment;
        _digits = Enumerable.Range(0, slotCount).Select(_ => new RaceHudDigitViewModel()).ToArray();
    }

    public RaceHudDigitViewModel Digit0 => _digits[0];

    public RaceHudDigitViewModel? Digit1 => _digits.Length > 1 ? _digits[1] : null;

    public RaceHudDigitViewModel? Digit2 => _digits.Length > 2 ? _digits[2] : null;

    /// <summary>The number shown (negative shows 0); a number with more digits than slots shows its last digits.</summary>
    public int Value
    {
        get => _value ?? 0;
        set
        {
            if (_value != value)
            {
                _value = value;
                LayOutDigits(value);
                NotifyPropertyChanged();
            }
        }
    }

    private void LayOutDigits(int number)
    {
        string text = Math.Max(0, number).ToString();
        if (text.Length > _digits.Length)
        {
            text = text[^_digits.Length..];
        }

        float scale = _boxHeight / (float)DigitHeight;
        int totalWidth = text.Sum(character => (int)Math.Round(DigitWidths[character - '0'] * scale));
        if (totalWidth > _boxWidth && totalWidth > 0)
        {
            scale *= _boxWidth / (float)totalWidth;
            totalWidth = text.Sum(character => (int)Math.Round(DigitWidths[character - '0'] * scale));
        }

        int x = _boxX + Math.Max(0, (int)Math.Round((_boxWidth - totalWidth) * _horizontalAlignment));
        for (int i = 0; i < _digits.Length; i++)
        {
            RaceHudDigitViewModel digit = _digits[i];
            if (i >= text.Length)
            {
                digit.Visibility = Visibility.Collapsed;
                continue;
            }

            int value = text[i] - '0';
            int width = (int)Math.Round(DigitWidths[value] * scale);
            int height = (int)Math.Round(DigitHeight * scale);
            digit.SourceName = $"Ui.Hud.Digit{value}";
            digit.Margin = new Thickness(x, _boxY + Math.Max(0, (_boxHeight - height) / 2), 0, 0);
            digit.Width = width;
            digit.Height = height;
            digit.Visibility = Visibility.Visible;
            x += width;
        }
    }
}

/// <summary>
/// Data context of <c>Screen.RaceHud</c>, refreshed by the screen every frame from the race session. The digit boxes are
/// those of the code-built HUD, doubled like the panels of RaceHud.xaml (the ingame.png boxes at 1.0 in the laps panel,
/// at 1.15 in the tachometer), at a responsive scale of 1: lap in the laps panel, speed and gear in the tachometer.
/// </summary>
public sealed class RaceHudViewModel : RaceViewModelBase
{
    public const int GameOverLineCount = 5;

    // Panel sizes of RaceHud.xaml at a responsive scale of 1, and their ingame.png sprite sizes.
    private const int TimesPanelHeight = 128;
    private const int TimesSpriteHeight = 128;
    private const int TopTimesPanelWidth = 282;
    private const int TopTimesSpriteWidth = 282;

    // A finished race shows at least this many line slots, the count of the code-built HUD, so short races keep its layout.
    private const int MinimumVisibleGameOverLines = 4;

    private readonly string[] _topTimes = Enumerable.Repeat(string.Empty, 5).ToArray();
    private readonly string[] _gameOverLines = Enumerable.Repeat(string.Empty, GameOverLineCount).ToArray();
    private readonly Visibility[] _gameOverLineVisibilities = Enumerable.Repeat(Visibility.Visible, GameOverLineCount).ToArray();
    private string _currentLapTime = string.Empty;
    private string _bestLapTime = string.Empty;
    private string _trackName = string.Empty;
    private float _needleRotation;
    private Visibility _gameOverVisibility = Visibility.Collapsed;
    private string _gameOverTitle = string.Empty;
    private string _exitHint = string.Empty;
    private int _timeFontSize = 38;
    private int _trackFontSize = 26;
    private int _rowFontSize = 30;

    public RaceHudNumberViewModel Lap { get; } = new(15, 12, 80, 133, 0f, 1);

    public RaceHudNumberViewModel Speed { get; } = new(211, 294, 170, 83, 0.5f, 3);

    public RaceHudNumberViewModel Gear { get; } = new(329, 171, 60, 83, 0.5f, 1);

    public string CurrentLapTime
    {
        get => _currentLapTime;
        set => SetProperty(ref _currentLapTime, value);
    }

    public string BestLapTime
    {
        get => _bestLapTime;
        set => SetProperty(ref _bestLapTime, value);
    }

    public string TrackName
    {
        get => _trackName;
        set => SetProperty(ref _trackName, value);
    }

    public string Top0Time { get => _topTimes[0]; set => SetTopTime(0, value); }

    public string Top1Time { get => _topTimes[1]; set => SetTopTime(1, value); }

    public string Top2Time { get => _topTimes[2]; set => SetTopTime(2, value); }

    public string Top3Time { get => _topTimes[3]; set => SetTopTime(3, value); }

    public string Top4Time { get => _topTimes[4]; set => SetTopTime(4, value); }

    /// <summary>Tachometer needle angle in degrees, applied as the needle's render transform rotation.</summary>
    public float NeedleRotation
    {
        get => _needleRotation;
        set => SetProperty(ref _needleRotation, value);
    }

    public Visibility GameOverVisibility
    {
        get => _gameOverVisibility;
        set => SetProperty(ref _gameOverVisibility, value);
    }

    public string GameOverTitle
    {
        get => _gameOverTitle;
        set => SetProperty(ref _gameOverTitle, value);
    }

    public string GameOverLine0 { get => _gameOverLines[0]; set => SetGameOverLineText(0, value); }

    public string GameOverLine1 { get => _gameOverLines[1]; set => SetGameOverLineText(1, value); }

    public string GameOverLine2 { get => _gameOverLines[2]; set => SetGameOverLineText(2, value); }

    public string GameOverLine3 { get => _gameOverLines[3]; set => SetGameOverLineText(3, value); }

    public string GameOverLine4 { get => _gameOverLines[4]; set => SetGameOverLineText(4, value); }

    public Visibility GameOverLine0Visibility => _gameOverLineVisibilities[0];

    public Visibility GameOverLine1Visibility => _gameOverLineVisibilities[1];

    public Visibility GameOverLine2Visibility => _gameOverLineVisibilities[2];

    public Visibility GameOverLine3Visibility => _gameOverLineVisibilities[3];

    public Visibility GameOverLine4Visibility => _gameOverLineVisibilities[4];

    public string ExitHint
    {
        get => _exitHint;
        set => SetProperty(ref _exitHint, value);
    }

    /// <summary>Font size of the current and best lap times.</summary>
    public int TimeFontSize
    {
        get => _timeFontSize;
        set => SetProperty(ref _timeFontSize, value);
    }

    /// <summary>Font size of the track name.</summary>
    public int TrackFontSize
    {
        get => _trackFontSize;
        set => SetProperty(ref _trackFontSize, value);
    }

    /// <summary>Font size of the best five times rows.</summary>
    public int RowFontSize
    {
        get => _rowFontSize;
        set => SetProperty(ref _rowFontSize, value);
    }

    /// <summary>
    /// Sizes the panel texts for the responsive UI scale as the code-built HUD drew them: the design font size times the
    /// panel's scaled size over its sprite size, rounded, at least 10. The responsive text scale is not used, since it does
    /// not follow the panels.
    /// </summary>
    public void UpdateTextSizes(float uiScaleFactor)
    {
        float timesScale = UIResponsiveMath.ScaleInt(TimesPanelHeight, uiScaleFactor) / (float)TimesSpriteHeight;
        float topTimesScale = UIResponsiveMath.ScaleInt(TopTimesPanelWidth, uiScaleFactor) / (float)TopTimesSpriteWidth;
        TimeFontSize = PanelFontSize(38, timesScale);
        TrackFontSize = PanelFontSize(26, topTimesScale);
        RowFontSize = PanelFontSize(30, topTimesScale);
    }

    public void SetTopTime(int index, string text)
    {
        SetProperty(ref _topTimes[index], text, $"Top{index}Time");
    }

    /// <summary>Fills the line slots of the race-finished panel: one line per completed lap, then the rank.</summary>
    public void SetGameOverLines(IReadOnlyList<string> lines)
    {
        int visibleCount = Math.Max(MinimumVisibleGameOverLines, Math.Min(lines.Count, GameOverLineCount));
        for (int i = 0; i < GameOverLineCount; i++)
        {
            SetGameOverLineText(i, i < lines.Count ? lines[i] : string.Empty);
            SetProperty(ref _gameOverLineVisibilities[i], i < visibleCount ? Visibility.Visible : Visibility.Collapsed, $"GameOverLine{i}Visibility");
        }
    }

    private void SetGameOverLineText(int index, string text)
    {
        SetProperty(ref _gameOverLines[index], text, $"GameOverLine{index}");
    }

    private static int PanelFontSize(int designFontSize, float scale)
    {
        return Math.Max(10, (int)Math.Round(designFontSize * scale));
    }
}
