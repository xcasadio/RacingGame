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
/// Data context of <c>Screen.Options</c>, RacingGame's options screen (ADR-0013): the menu decoration, the "OPTIONS"
/// header, the options panel with its baked labels, the areas redrawn over it, the player name, the round buttons, the
/// slider handles, the selection arrow and the B BACK button, laid out every frame in screen pixels with RacingGame's
/// formulas (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/Options.cs</c>, Update and Render). The panel and every
/// area on it are placed with CalcRectangleKeep4To3 of the texture rectangle, plus YToRes768(125), as RacingGame did.
/// <para/>
/// RacingGameCasaEngine's rows are drawn in the panel's style (ADR-0013): the resolution slots' baked labels are covered
/// and RacingGameCasaEngine's resolutions written over them, as RacingGame's later version did; the High Detail slot
/// shows Vertical Sync the same way; Show FPS, Gamepad Vibration and Driving Mode ("Simulation") are round buttons with a
/// text, on a grid aligned with the panel's columns (339 and 616). The selection arrow visits every row
/// (<see cref="StopCount"/> stops); RacingGame's reached the three sliders only, which keep its arrow positions.
/// </summary>
public sealed class RaceOptionsViewModel : RaceViewModelBase
{
    public const int ResolutionCount = 5;

    // Selection stops, in reading order. RacingGame's arrow started on the Sound slider.
    public const int StopResolution = 0;
    public const int StopFullscreen = 1;
    public const int StopDrivingMode = 2;
    public const int StopPostEffects = 3;
    public const int StopShadows = 4;
    public const int StopVSync = 5;
    public const int StopShowFps = 6;
    public const int StopVibration = 7;
    public const int StopSound = 8;
    public const int StopMusic = 9;
    public const int StopSensitivity = 10;
    public const int StopCount = 11;

    /// <summary>The tint of an option that is on (RacingGame's selColor), and of a round button that is on.</summary>
    public static readonly Color OnColor = new(255, 156, 0, 160);

    /// <summary>The tint of a round button that is off.</summary>
    public static readonly Color OffColor = new(180, 180, 180, 120);

    /// <summary>The colour of the selected resolution's label, and of Vertical Sync when on.</summary>
    public static readonly Color SelectedLabelColor = new(255, 156, 0);

    // Texture rectangles of OptionsScreenWindows.png (Options.cs Resolution*GfxRect, FullscreenGfxRect, ...).
    private static readonly Rectangle[] ResolutionSlots =
    [
        new(339, 112, 98, 32), new(454, 112, 98, 32), new(575, 112, 108, 32), new(704, 112, 116, 32), new(838, 112, 69, 32),
    ];

    private static readonly Rectangle FullscreenSlot = new(339, 182, 105, 36);
    private static readonly Rectangle PostEffectsSlot = new(339, 226, 206, 36);
    private static readonly Rectangle ShadowsSlot = new(616, 226, 90, 36);
    private static readonly Rectangle HighDetailSlot = new(784, 226, 120, 36);
    // Round-button rows: Show FPS where RacingGame's later version put it; Gamepad Vibration and Driving Mode in the
    // panel's second column, clear of the Show FPS and Fullscreen texts.
    private static readonly Rectangle ShowFpsSlot = new(339, 262, 110, 32);
    private static readonly Rectangle VibrationSlot = new(616, 262, 250, 32);
    private static readonly Rectangle DrivingModeSlot = new(616, 182, 160, 32);
    private static readonly Rectangle SoundSlot = new(384, 281, 448, 39);
    private static readonly Rectangle MusicSlot = new(384, 354, 448, 39);
    private static readonly Rectangle SensitivitySlot = new(384, 428, 448, 39);
    // Arrow positions: Line4/5/6ArrowGfxRect for the sliders; in front of the row's label for the resolution and
    // Fullscreen rows, as RacingGame's arrow pointed at the slider labels; in front of the option elsewhere.
    private static readonly Rectangle[] ArrowSlots =
    [
        new(78, 110, 62, 39), new(125, 180, 62, 39), new(550, 182, 62, 39), new(273, 224, 62, 39), new(550, 224, 62, 39),
        new(706, 224, 62, 39), new(273, 262, 62, 39), new(550, 262, 62, 39), new(154, 284, 62, 39), new(160, 354, 62, 39),
        new(72, 437, 62, 39),
    ];

