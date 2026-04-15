using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Components;

internal sealed class SimulationVehicleDynamicsSolver : IVehicleDynamicsSolver
{
    private const float DebugSampleIntervalSeconds = 0.2f;
    private const float MaxForwardSpeedUnitsPerSecond = 38f;
    private const float MaxReverseSpeedUnitsPerSecond = 12f;
    private const float MaxDriveForce = 7600f;
    private const float MaxReverseDriveForce = 4200f;
    private const float MaxBrakeForce = 9200f;
    private const float RollingResistanceForce = 520f;
    private const float LongitudinalDamping = 1900f;
    private const float LateralGrip = 2800f;
    private const float SuspensionSpringStrength = 28500f;
    private const float SuspensionDamperStrength = 3600f;
    private const float TireGripScale = 1.15f;
    private const float LinearDrag = 0.95f;
    private const float AngularDrag = 3.25f;
    private const float ChassisYawInertia = 3250f;
    private const float OrientationStabilization = 8.5f;
    private const float RideHeightCorrection = 9.5f;

    private float _debugElapsedSeconds;
    private float _nextDebugSampleSeconds;
    private float _smoothedTachometerAcceleration;
    private int _lastReportedGear = 1;
    private bool? _lastFallbackState;

    public VehicleDrivingMode DrivingMode => VehicleDrivingMode.Simulation;

