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

    /// <summary>
    /// The car colours of the author's capture of the original game's car selection (ADR-0009): a hue ring from orange
    /// to gold, the median of each colour square of the capture. The repository's RacingGame code had White, Yellow,
    /// Blue, Purple, Red, Green, Teal, Gray, Chocolate, MonoGameOrange and SeaGreen instead.
    /// </summary>
    public static IReadOnlyList<ColorOption> CarColors { get; } =
    [
        new("Orange", new Color(254, 94, 0)),
        new("Red", new Color(255, 29, 58)),
        new("Pink", new Color(255, 34, 171)),
        new("Purple", new Color(180, 28, 250)),
        new("Indigo", new Color(66, 29, 255)),
        new("Blue", new Color(0, 107, 255)),
        new("Cyan", new Color(0, 188, 210)),
        new("Spring Green", new Color(0, 221, 104)),
        new("Green", new Color(44, 222, 2)),
        new("Lime", new Color(155, 233, 0)),
        new("Gold", new Color(252, 181, 1)),
    ];

    public static IReadOnlyList<TrackDefinition> Tracks { get; } =
    [
        new("Beginner", "Short forgiving layout for the first race slice.", "2 laps", "Wide turns"),
        new("Advanced", "Faster flow with tighter sequencing and more braking.", "3 laps", "Mixed corners"),
        new("Expert", "High-speed route intended for the final migration target.", "4 laps", "Technical apexes"),
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
            .Select((profile, index) => new CarDefinition(
                profile.Name,
                profile.Summary,
                profile.AccentColor,
                profile,
                OriginalCarSelection.CreateBars(index)))
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
    CarSelectionBars SelectionBars);

/// <summary>
/// What RacingGame's car selection showed for a car: its top speed in mph and its six property bar values, where 1 is a
/// bar of 192 units of the 1024-unit layout (values can go past 1).
/// </summary>
internal sealed record CarSelectionBars(int MaxSpeedMph, float MaxSpeed, float Acceleration, float Mass, float Braking, float Friction, float Engine);

/// <summary>
/// RacingGame's car selection figures (<c>git show 4f840a3^:RacingGame.Shared/GameScreens/CarSelection.cs:19-53,
/// 229-245</c>) and the CarPhysics constants they use (<c>GameLogic/CarPhysics.cs:24, 38-39, 49, 92-94</c>), computed in
/// float as there. They are what the screen shows; the race keeps the catalogue's car profiles.
/// </summary>
internal static class OriginalCarSelection
{
    private const float MeterPerSecToMph = 1.609344f * ((60.0f * 60.0f) / 1000.0f);
    private const float MphToMeterPerSec = 1.0f / MeterPerSecToMph;
    private const float DefaultMaxSpeed = 275.0f * MphToMeterPerSec;
    private const float DefaultCarMass = 1000;
    private const float DefaultMaxAccelerationPerSec = 2.5f;

    private static readonly float[] CarTypeMaxSpeed = [DefaultMaxSpeed * 1.05f, DefaultMaxSpeed, DefaultMaxSpeed * 0.88f];
    private static readonly float[] CarTypeMass = [DefaultCarMass * 1.015f, DefaultCarMass * 1.175f, DefaultCarMass * 0.875f];
    private static readonly float[] CarTypeMaxAcceleration = [DefaultMaxAccelerationPerSec * 0.85f, DefaultMaxAccelerationPerSec * 1.2f, DefaultMaxAccelerationPerSec];

    public static CarSelectionBars CreateBars(int carIndex)
    {
        int car = Math.Clamp(carIndex, 0, CarTypeMaxSpeed.Length - 1);
        float maxSpeed = -1.5f + 2.45f * (CarTypeMaxSpeed[car] / DefaultMaxSpeed);
        float acceleration = -1.25f + 1.85f * (CarTypeMaxAcceleration[car] / DefaultMaxAccelerationPerSec);
        float mass = -0.65f + 1.5f * (CarTypeMass[car] / DefaultCarMass);
        float braking = -0.2f + acceleration - mass + maxSpeed;
        float friction = -1 + (1 / mass + maxSpeed / 5);
        float engine = -0.2f + 0.5f * (maxSpeed / mass + acceleration - maxSpeed * 5 + 5);
        if (engine > 0.95f)
        {
            engine = 0.95f;
        }

        return new CarSelectionBars((int)(CarTypeMaxSpeed[car] / MphToMeterPerSec), maxSpeed, acceleration, mass, braking, friction, engine);
    }
}

internal sealed record ColorOption(string Name, Color Value);

internal sealed record TrackDefinition(string Name, string Summary, string Laps, string Surface);

internal sealed record HighscoreEntry(string PlayerName, string Time);