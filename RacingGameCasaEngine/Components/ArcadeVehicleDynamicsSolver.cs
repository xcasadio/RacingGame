using CasaEngine.Framework.GameFramework;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Components;

internal sealed class ArcadeVehicleDynamicsSolver : IVehicleDynamicsSolver
{
    private const float DebugSampleIntervalSeconds = 0.15f;
    private const float DebugInitialSamplingWindowSeconds = 6.0f;
    private const float DebugTeleportDistanceThreshold = 1.25f;

    private float _speedUnitsPerSecond;
    private Vector3 _movementForward = Vector3.Forward;
    private int _segmentHint = -1;
    private bool _movementBasisInitialized;
    private float _debugElapsedSeconds;
    private float _nextDebugSampleSeconds;
    private float _lastLoggedThrottle = float.NaN;
    private float _lastLoggedSteering = float.NaN;
    private bool? _lastFallbackState;
    private bool _lastOutsideRoadBounds;
    private bool _lastTouchedBarrier;
    private float _tachometerAcceleration;
    private int _lastReportedGear = 1;

    public VehicleDrivingMode DrivingMode => VehicleDrivingMode.Arcade;

    public float ForwardAcceleration { get; set; } = 20f;

    public float ReverseAcceleration { get; set; } = 14f;

    public float MaxForwardSpeedUnitsPerSecond { get; set; } = 36f;

    public float MaxReverseSpeedUnitsPerSecond { get; set; } = 12f;

    public float TurnRateRadiansPerSecond { get; set; } = 1.7f;

    public float IdleDeceleration { get; set; } = 18f;

    public void Reset(VehicleDynamicsExecutionContext context)
    {
        _speedUnitsPerSecond = 0f;
        _movementForward = VehicleDynamicsMath.NormalizeOrFallback(context.Chassis.MovementForward, Vector3.Forward);
        _segmentHint = context.Chassis.SurfaceSegmentHint;
        _movementBasisInitialized = context.Chassis.HasValidSurface;
        _tachometerAcceleration = 0f;
        _lastReportedGear = 1;
        ResetDebugLoggingState();
    }

