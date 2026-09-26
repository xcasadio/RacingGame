using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.Gameplay;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Components;

internal sealed class SimulationVehicleDynamicsSolver : IVehicleDynamicsSolver
{
    private const float DebugSampleIntervalSeconds = 0.2f;
    private const float WheelSampleRequeryPadding = 1.25f;
    private const float FallbackSupportCorrectionMinimum = 3.5f;

    private float _debugElapsedSeconds;
    private float _nextDebugSampleSeconds;
    private float _smoothedTachometerAcceleration;
    private int _lastReportedGear = 1;
    private bool? _lastFallbackState;

    public VehicleDrivingMode DrivingMode => VehicleDrivingMode.Simulation;

    public void Reset(VehicleDynamicsExecutionContext context)
    {
        SimulationVehicleTuningProfile tuning = context.Profile.Simulation;
        context.Chassis.Mass = tuning.ChassisMass;
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
        SimulationVehicleTuningProfile tuning = context.Profile.Simulation;
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
        float totalDriveForce = 0f;
        float totalLongitudinalDampingForce = 0f;
        float totalBrakeOrRollingForce = 0f;
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

            if (!TrySampleWheelSurface(
                    context,
                    definition,
                    state.SurfaceSegmentHint >= 0 ? state.SurfaceSegmentHint : context.Chassis.SurfaceSegmentHint,
                    attachmentPoint,
                    out RaceTrackSurfaceSample sample))
            {
                VehicleDynamicsMath.ClearWheelState(definition, state);
                continue;
            }

            state.SurfaceSegmentHint = sample.SegmentIndex;
            float shoulderLimit = sample.HalfWidth + context.TrackPhysics.ShoulderWidth;
            float suspensionLength = Vector3.Dot(attachmentPoint - sample.SupportPoint, sample.Up) - definition.Radius;
            float maxExtension = definition.SuspensionRestLength + definition.SuspensionTravel;

            if (Math.Abs(sample.LateralOffset) > shoulderLimit)
            {
                VehicleDynamicsMath.ClearWheelState(definition, state);
                state.ContactPointWorld = sample.SupportPoint;
                state.ContactNormal = sample.Up;
                state.IsFallbackContact = true;
                continue;
            }

            groundedWheelCount++;

            float clampedSuspensionLength = Math.Clamp(suspensionLength, 0.04f, maxExtension);
            float targetSuspensionLength = Math.Clamp(definition.SuspensionRestLength, 0.04f, maxExtension);
            float previousCompression = state.SuspensionCompression;
            float compression = Math.Clamp(definition.SuspensionRestLength - clampedSuspensionLength, 0f, definition.SuspensionTravel);
            float compressionVelocity = context.ElapsedTime > 0f ? (compression - previousCompression) / context.ElapsedTime : 0f;
            float suspensionForce = Math.Max(0f, (compression * tuning.SuspensionSpringStrength) + (compressionVelocity * tuning.SuspensionDamperStrength));
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
                ? forwardThrottle * tuning.MaxDriveForce * transmissionFrame.DriveForceScale * definition.DriveForceRatio
                : context.Input.Throttle * tuning.MaxReverseDriveForce * definition.DriveForceRatio;
            float longitudinalDampingForce = longitudinalVelocity * tuning.LongitudinalDamping * definition.StaticLoadRatio;
            float longitudinalForce = driveForce - longitudinalDampingForce;
            totalDriveForce += driveForce;
            totalLongitudinalDampingForce += Math.Abs(longitudinalDampingForce);

            if (context.Input.Throttle < 0f && Math.Abs(longitudinalVelocity) > 0.25f)
            {
                float brakeForce = MathF.Sign(longitudinalVelocity) * tuning.MaxBrakeForce * definition.BrakeForceRatio;
                longitudinalForce -= brakeForce;
                totalBrakeOrRollingForce += Math.Abs(brakeForce);
            }
            else if (Math.Abs(context.Input.Throttle) < 0.01f)
            {
                float rollingResistanceForce = MathF.Sign(longitudinalVelocity) * tuning.RollingResistanceForce * definition.BrakeForceRatio;
                longitudinalForce -= rollingResistanceForce;
                totalBrakeOrRollingForce += Math.Abs(rollingResistanceForce);
            }

            float lateralForce = -lateralVelocity * tuning.LateralGrip * definition.StaticLoadRatio;
            Vector2 tireForce = new(longitudinalForce, lateralForce);
            float maxGripForce = Math.Max(900f, wheelLoad * tuning.TireGripScale);
            if (tireForce.LengthSquared() > maxGripForce * maxGripForce)
            {
                tireForce.Normalize();
                tireForce *= maxGripForce;
            }

            Vector3 planarContactForce = (wheelForward * tireForce.X) + (wheelRight * tireForce.Y);
            totalForce += planarContactForce;
            totalTorque += Vector3.Cross(sample.SupportPoint - context.Chassis.Position, planarContactForce);

            Vector3 attachmentTarget = sample.SupportPoint + (sample.Up * (definition.Radius + targetSuspensionLength));
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
        float linearDragEquivalentForce = Math.Abs(signedForwardSpeedBefore) * tuning.LinearDrag * context.Chassis.Mass;
        float estimatedNetForwardForce = totalDriveForce - totalLongitudinalDampingForce - totalBrakeOrRollingForce - linearDragEquivalentForce;
        float currentGearRedlineSpeedUnits = VehicleTransmissionLogic.ComputeForwardSpeedUnitsAtEngineRpm(
            context.TransmissionDefinition,
            transmissionFrame.Gear,
            context.TransmissionDefinition.RedlineRpm,
            context.WheelDefinitions);
        float currentGearRedlineMph = VehicleSpeedCalibration.ConvertSpeedUnitsToDisplayMph(currentGearRedlineSpeedUnits, VehicleDrivingMode.Simulation);

        Vector3 acceleration = totalForce / context.Chassis.Mass;
        context.Chassis.LinearVelocity += acceleration * context.ElapsedTime;
        context.Chassis.LinearVelocity -= averageSurfaceUp * Vector3.Dot(context.Chassis.LinearVelocity, averageSurfaceUp);
        context.Chassis.LinearVelocity -= context.Chassis.LinearVelocity * tuning.LinearDrag * context.ElapsedTime;
        context.Chassis.LinearVelocity = VehicleDynamicsMath.ClampMagnitude(
            context.Chassis.LinearVelocity,
            Math.Max(tuning.MaxForwardSpeedUnitsPerSecond, tuning.MaxReverseSpeedUnitsPerSecond));

        Vector3 angularAcceleration = totalTorque / tuning.ChassisYawInertia;
        context.Chassis.AngularVelocity += angularAcceleration * context.ElapsedTime;
        context.Chassis.AngularVelocity -= context.Chassis.AngularVelocity * tuning.AngularDrag * context.ElapsedTime;

        context.Chassis.Position += context.Chassis.LinearVelocity * context.ElapsedTime;
        context.Chassis.Position = Vector3.Lerp(context.Chassis.Position, averageSupportedPosition, Math.Clamp(context.ElapsedTime * tuning.RideHeightCorrection, 0f, 1f));

        Quaternion integratedOrientation = VehicleDynamicsMath.IntegrateAngularVelocity(currentOrientation, context.Chassis.AngularVelocity, context.ElapsedTime);
        Quaternion targetOrientation = VehicleDynamicsMath.CreateSurfaceOrientation(
            VehicleDynamicsMath.ProjectDirectionOntoSurface(VehicleDynamicsMath.GetForward(integratedOrientation), averageSurfaceUp, averageSurfaceForward),
            averageSurfaceUp);
        context.Chassis.Orientation = Quaternion.Slerp(integratedOrientation, targetOrientation, Math.Clamp(context.ElapsedTime * tuning.OrientationStabilization, 0f, 1f));
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
        MaybeLogSample(
            context,
            groundedWheelCount,
            touchedGuardRail,
            transmissionFrame,
            totalDriveForce,
            totalLongitudinalDampingForce,
            totalBrakeOrRollingForce,
            linearDragEquivalentForce,
            estimatedNetForwardForce,
            currentGearRedlineSpeedUnits,
            currentGearRedlineMph);
    }