    public void Reset(VehicleDynamicsExecutionContext context)
    {
        context.Chassis.Mass = 1325f;
        context.Chassis.LinearVelocity = Vector3.Zero;
        context.Chassis.AngularVelocity = Vector3.Zero;
        context.Chassis.MovementForward = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.MovementForward, Vector3.Forward);
        context.Chassis.SurfaceUp = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.SurfaceUp, Vector3.Up);
        context.Chassis.SurfaceSegmentHint = Math.Max(context.Chassis.SurfaceSegmentHint, 0);
        context.Chassis.HasValidSurface = false;

        for (int index = 0; index < context.WheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = context.WheelDefinitions[index];
            VehicleWheelRuntimeState state = context.WheelStates[index];
            state.SurfaceSegmentHint = context.Chassis.SurfaceSegmentHint;
            state.AttachmentPointWorld = context.Chassis.Position + VehicleDynamicsMath.TransformLocalOffset(context.Chassis.Orientation, definition.LocalAttachmentOffset);
            state.RotationAngleRadians = 0f;
            VehicleDynamicsMath.ClearWheelState(definition, state);
        }

        _debugElapsedSeconds = 0f;
        _nextDebugSampleSeconds = 0f;
        _smoothedTachometerAcceleration = 0f;
        _lastReportedGear = 1;
        _lastFallbackState = null;
        VehicleTransmissionLogic.Reset(context.TransmissionState, context.TransmissionDefinition);
    }

    public void Update(VehicleDynamicsExecutionContext context)
    {
        _debugElapsedSeconds += context.ElapsedTime;

        if (context.TrackPhysics == null)
        {
            ApplyFallback(context, "track physics component unavailable");
            return;
        }

        Quaternion currentOrientation = Quaternion.Normalize(context.Chassis.Orientation);
        Vector3 baseForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(
            VehicleDynamicsMath.GetForward(currentOrientation),
            VehicleDynamicsMath.GetUp(currentOrientation),
            context.Chassis.MovementForward);
        float signedForwardSpeedBefore = Vector3.Dot(context.Chassis.LinearVelocity, context.Chassis.MovementForward);
        float forwardThrottle = context.Input.Throttle > 0f && signedForwardSpeedBefore > -0.25f
            ? Math.Clamp(context.Input.Throttle, 0f, 1f)
            : 0f;
        VehicleTransmissionFrame transmissionFrame = VehicleTransmissionLogic.UpdateAutomaticForward(
            context.TransmissionState,
            context.TransmissionDefinition,
            VehicleTransmissionLogic.ComputeDrivenWheelAngularSpeed(context.WheelDefinitions, signedForwardSpeedBefore),
            forwardThrottle,
            context.ElapsedTime);
        Vector3 accumulatedSupportPosition = Vector3.Zero;
        Vector3 accumulatedSurfaceUp = Vector3.Zero;
        Vector3 accumulatedSurfaceForward = Vector3.Zero;
        Vector3 totalForce = Vector3.Zero;
        Vector3 totalTorque = Vector3.Zero;
        int groundedWheelCount = 0;
        bool touchedGuardRail = false;

        for (int index = 0; index < context.WheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = context.WheelDefinitions[index];
            VehicleWheelRuntimeState state = context.WheelStates[index];
            Vector3 wheelOffset = VehicleDynamicsMath.TransformLocalOffset(currentOrientation, definition.LocalAttachmentOffset);
            Vector3 attachmentPoint = context.Chassis.Position + wheelOffset;
            state.AttachmentPointWorld = attachmentPoint;
            state.SteeringAngleRadians = definition.CanSteer ? context.Input.Steering * definition.MaxSteeringAngleRadians : 0f;

            if (!context.TrackPhysics.TrySampleSurface(attachmentPoint, state.SurfaceSegmentHint >= 0 ? state.SurfaceSegmentHint : context.Chassis.SurfaceSegmentHint, out RaceTrackSurfaceSample sample))
            {
                VehicleDynamicsMath.ClearWheelState(definition, state);
                continue;
            }

            state.SurfaceSegmentHint = sample.SegmentIndex;
            float shoulderLimit = sample.HalfWidth + context.TrackPhysics.ShoulderWidth;
            float suspensionLength = Vector3.Dot(attachmentPoint - sample.SupportPoint, sample.Up) - definition.Radius;
            float maxExtension = definition.SuspensionRestLength + definition.SuspensionTravel;

            if (Math.Abs(sample.LateralOffset) > shoulderLimit || suspensionLength > maxExtension)
            {
                VehicleDynamicsMath.ClearWheelState(definition, state);
                state.ContactPointWorld = sample.SupportPoint;
                state.ContactNormal = sample.Up;
                state.IsFallbackContact = true;
                continue;
            }

            groundedWheelCount++;

            float clampedSuspensionLength = Math.Clamp(suspensionLength, 0.04f, maxExtension);
            float previousCompression = state.SuspensionCompression;
            float compression = Math.Clamp(definition.SuspensionRestLength - clampedSuspensionLength, 0f, definition.SuspensionTravel);
            float compressionVelocity = context.ElapsedTime > 0f ? (compression - previousCompression) / context.ElapsedTime : 0f;
            float suspensionForce = Math.Max(0f, (compression * SuspensionSpringStrength) + (compressionVelocity * SuspensionDamperStrength));
            float staticLoad = context.Chassis.Mass * 9.81f * definition.StaticLoadRatio;
            float wheelLoad = staticLoad + suspensionForce;
            touchedGuardRail |= Math.Abs(sample.LateralOffset) > Math.Max(0f, sample.HalfWidth - context.TrackPhysics.GuardRailInset);

            Vector3 wheelForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(baseForward, sample.Up, sample.Forward);
            if (definition.CanSteer)
            {
                wheelForward = VehicleDynamicsMath.RotateDirectionAroundAxis(wheelForward, sample.Up, state.SteeringAngleRadians, sample.Forward);
            }

            Vector3 wheelRight = VehicleDynamicsMath.NormalizeOrFallback(Vector3.Cross(sample.Up, wheelForward), sample.Right);
            Vector3 wheelVelocity = context.Chassis.LinearVelocity + Vector3.Cross(context.Chassis.AngularVelocity, wheelOffset);
            float longitudinalVelocity = Vector3.Dot(wheelVelocity, wheelForward);
            float lateralVelocity = Vector3.Dot(wheelVelocity, wheelRight);
            float driveForce = forwardThrottle > 0f
                ? forwardThrottle * MaxDriveForce * transmissionFrame.DriveForceScale * definition.DriveForceRatio
                : context.Input.Throttle * MaxReverseDriveForce * definition.DriveForceRatio;
            float longitudinalForce = driveForce - (longitudinalVelocity * LongitudinalDamping);

            if (context.Input.Throttle < 0f && Math.Abs(longitudinalVelocity) > 0.25f)
            {
                longitudinalForce -= MathF.Sign(longitudinalVelocity) * MaxBrakeForce * definition.BrakeForceRatio;
            }
            else if (Math.Abs(context.Input.Throttle) < 0.01f)
            {
                longitudinalForce -= MathF.Sign(longitudinalVelocity) * RollingResistanceForce * definition.BrakeForceRatio;
            }

            float lateralForce = -lateralVelocity * LateralGrip;
            Vector2 tireForce = new(longitudinalForce, lateralForce);
            float maxGripForce = Math.Max(900f, wheelLoad * TireGripScale);
            if (tireForce.LengthSquared() > maxGripForce * maxGripForce)
            {
                tireForce.Normalize();
                tireForce *= maxGripForce;
            }

            Vector3 contactForce = (sample.Up * suspensionForce) + (wheelForward * tireForce.X) + (wheelRight * tireForce.Y);
            totalForce += contactForce;
            totalTorque += Vector3.Cross(sample.SupportPoint - context.Chassis.Position, contactForce);

            Vector3 attachmentTarget = sample.SupportPoint + (sample.Up * (definition.Radius + clampedSuspensionLength));
            accumulatedSupportPosition += attachmentTarget - wheelOffset;
            accumulatedSurfaceUp += sample.Up;
            accumulatedSurfaceForward += wheelForward;

            state.HasContact = true;
            state.IsFallbackContact = !sample.IsWithinRoadBounds;
            state.ContactPointWorld = sample.SupportPoint;
            state.ContactNormal = sample.Up;
            state.ContactForward = wheelForward;
            state.SuspensionLength = clampedSuspensionLength;
            state.SuspensionCompression = compression;
            state.SuspensionCompressionVelocity = compressionVelocity;
            state.NormalizedCompression = definition.SuspensionTravel <= 0.0001f ? 0f : compression / definition.SuspensionTravel;
            state.RotationSpeedRadiansPerSecond = definition.Radius > 0.0001f ? longitudinalVelocity / definition.Radius : 0f;
            state.RotationAngleRadians += state.RotationSpeedRadiansPerSecond * context.ElapsedTime;
            state.SlipRatio = Math.Clamp((driveForce - longitudinalVelocity * 120f) / Math.Max(Math.Abs(longitudinalVelocity), 6f), -1f, 1f);
            state.SlipAngleRadians = MathF.Atan2(lateralVelocity, Math.Max(Math.Abs(longitudinalVelocity), 1f));
            state.ApproximateLoad = wheelLoad;
        }

        if (groundedWheelCount == 0)
        {
            ApplyFallback(context, "simulation lost all wheel contacts");
            return;
        }

        LogFallbackState(context.Session, false, $"simulation grounded on {groundedWheelCount} wheels");

        Vector3 averageSupportedPosition = accumulatedSupportPosition / groundedWheelCount;
        Vector3 averageSurfaceUp = VehicleDynamicsMath.NormalizeOrFallback(accumulatedSurfaceUp / groundedWheelCount, context.Chassis.SurfaceUp);
        Vector3 averageSurfaceForward = VehicleDynamicsMath.NormalizeOrFallback(accumulatedSurfaceForward / groundedWheelCount, baseForward);

        Vector3 acceleration = totalForce / context.Chassis.Mass;
        context.Chassis.LinearVelocity += acceleration * context.ElapsedTime;
        context.Chassis.LinearVelocity -= context.Chassis.LinearVelocity * LinearDrag * context.ElapsedTime;
        context.Chassis.LinearVelocity = VehicleDynamicsMath.ClampMagnitude(context.Chassis.LinearVelocity, MaxForwardSpeedUnitsPerSecond);

        Vector3 angularAcceleration = totalTorque / ChassisYawInertia;
        context.Chassis.AngularVelocity += angularAcceleration * context.ElapsedTime;
        context.Chassis.AngularVelocity -= context.Chassis.AngularVelocity * AngularDrag * context.ElapsedTime;

        context.Chassis.Position += context.Chassis.LinearVelocity * context.ElapsedTime;
        context.Chassis.Position = Vector3.Lerp(context.Chassis.Position, averageSupportedPosition, Math.Clamp(context.ElapsedTime * RideHeightCorrection, 0f, 1f));

        Quaternion integratedOrientation = VehicleDynamicsMath.IntegrateAngularVelocity(currentOrientation, context.Chassis.AngularVelocity, context.ElapsedTime);
        Quaternion targetOrientation = VehicleDynamicsMath.CreateSurfaceOrientation(
            VehicleDynamicsMath.ProjectDirectionOntoSurface(VehicleDynamicsMath.GetForward(integratedOrientation), averageSurfaceUp, averageSurfaceForward),
            averageSurfaceUp);
        context.Chassis.Orientation = Quaternion.Slerp(integratedOrientation, targetOrientation, Math.Clamp(context.ElapsedTime * OrientationStabilization, 0f, 1f));
        context.Chassis.MovementForward = VehicleDynamicsMath.GetForward(context.Chassis.Orientation);
        context.Chassis.SurfaceUp = averageSurfaceUp;
        context.Chassis.SurfaceSegmentHint = ResolveBestSegmentHint(context.WheelStates, context.Chassis.SurfaceSegmentHint);
        context.Chassis.HasValidSurface = true;

        if (touchedGuardRail)
        {
            context.Chassis.LinearVelocity *= MathF.Pow(context.TrackPhysics.EdgeSpeedRetainFactor, Math.Clamp(context.ElapsedTime * 60f, 0f, 8f));
        }

        VehicleTransmissionLogic.SampleCurrentGear(
            context.TransmissionState,
            context.TransmissionDefinition,
            VehicleTransmissionLogic.ComputeDrivenWheelAngularSpeed(
                context.WheelDefinitions,
                Vector3.Dot(context.Chassis.LinearVelocity, context.Chassis.MovementForward)),
            forwardThrottle);

        UpdateTelemetry(context);
        MaybeLogSample(context, groundedWheelCount, touchedGuardRail);
    }

    private void ApplyFallback(VehicleDynamicsExecutionContext context, string reason)
    {
        LogFallbackState(context.Session, true, reason);

        Vector3 forward = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.MovementForward, VehicleDynamicsMath.GetForward(context.Chassis.Orientation));
        Vector3 surfaceUp = Vector3.Up;
        if (context.TrackPhysics != null
            && context.TrackPhysics.TrySampleSurface(context.Chassis.Position, context.Chassis.SurfaceSegmentHint, out RaceTrackSurfaceSample sample))
        {
            context.Chassis.SurfaceSegmentHint = sample.SegmentIndex;
            surfaceUp = sample.Up;
            forward = VehicleDynamicsMath.ProjectDirectionOntoSurface(forward, sample.Up, sample.Forward);
            context.Chassis.Position = Vector3.Lerp(context.Chassis.Position, sample.Center, Math.Clamp(context.ElapsedTime * 3.5f, 0f, 1f));
        }

        if (Math.Abs(context.Input.Steering) > 0.001f && context.Chassis.LinearVelocity.LengthSquared() > 0.01f)
        {
            forward = VehicleDynamicsMath.RotateDirectionAroundAxis(forward, surfaceUp, context.Input.Steering * 0.6f * context.ElapsedTime, forward);
        }

        float forwardSpeed = Vector3.Dot(context.Chassis.LinearVelocity, forward);
        if (context.Input.Throttle > 0f)
        {
            forwardSpeed += 12f * context.Input.Throttle * context.ElapsedTime;
        }
        else if (context.Input.Throttle < 0f)
        {
            forwardSpeed += 10f * context.Input.Throttle * context.ElapsedTime;
        }
        else
        {
            forwardSpeed = VehicleDynamicsMath.MoveToward(forwardSpeed, 0f, 14f * context.ElapsedTime);
        }

        forwardSpeed = Math.Clamp(forwardSpeed, -MaxReverseSpeedUnitsPerSecond, MaxForwardSpeedUnitsPerSecond);
        context.Chassis.LinearVelocity = forward * forwardSpeed;
        context.Chassis.AngularVelocity *= 0.5f;
        context.Chassis.Position += context.Chassis.LinearVelocity * context.ElapsedTime;
        context.Chassis.Orientation = VehicleDynamicsMath.CreateSurfaceOrientation(forward, surfaceUp);
        context.Chassis.MovementForward = forward;
        context.Chassis.SurfaceUp = surfaceUp;
        context.Chassis.HasValidSurface = false;

        for (int index = 0; index < context.WheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = context.WheelDefinitions[index];
            VehicleWheelRuntimeState state = context.WheelStates[index];
            state.AttachmentPointWorld = context.Chassis.Position + VehicleDynamicsMath.TransformLocalOffset(context.Chassis.Orientation, definition.LocalAttachmentOffset);
            state.RotationSpeedRadiansPerSecond = definition.Radius > 0.0001f ? forwardSpeed / definition.Radius : 0f;
            state.RotationAngleRadians += state.RotationSpeedRadiansPerSecond * context.ElapsedTime;
            VehicleDynamicsMath.ClearWheelState(definition, state);
        }

        UpdateTelemetry(context);
        MaybeLogSample(context, groundedWheelCount: 0, touchedGuardRail: false);
    }

    private void UpdateTelemetry(VehicleDynamicsExecutionContext context)
    {
        float signedForwardSpeed = Vector3.Dot(context.Chassis.LinearVelocity, context.Chassis.MovementForward);
        float normalizedSpeed = Math.Clamp(Math.Abs(signedForwardSpeed) / MaxForwardSpeedUnitsPerSecond, 0f, 1f);
        float forwardThrottle = context.Input.Throttle > 0f && signedForwardSpeed > -0.25f
            ? Math.Clamp(context.Input.Throttle, 0f, 1f)
            : 0f;
        VehicleTransmissionLogic.SampleCurrentGear(
            context.TransmissionState,
            context.TransmissionDefinition,
            VehicleTransmissionLogic.ComputeDrivenWheelAngularSpeed(context.WheelDefinitions, signedForwardSpeed),
            forwardThrottle);
        float averageSlip = 0f;
        int contactedWheels = 0;
        for (int index = 0; index < context.WheelStates.Length; index++)
        {
            if (!context.WheelStates[index].HasContact)
            {
                continue;
            }

            contactedWheels++;
            averageSlip += Math.Abs(context.WheelStates[index].SlipRatio);
        }

        if (contactedWheels > 0)
        {
            averageSlip /= contactedWheels;
        }

        int gear = context.TransmissionState.CurrentGear;
        if (gear != _lastReportedGear)
        {
            _smoothedTachometerAcceleration = 0f;
            _lastReportedGear = gear;
        }

        float targetTachometer = Math.Clamp((context.TransmissionState.NormalizedRpm * 0.74f) + (Math.Abs(context.Input.Throttle) * 0.10f) + (averageSlip * 0.16f), 0f, 1f);
        float smoothingFactor = Math.Clamp(context.ElapsedTime * 5.5f, 0f, 1f);
        _smoothedTachometerAcceleration += (targetTachometer - _smoothedTachometerAcceleration) * smoothingFactor;

        context.Telemetry.DrivingMode = VehicleDrivingMode.Simulation;
        context.Telemetry.SpeedUnitsPerSecond = signedForwardSpeed;
        context.Telemetry.CurrentSpeedMph = normalizedSpeed * context.Pawn.TargetTopSpeedMph;
        context.Telemetry.SteeringInput = context.Input.Steering;
        context.Telemetry.TachometerAcceleration = _smoothedTachometerAcceleration;
        context.Telemetry.CurrentGear = gear;
        context.Telemetry.NormalizedSpeed = normalizedSpeed;
        context.Telemetry.EngineRpm = context.TransmissionState.EngineRpm;
        context.Telemetry.MovementForward = context.Chassis.MovementForward;
        context.Telemetry.SurfaceUp = context.Chassis.SurfaceUp;
        context.Telemetry.IsFallbackActive = !context.Chassis.HasValidSurface;
    }

    private int ResolveBestSegmentHint(IReadOnlyList<VehicleWheelRuntimeState> wheelStates, int fallbackSegmentHint)
    {
        for (int index = 0; index < wheelStates.Count; index++)
        {
            if (wheelStates[index].HasContact)
            {
                return wheelStates[index].SurfaceSegmentHint;
            }
        }

        return fallbackSegmentHint;
    }

    private void MaybeLogSample(VehicleDynamicsExecutionContext context, int groundedWheelCount, bool touchedGuardRail)
    {
        if (context.Session == null || _debugElapsedSeconds < _nextDebugSampleSeconds)
        {
            return;
        }

        _nextDebugSampleSeconds = _debugElapsedSeconds + DebugSampleIntervalSeconds;
        context.Session.AppendMovementDebug(
            "simulation",
            $"mode=simulation grounded={groundedWheelCount} guardRail={touchedGuardRail} speedUnits={context.Telemetry.SpeedUnitsPerSecond:0.000} speedMph={context.Telemetry.CurrentSpeedMph:0.0} throttle={context.Input.Throttle:0.0} steering={context.Input.Steering:0.0} pos={FormatVector(context.Chassis.Position)} forward={FormatVector(context.Chassis.MovementForward)} wheels={VehicleDynamicsMath.BuildWheelDebugSummary(context.WheelStates)}");
    }

    private void LogFallbackState(RuntimeRaceSession? session, bool fallbackEnabled, string reason)
    {
        if (_lastFallbackState == fallbackEnabled)
        {
            return;
        }

        _lastFallbackState = fallbackEnabled;
        session?.AppendMovementDebug(fallbackEnabled ? "simulation-fallback" : "simulation-surface", reason);
    }

    private static string FormatVector(Vector3 vector)
    {
        return $"({vector.X:0.000}, {vector.Y:0.000}, {vector.Z:0.000})";
    }
}