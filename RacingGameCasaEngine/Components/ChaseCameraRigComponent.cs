using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Gameplay;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;

namespace RacingGameCasaEngine.Components;

public sealed class ChaseCameraRigComponent : EntityComponent
{
    private Vector3 _smoothedPosition;
    private Vector3 _smoothedForward = Vector3.Forward;
    private Vector3 _smoothedUp = Vector3.Up;
    private float _smoothedZoomFactor = 1.0f;
    private float _targetZoomFactor = 1.0f;
    private float _finishOrbitAngle;
    private bool _hasInitializedPosition;
    private bool _hasInitializedOrientation;
    private bool _wasDebugCameraEnabled;

    public float FollowDistance { get; set; } = 8.5f;

    public float FollowHeight { get; set; } = 3.2f;

    public float LookAheadDistance { get; set; } = 9f;

    public float DynamicDistanceBySpeed { get; set; } = 6.0f;

    public float DynamicHeightBySpeed { get; set; } = 1.1f;

    public float DynamicLookAheadBySpeed { get; set; } = 6.5f;

    public float PositionSmoothing { get; set; } = 6f;

    public float OrientationSmoothing { get; set; } = 7.5f;

    public float ZoomSmoothing { get; set; } = 7.0f;

    public float ZoomChangeRate { get; set; } = 1.2f;

    public float MinZoomFactor { get; set; } = 0.55f;

    public float MaxZoomFactor { get; set; } = 1.85f;

    public float FinishOrbitDistance { get; set; } = 15.0f;

    public float FinishOrbitHeight { get; set; } = 5.5f;

    public float FinishOrbitSpeedRadiansPerSecond { get; set; } = MathHelper.TwoPi / 4.25f;

    public override EntityComponent Clone()
    {
        return new ChaseCameraRigComponent
        {
            FollowDistance = FollowDistance,
            FollowHeight = FollowHeight,
            LookAheadDistance = LookAheadDistance,
            DynamicDistanceBySpeed = DynamicDistanceBySpeed,
            DynamicHeightBySpeed = DynamicHeightBySpeed,
            DynamicLookAheadBySpeed = DynamicLookAheadBySpeed,
            PositionSmoothing = PositionSmoothing,
            OrientationSmoothing = OrientationSmoothing,
            ZoomSmoothing = ZoomSmoothing,
            ZoomChangeRate = ZoomChangeRate,
            MinZoomFactor = MinZoomFactor,
            MaxZoomFactor = MaxZoomFactor,
            FinishOrbitDistance = FinishOrbitDistance,
            FinishOrbitHeight = FinishOrbitHeight,
            FinishOrbitSpeedRadiansPerSecond = FinishOrbitSpeedRadiansPerSecond,
        };
    }

    public override void Update(float elapsedTime)
    {
        if (Owner?.RootComponent is not CameraLookAtComponent camera)
        {
            return;
        }

        if (Owner.World?.Game is not RacingGameCasaEngineGame game)
        {
            return;
        }

        bool debugCameraEnabled = game.RaceSession.IsDebugCameraEnabled;
        if (debugCameraEnabled)
        {
            _wasDebugCameraEnabled = true;
            return;
        }

        if (_wasDebugCameraEnabled)
        {
            _hasInitializedPosition = false;
            _hasInitializedOrientation = false;
            _wasDebugCameraEnabled = false;
        }

        RuntimeRaceSession session = game.RaceSession;
        RacingCarPawn? pawn = session.PlayerPawn;
        RaceGameMode? gameMode = session.GameMode;
        if (pawn?.RootComponent == null)
        {
            return;
        }

        UpdateZoom(game, session.PlayerController, elapsedTime, gameMode?.IsRaceFinished == true);

        Vector3 anchor = pawn.GetChaseCameraFocusPosition();
        Vector3 targetForward = NormalizeOrFallback(pawn.GetMovementForward(), pawn.GetVisualForward(), Vector3.Forward);
        Vector3 targetUp = NormalizeOrFallback(pawn.RootComponent.Up, Vector3.Up);
        UpdateSmoothedOrientation(targetForward, targetUp, elapsedTime);

        if (gameMode?.IsRaceFinished == true)
        {
            UpdateFinishOrbit(camera, anchor, elapsedTime);
            return;
        }

        UpdateFollowCamera(camera, anchor, pawn, elapsedTime);
    }

    private void UpdateFollowCamera(CameraLookAtComponent camera, Vector3 anchor, RacingCarPawn pawn, float elapsedTime)
    {
        float speedRatio = pawn.TargetTopSpeedMph <= 0.01f
            ? 0f
            : Math.Clamp(pawn.CurrentSpeedMph / pawn.TargetTopSpeedMph, 0f, 1f);
        float desiredDistance = (FollowDistance + DynamicDistanceBySpeed * speedRatio) * _smoothedZoomFactor;
        float desiredHeight = FollowHeight + DynamicHeightBySpeed * speedRatio + (_smoothedZoomFactor - 1f) * 1.35f;
        float desiredLookAhead = LookAheadDistance + DynamicLookAheadBySpeed * speedRatio;
        Vector3 desiredPosition = anchor - _smoothedForward * desiredDistance + _smoothedUp * desiredHeight;
        Vector3 lookTarget = anchor + _smoothedForward * desiredLookAhead + _smoothedUp * 0.45f;
        ApplyCameraPose(camera, desiredPosition, lookTarget, elapsedTime);
    }