    public void Update(VehicleDynamicsExecutionContext context)
    {
        RacingCarPawn pawn = context.Pawn;
        float elapsedTime = context.ElapsedTime;
        float throttle = context.Input.Throttle;
        float steering = context.Input.Steering;
        RuntimeRaceSession? session = context.Session;
        _debugElapsedSeconds += elapsedTime;

        MaybeLogInputChange(session, context, throttle, steering);

        float previousSpeedUnitsPerSecond = _speedUnitsPerSecond;
        UpdateSpeed(throttle, elapsedTime);

        RaceTrackPhysicsComponent? trackPhysics = context.TrackPhysics;
        if (trackPhysics == null
            || !trackPhysics.TrySampleSurface(context.Chassis.Position, _segmentHint, out RaceTrackSurfaceSample currentSurface))
        {
            LogFallbackState(
                session,
                fallbackEnabled: true,
                trackPhysics == null
                    ? "track physics component unavailable"
                    : "surface sampling failed for current position");
            UpdateFallbackMovement(context, steering, elapsedTime);
            PopulateSharedWheelState(context);
            UpdateTelemetry(context, steering, previousSpeedUnitsPerSecond, elapsedTime, fallbackActive: true);
            MaybeLogFallbackSample(session, context, throttle, steering);
            return;
        }

        LogFallbackState(
            session,
            fallbackEnabled: false,
            $"surface acquired seg={currentSurface.SegmentIndex} pos={FormatVector(context.Chassis.Position)} lateral={currentSurface.LateralOffset:0.000}");

        _segmentHint = currentSurface.SegmentIndex;

        if (!_movementBasisInitialized)
        {
            Vector3 alignedTrackForward = currentSurface.Forward;
            if (Vector3.Dot(alignedTrackForward, VehicleDynamicsMath.GetForward(context.Chassis.Orientation)) < 0f)
            {
                alignedTrackForward = -alignedTrackForward;
            }

            _movementForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(alignedTrackForward, currentSurface.Up, currentSurface.Forward);
            _movementBasisInitialized = true;
            session?.AppendMovementDebug(
                "basis",
                $"initialized seg={currentSurface.SegmentIndex} rootForward={FormatVector(VehicleDynamicsMath.GetForward(context.Chassis.Orientation))} surfaceForward={FormatVector(currentSurface.Forward)} movementForward={FormatVector(_movementForward)} lateral={currentSurface.LateralOffset:0.000}");
        }
        else
        {
            _movementForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(_movementForward, currentSurface.Up, currentSurface.Forward);
        }

        if (Math.Abs(_speedUnitsPerSecond) > 0.05f && Math.Abs(steering) > 0f)
        {
            float steeringScale = Math.Clamp(_speedUnitsPerSecond / MaxForwardSpeedUnitsPerSecond, -0.75f, 1f);
            float turnAmount = steering * GetSteeringSensitivityScale(pawn.Controller) * TurnRateRadiansPerSecond * steeringScale * elapsedTime;
            _movementForward = VehicleDynamicsMath.RotateDirectionAroundAxis(_movementForward, currentSurface.Up, turnAmount, currentSurface.Forward);
        }

        Vector3 startPosition = context.Chassis.Position;
        Vector3 desiredPosition = startPosition + _movementForward * (_speedUnitsPerSecond * elapsedTime);
        if (!trackPhysics.TrySampleSurface(desiredPosition, _segmentHint, out RaceTrackSurfaceSample nextSurface))
        {
            nextSurface = currentSurface;
            session?.AppendMovementDebug(
                "surface",
                $"desired sample failed; reusing current surface seg={currentSurface.SegmentIndex} desired={FormatVector(desiredPosition)}");
        }

        _segmentHint = nextSurface.SegmentIndex;

        Vector3 nextMovementForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(_movementForward, nextSurface.Up, nextSurface.Forward);
        TrackBarrierContact barrierContact = ResolveBarrierContact(trackPhysics, nextSurface, nextMovementForward);

        if (barrierContact.TouchedBarrier)
        {
            _speedUnitsPerSecond = ApplyBarrierSpeedPenalty(_speedUnitsPerSecond, trackPhysics, barrierContact.ImpactStrength, elapsedTime);
            nextMovementForward = ProjectDirectionAlongBarrier(nextMovementForward, barrierContact.BarrierNormal, nextSurface.Up, nextSurface.Forward);
        }
        else if (barrierContact.OutsideRoadBounds)
        {
            _speedUnitsPerSecond = ApplyShoulderDeceleration(_speedUnitsPerSecond, trackPhysics.ShoulderDeceleration, elapsedTime);
        }

        Vector3 resolvedPosition = nextSurface.Center + nextSurface.Right * barrierContact.ResolvedLateralOffset;
        context.Chassis.Position = resolvedPosition;
        _movementForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(nextMovementForward, nextSurface.Up, nextSurface.Forward);
        context.Chassis.Orientation = VehicleDynamicsMath.CreateSurfaceOrientation(_movementForward, nextSurface.Up);
        context.Chassis.LinearVelocity = _movementForward * _speedUnitsPerSecond;
        context.Chassis.AngularVelocity = nextSurface.Up * (steering * TurnRateRadiansPerSecond);
        context.Chassis.MovementForward = _movementForward;
        context.Chassis.SurfaceUp = nextSurface.Up;
        context.Chassis.SurfaceSegmentHint = nextSurface.SegmentIndex;
        context.Chassis.HasValidSurface = true;

        PopulateSharedWheelState(context);
        UpdateTelemetry(context, steering, previousSpeedUnitsPerSecond, elapsedTime, fallbackActive: false);
        LogBoundsState(session, trackPhysics, nextSurface, barrierContact.OutsideRoadBounds, barrierContact.TouchedBarrier, barrierContact.AllowedCenterHalfWidth);
        MaybeLogLargeDisplacement(session, startPosition, desiredPosition, resolvedPosition, nextSurface);
        MaybeLogMovementSample(session, context, throttle, steering, desiredPosition, resolvedPosition, nextSurface, barrierContact.TouchedBarrier, barrierContact.OutsideRoadBounds);
    }

