using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;
using RacingGameCasaEngine.Worlds;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Components;

public sealed class ArcadeCarMovementComponent : EntityComponent
{
    private const float DebugSampleIntervalSeconds = 0.15f;
    private const float DebugInitialSamplingWindowSeconds = 6.0f;
    private const float DebugTeleportDistanceThreshold = 1.25f;

    private float _speedUnitsPerSecond;
    private RaceTrackPhysicsComponent? _trackPhysicsComponent;
    private World? _trackPhysicsWorld;
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

    public float ForwardAcceleration { get; set; } = 20f;

    public float ReverseAcceleration { get; set; } = 14f;

    public float MaxForwardSpeedUnitsPerSecond { get; set; } = 36f;

    public float MaxReverseSpeedUnitsPerSecond { get; set; } = 12f;

    public float TurnRateRadiansPerSecond { get; set; } = 1.7f;

    public float IdleDeceleration { get; set; } = 18f;

    public ArcadeCarMovementComponent()
    {
    }

    private ArcadeCarMovementComponent(ArcadeCarMovementComponent other) : base(other)
    {
        _speedUnitsPerSecond = other._speedUnitsPerSecond;
        ForwardAcceleration = other.ForwardAcceleration;
        ReverseAcceleration = other.ReverseAcceleration;
        MaxForwardSpeedUnitsPerSecond = other.MaxForwardSpeedUnitsPerSecond;
        MaxReverseSpeedUnitsPerSecond = other.MaxReverseSpeedUnitsPerSecond;
        TurnRateRadiansPerSecond = other.TurnRateRadiansPerSecond;
        IdleDeceleration = other.IdleDeceleration;
    }

    public override EntityComponent Clone()
    {
        return new ArcadeCarMovementComponent(this);
    }