    private void ApplyFallback(VehicleDynamicsExecutionContext context, string reason)
    {
        SimulationVehicleTuningProfile tuning = context.Profile.Simulation;
        LogFallbackState(context.Session, true, reason);

        Quaternion currentOrientation = Quaternion.Normalize(context.Chassis.Orientation);
        Vector3 forward = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.MovementForward, VehicleDynamicsMath.GetForward(currentOrientation));
        Vector3 surfaceUp = Vector3.Up;
        if (TryResolveFallbackSupportPose(
                context,
                currentOrientation,
                out Vector3 supportedPosition,
                out Vector3 supportedSurfaceUp,
                out Vector3 supportedSurfaceForward))
        {
            surfaceUp = supportedSurfaceUp;
            forward = VehicleDynamicsMath.ProjectDirectionOntoSurface(forward, supportedSurfaceUp, supportedSurfaceForward);
            float correctionRate = Math.Max(FallbackSupportCorrectionMinimum, tuning.RideHeightCorrection);
            context.Chassis.Position = Vector3.Lerp(context.Chassis.Position, supportedPosition, Math.Clamp(context.ElapsedTime * correctionRate, 0f, 1f));
        }

        if (Math.Abs(context.Input.Steering) > 0.001f && context.Chassis.LinearVelocity.LengthSquared() > 0.01f)
        {
            forward = VehicleDynamicsMath.RotateDirectionAroundAxis(forward, surfaceUp, context.Input.Steering * tuning.FallbackSteeringRateRadiansPerSecond * context.ElapsedTime, forward);
        }