    private float GetSteeringSensitivityScale(Controller? controller)
    {
        return controller is RacingPlayerController racingPlayerController
            ? racingPlayerController.SteeringSensitivityScale
            : 1f;
    }

    private void UpdateSpeed(float throttle, float elapsedTime)
    {
        if (throttle > 0f)
        {
            _speedUnitsPerSecond += ForwardAcceleration * Math.Clamp(throttle, 0f, 1f) * elapsedTime;
        }
        else if (throttle < 0f)
        {
            _speedUnitsPerSecond += ReverseAcceleration * Math.Clamp(throttle, -1f, 0f) * elapsedTime;
        }
        else
        {
            _speedUnitsPerSecond = ApplyIdleDeceleration(_speedUnitsPerSecond, elapsedTime);
        }

        _speedUnitsPerSecond = Math.Clamp(_speedUnitsPerSecond, -MaxReverseSpeedUnitsPerSecond, MaxForwardSpeedUnitsPerSecond);
    }

    private void UpdateFallbackMovement(VehicleDynamicsExecutionContext context, float steering, float elapsedTime)
    {
        Vector3 currentForward = VehicleDynamicsMath.NormalizeOrFallback(VehicleDynamicsMath.GetForward(context.Chassis.Orientation), Vector3.Forward);
        if (Math.Abs(_speedUnitsPerSecond) > 0.05f && Math.Abs(steering) > 0f)
        {
            float steeringScale = Math.Clamp(_speedUnitsPerSecond / MaxForwardSpeedUnitsPerSecond, -0.75f, 1f);
            float turnAmount = steering * GetSteeringSensitivityScale(context.Pawn.Controller) * TurnRateRadiansPerSecond * steeringScale * elapsedTime;
            Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, turnAmount);
            currentForward = VehicleDynamicsMath.NormalizeOrFallback(Vector3.Transform(currentForward, rotation), currentForward);
        }