    public override void Update(float elapsedTime)
    {
        if (Owner is not RacingCarPawn pawn
            || pawn.RootComponent == null)
        {
            return;
        }

        if (pawn.Controller is not RacingPlayerController controller
            || !pawn.InputEnabled
            || !controller.IsInputEnable)
        {
            return;
        }

        var input = pawn.World?.Game?.InputComponent;
        if (input == null)
        {
            return;
        }

        RuntimeRaceSession? session = (pawn.World?.Game as RacingGameCasaEngineGame)?.RaceSession;
        float throttle = GetThrottle(input);
        float steering = GetSteering(input);
        _debugElapsedSeconds += elapsedTime;
        MaybeLogInputChange(session, pawn, controller, throttle, steering);

        UpdateSpeed(throttle, elapsedTime);

        RaceTrackPhysicsComponent? trackPhysics = ResolveTrackPhysics(pawn.World, session);
        if (trackPhysics == null
            || !trackPhysics.TrySampleSurface(pawn.RootComponent.LocalPosition, _segmentHint, out RaceTrackSurfaceSample currentSurface))
        {
            LogFallbackState(
                session,
                fallbackEnabled: true,
                trackPhysics == null
                    ? "track physics component unavailable"
                    : "surface sampling failed for current position");
            UpdateFallbackMovement(pawn, controller, steering, elapsedTime);
            UpdateTelemetry(pawn, steering);
            MaybeLogFallbackSample(session, pawn, throttle, steering);
            return;
        }

        LogFallbackState(
            session,
            fallbackEnabled: false,
            $"surface acquired seg={currentSurface.SegmentIndex} pos={FormatVector(pawn.RootComponent.LocalPosition)} lateral={currentSurface.LateralOffset:0.000}");

        _segmentHint = currentSurface.SegmentIndex;

        if (!_movementBasisInitialized)
        {
            Vector3 alignedTrackForward = currentSurface.Forward;
            if (Vector3.Dot(alignedTrackForward, pawn.RootComponent.Forward) < 0f)
            {
                alignedTrackForward = -alignedTrackForward;
            }

            _movementForward = ProjectDirectionOntoSurface(alignedTrackForward, currentSurface.Up, currentSurface.Forward);
            _movementBasisInitialized = true;
            session?.AppendMovementDebug(
                "basis",
                $"initialized seg={currentSurface.SegmentIndex} rootForward={FormatVector(pawn.RootComponent.Forward)} surfaceForward={FormatVector(currentSurface.Forward)} movementForward={FormatVector(_movementForward)} lateral={currentSurface.LateralOffset:0.000}");
        }
        else
        {
            _movementForward = ProjectDirectionOntoSurface(_movementForward, currentSurface.Up, currentSurface.Forward);
        }

        if (Math.Abs(_speedUnitsPerSecond) > 0.05f && Math.Abs(steering) > 0f)
        {
            float steeringScale = Math.Clamp(_speedUnitsPerSecond / MaxForwardSpeedUnitsPerSecond, -0.75f, 1f);
            float turnAmount = steering * controller.SteeringSensitivityScale * TurnRateRadiansPerSecond * steeringScale * elapsedTime;
            _movementForward = RotateDirectionAroundAxis(_movementForward, currentSurface.Up, turnAmount, currentSurface.Forward);
        }

        Vector3 startPosition = pawn.RootComponent.LocalPosition;
        Vector3 desiredPosition = startPosition + _movementForward * (_speedUnitsPerSecond * elapsedTime);
        if (!trackPhysics.TrySampleSurface(desiredPosition, _segmentHint, out RaceTrackSurfaceSample nextSurface))
        {
            nextSurface = currentSurface;
            session?.AppendMovementDebug(
                "surface",
                $"desired sample failed; reusing current surface seg={currentSurface.SegmentIndex} desired={FormatVector(desiredPosition)}");
        }

        _segmentHint = nextSurface.SegmentIndex;
        float allowedHalfWidth = nextSurface.HalfWidth + trackPhysics.ShoulderWidth;
        float clampedLateralOffset = Math.Clamp(nextSurface.LateralOffset, -allowedHalfWidth, allowedHalfWidth);
        bool touchedBarrier = Math.Abs(nextSurface.LateralOffset) > allowedHalfWidth;
        bool outsideRoadBounds = Math.Abs(nextSurface.LateralOffset) > nextSurface.HalfWidth;

        if (touchedBarrier)
        {
            _speedUnitsPerSecond *= trackPhysics.EdgeSpeedRetainFactor;
        }
        else if (outsideRoadBounds)
        {
            _speedUnitsPerSecond = ApplyShoulderDeceleration(_speedUnitsPerSecond, trackPhysics.ShoulderDeceleration, elapsedTime);
        }

        Vector3 resolvedPosition = nextSurface.Center + nextSurface.Right * clampedLateralOffset;
        pawn.RootComponent.LocalPosition = resolvedPosition;

        _movementForward = ProjectDirectionOntoSurface(_movementForward, nextSurface.Up, nextSurface.Forward);
        pawn.RootComponent.LocalOrientation = CreateSurfaceOrientation(_movementForward, nextSurface.Up);

        UpdateTelemetry(pawn, steering);
        LogBoundsState(session, trackPhysics, nextSurface, outsideRoadBounds, touchedBarrier);
        MaybeLogLargeDisplacement(session, startPosition, desiredPosition, resolvedPosition, nextSurface);
        MaybeLogMovementSample(session, pawn, throttle, steering, desiredPosition, resolvedPosition, nextSurface, touchedBarrier, outsideRoadBounds);
    }

    private void UpdateSpeed(float throttle, float elapsedTime)
    {
        if (throttle > 0f)
        {
            _speedUnitsPerSecond += ForwardAcceleration * elapsedTime;
        }
        else if (throttle < 0f)
        {
            _speedUnitsPerSecond -= ReverseAcceleration * elapsedTime;
        }
        else
        {
            _speedUnitsPerSecond = ApplyIdleDeceleration(_speedUnitsPerSecond, elapsedTime);
        }

        _speedUnitsPerSecond = Math.Clamp(
            _speedUnitsPerSecond,
            -MaxReverseSpeedUnitsPerSecond,
            MaxForwardSpeedUnitsPerSecond);
    }

