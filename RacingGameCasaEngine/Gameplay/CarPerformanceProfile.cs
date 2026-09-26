using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Gameplay;

internal sealed class CarPerformanceProfile
{
    public CarPerformanceProfile(
        string id,
        string name,
        string summary,
        Color accentColor,
        float targetTopSpeedMph,
        float legacyMassKilograms,
        float legacyMaxAccelerationPerSecond,
        VehicleTransmissionDefinition transmissionDefinition,
        VehicleWheelDefinition[] wheelDefinitions,
        ArcadeVehicleTuningProfile arcade,
        SimulationVehicleTuningProfile simulation)
    {
        Id = id;
        Name = name;
        Summary = summary;
        AccentColor = accentColor;
        TargetTopSpeedMph = targetTopSpeedMph;
        LegacyMassKilograms = legacyMassKilograms;
        LegacyMaxAccelerationPerSecond = legacyMaxAccelerationPerSecond;
        TransmissionDefinition = transmissionDefinition;
        WheelDefinitions = wheelDefinitions;
        Arcade = arcade.WithDerivedForwardSpeed(targetTopSpeedMph);
        Simulation = simulation.WithDerivedForwardSpeed(targetTopSpeedMph);
    }

    public string Id { get; }

    public string Name { get; }

    public string Summary { get; }

    public Color AccentColor { get; }

    public float TargetTopSpeedMph { get; }

    public float LegacyMassKilograms { get; }

    public float LegacyMaxAccelerationPerSecond { get; }

    public VehicleTransmissionDefinition TransmissionDefinition { get; }

    public VehicleWheelDefinition[] WheelDefinitions { get; }

    public ArcadeVehicleTuningProfile Arcade { get; }

    public SimulationVehicleTuningProfile Simulation { get; }
}

internal sealed record ArcadeVehicleTuningProfile(
    float ForwardAcceleration,
    float ReverseAcceleration,
    float MaxReverseSpeedUnitsPerSecond,
    float TurnRateRadiansPerSecond,
    float IdleDeceleration)
{
    public float MaxForwardSpeedUnitsPerSecond { get; init; }

    public ArcadeVehicleTuningProfile WithDerivedForwardSpeed(float targetTopSpeedMph)
    {
        return this with
        {
            MaxForwardSpeedUnitsPerSecond = VehicleSpeedCalibration.ComputeForwardSpeedUnitsPerSecond(targetTopSpeedMph, VehicleDrivingMode.Arcade),
        };
    }
}

internal sealed record SimulationVehicleTuningProfile(
    float ChassisMass,
    float MaxReverseSpeedUnitsPerSecond,
    float MaxDriveForce,
    float MaxReverseDriveForce,
    float MaxBrakeForce,
    float RollingResistanceForce,
    float LongitudinalDamping,
    float LateralGrip,
    float SuspensionSpringStrength,
    float SuspensionDamperStrength,
    float TireGripScale,
    float LinearDrag,
    float AngularDrag,
    float ChassisYawInertia,
    float OrientationStabilization,
    float RideHeightCorrection,
    float FallbackForwardAcceleration,
    float FallbackReverseAcceleration,
    float FallbackIdleDeceleration,
    float FallbackSteeringRateRadiansPerSecond)
{
    public float MaxForwardSpeedUnitsPerSecond { get; init; }

    public SimulationVehicleTuningProfile WithDerivedForwardSpeed(float targetTopSpeedMph)
    {
        return this with
        {
            MaxForwardSpeedUnitsPerSecond = VehicleSpeedCalibration.ComputeForwardSpeedUnitsPerSecond(targetTopSpeedMph, VehicleDrivingMode.Simulation),
        };
    }
}

internal static class VehicleSpeedCalibration
{
    private const float ReferenceTopSpeedMph = 275f;
    private const float ArcadeReferenceForwardSpeedUnitsPerSecond = 36f;
    private const float SimulationReferenceForwardSpeedUnitsPerSecond = 38f;

    public static float ComputeForwardSpeedUnitsPerSecond(float targetTopSpeedMph, VehicleDrivingMode drivingMode)
    {
        if (targetTopSpeedMph <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTopSpeedMph));
        }

        return targetTopSpeedMph * GetForwardSpeedUnitsPerMph(drivingMode);
    }

    public static float ConvertSpeedUnitsToDisplayMph(float speedUnitsPerSecond, VehicleDrivingMode drivingMode)
    {
        float unitsPerMph = GetForwardSpeedUnitsPerMph(drivingMode);
        return unitsPerMph <= 0.0001f
            ? 0f
            : Math.Abs(speedUnitsPerSecond) / unitsPerMph;
    }

    private static float GetForwardSpeedUnitsPerMph(VehicleDrivingMode drivingMode)
    {
        return drivingMode == VehicleDrivingMode.Simulation
            ? SimulationReferenceForwardSpeedUnitsPerSecond / ReferenceTopSpeedMph
            : ArcadeReferenceForwardSpeedUnitsPerSecond / ReferenceTopSpeedMph;
    }
}