using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;

namespace RacingGameCasaEngine.Components;

internal enum VehicleDrivingMode
{
    Arcade = 0,
    Simulation = 1,
}

internal enum VehicleWheelSlot
{
    FrontLeft = 0,
    FrontRight = 1,
    RearLeft = 2,
    RearRight = 3,
}

internal readonly record struct VehicleControlInput(float Throttle, float Steering)
{
    public static VehicleControlInput Zero => new(0f, 0f);
}

internal sealed class VehicleTelemetrySnapshot
{
    public VehicleDrivingMode DrivingMode { get; set; } = VehicleDrivingMode.Arcade;

    public float SpeedUnitsPerSecond { get; set; }

    public float CurrentSpeedMph { get; set; }

    public float SteeringInput { get; set; }

    public float TachometerAcceleration { get; set; }

    public int CurrentGear { get; set; } = 1;

    public float NormalizedSpeed { get; set; }

    public float EngineRpm { get; set; } = 1000f;

    public Vector3 MovementForward { get; set; } = Vector3.Forward;

    public Vector3 SurfaceUp { get; set; } = Vector3.Up;

    public bool IsFallbackActive { get; set; }
}

internal sealed class VehicleTransmissionDefinition
{
    public VehicleTransmissionDefinition(
        IReadOnlyList<float> forwardGearRatios,
        float reverseGearRatio,
        float finalDriveRatio,
        float idleRpm,
        float upshiftRpm,
        float downshiftRpm,
        float redlineRpm,
        float shiftDurationSeconds)
    {
        if (forwardGearRatios.Count == 0)
        {
            throw new ArgumentException("At least one forward gear ratio is required.", nameof(forwardGearRatios));
        }

        ForwardGearRatios = forwardGearRatios;
        ReverseGearRatio = reverseGearRatio;
        FinalDriveRatio = finalDriveRatio;
        IdleRpm = idleRpm;
        UpshiftRpm = upshiftRpm;
        DownshiftRpm = downshiftRpm;
        RedlineRpm = redlineRpm;
        ShiftDurationSeconds = shiftDurationSeconds;
    }

    public IReadOnlyList<float> ForwardGearRatios { get; }

    public float ReverseGearRatio { get; }

    public float FinalDriveRatio { get; }

    public float IdleRpm { get; }

    public float UpshiftRpm { get; }

    public float DownshiftRpm { get; }

    public float RedlineRpm { get; }

    public float ShiftDurationSeconds { get; }

    public int ForwardGearCount => ForwardGearRatios.Count;

    public float FirstGearRatio => ForwardGearRatios[0];

    public float TopGearRatio => ForwardGearRatios[ForwardGearRatios.Count - 1];

    public float GetForwardGearRatio(int gear)
    {
        if (gear < 1 || gear > ForwardGearRatios.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(gear));
        }

        return ForwardGearRatios[gear - 1];
    }
}

internal sealed class VehicleTransmissionRuntimeState
{
    public int CurrentGear { get; set; } = 1;

    public float EngineRpm { get; set; } = 950f;

    public float ShiftTimerSeconds { get; set; }

    public float NormalizedRpm { get; set; }

    public bool IsShifting => ShiftTimerSeconds > 0.0001f;
}

internal readonly record struct VehicleTransmissionFrame(
    int Gear,
    float EngineRpm,
    float NormalizedRpm,
    float DriveForceScale,
    float ShiftTorqueScale,
    bool GearChanged,
    bool IsShifting);

internal sealed class VehicleChassisRuntimeState
{
    public Vector3 Position { get; set; }

    public Quaternion Orientation { get; set; } = Quaternion.Identity;

    public Vector3 LinearVelocity { get; set; }

    public Vector3 AngularVelocity { get; set; }

    public Vector3 MovementForward { get; set; } = Vector3.Forward;

    public Vector3 SurfaceUp { get; set; } = Vector3.Up;

    public float Mass { get; set; } = 1325f;

    public int SurfaceSegmentHint { get; set; } = -1;

    public bool HasValidSurface { get; set; }
}