    private void UpdateFallbackMovement(RacingCarPawn pawn, RacingPlayerController controller, float steering, float elapsedTime)
    {
        SceneComponent? rootComponent = pawn.RootComponent;
        if (rootComponent == null)
        {
            return;
        }

        if (Math.Abs(_speedUnitsPerSecond) > 0.05f && Math.Abs(steering) > 0f)
        {
            float steeringScale = Math.Clamp(_speedUnitsPerSecond / MaxForwardSpeedUnitsPerSecond, -0.75f, 1f);
            float turnAmount = steering * controller.SteeringSensitivityScale * TurnRateRadiansPerSecond * steeringScale * elapsedTime;
            Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, turnAmount);
            rootComponent.LocalOrientation = Quaternion.Normalize(rotation * rootComponent.LocalOrientation);
        }

        Vector3 nextPosition = rootComponent.LocalPosition + rootComponent.Forward * (_speedUnitsPerSecond * elapsedTime);
        rootComponent.LocalPosition = new Vector3(nextPosition.X, rootComponent.LocalPosition.Y, nextPosition.Z);
        _movementForward = rootComponent.Forward;
        _movementBasisInitialized = true;
    }

    private RaceTrackPhysicsComponent? ResolveTrackPhysics(World? world, RuntimeRaceSession? session)
    {
        if (world == null)
        {
            if (_trackPhysicsWorld != null)
            {
                session?.AppendMovementDebug("world", "current world became unavailable; clearing movement basis");
            }

            _trackPhysicsComponent = null;
            _trackPhysicsWorld = null;
            _segmentHint = -1;
            _movementBasisInitialized = false;
            ResetDebugLoggingState();
            return null;
        }

        if (!ReferenceEquals(world, _trackPhysicsWorld))
        {
            _trackPhysicsWorld = world;
            _trackPhysicsComponent = null;
            _segmentHint = 0;
            _movementBasisInitialized = false;
            ResetDebugLoggingState();
            session?.AppendMovementDebug("world", $"bound movement component to world type={world.GetType().Name}");

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
                    : $"race track physics component resolved shoulderWidth={_trackPhysicsComponent.ShoulderWidth:0.000} edgeRetain={_trackPhysicsComponent.EdgeSpeedRetainFactor:0.000}");
        }

        return _trackPhysicsComponent;
    }

    private void UpdateTelemetry(RacingCarPawn pawn, float steering)
    {
        float normalizedSpeed = Math.Clamp(Math.Abs(_speedUnitsPerSecond) / MaxForwardSpeedUnitsPerSecond, 0f, 1f);
        pawn.CurrentSpeedMph = normalizedSpeed * pawn.TargetTopSpeedMph;
        pawn.SteeringInput = steering;
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

    private static Vector3 RotateDirectionAroundAxis(Vector3 direction, Vector3 axis, float angle, Vector3 fallbackDirection)
    {
        Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angle);
        return ProjectDirectionOntoSurface(Vector3.Transform(direction, rotation), axis, fallbackDirection);
    }

    private static Vector3 ProjectDirectionOntoSurface(Vector3 direction, Vector3 surfaceUp, Vector3 fallbackDirection)
    {
        Vector3 projected = direction - surfaceUp * Vector3.Dot(direction, surfaceUp);
        if (projected.LengthSquared() < 0.0001f)
        {
            projected = fallbackDirection - surfaceUp * Vector3.Dot(fallbackDirection, surfaceUp);
        }

        if (projected.LengthSquared() < 0.0001f)
        {
            projected = Vector3.Cross(surfaceUp, Vector3.Right);
        }

        if (projected.LengthSquared() < 0.0001f)
        {
            projected = Vector3.Forward;
        }

        projected.Normalize();
        return projected;
    }

    private static Quaternion CreateSurfaceOrientation(Vector3 forward, Vector3 surfaceUp)
    {
        Vector3 normalizedForward = ProjectDirectionOntoSurface(forward, surfaceUp, Vector3.Forward);
        Vector3 normalizedUp = surfaceUp;
        if (normalizedUp.LengthSquared() < 0.0001f)
        {
            normalizedUp = Vector3.Up;
        }
        else
        {
            normalizedUp.Normalize();
        }

        Matrix orientation = Matrix.CreateWorld(Vector3.Zero, normalizedForward, normalizedUp);
        return Quaternion.CreateFromRotationMatrix(orientation);
    }

    private static float GetThrottle(CasaEngine.Framework.Input.InputComponent input)
    {
        bool accelerate = input.KeyboardManager.IsKeyPressed(XnaKeys.W) || input.KeyboardManager.IsKeyPressed(XnaKeys.Up);
        bool brake = input.KeyboardManager.IsKeyPressed(XnaKeys.S) || input.KeyboardManager.IsKeyPressed(XnaKeys.Down);

        if (accelerate == brake)
        {
            return 0f;
        }

        return accelerate ? 1f : -1f;
    }

    private static float GetSteering(CasaEngine.Framework.Input.InputComponent input)
    {
        bool left = input.KeyboardManager.IsKeyPressed(XnaKeys.A) || input.KeyboardManager.IsKeyPressed(XnaKeys.Left);
        bool right = input.KeyboardManager.IsKeyPressed(XnaKeys.D) || input.KeyboardManager.IsKeyPressed(XnaKeys.Right);

        if (left == right)
        {
            return 0f;
        }

        return left ? 1f : -1f;
    }

    private void MaybeLogInputChange(RuntimeRaceSession? session, RacingCarPawn pawn, RacingPlayerController controller, float throttle, float steering)
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
        session.AppendMovementDebug(
            "input",
            $"throttle={throttle:0.0} steering={steering:0.0} enabled={pawn.InputEnabled}/{controller.IsInputEnable} speedUnits={_speedUnitsPerSecond:0.000} pos={FormatVector(pawn.RootComponent?.LocalPosition ?? Vector3.Zero)}");
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

    private void LogBoundsState(RuntimeRaceSession? session, RaceTrackPhysicsComponent trackPhysics, RaceTrackSurfaceSample surface, bool outsideRoadBounds, bool touchedBarrier)
    {
        if (session != null && touchedBarrier != _lastTouchedBarrier)
        {
            session.AppendMovementDebug(
                "barrier",
                touchedBarrier
                    ? $"hit barrier seg={surface.SegmentIndex} lateral={surface.LateralOffset:0.000} allowed={surface.HalfWidth + trackPhysics.ShoulderWidth:0.000}"
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

    private void MaybeLogFallbackSample(RuntimeRaceSession? session, RacingCarPawn pawn, float throttle, float steering)
    {
        if (session == null || _debugElapsedSeconds < _nextDebugSampleSeconds)
        {
            return;
        }

        _nextDebugSampleSeconds = _debugElapsedSeconds + DebugSampleIntervalSeconds;
        session.AppendMovementDebug(
            "fallback-sample",
            $"speedUnits={_speedUnitsPerSecond:0.000} throttle={throttle:0.0} steering={steering:0.0} pos={FormatVector(pawn.RootComponent?.LocalPosition ?? Vector3.Zero)} forward={FormatVector(pawn.RootComponent?.Forward ?? Vector3.Forward)}");
    }

    private void MaybeLogMovementSample(RuntimeRaceSession? session, RacingCarPawn pawn, float throttle, float steering, Vector3 desiredPosition, Vector3 resolvedPosition, RaceTrackSurfaceSample surface, bool touchedBarrier, bool outsideRoadBounds)
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
            $"seg={surface.SegmentIndex} speedUnits={_speedUnitsPerSecond:0.000} speedMph={pawn.CurrentSpeedMph:0.0} throttle={throttle:0.0} steering={steering:0.0} desired={FormatVector(desiredPosition)} resolved={FormatVector(resolvedPosition)} movementForward={FormatVector(_movementForward)} rootForward={FormatVector(pawn.RootComponent?.Forward ?? Vector3.Forward)} surfaceForward={FormatVector(surface.Forward)} surfaceUp={FormatVector(surface.Up)} lateral={surface.LateralOffset:0.000}/{surface.HalfWidth:0.000} dist2={surface.DistanceSquared:0.000}");
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
}