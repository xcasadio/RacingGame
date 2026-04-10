using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

internal static class RaceFrontEndCatalog
{
    public static IReadOnlyList<CarDefinition> Cars { get; } =
    [
        new("Car 1", "Balanced starter", Color.White, ["Max Speed: 168 mph", "Acceleration: Medium", "Mass: 1210 kg", "Braking: Stable"]),
        new("Car 2", "Fast but less forgiving", Color.CornflowerBlue, ["Max Speed: 181 mph", "Acceleration: High", "Mass: 1140 kg", "Braking: Reactive"]),
        new("Car 3", "Heavy grip machine", Color.OrangeRed, ["Max Speed: 172 mph", "Acceleration: Strong", "Mass: 1290 kg", "Braking: Strong"]),
    ];

    public static IReadOnlyList<ColorOption> CarColors { get; } =
    [
        new("White", Color.White),
        new("Yellow", Color.Yellow),
        new("Blue", Color.Blue),
        new("Purple", Color.Purple),
        new("Red", Color.Red),
        new("Green", Color.Green),
        new("Teal", Color.Teal),
        new("Gray", Color.Gray),
        new("Chocolate", Color.Chocolate),
        new("Orange", Color.Orange),
        new("Sea Green", Color.SeaGreen),
    ];

    public static IReadOnlyList<TrackDefinition> Tracks { get; } =
    [
        new("Beginner", "Short forgiving layout for the first race slice.", "2 laps", "Wide turns"),
        new("Advanced", "Faster flow with tighter sequencing and more braking.", "3 laps", "Mixed corners"),
        new("Expert", "High-speed route intended for the final migration target.", "4 laps", "Technical apexes"),
    ];

    public static IReadOnlyList<HelpSection> HelpSections { get; } =
    [
        new("Race Controls", ["Accelerate with Up, W, GamePad A, the right trigger, or D-pad up.", "Brake or reverse with Down, S, GamePad B, the left trigger, or D-pad down."]),
        new("Steering", ["Steer with Left and Right, A and D, the left stick, or the D-pad.", "Controller sensitivity from Options scales the analog steering response." ]),
        new("Camera", ["Use Page Up and Page Down, or GamePad X and Y, to change chase distance during a race.", "The chase camera now widens with speed and switches to an orbit view when the race is finished."]),
        new("Menus", ["This front-end uses UIRoot, ScreenStack, and GameScreenManager.", "No legacy RacingGame.Shared renderer is needed here."]),
        new("Session Flow", ["Splash -> Menu -> Car -> Track -> HUD is now the target path.", "Race world loading will replace the placeholder HUD next."]),
    ];

    public static IReadOnlyDictionary<string, IReadOnlyList<HighscoreEntry>> Highscores { get; } =
        new Dictionary<string, IReadOnlyList<HighscoreEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Beginner"] =
            [
                new("A. Vega", "01:11.82"),
                new("M. Ford", "01:13.21"),
                new("L. Stone", "01:14.07"),
                new("T. Park", "01:14.93"),
                new("N. Hart", "01:15.42"),
            ],
            ["Advanced"] =
            [
                new("J. Cruz", "01:48.22"),
                new("S. Bell", "01:49.64"),
                new("K. Hall", "01:50.11"),
                new("R. Dean", "01:51.03"),
                new("B. Holt", "01:52.80"),
            ],
            ["Expert"] =
            [
                new("C. Flynn", "02:31.51"),
                new("D. Nash", "02:33.10"),
                new("P. Wells", "02:34.08"),
                new("G. North", "02:35.77"),
                new("Y. Stone", "02:37.19"),
            ],
        };
}

internal sealed record CarDefinition(string Name, string Summary, Color AccentColor, IReadOnlyList<string> Stats);

internal sealed record ColorOption(string Name, Color Value);

internal sealed record TrackDefinition(string Name, string Summary, string Laps, string Surface);

internal sealed record HelpSection(string Title, IReadOnlyList<string> Lines);

internal sealed record HighscoreEntry(string PlayerName, string Time);