    // UIRenderer.SelectionRadioButtonGfxRect (39x39).
    private const int RadioSize = 39;
    // TextureFont: glyphs drawn YToRes1050(5) above the position, line height YToRes1050(36 - 5).
    private const int FontRaise = 5;
    private const int FontHeight = 31;

    private readonly Rectangle[] _resolutionRects = new Rectangle[ResolutionCount];
    private readonly string[] _resolutionLabels = [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty];
    private readonly Rectangle[] _stopRects = new Rectangle[StopCount];
    private Rectangle _backButtonRect;
    private string _nameDisplay = string.Empty;
    private int _selectedStop = StopSound;

    public RaceOptionsViewModel()
    {
        ResolutionCovers = [Resolution0, Resolution1, Resolution2, Resolution3, Resolution4];
        ResolutionTexts = [ResolutionText0, ResolutionText1, ResolutionText2, ResolutionText3, ResolutionText4];
    }

    public RaceMenuDecorationViewModel Decoration { get; } = new();

    /// <summary>The "OPTIONS" header (headers.png HeaderOptionsGfxRect), drawn with RenderOnScreenRelative1600(10, 18).</summary>
    public RaceScreenRectViewModel Header { get; } = new();

    /// <summary>The options panel (OptionsScreenWindows.png), drawn with RenderOnScreenRelative4To3(0, 125).</summary>
    public RaceScreenRectViewModel Panel { get; } = new();

    /// <summary>A resolution slot's area, redrawn darkened over the baked label.</summary>
    public RaceScreenRectViewModel Resolution0 { get; } = new();

    public RaceScreenRectViewModel Resolution1 { get; } = new();

    public RaceScreenRectViewModel Resolution2 { get; } = new();

    public RaceScreenRectViewModel Resolution3 { get; } = new();

