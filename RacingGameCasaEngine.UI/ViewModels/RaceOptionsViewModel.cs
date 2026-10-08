using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.UI.ViewModels;

/// <summary>The option values the Options screen shows, read from the front-end state every frame.</summary>
public readonly record struct RaceOptionsValues(
    string PlayerName,
    int ResolutionIndex,
    bool IsFullscreen,
    bool IsSimulation,
    bool EnablePostEffects,
    bool EnableShadows,
    bool EnableVSync,
    bool ShowFps,
    bool EnableVibration,
    int SoundVolume,
    int MusicVolume,
    int ControllerSensitivity);

/// <summary>
/// Data context of <c>Screen.Options</c> (ADR-0014): the previous arrangement of the options, a column of labels and a
/// column of controls, drawn with RacingGame's visual vocabulary and laid out every frame in screen pixels with
/// RacingGame's formulas and 1024x640 units, as the other menus. It holds the menu decoration, the black band, the
/// "OPTIONS" header, the rows, the selection arrow and the B BACK button.
/// <para/>
/// The rows, from the top, are Player Name, Resolution, Driving Mode, the six options and the three sliders. Labels are
/// written in GameFont, right-aligned on x 380, and the controls start at x 400:
/// <list type="bullet">
/// <item>the name in the original's name field;</item>
/// <item>the resolutions and the driving modes as labels, amber when chosen;</item>
/// <item>an option as the original's round button, orange when on and grey when off;</item>
/// <item>a slider as the original's track with a round handle, followed by its value.</item>
/// </list>
/// The selection arrow stands in front of the selected row's label and swings as RacingGame's (Options.cs,
/// RenderSelectionArrow). Texts are written as TextureFont did: at their position minus YToRes1050(5), and scaled by the
/// screen with <see cref="TextScaleX"/> and <see cref="TextScaleY"/>.
/// </summary>
public sealed class RaceOptionsViewModel : RaceViewModelBase
{
    public const int ResolutionCount = 5;
    public const int DrivingModeCount = 2;
    public const int ToggleCount = 6;
    public const int SliderCount = 3;
    public const int RowCount = 12;

    // Selection stops, one per row below Player Name. RacingGame's arrow started on the Sound slider.
    public const int StopResolution = 0;
    public const int StopDrivingMode = 1;
    public const int StopFullscreen = 2;
    public const int StopVSync = 3;
    public const int StopPostEffects = 4;
    public const int StopShadows = 5;
    public const int StopShowFps = 6;
    public const int StopVibration = 7;
    public const int StopSound = 8;
    public const int StopMusic = 9;
    public const int StopSensitivity = 10;
    public const int StopCount = 11;

    /// <summary>The row labels, from the top.</summary>
    public static readonly string[] RowLabels =
    [
        "Player Name", "Resolution", "Driving Mode", "Fullscreen", "Vertical Sync", "Post Screen Effects", "Shadows", "Show FPS",
        "Gamepad Vibration", "Sound Volume", "Music Volume", "Controller Sensitivity",
    ];

    /// <summary>The driving mode labels: Arcade, then Simulation.</summary>
    public static readonly string[] DrivingModeLabels = ["Arcade", "Simulation"];

    /// <summary>The colour of a chosen resolution or driving mode (RacingGame's selected resolution label).</summary>
    public static readonly Color ChosenColor = new(255, 156, 0);

    private const int BandTop = 110;
    private const int BandHeight = 370;
    private const int FirstRowTop = 122;
    private const int RowPitch = 27;
    // The rows' visible text sits about 8 units below their position; the controls are centred there.
    private const int RowCentre = 8;
    private const int LabelRight = 380;
    private const int ControlLeft = 400;
    private const int ResolutionGap = 16;
    private const int DrivingModeGap = 24;
    private const int NameFieldLeft = 395;
    private const int NameFieldWidth = 400;
    private const int NameFieldHeight = 25;
    private const int NameLeft = 405;
    private const int ButtonSize = 22;
    private const int TrackWidth = 300;
    private const int TrackHeight = 6;
    private const int ValueLeft = 720;
    // UIRenderer.SelectionArrowGfxRect (53x39).
    private const int ArrowArtWidth = 53;
    private const int ArrowArtHeight = 39;
    // TextureFont drew its glyphs YToRes1050(5) above the given position (SubRenderHeight).
    private const int FontRaise = 5;

