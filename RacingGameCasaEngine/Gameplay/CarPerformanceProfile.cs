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
        Arcade = arcade;
        Simulation = simulation;
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
    float MaxForwardSpeedUnitsPerSecond,
    float MaxReverseSpeedUnitsPerSecond,
    float TurnRateRadiansPerSecond,
    float IdleDeceleration);

internal sealed record SimulationVehicleTuningProfile(
    float ChassisMass,
    float MaxForwardSpeedUnitsPerSecond,
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
    float FallbackSteeringRateRadiansPerSecond);