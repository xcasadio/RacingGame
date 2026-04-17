using System.Linq;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Gameplay;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

internal static class RaceFrontEndCatalog
{
    private const float BaselineArcadeForwardSpeedUnitsPerSecond = 36f;
    private const float BaselineSimulationForwardSpeedUnitsPerSecond = 38f;

    public static IReadOnlyList<CarPerformanceProfile> CarProfiles { get; } = CreateCarProfiles();

    public static IReadOnlyList<CarDefinition> Cars { get; } =
        CreateCars(CarProfiles);

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
        new("Race Flow", ["Pause with Escape or GamePad Start once the countdown is over.", "Finish the race to open the result panel, then return to the main menu."]),
        new("Menus", ["This front-end uses UIRoot, ScreenStack, and GameScreenManager.", "No legacy RacingGame.Shared renderer is needed here."]),
        new("Session Flow", ["Splash -> Menu -> Car -> Track -> HUD is now fully wired.", "The in-game HUD keeps the original MGUI layout, while pause still runs through the same screen stack."]),
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

    public static CarPerformanceProfile ResolveCarProfile(int carIndex)
    {
        int resolvedIndex = Math.Clamp(carIndex, 0, CarProfiles.Count - 1);
        return CarProfiles[resolvedIndex];
    }

    private static IReadOnlyList<CarDefinition> CreateCars(IReadOnlyList<CarPerformanceProfile> profiles)
    {
        return profiles
            .Select(profile => new CarDefinition(
                profile.Name,
                profile.Summary,
                profile.AccentColor,
                profile,
                BuildStatLabels(profile),
                BuildSelectionStats(profile, profiles)))
            .ToArray();
    }

    private static IReadOnlyList<CarPerformanceProfile> CreateCarProfiles()
    {
        return
        [
            new CarPerformanceProfile(
                "car-1",
                "Car 1",
                "Long gearing and the highest top speed in the roster.",
                Color.White,
                288f,
                1015f,
                4.0f,
                VehicleTransmissionLogic.CreateDefaultFiveSpeedDefinition(),
                CreateBaselineWheelDefinitions(),
                new ArcadeVehicleTuningProfile(
                    18.5f,
                    13.0f,
                    BaselineArcadeForwardSpeedUnitsPerSecond * 1.05f,
                    12f,
                    1.68f,
                    17.5f),
                new SimulationVehicleTuningProfile(
                    1015f,
                    BaselineSimulationForwardSpeedUnitsPerSecond * 1.05f,
                    12f,
                    7400f,
                    4000f,
                    8900f,
                    500f,
                    1850f,
                    2550f,
                    27000f,
                    3400f,
                    1.08f,
                    0.92f,
                    3.05f,
                    2860f,
                    8.0f,
                    9.2f,
                    11.5f,
                    9.0f,
                    13.0f,
                    0.56f)),
            new CarPerformanceProfile(
                "car-2",
                "Car 2",
                "Shorter gearing with the strongest launch, but more weight to settle.",
                Color.CornflowerBlue,
                275f,
                1175f,
                6.0f,
                VehicleTransmissionLogic.CreateDefaultFiveSpeedDefinition(),
                CreateBaselineWheelDefinitions(),
                new ArcadeVehicleTuningProfile(
                    24.0f,
                    15.0f,
                    BaselineArcadeForwardSpeedUnitsPerSecond,
                    12f,
                    1.60f,
                    19.0f),
                new SimulationVehicleTuningProfile(
                    1175f,
                    BaselineSimulationForwardSpeedUnitsPerSecond,
                    12f,
                    8450f,
                    4600f,
                    9600f,
                    540f,
                    1980f,
                    2500f,
                    30000f,
                    3800f,
                    1.05f,
                    0.97f,
                    3.35f,
                    3480f,
                    8.8f,
                    9.8f,
                    12.5f,
                    9.5f,
                    14.0f,
                    0.52f)),
            new CarPerformanceProfile(
                "car-3",
                "Car 3",
                "Lightweight chassis with the quickest rotation and the best grip reserve.",
                Color.OrangeRed,
                240f,
                875f,
                5.0f,
                VehicleTransmissionLogic.CreateDefaultFiveSpeedDefinition(),
                CreateBaselineWheelDefinitions(),
                new ArcadeVehicleTuningProfile(
                    21.0f,
                    14.5f,
                    BaselineArcadeForwardSpeedUnitsPerSecond * 0.88f,
                    12f,
                    1.88f,
                    16.5f),
                new SimulationVehicleTuningProfile(
                    875f,
                    BaselineSimulationForwardSpeedUnitsPerSecond * 0.88f,
                    12f,
                    7800f,
                    4300f,
                    9000f,
                    470f,
                    1760f,
                    3250f,
                    25500f,
                    3250f,
                    1.22f,
                    0.90f,
                    2.95f,
                    2550f,
                    8.2f,
                    9.0f,
                    12.0f,
                    9.2f,
                    12.5f,
                    0.62f)),
        ];
    }

