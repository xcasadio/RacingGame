using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework;
using CasaEngine.Framework.Gameplay;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Gameplay;

namespace RacingGameCasaEngine.Components;

public sealed class VehicleDynamicsComponent : EntityComponent
{
    private VehicleTransmissionDefinition _transmissionDefinition;
    private readonly VehicleTransmissionRuntimeState _transmissionState = new();
    private VehicleWheelDefinition[] _wheelDefinitions;
    private VehicleWheelRuntimeState[] _wheelStates;
    private readonly VehicleTelemetrySnapshot _telemetry = new();
    private readonly VehicleChassisRuntimeState _chassisState = new();
    private readonly IVehicleDynamicsSolver _arcadeSolver = new ArcadeVehicleDynamicsSolver();
    private readonly IVehicleDynamicsSolver _simulationSolver = new SimulationVehicleDynamicsSolver();

    private RaceTrackPhysicsComponent? _trackPhysicsComponent;
    private World? _trackPhysicsWorld;
    private CarPerformanceProfile? _activeProfile;
    private VehicleDrivingMode _activeDrivingMode = VehicleDrivingMode.Arcade;
    private bool _runtimeInitialized;

    public VehicleDynamicsComponent()
    {
        _transmissionDefinition = VehicleTransmissionLogic.CreateDefaultFiveSpeedDefinition();
        _wheelDefinitions = CreateFallbackWheelDefinitions();
        _wheelStates = CreateWheelStates(_wheelDefinitions);
    }

    private VehicleDynamicsComponent(VehicleDynamicsComponent other)
        : base(other)
    {
        _transmissionDefinition = VehicleTransmissionLogic.CreateDefaultFiveSpeedDefinition();
        _wheelDefinitions = CreateFallbackWheelDefinitions();
        _wheelStates = CreateWheelStates(_wheelDefinitions);
    }

    internal VehicleDrivingMode ActiveDrivingMode => _activeDrivingMode;

    internal CarPerformanceProfile? ActiveProfile => _activeProfile;

    internal VehicleTelemetrySnapshot Telemetry => _telemetry;

    internal VehicleTransmissionDefinition TransmissionDefinition => _transmissionDefinition;

    internal VehicleTransmissionRuntimeState TransmissionState => _transmissionState;

    internal VehicleChassisRuntimeState ChassisState => _chassisState;

    internal IReadOnlyList<VehicleWheelDefinition> WheelDefinitions => _wheelDefinitions;

    internal IReadOnlyList<VehicleWheelRuntimeState> WheelStates => _wheelStates;

    public override EntityComponent Clone()
    {
        return new VehicleDynamicsComponent(this);
    }

    public override void Update(float elapsedTime)
    {
        if (Owner is not RacingCarPawn pawn
            || pawn.RootComponent == null)
        {
            return;
        }

        RuntimeRaceSession? session = (pawn.World?.Game as RacingGameCasaEngineGame)?.RaceSession;
        RaceTrackPhysicsComponent? trackPhysics = ResolveTrackPhysics(pawn.World, session);
        bool profileChanged = BindProfile(pawn, session);
        bool wasInitialized = _runtimeInitialized;
        EnsureRuntimeInitialized(pawn, trackPhysics);

        VehicleDrivingMode desiredMode = pawn.DrivingMode;
        bool modeChanged = _activeDrivingMode != desiredMode;
        if (wasInitialized && (modeChanged || profileChanged))
        {
            _activeDrivingMode = desiredMode;
            ResetRuntimeFromPawn(pawn, trackPhysics);
            GetSolver(desiredMode).Reset(CreateContext(pawn, 0f, VehicleControlInput.Zero, trackPhysics, session));
            if (profileChanged && _activeProfile != null)
            {
                session?.AppendMovementDebug("profile", $"vehicle profile switched to {_activeProfile.Id}.");
            }

            if (modeChanged)
            {
                session?.AppendMovementDebug("mode", $"vehicle driving mode switched to {_activeDrivingMode}.");
            }
        }

        if (pawn.Controller is not RacingPlayerController controller
            || !pawn.InputEnabled
            || !controller.IsInputEnable)
        {
            SyncPawnCompatibility(pawn);
            return;
        }

        VehicleControlInput controlInput;
        if (pawn.ForcedControlInput.HasValue)
        {
            controlInput = pawn.ForcedControlInput.Value;
        }
        else
        {
            CasaEngine.Framework.Input.InputComponent? input = pawn.World?.Game?.InputComponent;
            if (input == null)
            {
                SyncPawnCompatibility(pawn);
                return;
            }

            controlInput = VehicleInputReader.Read(input, controller);
        }

        VehicleDynamicsExecutionContext context = CreateContext(pawn, elapsedTime, controlInput, trackPhysics, session);
        GetSolver(_activeDrivingMode).Update(context);
        ApplyRuntimeToPawn(pawn);
    }