    public RaceScreenRectViewModel Resolution4 { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> ResolutionCovers { get; }

    /// <summary>The resolution labels: RacingGameCasaEngine's resolutions, then "Auto".</summary>
    public string ResolutionLabel0 { get => _resolutionLabels[0]; set => SetLabel(0, value); }

    public string ResolutionLabel1 { get => _resolutionLabels[1]; set => SetLabel(1, value); }

    public string ResolutionLabel2 { get => _resolutionLabels[2]; set => SetLabel(2, value); }

    public string ResolutionLabel3 { get => _resolutionLabels[3]; set => SetLabel(3, value); }

    public string ResolutionLabel4 { get => _resolutionLabels[4]; set => SetLabel(4, value); }

    /// <summary>Where a resolution label is written, centred in its slot.</summary>
    public RaceScreenRectViewModel ResolutionText0 { get; } = new();

    public RaceScreenRectViewModel ResolutionText1 { get; } = new();

    public RaceScreenRectViewModel ResolutionText2 { get; } = new();

    public RaceScreenRectViewModel ResolutionText3 { get; } = new();

    public RaceScreenRectViewModel ResolutionText4 { get; } = new();

    public IReadOnlyList<RaceScreenRectViewModel> ResolutionTexts { get; }

    /// <summary>A graphics option's area, redrawn tinted while the option is on (<see cref="RaceScreenRectViewModel.IsHighlighted"/>).</summary>
    public RaceScreenRectViewModel Fullscreen { get; } = new();

    public RaceScreenRectViewModel PostScreenEffects { get; } = new();

    public RaceScreenRectViewModel Shadows { get; } = new();

    /// <summary>The High Detail slot, redrawn darkened over its baked label.</summary>
    public RaceScreenRectViewModel VSyncCover { get; } = new();

    /// <summary>Where "Vertical Sync" is written, centred in the High Detail slot.</summary>
    public RaceScreenRectViewModel VSyncText { get; } = new();

    /// <summary>A round button: shown off (<see cref="RaceScreenRectViewModel.IsShown"/>) or on (highlighted).</summary>
    public RaceScreenRectViewModel ShowFpsButton { get; } = new();

    public RaceScreenRectViewModel VibrationButton { get; } = new();

    public RaceScreenRectViewModel DrivingModeButton { get; } = new();

    /// <summary>Where a round button's text is written.</summary>
    public RaceScreenRectViewModel ShowFpsText { get; } = new();

    public RaceScreenRectViewModel VibrationText { get; } = new();

    public RaceScreenRectViewModel DrivingModeText { get; } = new();

    /// <summary>A slider's handle.</summary>
    public RaceScreenRectViewModel SoundHandle { get; } = new();

    public RaceScreenRectViewModel MusicHandle { get; } = new();

    public RaceScreenRectViewModel SensitivityHandle { get; } = new();

    /// <summary>The selection arrow, in front of the selected row.</summary>
    public RaceScreenRectViewModel Arrow { get; } = new();

    /// <summary>Where the player name is written.</summary>
    public RaceScreenRectViewModel NameText { get; } = new();

    /// <summary>The player name, followed by the blinking "|" cursor.</summary>
    public string NameDisplay
    {
        get => _nameDisplay;
        set => SetProperty(ref _nameDisplay, value);
    }

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

    /// <summary>The colour of "Vertical Sync" after the last <see cref="Update"/>.</summary>
    public Color VSyncColor { get; private set; } = Color.White;

    /// <summary>Lays everything out for the viewport and the values.</summary>
    /// <param name="totalSeconds">The game's total time, as RacingGame's <c>BaseGame.TotalTime</c>, for the arrow's swing and the cursor's blink.</param>
    /// <param name="pinAnimations">Holds the arrow and the cursor still, for the capture automation.</param>
    /// <param name="measure">Width of a text in GameFont at its native size.</param>
    public void Update(int viewportWidth, int viewportHeight, int mouseX, int mouseY, double totalSeconds, bool pinAnimations,
        in RaceOptionsValues values, Func<string, float> measure)
    {
        var layout = new LegacyScreenLayout(viewportWidth, viewportHeight);
        Set(Header, layout.CalcRectangle1600(10, 18, 512, 100));
        Set(Panel, layout.CalcRectangleKeep4To3(0, 125, 1024, 512));

        TextScaleX = layout.Width / 1400f;
        TextScaleY = layout.Height / 1050f;
        int raise = layout.YToRes1050(FontRaise);
        int fontHeight = layout.YToRes1050(FontHeight);

        // The name, and "|" every other 0.35 s.
        bool cursorShown = pinAnimations || (int)(totalSeconds / 0.35f) % 2 == 0;
        NameDisplay = values.PlayerName + (cursorShown ? "|" : string.Empty);
        NameText.Set(layout.XToRes(352), layout.YToRes768(125 + 65 - 20) - raise, 0, 0);

        for (int i = 0; i < ResolutionCount; i++)
        {
            Rectangle slot = OnPanel(layout, ResolutionSlots[i]);
            _resolutionRects[i] = slot;
            Set(ResolutionCovers[i], slot);
            PlaceCentred(ResolutionTexts[i], slot, _resolutionLabels[i], layout, measure, raise, fontHeight);
            ResolutionColors[i] = i == values.ResolutionIndex ? SelectedLabelColor : Color.White;
        }

        SetToggle(Fullscreen, StopFullscreen, OnPanel(layout, FullscreenSlot), values.IsFullscreen);
        SetToggle(PostScreenEffects, StopPostEffects, OnPanel(layout, PostEffectsSlot), values.EnablePostEffects);
        SetToggle(Shadows, StopShadows, OnPanel(layout, ShadowsSlot), values.EnableShadows);
        Rectangle highDetail = OnPanel(layout, HighDetailSlot);
        _stopRects[StopVSync] = highDetail;
        Set(VSyncCover, highDetail);
        PlaceCentred(VSyncText, highDetail, "Vertical Sync", layout, measure, raise, fontHeight);
        VSyncColor = values.EnableVSync ? SelectedLabelColor : Color.White;

        SetRoundButton(ShowFpsButton, ShowFpsText, StopShowFps, OnPanel(layout, ShowFpsSlot), values.ShowFps, layout, raise, fontHeight);
        SetRoundButton(VibrationButton, VibrationText, StopVibration, OnPanel(layout, VibrationSlot), values.EnableVibration, layout, raise, fontHeight);
        SetRoundButton(DrivingModeButton, DrivingModeText, StopDrivingMode, OnPanel(layout, DrivingModeSlot), values.IsSimulation, layout, raise, fontHeight);

        SetSlider(SoundHandle, StopSound, OnPanel(layout, SoundSlot), values.SoundVolume, layout);
        SetSlider(MusicHandle, StopMusic, OnPanel(layout, MusicSlot), values.MusicVolume, layout);
        SetSlider(SensitivityHandle, StopSensitivity, OnPanel(layout, SensitivitySlot), values.ControllerSensitivity, layout);

        // The arrow swings left by 0 to 16 units (8 + 8 sin(t / 0.21212)).
        Rectangle arrow = OnPanel(layout, ArrowSlots[_selectedStop]);
        double swing = pinAnimations ? 0.0 : Math.Sin(totalSeconds / 0.21212f);
        arrow.X -= layout.XToRes(8 + (int)Math.Round(8 * swing));
        Set(Arrow, arrow);

        _backButtonRect = layout.BackButton();
        bool hovered = _backButtonRect.Contains(mouseX, mouseY);
        Set(BackButton, hovered ? layout.GrowBottomButton(_backButtonRect) : _backButtonRect);
        BackButton.IsHighlighted = hovered;
    }

    /// <summary>The resolution slot under the point, or -1.</summary>
    public int GetResolutionAt(int x, int y) => Array.FindIndex(_resolutionRects, rect => rect.Contains(x, y));

    /// <summary>The option (stop) whose area is under the point, or -1. Options come before the sliders: a click on a
    /// round button does not also move the Sound slider it overlaps, as it did in RacingGame's later version.</summary>
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

    private void SetLabel(int index, string value) => SetProperty(ref _resolutionLabels[index], value ?? string.Empty, "ResolutionLabel" + index);

    // RacingGame: CalcRectangleKeep4To3(gfxRect), then Y += YToRes768(125).
    private static Rectangle OnPanel(LegacyScreenLayout layout, Rectangle textureRect)
    {
        Rectangle rect = layout.CalcRectangleKeep4To3(textureRect);
        rect.Y += layout.YToRes768(125);
        return rect;
    }

    private void SetToggle(RaceScreenRectViewModel target, int stop, Rectangle rect, bool isOn)
    {
        _stopRects[stop] = rect;
        Set(target, rect);
        target.IsHighlighted = isOn;
    }

    // RacingGame's later version: the 39x39 round button at the area's top left, the text XToRes(39 + 4) to its right.
    private void SetRoundButton(RaceScreenRectViewModel button, RaceScreenRectViewModel text, int stop, Rectangle rect, bool isOn,
        LegacyScreenLayout layout, int raise, int fontHeight)
    {
        _stopRects[stop] = rect;
        button.Set(rect.X, rect.Y, layout.XToRes(RadioSize), layout.YToRes768(RadioSize));
        button.IsShown = !isOn;
        button.IsHighlighted = isOn;
        text.Set(rect.X + layout.XToRes(RadioSize + 4), rect.Y + (rect.Height - fontHeight) / 2 - raise, 0, 0);
    }

    // The handle: the round button centred on the value's x, at the slider area's top.
    private void SetSlider(RaceScreenRectViewModel handle, int stop, Rectangle rect, int value, LegacyScreenLayout layout)
    {
        _stopRects[stop] = rect;
        float fraction = Math.Clamp(value, 0, 100) / 100f;
        handle.Set(rect.X + (int)(rect.Width * fraction) - layout.XToRes(RadioSize) / 2, rect.Y, layout.XToRes(RadioSize), layout.YToRes768(RadioSize));
    }

    // TextureFont.WriteText at x = slot.X + (slot.Width - width) / 2, y = slot.Y + (slot.Height - Height) / 2.
    private void PlaceCentred(RaceScreenRectViewModel target, Rectangle slot, string text, LegacyScreenLayout layout,
        Func<string, float> measure, int raise, int fontHeight)
    {
        int width = (int)Math.Round(measure(text) * TextScaleX);
        target.Set(slot.X + (slot.Width - width) / 2, slot.Y + (slot.Height - fontHeight) / 2 - raise, 0, 0);
    }

    private static void Set(RaceScreenRectViewModel target, Rectangle rect) => target.Set(rect.X, rect.Y, rect.Width, rect.Height);
}