    private static IReadOnlyList<string> BuildStatLabels(CarPerformanceProfile profile)
    {
        return
        [
            $"Max Speed: {profile.TargetTopSpeedMph:0} mph",
            $"Acceleration: {profile.LegacyMaxAccelerationPerSecond:0.0} m/s^2",
            $"Mass: {profile.LegacyMassKilograms:0} kg",
            $"Handling: {BuildHandlingDescriptor(profile)}",
        ];
    }

    private static IReadOnlyList<CarSelectionDisplayStat> BuildSelectionStats(CarPerformanceProfile profile, IReadOnlyList<CarPerformanceProfile> profiles)
    {
        return
        [
            new CarSelectionDisplayStat($"Max Speed: {profile.TargetTopSpeedMph:0} mph", NormalizeMetric(profiles, profile, static entry => entry.TargetTopSpeedMph)),
            new CarSelectionDisplayStat($"Acceleration: {profile.LegacyMaxAccelerationPerSecond:0.0} m/s^2", NormalizeMetric(profiles, profile, static entry => entry.LegacyMaxAccelerationPerSecond)),
            new CarSelectionDisplayStat($"Mass: {profile.LegacyMassKilograms:0} kg", NormalizeMetric(profiles, profile, static entry => entry.LegacyMassKilograms)),
            new CarSelectionDisplayStat($"Handling: {BuildHandlingDescriptor(profile)}", NormalizeMetric(profiles, profile, ComputeHandlingScore)),
        ];
    }

    private static float ComputeHandlingScore(CarPerformanceProfile profile)
    {
        float normalizedGrip = profile.Simulation.LateralGrip / Math.Max(1f, profile.Simulation.ChassisMass);
        float steeringScale = profile.Arcade.TurnRateRadiansPerSecond / Math.Max(0.01f, profile.Arcade.MaxForwardSpeedUnitsPerSecond);
        return (normalizedGrip * 220f) + (steeringScale * 9f);
    }

    private static string BuildHandlingDescriptor(CarPerformanceProfile profile)
    {
        float handlingScore = ComputeHandlingScore(profile);
        if (handlingScore >= 1.12f)
        {
            return "Agile";
        }

        if (handlingScore >= 0.94f)
        {
            return "Balanced";
        }

        return "Stable";
    }

    private static float NormalizeMetric(
        IReadOnlyList<CarPerformanceProfile> profiles,
        CarPerformanceProfile profile,
        Func<CarPerformanceProfile, float> selector)
    {
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        float currentValue = selector(profile);

        for (int index = 0; index < profiles.Count; index++)
        {
            float candidateValue = selector(profiles[index]);
            minValue = Math.Min(minValue, candidateValue);
            maxValue = Math.Max(maxValue, candidateValue);
        }

        if (maxValue - minValue <= 0.0001f)
        {
            return 50f;
        }

        return ((currentValue - minValue) / (maxValue - minValue)) * 100f;
    }

    private static VehicleWheelDefinition[] CreateBaselineWheelDefinitions()
    {
        const float wheelRadius = 0.43f;
        const float restLength = 0.42f;
        const float travel = 0.22f;
        const float frontSteering = 0.46f;
        const float sideOffset = 1.05f;
        const float frontOffset = -1.68f;
        const float rearOffset = 1.88f;
        const float attachmentHeight = wheelRadius + restLength;

        return
        [
            new VehicleWheelDefinition(VehicleWheelSlot.FrontLeft, "WheelFrontLeft", new Vector3(sideOffset, attachmentHeight, frontOffset), wheelRadius, restLength, travel, frontSteering, 0.25f, 0.25f, 0.27f),
            new VehicleWheelDefinition(VehicleWheelSlot.FrontRight, "WheelFrontRight", new Vector3(-sideOffset, attachmentHeight, frontOffset), wheelRadius, restLength, travel, frontSteering, 0.25f, 0.25f, 0.27f),
            new VehicleWheelDefinition(VehicleWheelSlot.RearLeft, "WheelBackLeft", new Vector3(sideOffset, attachmentHeight, rearOffset), wheelRadius, restLength, travel, 0f, 0.25f, 0.25f, 0.23f),
            new VehicleWheelDefinition(VehicleWheelSlot.RearRight, "WheelBackRight", new Vector3(-sideOffset, attachmentHeight, rearOffset), wheelRadius, restLength, travel, 0f, 0.25f, 0.25f, 0.23f),
        ];
    }
}

internal sealed record CarDefinition(
    string Name,
    string Summary,
    Color AccentColor,
    CarPerformanceProfile PerformanceProfile,
    IReadOnlyList<string> Stats,
    IReadOnlyList<CarSelectionDisplayStat> SelectionStats);

internal readonly record struct CarSelectionDisplayStat(string Label, float FillPercent);

internal sealed record ColorOption(string Name, Color Value);

internal sealed record TrackDefinition(string Name, string Summary, string Laps, string Surface);

internal sealed record HelpSection(string Title, IReadOnlyList<string> Lines);

internal sealed record HighscoreEntry(string PlayerName, string Time);