    internal string BuildDebugSummary()
    {
        string profileId = _activeProfile?.Id ?? "fallback";
        float targetTopSpeedMph = _activeProfile?.TargetTopSpeedMph ?? 0f;
        return $"profile={profileId} mode={_activeDrivingMode} targetMph={_telemetry.CurrentSpeedMph:0.0}/{targetTopSpeedMph:0.0} speed={_telemetry.SpeedUnitsPerSecond:0.000} rpm={_telemetry.EngineRpm:0} fallback={_telemetry.IsFallbackActive} wheels={VehicleDynamicsMath.BuildWheelDebugSummary(_wheelStates)}";
    }

    internal string BuildDetailedDebugReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Summary: {BuildDebugSummary()}");

        if (_activeProfile == null)
        {
            return builder.ToString().TrimEnd();
        }

        CarPerformanceProfile profile = _activeProfile;
        VehicleTransmissionDefinition transmission = _transmissionDefinition;
        ArcadeVehicleTuningProfile arcade = profile.Arcade;
        SimulationVehicleTuningProfile simulation = profile.Simulation;

        float totalDriveRatio = 0f;
        float totalBrakeRatio = 0f;
        float totalStaticLoadRatio = 0f;
        float weightedDrivenWheelRadius = 0f;
        for (int index = 0; index < _wheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = _wheelDefinitions[index];
            totalDriveRatio += definition.DriveForceRatio;
            totalBrakeRatio += definition.BrakeForceRatio;
            totalStaticLoadRatio += definition.StaticLoadRatio;
            weightedDrivenWheelRadius += definition.Radius * definition.DriveForceRatio;
        }

        float averageDrivenWheelRadius = totalDriveRatio > 0.0001f
            ? weightedDrivenWheelRadius / totalDriveRatio
            : 0f;