    private void UpdateFinishOrbit(CameraLookAtComponent camera, Vector3 anchor, float elapsedTime)
    {
        _finishOrbitAngle += FinishOrbitSpeedRadiansPerSecond * elapsedTime;
        if (_finishOrbitAngle > MathHelper.TwoPi)
        {
            _finishOrbitAngle -= MathHelper.TwoPi;
        }

        float orbitDistance = Math.Max(FinishOrbitDistance, FollowDistance * _smoothedZoomFactor + DynamicDistanceBySpeed * 0.75f);
        Vector3 baseOffset = -_smoothedForward * orbitDistance;
        Matrix orbitRotation = Matrix.CreateFromAxisAngle(_smoothedUp, _finishOrbitAngle);
        Vector3 orbitOffset = Vector3.TransformNormal(baseOffset, orbitRotation);
        Vector3 desiredPosition = anchor + orbitOffset + _smoothedUp * FinishOrbitHeight;
        Vector3 lookTarget = anchor + _smoothedUp * 0.9f;
        ApplyCameraPose(camera, desiredPosition, lookTarget, elapsedTime);
    }

    private void UpdateZoom(RacingGameCasaEngineGame game, RacingPlayerController? playerController, float elapsedTime, bool isRaceFinished)
    {
        if (isRaceFinished)
        {
            return;
        }

        float zoomInput = 0f;
        if (game.InputComponent.KeyboardManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.PageUp))
        {
            zoomInput -= 1f;
        }

        if (game.InputComponent.KeyboardManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.PageDown))
        {
            zoomInput += 1f;
        }

        CasaEngine.Engine.Input.GamePad? gamePad = TryGetPlayerGamePad(game, playerController);
        if (gamePad?.IsConnected == true)
        {
            if (gamePad.XPressed)
            {
                zoomInput -= 1f;
            }

            if (gamePad.YPressed)
            {
                zoomInput += 1f;
            }
        }

        if (zoomInput != 0f)
        {
            _targetZoomFactor = Math.Clamp(_targetZoomFactor + zoomInput * ZoomChangeRate * elapsedTime, MinZoomFactor, MaxZoomFactor);
        }

        if (!_hasInitializedOrientation)
        {
            _smoothedZoomFactor = _targetZoomFactor;
            return;
        }

        float blend = 1f - MathF.Exp(-ZoomSmoothing * elapsedTime);
        _smoothedZoomFactor = MathHelper.Lerp(_smoothedZoomFactor, _targetZoomFactor, blend);
    }

    private void UpdateSmoothedOrientation(Vector3 forward, Vector3 up, float elapsedTime)
    {
        if (!_hasInitializedOrientation)
        {
            _smoothedForward = forward;
            _smoothedUp = up;
            _hasInitializedOrientation = true;
            return;
        }

        float blend = 1f - MathF.Exp(-OrientationSmoothing * elapsedTime);
        _smoothedForward = NormalizeOrFallback(Vector3.Lerp(_smoothedForward, forward, blend), forward, Vector3.Forward);
        _smoothedUp = NormalizeOrFallback(Vector3.Lerp(_smoothedUp, up, blend), up, Vector3.Up);

        Vector3 right = NormalizeOrFallback(Vector3.Cross(_smoothedUp, _smoothedForward), Vector3.Right);
        _smoothedForward = NormalizeOrFallback(Vector3.Cross(right, _smoothedUp), _smoothedForward, Vector3.Forward);
        _smoothedUp = NormalizeOrFallback(Vector3.Cross(_smoothedForward, right), _smoothedUp, Vector3.Up);
    }

    private void ApplyCameraPose(CameraLookAtComponent camera, Vector3 desiredPosition, Vector3 lookTarget, float elapsedTime)
    {
        if (!_hasInitializedPosition)
        {
            _smoothedPosition = desiredPosition;
            _hasInitializedPosition = true;
        }
        else
        {
            float blend = 1f - MathF.Exp(-PositionSmoothing * elapsedTime);
            _smoothedPosition = Vector3.Lerp(_smoothedPosition, desiredPosition, blend);
        }

        camera.SetPositionAndTarget(_smoothedPosition, lookTarget);
        camera.LocalOrientation = CreateCameraOrientation(lookTarget - _smoothedPosition, _smoothedUp);
    }

    private static Quaternion CreateCameraOrientation(Vector3 forward, Vector3 up)
    {
        Vector3 normalizedForward = NormalizeOrFallback(forward, Vector3.Forward);
        Vector3 normalizedUp = NormalizeOrFallback(up, Vector3.Up);
        Matrix orientation = Matrix.CreateWorld(Vector3.Zero, normalizedForward, normalizedUp);
        return Quaternion.CreateFromRotationMatrix(orientation);
    }

    private static CasaEngine.Engine.Input.GamePad? TryGetPlayerGamePad(RacingGameCasaEngineGame game, RacingPlayerController? playerController)
    {
        if (playerController?.Player is not LocalPlayer localPlayer)
        {
            return null;
        }

        return game.InputComponent.GamePadManager.GetGamePad(localPlayer.ControllerId);
    }

    private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
    {
        if (value.LengthSquared() < 0.001f)
        {
            value = fallback;
        }

        if (value.LengthSquared() < 0.001f)
        {
            return Vector3.Forward;
        }

        value.Normalize();
        return value;
    }

    private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback, Vector3 finalFallback)
    {
        if (value.LengthSquared() < 0.001f)
        {
            value = fallback;
        }

        return NormalizeOrFallback(value, finalFallback);
    }
}