    private readonly Rectangle[] _resolutionRects = new Rectangle[ResolutionCount];
    private readonly Rectangle[] _drivingModeRects = new Rectangle[DrivingModeCount];
    private readonly Rectangle[] _stopRects = new Rectangle[StopCount];
    private readonly string[] _resolutionLabels = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];
    private readonly int[] _labelLefts = new int[RowCount];
    private int _bandTopPixels;
    private int _bandHeightPixels;
    private Rectangle _backButtonRect;
    private string _nameDisplay = string.Empty;
    private string _soundValue = string.Empty;
    private string _musicValue = string.Empty;
    private string _sensitivityValue = string.Empty;
    private int _selectedStop = StopSound;

    public RaceOptionsViewModel()
    {
        Labels = [Label0, Label1, Label2, Label3, Label4, Label5, Label6, Label7, Label8, Label9, Label10, Label11];
        ResolutionTexts = [ResolutionText0, ResolutionText1, ResolutionText2, ResolutionText3, ResolutionText4];
        DrivingModeTexts = [ArcadeText, SimulationText];
        ToggleButtons = [FullscreenButton, VSyncButton, PostEffectsButton, ShadowsButton, ShowFpsButton, VibrationButton];
        Tracks = [SoundTrack, MusicTrack, SensitivityTrack];
        Handles = [SoundHandle, MusicHandle, SensitivityHandle];
        ValueTexts = [SoundValueText, MusicValueText, SensitivityValueText];
    }

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    public int BandTopPixels
    {
        get => _bandTopPixels;
        set
        {
            if (SetProperty(ref _bandTopPixels, value))
            {
                NotifyPropertyChanged(nameof(BandMargin));
            }
        }
    }

    public int BandHeightPixels
    {
        get => _bandHeightPixels;
        set => SetProperty(ref _bandHeightPixels, value);
    }

    public MonoGame.Extended.Thickness BandMargin => new(0, _bandTopPixels, 0, 0);

    /// <summary>The "OPTIONS" header (headers.png HeaderOptionsGfxRect), drawn with RenderOnScreenRelative1600(10, 18).</summary>
    public RaceScreenRectViewModel Header { get; } = new();

    /// <summary>Where each row label (<see cref="RowLabels"/>) is written, right-aligned on x 380.</summary>
    public RaceScreenRectViewModel Label0 { get; } = new();

    public RaceScreenRectViewModel Label1 { get; } = new();

    public RaceScreenRectViewModel Label2 { get; } = new();

    public RaceScreenRectViewModel Label3 { get; } = new();

    public RaceScreenRectViewModel Label4 { get; } = new();

    public RaceScreenRectViewModel Label5 { get; } = new();

    public RaceScreenRectViewModel Label6 { get; } = new();

    public RaceScreenRectViewModel Label7 { get; } = new();

    public RaceScreenRectViewModel Label8 { get; } = new();

    public RaceScreenRectViewModel Label9 { get; } = new();

    public RaceScreenRectViewModel Label10 { get; } = new();

    public RaceScreenRectViewModel Label11 { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> Labels { get; }

    /// <summary>The original's name field (OptionsScreenWindows.png), behind the name.</summary>
    public RaceScreenRectViewModel NameField { get; } = new();

    /// <summary>Where the player name is written.</summary>
    public RaceScreenRectViewModel NameText { get; } = new();

    /// <summary>The player name, followed by the blinking "|" cursor.</summary>
    public string NameDisplay
    {
        get => _nameDisplay;
        set => SetProperty(ref _nameDisplay, value);
    }

    /// <summary>The resolution labels: RacingGameCasaEngine's resolutions, then "Auto".</summary>
    public string ResolutionLabel0 { get => _resolutionLabels[0]; set => SetLabel(0, value); }

    public string ResolutionLabel1 { get => _resolutionLabels[1]; set => SetLabel(1, value); }

    public string ResolutionLabel2 { get => _resolutionLabels[2]; set => SetLabel(2, value); }

    public string ResolutionLabel3 { get => _resolutionLabels[3]; set => SetLabel(3, value); }

    public string ResolutionLabel4 { get => _resolutionLabels[4]; set => SetLabel(4, value); }

    /// <summary>Where each resolution label is written.</summary>
    public RaceScreenRectViewModel ResolutionText0 { get; } = new();

    public RaceScreenRectViewModel ResolutionText1 { get; } = new();

    public RaceScreenRectViewModel ResolutionText2 { get; } = new();

    public RaceScreenRectViewModel ResolutionText3 { get; } = new();

    public RaceScreenRectViewModel ResolutionText4 { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> ResolutionTexts { get; }

    /// <summary>Where "Arcade" and "Simulation" are written.</summary>
    public RaceScreenRectViewModel ArcadeText { get; } = new();

    public RaceScreenRectViewModel SimulationText { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> DrivingModeTexts { get; }

    /// <summary>An option's round button: shown off (<see cref="RaceScreenRectViewModel.IsShown"/>) or on (highlighted).</summary>
    public RaceScreenRectViewModel FullscreenButton { get; } = new();

    public RaceScreenRectViewModel VSyncButton { get; } = new();

    public RaceScreenRectViewModel PostEffectsButton { get; } = new();

    public RaceScreenRectViewModel ShadowsButton { get; } = new();

    public RaceScreenRectViewModel ShowFpsButton { get; } = new();

    public RaceScreenRectViewModel VibrationButton { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> ToggleButtons { get; }

    /// <summary>A slider's track (the original's, the car selection's stat bar).</summary>
    public RaceScreenRectViewModel SoundTrack { get; } = new();

    public RaceScreenRectViewModel MusicTrack { get; } = new();

    public RaceScreenRectViewModel SensitivityTrack { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> Tracks { get; }

    /// <summary>A slider's handle, the round button centred on its value.</summary>
    public RaceScreenRectViewModel SoundHandle { get; } = new();

    public RaceScreenRectViewModel MusicHandle { get; } = new();

    public RaceScreenRectViewModel SensitivityHandle { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> Handles { get; }

    /// <summary>Where a slider's value is written.</summary>
    public RaceScreenRectViewModel SoundValueText { get; } = new();

    public RaceScreenRectViewModel MusicValueText { get; } = new();

    public RaceScreenRectViewModel SensitivityValueText { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> ValueTexts { get; }

    public string SoundValue
    {
        get => _soundValue;
        set => SetProperty(ref _soundValue, value);
    }

    public string MusicValue
    {
        get => _musicValue;
        set => SetProperty(ref _musicValue, value);
    }

    public string SensitivityValue
    {
        get => _sensitivityValue;
        set => SetProperty(ref _sensitivityValue, value);
    }

    /// <summary>The selection arrow, in front of the selected row's label.</summary>
    public RaceScreenRectViewModel Arrow { get; } = new();

    /// <summary>The B BACK button; highlighted (grown, with the orange outline) while the mouse is over it.</summary>
    public RaceScreenRectViewModel BackButton { get; } = new();

    /// <summary>Horizontal draw scale of the GameFont texts (TextureFont's XToRes1400).</summary>
    public float TextScaleX { get; private set; } = 1f;

    /// <summary>Vertical draw scale of the GameFont texts (TextureFont's YToRes1050).</summary>
    public float TextScaleY { get; private set; } = 1f;

    /// <summary>The selection stop the arrow is on (<see cref="StopResolution"/> to <see cref="StopSensitivity"/>).</summary>
    public int SelectedStop
    {
        get => _selectedStop;
        set => SetProperty(ref _selectedStop, Math.Clamp(value, 0, StopCount - 1));
    }

    /// <summary>The colour of each resolution label after the last <see cref="Update"/>.</summary>
    public Color[] ResolutionColors { get; } = new Color[ResolutionCount];

    /// <summary>The colour of each driving mode label after the last <see cref="Update"/>.</summary>
    public Color[] DrivingModeColors { get; } = new Color[DrivingModeCount];

    /// <summary>Lays everything out for the viewport and the values.</summary>
    /// <param name="totalSeconds">The game's total time, as RacingGame's <c>BaseGame.TotalTime</c>, for the arrow's swing and the cursor's blink.</param>
    /// <param name="pinAnimations">Holds the arrow and the cursor still, for the capture automation.</param>
    /// <param name="measure">Width of a text in GameFont at its native size.</param>
    public void Update(int viewportWidth, int viewportHeight, int mouseX, int mouseY, double totalSeconds, bool pinAnimations,
        in RaceOptionsValues values, Func<string, float> measure)
    {
        var layout = new LegacyScreenLayout(viewportWidth, viewportHeight);
        Rectangle band = layout.CalcRectangle(0, BandTop, 1024, BandHeight);
        BandTopPixels = band.Y;
        BandHeightPixels = band.Height;
        Set(Header, layout.CalcRectangle1600(10, 18, 512, 100));

        TextScaleX = layout.Width / 1400f;
        TextScaleY = layout.Height / 1050f;
        int raise = layout.YToRes1050(FontRaise);
        int rowHeight = layout.YToRes(RowPitch);
        int labelRight = layout.XToRes(LabelRight);
        int controlLeft = layout.XToRes(ControlLeft);
        int buttonSize = layout.YToRes(ButtonSize);

        for (int row = 0; row < RowCount; row++)
        {
            int left = labelRight - TextWidth(measure, RowLabels[row]);
            _labelLefts[row] = left;
            Labels[row].Set(left, RowTop(layout, row) - raise, 0, 0);
        }

        // Player Name: the original's field, the name and "|" every other 0.35 s.
        Set(NameField, layout.CalcRectangle(NameFieldLeft, FirstRowTop - 3, NameFieldWidth, NameFieldHeight));
        bool cursorShown = pinAnimations || (int)(totalSeconds / 0.35f) % 2 == 0;
        NameDisplay = values.PlayerName + (cursorShown ? "|" : string.Empty);
        NameText.Set(layout.XToRes(NameLeft), RowTop(layout, 0) - raise, 0, 0);

        // Resolution and Driving Mode: labels side by side, amber when chosen.
        PlaceChoices(layout, measure, 1, _resolutionLabels, ResolutionTexts, _resolutionRects, layout.XToRes(ResolutionGap), raise, rowHeight);
        for (int i = 0; i < ResolutionCount; i++)
        {
            ResolutionColors[i] = i == values.ResolutionIndex ? ChosenColor : Color.White;
        }

        _stopRects[StopResolution] = Span(_resolutionRects);
        PlaceChoices(layout, measure, 2, DrivingModeLabels, DrivingModeTexts, _drivingModeRects, layout.XToRes(DrivingModeGap), raise, rowHeight);
        DrivingModeColors[0] = values.IsSimulation ? Color.White : ChosenColor;
        DrivingModeColors[1] = values.IsSimulation ? ChosenColor : Color.White;
        _stopRects[StopDrivingMode] = Span(_drivingModeRects);

        // Options: the round button, orange when on; the whole row (label and button) is clickable.
        bool[] on = [values.IsFullscreen, values.EnableVSync, values.EnablePostEffects, values.EnableShadows, values.ShowFps, values.EnableVibration];
        for (int i = 0; i < ToggleCount; i++)
        {
            int row = 3 + i;
            int centre = layout.YToRes(FirstRowTop + row * RowPitch + RowCentre);
            RaceScreenRectViewModel button = ToggleButtons[i];
            button.Set(controlLeft, centre - buttonSize / 2, buttonSize, buttonSize);
            button.IsShown = !on[i];
            button.IsHighlighted = on[i];
            _stopRects[StopFullscreen + i] = RowArea(layout, row, controlLeft + buttonSize, rowHeight);
        }

        // Sliders: the track, the handle centred on the value, then the value.
        int[] sliderValues = [values.SoundVolume, values.MusicVolume, values.ControllerSensitivity];
        SoundValue = sliderValues[0].ToString();
        MusicValue = sliderValues[1].ToString();
        SensitivityValue = sliderValues[2].ToString();
        int trackWidth = layout.XToRes(TrackWidth);
        int trackHeight = Math.Max(1, layout.YToRes(TrackHeight));
        for (int i = 0; i < SliderCount; i++)
        {
            int row = 9 + i;
            int centre = layout.YToRes(FirstRowTop + row * RowPitch + RowCentre);
            Tracks[i].Set(controlLeft, centre - trackHeight / 2, trackWidth, trackHeight);
            float fraction = Math.Clamp(sliderValues[i], 0, 100) / 100f;
            Handles[i].Set(controlLeft + (int)(trackWidth * fraction) - buttonSize / 2, centre - buttonSize / 2, buttonSize, buttonSize);
            ValueTexts[i].Set(layout.XToRes(ValueLeft), RowTop(layout, row) - raise, 0, 0);
            _stopRects[StopSound + i] = new Rectangle(controlLeft, RowTop(layout, row) - raise, trackWidth, rowHeight);
        }

        // The arrow, in front of the selected row's label, swings left by 0 to 16 units (8 + 8 sin(t / 0.21212)).
        int selectedRow = _selectedStop + 1;
        int arrowHeight = buttonSize;
        int arrowWidth = (int)Math.Round(arrowHeight * ArrowArtWidth / (float)ArrowArtHeight);
        int arrowCentre = layout.YToRes(FirstRowTop + selectedRow * RowPitch + RowCentre);
        double swing = pinAnimations ? 0.0 : Math.Sin(totalSeconds / 0.21212f);
        int arrowLeft = _labelLefts[selectedRow] - arrowWidth - layout.XToRes(8 + (int)Math.Round(8 * swing));
        Arrow.Set(arrowLeft, arrowCentre - arrowHeight / 2, arrowWidth, arrowHeight);

        _backButtonRect = layout.BackButton();
        bool hovered = _backButtonRect.Contains(mouseX, mouseY);
        Set(BackButton, hovered ? layout.GrowBottomButton(_backButtonRect) : _backButtonRect);
        BackButton.IsHighlighted = hovered;
    }

    /// <summary>The resolution label under the point, or -1.</summary>
    public int GetResolutionAt(int x, int y) => Array.FindIndex(_resolutionRects, rect => rect.Contains(x, y));

    /// <summary>The driving mode label under the point (0 Arcade, 1 Simulation), or -1.</summary>
    public int GetDrivingModeAt(int x, int y) => Array.FindIndex(_drivingModeRects, rect => rect.Contains(x, y));

    /// <summary>The option row or the slider track under the point (its stop), or -1.</summary>
    public int GetStopAt(int x, int y)
    {
        for (int stop = StopFullscreen; stop < StopCount; stop++)
        {
            if (_stopRects[stop].Contains(x, y))
            {
                return stop;
            }
        }

        return -1;
    }

    /// <summary>A slider's value (0 to 100) for a click at <paramref name="x"/>, as RacingGame computed it.</summary>
    public int GetSliderValueAt(int stop, int x)
    {
        Rectangle rect = _stopRects[stop];
        return (int)Math.Round(Math.Clamp((x - rect.X) / (float)rect.Width, 0f, 1f) * 100f);
    }

    /// <summary>Whether the point is on the B button, as RacingGame tested it (before the hover growth).</summary>
    public bool IsOverBackButton(int x, int y) => _backButtonRect.Contains(x, y);

    public static bool IsSlider(int stop) => stop is StopSound or StopMusic or StopSensitivity;

    private static int RowTop(LegacyScreenLayout layout, int row) => layout.YToRes(FirstRowTop + row * RowPitch);

    private int TextWidth(Func<string, float> measure, string text) => (int)Math.Round(measure(text) * TextScaleX);

    // Labels side by side from the control column, each clickable over its width and the row's height.
    private void PlaceChoices(LegacyScreenLayout layout, Func<string, float> measure, int row, IReadOnlyList<string> labels,
        IReadOnlyList<RaceScreenRectViewModel> texts, Rectangle[] rects, int gap, int raise, int rowHeight)
    {
        int x = layout.XToRes(ControlLeft);
        int top = RowTop(layout, row) - raise;
        for (int i = 0; i < labels.Count; i++)
        {
            int width = TextWidth(measure, labels[i]);
            texts[i].Set(x, top, 0, 0);
            rects[i] = new Rectangle(x, top, width, rowHeight);
            x += width + gap;
        }
    }

    // A row from its label's left to the given right edge, over the row's height.
    private Rectangle RowArea(LegacyScreenLayout layout, int row, int right, int rowHeight)
    {
        int top = RowTop(layout, row) - layout.YToRes1050(FontRaise);
        return new Rectangle(_labelLefts[row], top, right - _labelLefts[row], rowHeight);
    }

    private static Rectangle Span(Rectangle[] rects) => new(rects[0].X, rects[0].Y, rects[^1].Right - rects[0].X, rects[0].Height);

    private void SetLabel(int index, string value) => SetProperty(ref _resolutionLabels[index], value ?? string.Empty, "ResolutionLabel" + index);

    private static void Set(RaceScreenRectViewModel target, Rectangle rect) => target.Set(rect.X, rect.Y, rect.Width, rect.Height);
}