        builder.AppendLine($"Profile: id={profile.Id} name='{profile.Name}' targetTopSpeedMph={profile.TargetTopSpeedMph:0.0} legacyMassKg={profile.LegacyMassKilograms:0.0} legacyAccel={profile.LegacyMaxAccelerationPerSecond:0.00}");
        builder.AppendLine($"Transmission: forward={BuildForwardGearRatioSummary(transmission)} reverse={transmission.ReverseGearRatio.ToString("0.00", CultureInfo.InvariantCulture)} finalDrive={transmission.FinalDriveRatio.ToString("0.00", CultureInfo.InvariantCulture)} idleRpm={transmission.IdleRpm:0} upshiftRpm={transmission.UpshiftRpm:0} downshiftRpm={transmission.DownshiftRpm:0} redlineRpm={transmission.RedlineRpm:0}");
        builder.AppendLine($"WheelModel: drivenRadius={averageDrivenWheelRadius.ToString("0.000", CultureInfo.InvariantCulture)} driveRatioSum={totalDriveRatio.ToString("0.00", CultureInfo.InvariantCulture)} brakeRatioSum={totalBrakeRatio.ToString("0.00", CultureInfo.InvariantCulture)} staticLoadRatioSum={totalStaticLoadRatio.ToString("0.00", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"ArcadeTuning: accel={arcade.ForwardAcceleration.ToString("0.00", CultureInfo.InvariantCulture)} reverseAccel={arcade.ReverseAcceleration.ToString("0.00", CultureInfo.InvariantCulture)} maxUnits={arcade.MaxForwardSpeedUnitsPerSecond.ToString("0.00", CultureInfo.InvariantCulture)} idleDecel={arcade.IdleDeceleration.ToString("0.00", CultureInfo.InvariantCulture)} turnRate={arcade.TurnRateRadiansPerSecond.ToString("0.00", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"SimulationTuning: mass={simulation.ChassisMass.ToString("0.00", CultureInfo.InvariantCulture)} maxUnits={simulation.MaxForwardSpeedUnitsPerSecond.ToString("0.00", CultureInfo.InvariantCulture)} maxDrive={simulation.MaxDriveForce.ToString("0.00", CultureInfo.InvariantCulture)} reverseDrive={simulation.MaxReverseDriveForce.ToString("0.00", CultureInfo.InvariantCulture)} brake={simulation.MaxBrakeForce.ToString("0.00", CultureInfo.InvariantCulture)} longDamp={simulation.LongitudinalDamping.ToString("0.00", CultureInfo.InvariantCulture)} linearDrag={simulation.LinearDrag.ToString("0.000", CultureInfo.InvariantCulture)} lateralGrip={simulation.LateralGrip.ToString("0.00", CultureInfo.InvariantCulture)} tireGripScale={simulation.TireGripScale.ToString("0.00", CultureInfo.InvariantCulture)}");

        builder.AppendLine("GearRedline:");
        for (int gear = 1; gear <= transmission.ForwardGearCount; gear++)
        {
            float redlineSpeedUnits = VehicleTransmissionLogic.ComputeForwardSpeedUnitsAtEngineRpm(transmission, gear, transmission.RedlineRpm, _wheelDefinitions);
            float redlineMph = VehicleSpeedCalibration.ConvertSpeedUnitsToDisplayMph(redlineSpeedUnits, VehicleDrivingMode.Simulation);
            builder.AppendLine($"  G{gear}: redlineUnits={redlineSpeedUnits.ToString("0.00", CultureInfo.InvariantCulture)} redlineMph={redlineMph.ToString("0.0", CultureInfo.InvariantCulture)}");
        }

        builder.AppendLine("SimulationFullThrottleEstimate:");
        for (int gear = 1; gear <= transmission.ForwardGearCount; gear++)
        {
            SimulationGearEstimate estimate = EstimateSimulationGearEquilibrium(profile, transmission, _wheelDefinitions, gear, totalDriveRatio, totalStaticLoadRatio);
            builder.AppendLine($"  G{gear}: eqUnits={estimate.SpeedUnitsPerSecond.ToString("0.00", CultureInfo.InvariantCulture)} eqMph={estimate.DisplayMph.ToString("0.0", CultureInfo.InvariantCulture)} rpm={estimate.EngineRpm.ToString("0", CultureInfo.InvariantCulture)} drive={estimate.DriveForce.ToString("0", CultureInfo.InvariantCulture)} resist={estimate.ResistiveForce.ToString("0", CultureInfo.InvariantCulture)} net={estimate.NetForce.ToString("0", CultureInfo.InvariantCulture)}");
        }

        return builder.ToString().TrimEnd();
    }

    private VehicleDynamicsExecutionContext CreateContext(
        RacingCarPawn pawn,
        float elapsedTime,
        VehicleControlInput input,
        RaceTrackPhysicsComponent? trackPhysics,
        RuntimeRaceSession? session)
    {
        CarPerformanceProfile profile = _activeProfile ?? ResolveProfile(pawn);
        return new VehicleDynamicsExecutionContext(
            pawn,
            elapsedTime,
            input,
            trackPhysics,
            session,
            profile,
            _telemetry,
            _transmissionDefinition,
            _transmissionState,
            _chassisState,
            _wheelDefinitions,
            _wheelStates);
    }

    private void EnsureRuntimeInitialized(RacingCarPawn pawn, RaceTrackPhysicsComponent? trackPhysics)
    {
        if (_runtimeInitialized)
        {
            return;
        }

        ResetRuntimeFromPawn(pawn, trackPhysics);
        GetSolver(pawn.DrivingMode).Reset(CreateContext(pawn, 0f, VehicleControlInput.Zero, trackPhysics, (pawn.World?.Game as RacingGameCasaEngineGame)?.RaceSession));
        _activeDrivingMode = pawn.DrivingMode;
        _runtimeInitialized = true;
    }

    private void ResetRuntimeFromPawn(RacingCarPawn pawn, RaceTrackPhysicsComponent? trackPhysics)
    {
        CarPerformanceProfile profile = _activeProfile ?? ResolveProfile(pawn);
        SceneComponent rootComponent = pawn.RootComponent!;
        _chassisState.Position = rootComponent.LocalPosition;
        _chassisState.Orientation = rootComponent.LocalOrientation;
        _chassisState.LinearVelocity = Vector3.Zero;
        _chassisState.AngularVelocity = Vector3.Zero;
        _chassisState.MovementForward = VehicleDynamicsMath.NormalizeOrFallback(rootComponent.Forward, Vector3.Forward);
        _chassisState.SurfaceUp = VehicleDynamicsMath.NormalizeOrFallback(rootComponent.Up, Vector3.Up);
        _chassisState.Mass = profile.Simulation.ChassisMass;
        _chassisState.SurfaceSegmentHint = trackPhysics == null ? -1 : 0;
        _chassisState.HasValidSurface = false;

        _telemetry.DrivingMode = pawn.DrivingMode;
        _telemetry.SpeedUnitsPerSecond = 0f;
        _telemetry.CurrentSpeedMph = 0f;
        _telemetry.SteeringInput = 0f;
        _telemetry.TachometerAcceleration = 0f;
        _telemetry.CurrentGear = 1;
        _telemetry.NormalizedSpeed = 0f;
        _telemetry.EngineRpm = _transmissionDefinition.IdleRpm;
        _telemetry.MovementForward = _chassisState.MovementForward;
        _telemetry.SurfaceUp = _chassisState.SurfaceUp;
        _telemetry.IsFallbackActive = false;
        _telemetry.CurrentSpeedMph = 0f;

        VehicleTransmissionLogic.Reset(_transmissionState, _transmissionDefinition);

        for (int index = 0; index < _wheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = _wheelDefinitions[index];
            VehicleWheelRuntimeState state = _wheelStates[index];
            state.SurfaceSegmentHint = _chassisState.SurfaceSegmentHint;
            state.AttachmentPointWorld = _chassisState.Position + VehicleDynamicsMath.TransformLocalOffset(_chassisState.Orientation, definition.LocalAttachmentOffset);
            state.RotationAngleRadians = 0f;
            VehicleDynamicsMath.ClearWheelState(definition, state);
        }

        SyncPawnCompatibility(pawn);
    }

    private void ApplyRuntimeToPawn(RacingCarPawn pawn)
    {
        SceneComponent rootComponent = pawn.RootComponent!;
        rootComponent.LocalPosition = _chassisState.Position;
        rootComponent.LocalOrientation = Quaternion.Normalize(_chassisState.Orientation);
        SyncPawnCompatibility(pawn);
    }

    private void SyncPawnCompatibility(RacingCarPawn pawn)
    {
        if (_activeProfile != null)
        {
            pawn.CarProfile = _activeProfile;
            pawn.CarLabel = _activeProfile.Name;
            pawn.TargetTopSpeedMph = _activeProfile.TargetTopSpeedMph;
        }

        pawn.CurrentSpeedMph = _telemetry.CurrentSpeedMph;
        pawn.SteeringInput = _telemetry.SteeringInput;
        pawn.TachometerAcceleration = _telemetry.TachometerAcceleration;
        pawn.CurrentGear = _telemetry.CurrentGear;
    }

    private bool BindProfile(RacingCarPawn pawn, RuntimeRaceSession? session)
    {
        CarPerformanceProfile profile = ResolveProfile(pawn);
        pawn.CarProfile = profile;
        pawn.CarLabel = profile.Name;
        pawn.TargetTopSpeedMph = profile.TargetTopSpeedMph;

        if (ReferenceEquals(_activeProfile, profile))
        {
            return false;
        }

        _activeProfile = profile;
        _transmissionDefinition = profile.TransmissionDefinition;
        _wheelDefinitions = profile.WheelDefinitions;
        if (_wheelStates.Length != _wheelDefinitions.Length)
        {
            _wheelStates = CreateWheelStates(_wheelDefinitions);
        }

        session?.AppendMovementDebug(
            "profile",
            $"vehicle dynamics bound profile={profile.Id} targetTopSpeedMph={profile.TargetTopSpeedMph:0} massKg={profile.Simulation.ChassisMass:0}");
        return true;
    }

    private static CarPerformanceProfile ResolveProfile(RacingCarPawn pawn)
    {
        return pawn.CarProfile ?? RaceFrontEndCatalog.ResolveCarProfile(pawn.SelectedCarIndex);
    }

    private IVehicleDynamicsSolver GetSolver(VehicleDrivingMode mode)
    {
        return mode == VehicleDrivingMode.Simulation ? _simulationSolver : _arcadeSolver;
    }

    private RaceTrackPhysicsComponent? ResolveTrackPhysics(World? world, RuntimeRaceSession? session)
    {
        if (world == null)
        {
            _trackPhysicsComponent = null;
            _trackPhysicsWorld = null;
            _runtimeInitialized = false;
            return null;
        }

        if (!ReferenceEquals(world, _trackPhysicsWorld))
        {
            _trackPhysicsWorld = world;
            _trackPhysicsComponent = null;
            _runtimeInitialized = false;
            session?.AppendMovementDebug("world", $"bound vehicle dynamics to world type={world.GetType().Name}");

            foreach (Entity entity in world.Entities)
            {
                RaceTrackPhysicsComponent? component = entity.GetComponent<RaceTrackPhysicsComponent>();
                if (component != null)
                {
                    _trackPhysicsComponent = component;
                    break;
                }
            }

            session?.AppendMovementDebug(
                "track",
                _trackPhysicsComponent == null
                    ? "race track physics component not found"
                    : $"race track physics component resolved shoulderWidth={_trackPhysicsComponent.ShoulderWidth:0.000} guardRailInset={_trackPhysicsComponent.GuardRailInset:0.000}");
        }

        return _trackPhysicsComponent;
    }

    private static VehicleWheelDefinition[] CreateFallbackWheelDefinitions()
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

    private static VehicleWheelRuntimeState[] CreateWheelStates(VehicleWheelDefinition[] wheelDefinitions)
    {
        var wheelStates = new VehicleWheelRuntimeState[wheelDefinitions.Length];
        for (int index = 0; index < wheelDefinitions.Length; index++)
        {
            wheelStates[index] = new VehicleWheelRuntimeState(wheelDefinitions[index].Slot);
        }

        return wheelStates;
    }

    private static string BuildForwardGearRatioSummary(VehicleTransmissionDefinition transmission)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < transmission.ForwardGearRatios.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('/');
            }