internal sealed class VehicleWheelDefinition
{
    public VehicleWheelDefinition(
        VehicleWheelSlot slot,
        string visualFrameName,
        Vector3 localAttachmentOffset,
        float radius,
        float suspensionRestLength,
        float suspensionTravel,
        float maxSteeringAngleRadians,
        float driveForceRatio,
        float brakeForceRatio,
        float staticLoadRatio)
    {
        Slot = slot;
        VisualFrameName = visualFrameName;
        LocalAttachmentOffset = localAttachmentOffset;
        Radius = radius;
        SuspensionRestLength = suspensionRestLength;
        SuspensionTravel = suspensionTravel;
        MaxSteeringAngleRadians = maxSteeringAngleRadians;
        DriveForceRatio = driveForceRatio;
        BrakeForceRatio = brakeForceRatio;
        StaticLoadRatio = staticLoadRatio;
    }

    public VehicleWheelSlot Slot { get; }

    public string VisualFrameName { get; }

    public Vector3 LocalAttachmentOffset { get; }

    public float Radius { get; }

    public float SuspensionRestLength { get; }

    public float SuspensionTravel { get; }

    public float MaxSteeringAngleRadians { get; }

    public float DriveForceRatio { get; }

    public float BrakeForceRatio { get; }

    public float StaticLoadRatio { get; }

    public bool CanSteer => MaxSteeringAngleRadians > 0.0001f;
}

internal sealed class VehicleWheelRuntimeState
{
    public VehicleWheelRuntimeState(VehicleWheelSlot slot)
    {
        Slot = slot;
    }

    public VehicleWheelSlot Slot { get; }

    public bool HasContact { get; set; }

    public bool IsFallbackContact { get; set; }

    public int SurfaceSegmentHint { get; set; } = -1;

    public Vector3 AttachmentPointWorld { get; set; }

    public Vector3 ContactPointWorld { get; set; }

    public Vector3 ContactNormal { get; set; } = Vector3.Up;

    public Vector3 ContactForward { get; set; } = Vector3.Forward;

    public float SuspensionLength { get; set; }

    public float SuspensionCompression { get; set; }

    public float SuspensionCompressionVelocity { get; set; }

    public float NormalizedCompression { get; set; }

    public float SteeringAngleRadians { get; set; }

    public float RotationAngleRadians { get; set; }

    public float RotationSpeedRadiansPerSecond { get; set; }

    public float SlipRatio { get; set; }

    public float SlipAngleRadians { get; set; }

    public float ApproximateLoad { get; set; }
}

internal sealed class VehicleDynamicsExecutionContext
{
    public VehicleDynamicsExecutionContext(
        RacingCarPawn pawn,
        float elapsedTime,
        VehicleControlInput input,
        RaceTrackPhysicsComponent? trackPhysics,
        RuntimeRaceSession? session,
        VehicleTelemetrySnapshot telemetry,
        VehicleTransmissionDefinition transmissionDefinition,
        VehicleTransmissionRuntimeState transmissionState,
        VehicleChassisRuntimeState chassis,
        VehicleWheelDefinition[] wheelDefinitions,
        VehicleWheelRuntimeState[] wheelStates)
    {
        Pawn = pawn;
        ElapsedTime = elapsedTime;
        Input = input;
        TrackPhysics = trackPhysics;
        Session = session;
        Telemetry = telemetry;
        TransmissionDefinition = transmissionDefinition;
        TransmissionState = transmissionState;
        Chassis = chassis;
        WheelDefinitions = wheelDefinitions;
        WheelStates = wheelStates;
    }

    public RacingCarPawn Pawn { get; }

    public float ElapsedTime { get; }

    public VehicleControlInput Input { get; }

    public RaceTrackPhysicsComponent? TrackPhysics { get; }

    public RuntimeRaceSession? Session { get; }

    public VehicleTelemetrySnapshot Telemetry { get; }

    public VehicleTransmissionDefinition TransmissionDefinition { get; }

    public VehicleTransmissionRuntimeState TransmissionState { get; }

    public VehicleChassisRuntimeState Chassis { get; }

    public VehicleWheelDefinition[] WheelDefinitions { get; }

    public VehicleWheelRuntimeState[] WheelStates { get; }
}

internal interface IVehicleDynamicsSolver
{
    VehicleDrivingMode DrivingMode { get; }

    void Reset(VehicleDynamicsExecutionContext context);

    void Update(VehicleDynamicsExecutionContext context);
}