        context.Chassis.Position += currentForward * (_speedUnitsPerSecond * elapsedTime);
        context.Chassis.Orientation = VehicleDynamicsMath.CreateSurfaceOrientation(currentForward, Vector3.Up);
        context.Chassis.LinearVelocity = currentForward * _speedUnitsPerSecond;
        context.Chassis.AngularVelocity = Vector3.Zero;
        context.Chassis.MovementForward = currentForward;
        context.Chassis.SurfaceUp = Vector3.Up;
        context.Chassis.HasValidSurface = false;
        _movementForward = currentForward;
        _movementBasisInitialized = true;
    }

    private void PopulateSharedWheelState(VehicleDynamicsExecutionContext context)
    {
        Quaternion orientation = context.Chassis.Orientation;
        Vector3 chassisForward = context.Chassis.MovementForward;
        Vector3 chassisUp = context.Chassis.SurfaceUp;

        for (int index = 0; index < context.WheelDefinitions.Length; index++)
        {
            VehicleWheelDefinition definition = context.WheelDefinitions[index];
            VehicleWheelRuntimeState state = context.WheelStates[index];
            Vector3 attachmentPoint = context.Chassis.Position + VehicleDynamicsMath.TransformLocalOffset(orientation, definition.LocalAttachmentOffset);
            state.AttachmentPointWorld = attachmentPoint;
            state.SteeringAngleRadians = definition.CanSteer ? context.Input.Steering * definition.MaxSteeringAngleRadians : 0f;
            state.ApproximateLoad = context.Chassis.Mass * 9.81f * definition.StaticLoadRatio;

            if (context.TrackPhysics == null
                || !context.TrackPhysics.TrySampleSurface(attachmentPoint, state.SurfaceSegmentHint, out RaceTrackSurfaceSample sample))
            {
                VehicleDynamicsMath.ClearWheelState(definition, state);
                state.RotationSpeedRadiansPerSecond = definition.Radius > 0.0001f ? _speedUnitsPerSecond / definition.Radius : 0f;
                state.RotationAngleRadians += state.RotationSpeedRadiansPerSecond * context.ElapsedTime;
                continue;
            }

            state.SurfaceSegmentHint = sample.SegmentIndex;
            state.HasContact = true;
            state.IsFallbackContact = !sample.IsWithinRoadBounds;
            state.ContactPointWorld = sample.SupportPoint;
            state.ContactNormal = sample.Up;
            Vector3 wheelForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(chassisForward, sample.Up, sample.Forward);
            if (definition.CanSteer)
            {
                wheelForward = VehicleDynamicsMath.RotateDirectionAroundAxis(wheelForward, sample.Up, state.SteeringAngleRadians, sample.Forward);
            }

            state.ContactForward = wheelForward;
            float suspensionLength = Math.Max(0.04f, Vector3.Dot(attachmentPoint - sample.SupportPoint, sample.Up) - definition.Radius);
            float clampedSuspensionLength = Math.Clamp(suspensionLength, 0.04f, definition.SuspensionRestLength + definition.SuspensionTravel);
            state.SuspensionLength = clampedSuspensionLength;
            state.SuspensionCompressionVelocity = 0f;
            state.SuspensionCompression = Math.Clamp(definition.SuspensionRestLength - clampedSuspensionLength, 0f, definition.SuspensionTravel);
            state.NormalizedCompression = definition.SuspensionTravel <= 0.0001f ? 0f : state.SuspensionCompression / definition.SuspensionTravel;
            float wheelLongitudinalVelocity = Vector3.Dot(context.Chassis.LinearVelocity, wheelForward);
            state.RotationSpeedRadiansPerSecond = definition.Radius > 0.0001f ? wheelLongitudinalVelocity / definition.Radius : 0f;
            state.RotationAngleRadians += state.RotationSpeedRadiansPerSecond * context.ElapsedTime;
            state.SlipRatio = Math.Clamp(sample.LateralOffset / Math.Max(sample.HalfWidth, 0.001f), -1f, 1f);
            state.SlipAngleRadians = state.SteeringAngleRadians * 0.35f;
        }
    }

    private void UpdateTelemetry(VehicleDynamicsExecutionContext context, float steering, float previousSpeedUnitsPerSecond, float elapsedTime, bool fallbackActive)
    {
        float normalizedSpeed = Math.Clamp(Math.Abs(_speedUnitsPerSecond) / MaxForwardSpeedUnitsPerSecond, 0f, 1f);
        context.Telemetry.DrivingMode = VehicleDrivingMode.Arcade;
        context.Telemetry.SpeedUnitsPerSecond = _speedUnitsPerSecond;
        context.Telemetry.CurrentSpeedMph = normalizedSpeed * context.Pawn.TargetTopSpeedMph;
        context.Telemetry.SteeringInput = steering;
        context.Telemetry.NormalizedSpeed = normalizedSpeed;
        context.Telemetry.EngineRpm = MathHelper.Lerp(950f, 7200f, normalizedSpeed);
        context.Telemetry.MovementForward = _movementForward;
        context.Telemetry.SurfaceUp = context.Chassis.SurfaceUp;
        context.Telemetry.IsFallbackActive = fallbackActive;

        int gear = Math.Clamp(1 + (int)(5 * normalizedSpeed), 1, 5);
        if (gear != _lastReportedGear)
        {
            _tachometerAcceleration = 0f;
            _lastReportedGear = gear;
        }
        else if (elapsedTime > 0f)
        {
            float speedDelta = _speedUnitsPerSecond - previousSpeedUnitsPerSecond;
            float accelerationReference = speedDelta >= 0f
                ? ForwardAcceleration
                : Math.Max(IdleDeceleration, ReverseAcceleration);

            if (accelerationReference > 0.0001f)
            {
                float normalizedAcceleration = speedDelta / (accelerationReference * elapsedTime);
                float smoothingFactor = Math.Clamp(elapsedTime * 10f, 0f, 1f);
                _tachometerAcceleration += (normalizedAcceleration - _tachometerAcceleration) * smoothingFactor;
                _tachometerAcceleration = Math.Clamp(_tachometerAcceleration, -0.25f, 1f);
            }
        }

        context.Telemetry.CurrentGear = gear;
        context.Telemetry.TachometerAcceleration = _tachometerAcceleration;
    }

    private static float ApplyShoulderDeceleration(float speed, float shoulderDeceleration, float elapsedTime)
    {
        float deceleration = shoulderDeceleration * elapsedTime;
        if (Math.Abs(speed) <= deceleration)
        {
            return 0f;
        }

        return speed > 0f ? speed - deceleration : speed + deceleration;
    }

    private static float ApplyBarrierSpeedPenalty(float speed, RaceTrackPhysicsComponent trackPhysics, float impactStrength, float elapsedTime)
    {
        float retainFactor = MathHelper.Lerp(trackPhysics.BarrierGlancingSpeedRetainFactor, trackPhysics.EdgeSpeedRetainFactor, impactStrength);
        retainFactor = Math.Clamp(retainFactor, 0f, 1f);
        float frameRateAdjustedRetainFactor = MathF.Pow(retainFactor, Math.Clamp(elapsedTime * 60f, 0f, 8f));
        return speed * frameRateAdjustedRetainFactor;
    }

    private static TrackBarrierContact ResolveBarrierContact(RaceTrackPhysicsComponent trackPhysics, RaceTrackSurfaceSample surface, Vector3 movementForward)
    {
        Vector3 carForward = VehicleDynamicsMath.ProjectDirectionOntoSurface(movementForward, surface.Up, surface.Forward);
        Vector3 carRight = Vector3.Cross(surface.Up, carForward);
        carRight = VehicleDynamicsMath.NormalizeOrFallback(carRight, surface.Right);

        float halfCarWidth = RacingCarPawn.CollisionWidth * 0.5f;
        float halfCarLength = RacingCarPawn.CollisionLength * 0.5f;
        float lateralExtent =
            Math.Abs(Vector3.Dot(carRight, surface.Right)) * halfCarWidth +
            Math.Abs(Vector3.Dot(carForward, surface.Right)) * halfCarLength;

        float roadCenterHalfWidth = Math.Max(0f, surface.HalfWidth - lateralExtent);
        float guardRailHalfWidth = Math.Max(0f, surface.HalfWidth - trackPhysics.GuardRailInset);
        float barrierCenterHalfWidth = Math.Max(0f, guardRailHalfWidth - lateralExtent);
        bool touchedBarrier = Math.Abs(surface.LateralOffset) > barrierCenterHalfWidth;
        bool outsideRoadBounds = Math.Abs(surface.LateralOffset) > roadCenterHalfWidth;

        float resolvedCenterHalfWidth = touchedBarrier
            ? Math.Max(0f, barrierCenterHalfWidth - trackPhysics.BarrierContactMargin)
            : barrierCenterHalfWidth;

        float resolvedLateralOffset = Math.Clamp(surface.LateralOffset, -resolvedCenterHalfWidth, resolvedCenterHalfWidth);
        Vector3 barrierNormal = Vector3.Zero;
        float impactStrength = 0f;

        if (touchedBarrier)
        {
            barrierNormal = surface.LateralOffset >= 0f ? -surface.Right : surface.Right;
            barrierNormal = VehicleDynamicsMath.NormalizeOrFallback(barrierNormal, surface.Right);
            impactStrength = Math.Clamp(Math.Abs(Vector3.Dot(carForward, barrierNormal)), 0f, 1f);
        }

        return new TrackBarrierContact(
            resolvedLateralOffset,
            barrierCenterHalfWidth,
            roadCenterHalfWidth,
            lateralExtent,
            touchedBarrier,
            outsideRoadBounds,
            barrierNormal,
            impactStrength);
    }

    private static Vector3 ProjectDirectionAlongBarrier(Vector3 direction, Vector3 barrierNormal, Vector3 surfaceUp, Vector3 fallbackDirection)
    {
        if (barrierNormal.LengthSquared() < 0.0001f)
        {
            return VehicleDynamicsMath.ProjectDirectionOntoSurface(direction, surfaceUp, fallbackDirection);
        }

        float penetrationSpeed = Vector3.Dot(direction, barrierNormal);
        if (penetrationSpeed > 0f)
        {
            direction -= barrierNormal * penetrationSpeed;
        }

        return VehicleDynamicsMath.ProjectDirectionOntoSurface(direction, surfaceUp, fallbackDirection);
    }

    private void MaybeLogInputChange(RuntimeRaceSession? session, VehicleDynamicsExecutionContext context, float throttle, float steering)
    {
        if (session == null)
        {
            _lastLoggedThrottle = throttle;
            _lastLoggedSteering = steering;
            return;
        }

        if (throttle == _lastLoggedThrottle && steering == _lastLoggedSteering)
        {
            return;
        }

        _lastLoggedThrottle = throttle;
        _lastLoggedSteering = steering;
        bool inputEnabled = context.Pawn.Controller is RacingPlayerController racingPlayerController && racingPlayerController.IsInputEnable;
        session.AppendMovementDebug(
            "input",
            $"mode=arcade throttle={throttle:0.0} steering={steering:0.0} enabled={context.Pawn.InputEnabled}/{inputEnabled} speedUnits={_speedUnitsPerSecond:0.000} pos={FormatVector(context.Chassis.Position)}");
    }

    private void LogFallbackState(RuntimeRaceSession? session, bool fallbackEnabled, string reason)
    {
        if (_lastFallbackState == fallbackEnabled)
        {
            return;
        }

        _lastFallbackState = fallbackEnabled;
        session?.AppendMovementDebug(fallbackEnabled ? "fallback" : "surface", reason);
    }

    private void LogBoundsState(RuntimeRaceSession? session, RaceTrackPhysicsComponent trackPhysics, RaceTrackSurfaceSample surface, bool outsideRoadBounds, bool touchedBarrier, float allowedCenterHalfWidth)
    {
        if (session != null && touchedBarrier != _lastTouchedBarrier)
        {
            session.AppendMovementDebug(
                "barrier",
                touchedBarrier
                    ? $"hit barrier seg={surface.SegmentIndex} lateral={surface.LateralOffset:0.000} centerLimit={allowedCenterHalfWidth:0.000} guardRailInset={trackPhysics.GuardRailInset:0.000}"
                    : $"left barrier seg={surface.SegmentIndex} lateral={surface.LateralOffset:0.000}");
        }

        if (session != null && outsideRoadBounds != _lastOutsideRoadBounds)
        {
            session.AppendMovementDebug(
                "shoulder",
                outsideRoadBounds
                    ? $"entered shoulder seg={surface.SegmentIndex} lateral={surface.LateralOffset:0.000} roadHalfWidth={surface.HalfWidth:0.000}"
                    : $"returned to road seg={surface.SegmentIndex} lateral={surface.LateralOffset:0.000}");
        }

        _lastTouchedBarrier = touchedBarrier;
        _lastOutsideRoadBounds = outsideRoadBounds;
    }

    private void MaybeLogLargeDisplacement(RuntimeRaceSession? session, Vector3 startPosition, Vector3 desiredPosition, Vector3 resolvedPosition, RaceTrackSurfaceSample surface)
    {
        float frameDisplacement = Vector3.Distance(startPosition, resolvedPosition);
        if (session == null || frameDisplacement <= DebugTeleportDistanceThreshold)
        {
            return;
        }

        session.AppendMovementDebug(
            "jump",
            $"frameDisplacement={frameDisplacement:0.000} seg={surface.SegmentIndex} start={FormatVector(startPosition)} desired={FormatVector(desiredPosition)} resolved={FormatVector(resolvedPosition)} movementForward={FormatVector(_movementForward)} surfaceForward={FormatVector(surface.Forward)}");
    }

    private void MaybeLogFallbackSample(RuntimeRaceSession? session, VehicleDynamicsExecutionContext context, float throttle, float steering)
    {
        if (session == null || _debugElapsedSeconds < _nextDebugSampleSeconds)
        {
            return;
        }

        _nextDebugSampleSeconds = _debugElapsedSeconds + DebugSampleIntervalSeconds;
        session.AppendMovementDebug(
            "fallback-sample",
            $"mode=arcade speedUnits={_speedUnitsPerSecond:0.000} throttle={throttle:0.0} steering={steering:0.0} pos={FormatVector(context.Chassis.Position)} forward={FormatVector(context.Chassis.MovementForward)} wheels={VehicleDynamicsMath.BuildWheelDebugSummary(context.WheelStates)}");
    }

    private void MaybeLogMovementSample(RuntimeRaceSession? session, VehicleDynamicsExecutionContext context, float throttle, float steering, Vector3 desiredPosition, Vector3 resolvedPosition, RaceTrackSurfaceSample surface, bool touchedBarrier, bool outsideRoadBounds)
    {
        if (session == null)
        {
            return;
        }

        bool shouldSample = _debugElapsedSeconds <= DebugInitialSamplingWindowSeconds
            || Math.Abs(throttle) > 0f
            || Math.Abs(steering) > 0f
            || touchedBarrier
            || outsideRoadBounds;

        if (!shouldSample || _debugElapsedSeconds < _nextDebugSampleSeconds)
        {
            return;
        }

        _nextDebugSampleSeconds = _debugElapsedSeconds + DebugSampleIntervalSeconds;
        session.AppendMovementDebug(
            "sample",
            $"mode=arcade seg={surface.SegmentIndex} speedUnits={_speedUnitsPerSecond:0.000} speedMph={context.Telemetry.CurrentSpeedMph:0.0} throttle={throttle:0.0} steering={steering:0.0} desired={FormatVector(desiredPosition)} resolved={FormatVector(resolvedPosition)} movementForward={FormatVector(_movementForward)} surfaceForward={FormatVector(surface.Forward)} surfaceUp={FormatVector(surface.Up)} lateral={surface.LateralOffset:0.000}/{surface.HalfWidth:0.000} wheels={VehicleDynamicsMath.BuildWheelDebugSummary(context.WheelStates)}");
    }

    private void ResetDebugLoggingState()
    {
        _debugElapsedSeconds = 0f;
        _nextDebugSampleSeconds = 0f;
        _lastLoggedThrottle = float.NaN;
        _lastLoggedSteering = float.NaN;
        _lastFallbackState = null;
        _lastOutsideRoadBounds = false;
        _lastTouchedBarrier = false;
    }

    private static string FormatVector(Vector3 vector)
    {
        return $"({vector.X:0.000}, {vector.Y:0.000}, {vector.Z:0.000})";
    }

    private float ApplyIdleDeceleration(float speed, float elapsedTime)
    {
        float delta = IdleDeceleration * elapsedTime;
        if (Math.Abs(speed) <= delta)
        {
            return 0f;
        }

        return speed > 0f ? speed - delta : speed + delta;
    }

    private readonly struct TrackBarrierContact(
        float resolvedLateralOffset,
        float allowedCenterHalfWidth,
        float roadCenterHalfWidth,
        float lateralExtent,
        bool touchedBarrier,
        bool outsideRoadBounds,
        Vector3 barrierNormal,
        float impactStrength)
    {
        public float ResolvedLateralOffset { get; } = resolvedLateralOffset;

        public float AllowedCenterHalfWidth { get; } = allowedCenterHalfWidth;

        public float RoadCenterHalfWidth { get; } = roadCenterHalfWidth;

        public float LateralExtent { get; } = lateralExtent;

        public bool TouchedBarrier { get; } = touchedBarrier;

        public bool OutsideRoadBounds { get; } = outsideRoadBounds;

        public Vector3 BarrierNormal { get; } = barrierNormal;

        public float ImpactStrength { get; } = impactStrength;
    }
}