            builder.Append(transmission.ForwardGearRatios[index].ToString("0.00", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static SimulationGearEstimate EstimateSimulationGearEquilibrium(
        CarPerformanceProfile profile,
        VehicleTransmissionDefinition transmission,
        IReadOnlyList<VehicleWheelDefinition> wheelDefinitions,
        int gear,
        float totalDriveRatio,
        float totalStaticLoadRatio)
    {
        SimulationVehicleTuningProfile tuning = profile.Simulation;
        float gearRedlineSpeedUnits = VehicleTransmissionLogic.ComputeForwardSpeedUnitsAtEngineRpm(
            transmission,
            gear,
            transmission.RedlineRpm,
            wheelDefinitions);
        float bestSpeedUnits = 0f;
        float bestDisplayMph = 0f;
        float bestEngineRpm = transmission.IdleRpm;
        float bestDriveForce = 0f;
        float bestResistiveForce = 0f;
        float bestNetForce = float.MaxValue;
        int maxSampleIndex = Math.Max(1, (int)MathF.Ceiling(gearRedlineSpeedUnits * 10f));

        for (int sampleIndex = 0; sampleIndex <= maxSampleIndex; sampleIndex++)
        {
            float speedUnits = Math.Min(sampleIndex * 0.1f, gearRedlineSpeedUnits);
            float drivenWheelAngularSpeed = VehicleTransmissionLogic.ComputeDrivenWheelAngularSpeed(wheelDefinitions, speedUnits);
            VehicleTransmissionFrame frame = VehicleTransmissionLogic.EvaluateForwardFrame(transmission, gear, drivenWheelAngularSpeed, 1f);
            float driveForce = tuning.MaxDriveForce * frame.DriveForceScale * totalDriveRatio;
            float resistiveForce = (speedUnits * tuning.LongitudinalDamping * totalStaticLoadRatio)
                + (Math.Abs(speedUnits) * tuning.ChassisMass * tuning.LinearDrag);
            float netForce = driveForce - resistiveForce;

            if (Math.Abs(netForce) >= Math.Abs(bestNetForce))
            {
                continue;
            }

            bestNetForce = netForce;
            bestSpeedUnits = speedUnits;
            bestDisplayMph = VehicleSpeedCalibration.ConvertSpeedUnitsToDisplayMph(speedUnits, VehicleDrivingMode.Simulation);
            bestEngineRpm = frame.EngineRpm;
            bestDriveForce = driveForce;
            bestResistiveForce = resistiveForce;
        }

        return new SimulationGearEstimate(gear, bestSpeedUnits, bestDisplayMph, bestEngineRpm, bestDriveForce, bestResistiveForce, bestNetForce);
    }

    private readonly record struct SimulationGearEstimate(
        int Gear,
        float SpeedUnitsPerSecond,
        float DisplayMph,
        float EngineRpm,
        float DriveForce,
        float ResistiveForce,
        float NetForce);
}