        float forwardSpeed = Vector3.Dot(context.Chassis.LinearVelocity, forward);
        if (context.Input.Throttle > 0f)
        {
            forwardSpeed += tuning.FallbackForwardAcceleration * context.Input.Throttle * context.ElapsedTime;
        }
        else if (context.Input.Throttle < 0f)
        {
            forwardSpeed += tuning.FallbackReverseAcceleration * context.Input.Throttle * context.ElapsedTime;
        }
        else
        {
            forwardSpeed = VehicleDynamicsMath.MoveToward(forwardSpeed, 0f, tuning.FallbackIdleDeceleration * context.ElapsedTime);
        }

        forwardSpeed = Math.Clamp(forwardSpeed, -tuning.MaxReverseSpeedUnitsPerSecond, tuning.MaxForwardSpeedUnitsPerSecond);
        context.Chassis.LinearVelocity = forward * forwardSpeed;
        context.Chassis.AngularVelocity *= 0.5f;
        context.Chassis.Position += context.Chassis.LinearVelocity * context.ElapsedTime;
        context.Chassis.Orientation = VehicleDynamicsMath.CreateSurfaceOrientation(forward, surfaceUp);
        context.Chassis.MovementForward = forward;
        context.Chassis.SurfaceUp = surfaceUp;
        context.Chassis.HasValidSurface = false;

        int fallbackSupportedWheelCount = PopulateFallbackWheelState(context, forwardSpeed);

