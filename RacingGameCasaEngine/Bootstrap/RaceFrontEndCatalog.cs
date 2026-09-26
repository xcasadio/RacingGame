using System.Linq;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Gameplay;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

internal static class RaceFrontEndCatalog
{
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
            CreateCarProfile(
                "car-1",
                "Car 1",
                "Long gearing and the highest top speed in the roster.",
                Color.White,
                288f,
                1015f,
                4.0f,
                [3.05f, 2.06f, 1.48f, 1.15f, 0.91f],
                3.00f,
                950f,
                5200f,
                2550f,
                7800f,
                0.24f,
                CreateWheelDefinitions(1.04f, -1.70f, 1.90f, 0.44f, 0.24f, 0.26f, 0.27f, 0.23f, 0.27f, 0.23f),
                new ArcadeVehicleTuningProfile(
                    18.5f,
                    13.0f,
                    12f,
                    1.68f,
                    17.5f),
                new SimulationVehicleTuningProfile(
                    1015f,
                    12f,
                    7400f,
                    4000f,
                    8900f,
                    65f,
                    12f,
                    2550f,
                    27000f,
                    3400f,
                    1.08f,
                    0.12f,
                    3.05f,
                    2860f,
                    8.0f,
                    9.2f,
                    11.5f,
                    9.0f,
                    13.0f,
                    0.56f)),
            CreateCarProfile(
                "car-2",
                "Car 2",
                "Shorter gearing with the strongest launch, but more weight to settle.",
                Color.CornflowerBlue,
                275f,
                1175f,
                6.0f,
                [3.32f, 2.24f, 1.64f, 1.28f, 1.03f],
                3.08f,
                950f,
                6400f,
                3300f,
                7700f,
                0.20f,
                CreateWheelDefinitions(1.06f, -1.66f, 1.86f, 0.42f, 0.24f, 0.26f, 0.28f, 0.22f, 0.29f, 0.21f),
                new ArcadeVehicleTuningProfile(
                    24.0f,
                    15.0f,
                    12f,
                    1.60f,
                    19.0f),
                new SimulationVehicleTuningProfile(
                    1175f,
                    12f,
                    8450f,
                    4600f,
                    9600f,
                    75f,
                    8f,
                    2500f,
                    30000f,
                    3800f,
                    1.05f,
                    0.13f,
                    3.35f,
                    3480f,
                    8.8f,
                    9.8f,
                    12.5f,
                    9.5f,
                    14.0f,
                    0.52f)),
            CreateCarProfile(
                "car-3",
                "Car 3",
                "Lightweight chassis with the quickest rotation and the best grip reserve.",
                Color.OrangeRed,
                240f,
                875f,
                5.0f,
                [3.20f, 2.18f, 1.58f, 1.19f, 0.95f],
                2.98f,
                950f,
                6700f,
                3000f,
                7900f,
                0.18f,
                CreateWheelDefinitions(1.02f, -1.72f, 1.82f, 0.50f, 0.23f, 0.27f, 0.26f, 0.24f, 0.25f, 0.25f),
                new ArcadeVehicleTuningProfile(
                    21.0f,
                    14.5f,
                    12f,
                    1.88f,
                    16.5f),
                new SimulationVehicleTuningProfile(
                    875f,
                    12f,
                    7800f,
                    4300f,
                    9000f,
                    60f,
                    70f,
                    3250f,
                    25500f,
                    3250f,
                    1.22f,
                    0.11f,
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

    private static CarPerformanceProfile CreateCarProfile(
        string id,
        string name,
        string summary,
        Color accentColor,
        float targetTopSpeedMph,
        float legacyMassKilograms,
        float legacyMaxAccelerationPerSecond,
        IReadOnlyList<float> forwardGearRatios,
        float reverseGearRatio,
        float idleRpm,
        float upshiftRpm,
        float downshiftRpm,
        float redlineRpm,
        float shiftDurationSeconds,
        VehicleWheelDefinition[] wheelDefinitions,
        ArcadeVehicleTuningProfile arcade,
        SimulationVehicleTuningProfile simulation)
    {
        VehicleTransmissionDefinition transmissionDefinition = CreateTransmissionDefinitionAlignedToSimulationTopSpeed(
            targetTopSpeedMph,
            wheelDefinitions,
            forwardGearRatios,
            reverseGearRatio,
            idleRpm,
            upshiftRpm,
            downshiftRpm,
            redlineRpm,
            shiftDurationSeconds);

        return new CarPerformanceProfile(
            id,
            name,
            summary,
            accentColor,
            targetTopSpeedMph,
            legacyMassKilograms,
            legacyMaxAccelerationPerSecond,
            transmissionDefinition,
            wheelDefinitions,
            arcade,
            simulation);
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

    private static VehicleTransmissionDefinition CreateTransmissionDefinitionAlignedToSimulationTopSpeed(
        float targetTopSpeedMph,
        IReadOnlyList<VehicleWheelDefinition> wheelDefinitions,
        IReadOnlyList<float> forwardGearRatios,
        float reverseGearRatio,
        float idleRpm,
        float upshiftRpm,
        float downshiftRpm,
        float redlineRpm,
        float shiftDurationSeconds)
    {
        if (targetTopSpeedMph <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTopSpeedMph));
        }

        float finalDriveRatio = VehicleTransmissionLogic.ComputeFinalDriveRatioForTargetForwardSpeed(
            forwardGearRatios,
            forwardGearRatios.Count,
            VehicleSpeedCalibration.ComputeForwardSpeedUnitsPerSecond(targetTopSpeedMph, VehicleDrivingMode.Simulation),
            redlineRpm,
            wheelDefinitions);

        return new VehicleTransmissionDefinition(
            forwardGearRatios,
            reverseGearRatio,
            finalDriveRatio,
            idleRpm,
            upshiftRpm,
            downshiftRpm,
            redlineRpm,
            shiftDurationSeconds);
    }

    private static VehicleWheelDefinition[] CreateWheelDefinitions(
        float sideOffset,
        float frontOffset,
        float rearOffset,
        float frontSteering,
        float frontDriveRatio,
        float rearDriveRatio,
        float frontBrakeRatio,
        float rearBrakeRatio,
        float frontLoadRatio,
        float rearLoadRatio)
    {
        const float wheelRadius = 0.43f;
        const float restLength = 0.42f;
        const float travel = 0.22f;
        const float attachmentHeight = wheelRadius + restLength;

        return
        [
            new VehicleWheelDefinition(VehicleWheelSlot.FrontLeft, "WheelFrontLeft", new Vector3(sideOffset, attachmentHeight, frontOffset), wheelRadius, restLength, travel, frontSteering, frontDriveRatio, frontBrakeRatio, frontLoadRatio),
            new VehicleWheelDefinition(VehicleWheelSlot.FrontRight, "WheelFrontRight", new Vector3(-sideOffset, attachmentHeight, frontOffset), wheelRadius, restLength, travel, frontSteering, frontDriveRatio, frontBrakeRatio, frontLoadRatio),
            new VehicleWheelDefinition(VehicleWheelSlot.RearLeft, "WheelBackLeft", new Vector3(sideOffset, attachmentHeight, rearOffset), wheelRadius, restLength, travel, 0f, rearDriveRatio, rearBrakeRatio, rearLoadRatio),
            new VehicleWheelDefinition(VehicleWheelSlot.RearRight, "WheelBackRight", new Vector3(-sideOffset, attachmentHeight, rearOffset), wheelRadius, restLength, travel, 0f, rearDriveRatio, rearBrakeRatio, rearLoadRatio),
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