using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using CasaEngine.Framework.Gameplay;
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
    private const float GamePadStickDeadZone = 0.12f;
    private const float GamePadTriggerDeadZone = 0.08f;

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
    private float _tachometerAcceleration;
    private int _lastReportedGear = 1;

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
        float throttle = GetThrottle(input, controller);
        float steering = GetSteering(input, controller);
        _debugElapsedSeconds += elapsedTime;
        MaybeLogInputChange(session, pawn, controller, throttle, steering);

        float previousSpeedUnitsPerSecond = _speedUnitsPerSecond;
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
            UpdateTelemetry(pawn, steering, previousSpeedUnitsPerSecond, elapsedTime);
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

        Vector3 nextMovementForward = ProjectDirectionOntoSurface(_movementForward, nextSurface.Up, nextSurface.Forward);
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
        pawn.RootComponent.LocalPosition = resolvedPosition;

        _movementForward = ProjectDirectionOntoSurface(nextMovementForward, nextSurface.Up, nextSurface.Forward);
        pawn.RootComponent.LocalOrientation = CreateSurfaceOrientation(_movementForward, nextSurface.Up);

        UpdateTelemetry(pawn, steering, previousSpeedUnitsPerSecond, elapsedTime);
        LogBoundsState(session, trackPhysics, nextSurface, barrierContact.OutsideRoadBounds, barrierContact.TouchedBarrier, barrierContact.AllowedCenterHalfWidth);
        MaybeLogLargeDisplacement(session, startPosition, desiredPosition, resolvedPosition, nextSurface);
        MaybeLogMovementSample(session, pawn, throttle, steering, desiredPosition, resolvedPosition, nextSurface, barrierContact.TouchedBarrier, barrierContact.OutsideRoadBounds);
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
                    : $"race track physics component resolved shoulderWidth={_trackPhysicsComponent.ShoulderWidth:0.000} guardRailInset={_trackPhysicsComponent.GuardRailInset:0.000} barrierRetain={_trackPhysicsComponent.BarrierGlancingSpeedRetainFactor:0.000}/{_trackPhysicsComponent.EdgeSpeedRetainFactor:0.000}");
        }

        return _trackPhysicsComponent;
    }

    private void UpdateTelemetry(RacingCarPawn pawn, float steering, float previousSpeedUnitsPerSecond, float elapsedTime)
    {
        float normalizedSpeed = Math.Clamp(Math.Abs(_speedUnitsPerSecond) / MaxForwardSpeedUnitsPerSecond, 0f, 1f);
        pawn.CurrentSpeedMph = normalizedSpeed * pawn.TargetTopSpeedMph;
        pawn.SteeringInput = steering;

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

        pawn.CurrentGear = gear;
        pawn.TachometerAcceleration = _tachometerAcceleration;
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
        float retainFactor = MathHelper.Lerp(
            trackPhysics.BarrierGlancingSpeedRetainFactor,
            trackPhysics.EdgeSpeedRetainFactor,
            impactStrength);

        retainFactor = Math.Clamp(retainFactor, 0f, 1f);
        float frameRateAdjustedRetainFactor = MathF.Pow(retainFactor, Math.Clamp(elapsedTime * 60f, 0f, 8f));
        return speed * frameRateAdjustedRetainFactor;
    }

    private static TrackBarrierContact ResolveBarrierContact(RaceTrackPhysicsComponent trackPhysics, RaceTrackSurfaceSample surface, Vector3 movementForward)
    {
        Vector3 carForward = ProjectDirectionOntoSurface(movementForward, surface.Up, surface.Forward);
        Vector3 carRight = Vector3.Cross(surface.Up, carForward);
        carRight = NormalizeOrFallback(carRight, surface.Right);

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
            barrierNormal = NormalizeOrFallback(barrierNormal, surface.Right);
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
            return ProjectDirectionOntoSurface(direction, surfaceUp, fallbackDirection);
        }

        float penetrationSpeed = Vector3.Dot(direction, barrierNormal);
        if (penetrationSpeed > 0f)
        {
            direction -= barrierNormal * penetrationSpeed;
        }

        return ProjectDirectionOntoSurface(direction, surfaceUp, fallbackDirection);
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

    private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
    {
        if (value.LengthSquared() < 0.0001f)
        {
            value = fallback;
        }

        if (value.LengthSquared() < 0.0001f)
        {
            return Vector3.Right;
        }

        value.Normalize();
        return value;
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

    private static float GetThrottle(CasaEngine.Framework.Input.InputComponent input, RacingPlayerController controller)
    {
        bool accelerate = input.KeyboardManager.IsKeyPressed(XnaKeys.W) || input.KeyboardManager.IsKeyPressed(XnaKeys.Up);
        bool brake = input.KeyboardManager.IsKeyPressed(XnaKeys.S) || input.KeyboardManager.IsKeyPressed(XnaKeys.Down);

        float keyboardThrottle = 0f;
        if (accelerate != brake)
        {
            keyboardThrottle = accelerate ? 1f : -1f;
        }

        CasaEngine.Engine.Input.GamePad playerGamePad = GetPlayerGamePad(input, controller);
        if (!playerGamePad.IsConnected)
        {
            return keyboardThrottle;
        }

        float accelerateValue = Math.Max(
            ApplyAnalogDeadZone(playerGamePad.RightTrigger, GamePadTriggerDeadZone),
            playerGamePad.APressed || playerGamePad.DPadUpPressed ? 1f : 0f);
        float brakeValue = Math.Max(
            ApplyAnalogDeadZone(playerGamePad.LeftTrigger, GamePadTriggerDeadZone),
            playerGamePad.BPressed || playerGamePad.DPadDownPressed ? 1f : 0f);
        float gamePadThrottle = Math.Clamp(accelerateValue - brakeValue, -1f, 1f);

        if (Math.Abs(keyboardThrottle) >= Math.Abs(gamePadThrottle))
        {
            return keyboardThrottle;
        }

        return gamePadThrottle;
    }

    private static float GetSteering(CasaEngine.Framework.Input.InputComponent input, RacingPlayerController controller)
    {
        bool left = input.KeyboardManager.IsKeyPressed(XnaKeys.A) || input.KeyboardManager.IsKeyPressed(XnaKeys.Left);
        bool right = input.KeyboardManager.IsKeyPressed(XnaKeys.D) || input.KeyboardManager.IsKeyPressed(XnaKeys.Right);

        float keyboardSteering = 0f;
        if (left != right)
        {
            keyboardSteering = left ? 1f : -1f;
        }

        CasaEngine.Engine.Input.GamePad playerGamePad = GetPlayerGamePad(input, controller);
        if (!playerGamePad.IsConnected)
        {
            return keyboardSteering;
        }

        float analogSteering = ApplySignedDeadZone(-playerGamePad.LeftStickX, GamePadStickDeadZone);
        float dpadSteering = 0f;
        if (playerGamePad.DPadLeftPressed != playerGamePad.DPadRightPressed)
        {
            dpadSteering = playerGamePad.DPadLeftPressed ? 1f : -1f;
        }

        float gamePadSteering = Math.Clamp(analogSteering + dpadSteering, -1f, 1f);
        if (Math.Abs(keyboardSteering) >= Math.Abs(gamePadSteering))
        {
            return keyboardSteering;
        }

        return gamePadSteering;
    }

    private static CasaEngine.Engine.Input.GamePad GetPlayerGamePad(CasaEngine.Framework.Input.InputComponent input, RacingPlayerController controller)
    {
        PlayerIndex playerIndex = controller.Player is LocalPlayer localPlayer
            ? localPlayer.ControllerId
            : PlayerIndex.One;
        return input.GamePadManager.GetGamePad(playerIndex);
    }

    private static float ApplyAnalogDeadZone(float value, float deadZone)
    {
        if (value <= deadZone)
        {
            return 0f;
        }

        return Math.Clamp((value - deadZone) / (1f - deadZone), 0f, 1f);
    }

    private static float ApplySignedDeadZone(float value, float deadZone)
    {
        float absoluteValue = Math.Abs(value);
        if (absoluteValue <= deadZone)
        {
            return 0f;
        }

        float normalized = (absoluteValue - deadZone) / (1f - deadZone);
        return Math.Clamp(MathF.Sign(value) * normalized, -1f, 1f);
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