        UpdateTelemetry(context);
        MaybeLogSample(context, groundedWheelCount: fallbackSupportedWheelCount, touchedGuardRail: false);
    }

    private static bool TryResolveFallbackSupportPose(
        VehicleDynamicsExecutionContext context,
        Quaternion orientation,
        out Vector3 supportedPosition,
        out Vector3 surfaceUp,
        out Vector3 surfaceForward)
    {
        supportedPosition = context.Chassis.Position;
        surfaceUp = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.SurfaceUp, Vector3.Up);
        surfaceForward = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.MovementForward, VehicleDynamicsMath.GetForward(orientation));

        if (context.TrackPhysics == null)
        {
            return false;
        }

        Vector3 accumulatedSupportedPosition = Vector3.Zero;
        Vector3 accumulatedSurfaceUp = Vector3.Zero;
        Vector3 accumulatedSurfaceForward = Vector3.Zero;
        int supportCount = 0;
        int resolvedSegmentHint = context.Chassis.SurfaceSegmentHint;

        for (int index = 0; index < context.WheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = context.WheelDefinitions[index];
            VehicleWheelRuntimeState state = context.WheelStates[index];
            Vector3 wheelOffset = VehicleDynamicsMath.TransformLocalOffset(orientation, definition.LocalAttachmentOffset);
            Vector3 attachmentPoint = context.Chassis.Position + wheelOffset;

            if (!TrySampleWheelSurface(
                    context,
                    definition,
                    state.SurfaceSegmentHint >= 0 ? state.SurfaceSegmentHint : context.Chassis.SurfaceSegmentHint,
                    attachmentPoint,
                    out RaceTrackSurfaceSample sample))
            {
                continue;
            }

            float maxExtension = definition.SuspensionRestLength + definition.SuspensionTravel;
            float targetSuspensionLength = Math.Clamp(definition.SuspensionRestLength, 0.04f, maxExtension);
            Vector3 attachmentTarget = sample.SupportPoint + (sample.Up * (definition.Radius + targetSuspensionLength));

            accumulatedSupportedPosition += attachmentTarget - wheelOffset;
            accumulatedSurfaceUp += sample.Up;
            accumulatedSurfaceForward += sample.Forward;
            resolvedSegmentHint = sample.SegmentIndex;
            supportCount++;
        }

        if (supportCount > 0)
        {
            supportedPosition = accumulatedSupportedPosition / supportCount;
            surfaceUp = VehicleDynamicsMath.NormalizeOrFallback(accumulatedSurfaceUp / supportCount, surfaceUp);
            surfaceForward = VehicleDynamicsMath.NormalizeOrFallback(accumulatedSurfaceForward / supportCount, surfaceForward);
            context.Chassis.SurfaceSegmentHint = resolvedSegmentHint;
            return true;
        }

        float fallbackMaxDistance = RacingCarPawn.CollisionLength + context.TrackPhysics.ShoulderWidth + WheelSampleRequeryPadding;
        if (!TrySampleStableSurface(
                context.TrackPhysics,
                context.Chassis.Position,
                context.Chassis.SurfaceSegmentHint,
                fallbackMaxDistance * fallbackMaxDistance,
                out RaceTrackSurfaceSample fallbackSample))
        {
            return false;
        }

        supportedPosition = fallbackSample.SupportPoint;
        surfaceUp = fallbackSample.Up;
        surfaceForward = fallbackSample.Forward;
        context.Chassis.SurfaceSegmentHint = fallbackSample.SegmentIndex;
        return true;
    }

    private static int PopulateFallbackWheelState(VehicleDynamicsExecutionContext context, float forwardSpeed)
    {
        Quaternion orientation = Quaternion.Normalize(context.Chassis.Orientation);
        Vector3 chassisForward = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.MovementForward, VehicleDynamicsMath.GetForward(orientation));
        int supportedWheelCount = 0;

        for (int index = 0; index < context.WheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = context.WheelDefinitions[index];
            VehicleWheelRuntimeState state = context.WheelStates[index];
            Vector3 attachmentPoint = context.Chassis.Position + VehicleDynamicsMath.TransformLocalOffset(orientation, definition.LocalAttachmentOffset);
            state.AttachmentPointWorld = attachmentPoint;
            state.SteeringAngleRadians = definition.CanSteer ? context.Input.Steering * definition.MaxSteeringAngleRadians : 0f;
            state.RotationSpeedRadiansPerSecond = definition.Radius > 0.0001f ? forwardSpeed / definition.Radius : 0f;
            state.RotationAngleRadians += state.RotationSpeedRadiansPerSecond * context.ElapsedTime;

            if (!TrySampleWheelSurface(
                    context,
                    definition,
                    state.SurfaceSegmentHint >= 0 ? state.SurfaceSegmentHint : context.Chassis.SurfaceSegmentHint,
                    attachmentPoint,
                    out RaceTrackSurfaceSample sample))
            {
                VehicleDynamicsMath.ClearWheelState(definition, state);
                continue;
            }

            float maxExtension = definition.SuspensionRestLength + definition.SuspensionTravel;
            float suspensionLength = Math.Clamp(
                Vector3.Dot(attachmentPoint - sample.SupportPoint, sample.Up) - definition.Radius,
                0.04f,
                maxExtension);
            float suspensionCompression = Math.Clamp(definition.SuspensionRestLength - suspensionLength, 0f, definition.SuspensionTravel);
            Vector3 wheelForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(chassisForward, sample.Up, sample.Forward);
            if (definition.CanSteer)
            {
                wheelForward = VehicleDynamicsMath.RotateDirectionAroundAxis(wheelForward, sample.Up, state.SteeringAngleRadians, sample.Forward);
            }

            state.HasContact = true;
            state.IsFallbackContact = true;
            state.SurfaceSegmentHint = sample.SegmentIndex;
            state.ContactPointWorld = sample.SupportPoint;
            state.ContactNormal = sample.Up;
            state.ContactForward = wheelForward;
            state.SuspensionLength = suspensionLength;
            state.SuspensionCompression = suspensionCompression;
            state.SuspensionCompressionVelocity = 0f;
            state.NormalizedCompression = definition.SuspensionTravel <= 0.0001f ? 0f : suspensionCompression / definition.SuspensionTravel;
            state.SlipRatio = 0f;
            state.SlipAngleRadians = 0f;
            state.ApproximateLoad = 0f;
            supportedWheelCount++;
        }

        return supportedWheelCount;
    }

    private static bool TrySampleWheelSurface(
        VehicleDynamicsExecutionContext context,
        VehicleWheelDefinition definition,
        int segmentHint,
        Vector3 attachmentPoint,
        out RaceTrackSurfaceSample sample)
    {
        RaceTrackPhysicsComponent? trackPhysics = context.TrackPhysics;
        if (trackPhysics == null)
        {
            sample = default;
            return false;
        }

        float maxExpectedDistance = definition.Radius
            + definition.SuspensionRestLength
            + definition.SuspensionTravel
            + trackPhysics.ShoulderWidth
            + WheelSampleRequeryPadding;

        return TrySampleStableSurface(
            trackPhysics,
            attachmentPoint,
            segmentHint,
            maxExpectedDistance * maxExpectedDistance,
            out sample);
    }

    private static bool TrySampleStableSurface(
        RaceTrackPhysicsComponent trackPhysics,
        Vector3 position,
        int segmentHint,
        float maxAcceptedDistanceSquared,
        out RaceTrackSurfaceSample sample)
    {
        if (!trackPhysics.TrySampleSurface(position, segmentHint, out sample))
        {
            return false;
        }

        if (segmentHint < 0 || sample.DistanceSquared <= maxAcceptedDistanceSquared)
        {
            return true;
        }

        if (trackPhysics.TrySampleSurface(position, -1, out RaceTrackSurfaceSample globalSample)
            && globalSample.DistanceSquared < sample.DistanceSquared)
        {
            sample = globalSample;
        }

        return true;
    }

    private void UpdateTelemetry(VehicleDynamicsExecutionContext context)
    {
        SimulationVehicleTuningProfile tuning = context.Profile.Simulation;
        float signedForwardSpeed = Vector3.Dot(context.Chassis.LinearVelocity, context.Chassis.MovementForward);
        float normalizedSpeed = Math.Clamp(Math.Abs(signedForwardSpeed) / tuning.MaxForwardSpeedUnitsPerSecond, 0f, 1f);
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
        context.Telemetry.CurrentSpeedMph = VehicleSpeedCalibration.ConvertSpeedUnitsToDisplayMph(signedForwardSpeed, VehicleDrivingMode.Simulation);
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

    private void MaybeLogSample(
        VehicleDynamicsExecutionContext context,
        int groundedWheelCount,
        bool touchedGuardRail,
        VehicleTransmissionFrame? transmissionFrame = null,
        float totalDriveForce = 0f,
        float totalLongitudinalDampingForce = 0f,
        float totalBrakeOrRollingForce = 0f,
        float linearDragEquivalentForce = 0f,
        float estimatedNetForwardForce = 0f,
        float currentGearRedlineSpeedUnits = 0f,
        float currentGearRedlineMph = 0f)
    {
        if (context.Session == null || _debugElapsedSeconds < _nextDebugSampleSeconds)
        {
            return;
        }

        _nextDebugSampleSeconds = _debugElapsedSeconds + DebugSampleIntervalSeconds;
        string transmissionDiagnostics = transmissionFrame.HasValue
            ? $" gear={transmissionFrame.Value.Gear} rpm={transmissionFrame.Value.EngineRpm:0} driveScale={transmissionFrame.Value.DriveForceScale:0.000} drive={totalDriveForce:0} damping={totalLongitudinalDampingForce:0} coastBrake={totalBrakeOrRollingForce:0} drag={linearDragEquivalentForce:0} net={estimatedNetForwardForce:0} gearRedlineUnits={currentGearRedlineSpeedUnits:0.000} gearRedlineMph={currentGearRedlineMph:0.0}"
            : string.Empty;
        context.Session.AppendMovementDebug(
            "simulation",
            $"mode=simulation grounded={groundedWheelCount} guardRail={touchedGuardRail}{transmissionDiagnostics} speedUnits={context.Telemetry.SpeedUnitsPerSecond:0.000}/{context.Profile.Simulation.MaxForwardSpeedUnitsPerSecond:0.000} speedMph={context.Telemetry.CurrentSpeedMph:0.0}/{context.Pawn.TargetTopSpeedMph:0.0} throttle={context.Input.Throttle:0.0} steering={context.Input.Steering:0.0} pos={FormatVector(context.Chassis.Position)} forward={FormatVector(context.Chassis.MovementForward)} wheels={VehicleDynamicsMath.BuildWheelDebugSummary(context.WheelStates)}");
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