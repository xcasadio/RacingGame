using CasaEngine.Framework.Entities.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RacingGameCasaEngine.Bootstrap;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaPoint = Microsoft.Xna.Framework.Point;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace RacingGameCasaEngine.Components;

public sealed class DebugFreeCameraComponent : EntityComponent
{
    private const float PitchLimitRadians = 1.45f;
    private const float TargetDistance = 10.0f;

    private float _yaw;
    private float _pitch;
    private bool _wasActive;

    public float MoveSpeedUnitsPerSecond { get; set; } = 18.0f;

    public float LookSensitivity { get; set; } = 0.0065f;

    public override EntityComponent Clone()
    {
        return new DebugFreeCameraComponent
        {
            MoveSpeedUnitsPerSecond = MoveSpeedUnitsPerSecond,
            LookSensitivity = LookSensitivity,
        };
    }

    public override void Update(float elapsedTime)
    {
        if (Owner?.RootComponent is not CameraLookAtComponent camera)
        {
            return;
        }

        if (Owner.World?.Game is not RacingGameCasaEngineGame game || !game.IsActive)
        {
            return;
        }

        RuntimeRaceSession session = game.RaceSession;
        if (!session.IsActive || !session.IsDebugCameraEnabled)
        {
            _wasActive = false;
            return;
        }

        if (!_wasActive)
        {
            InitializeFromCamera(camera);
            CenterMouse(game);
            _wasActive = true;
            return;
        }

        XnaPoint mouseCenter = GetMouseCenter(game);
        XnaPoint mousePosition = game.InputComponent.MouseManager.Position;
        float mouseOffsetX = mousePosition.X - mouseCenter.X;
        float mouseOffsetY = mousePosition.Y - mouseCenter.Y;

        _yaw -= mouseOffsetX * LookSensitivity;
        _pitch = Math.Clamp(_pitch - mouseOffsetY * LookSensitivity, -PitchLimitRadians, PitchLimitRadians);

        Quaternion orientation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0.0f);
        Vector3 forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, orientation));
        Vector3 right = Vector3.Cross(forward, Vector3.Up);
        if (right.LengthSquared() < 0.0001f)
        {
            right = Vector3.Right;
        }
        else
        {
            right.Normalize();
        }

        float forwardAxis = 0.0f;
        float strafeAxis = 0.0f;
        float verticalAxis = 0.0f;

        KeyboardState keyboardState = game.InputComponent.KeyboardManager.State;
        if (keyboardState.IsKeyDown(XnaKeys.Up))
        {
            forwardAxis += 1.0f;
        }

        if (keyboardState.IsKeyDown(XnaKeys.Down))
        {
            forwardAxis -= 1.0f;
        }

        if (keyboardState.IsKeyDown(XnaKeys.Right))
        {
            strafeAxis += 1.0f;
        }

        if (keyboardState.IsKeyDown(XnaKeys.Left))
        {
            strafeAxis -= 1.0f;
        }

        if (keyboardState.IsKeyDown(XnaKeys.PageUp))
        {
            verticalAxis += 1.0f;
        }

        if (keyboardState.IsKeyDown(XnaKeys.PageDown))
        {
            verticalAxis -= 1.0f;
        }

        Vector3 displacement = forward * forwardAxis + right * strafeAxis + Vector3.Up * verticalAxis;
        if (displacement.LengthSquared() > 1.0f)
        {
            displacement.Normalize();
        }

        Vector3 position = camera.Position + displacement * (MoveSpeedUnitsPerSecond * elapsedTime);
        camera.SetPositionAndTarget(position, position + forward * TargetDistance);
        CenterMouse(game);
    }

    private void InitializeFromCamera(CameraLookAtComponent camera)
    {
        Vector3 forward = camera.Target - camera.Position;
        if (forward.LengthSquared() < 0.0001f)
        {
            forward = Vector3.Forward;
        }
        else
        {
            forward.Normalize();
        }

        _yaw = MathF.Atan2(forward.X, -forward.Z);
        _pitch = Math.Clamp(MathF.Asin(forward.Y), -PitchLimitRadians, PitchLimitRadians);
    }

    private static XnaPoint GetMouseCenter(RacingGameCasaEngineGame game)
    {
        XnaRectangle bounds = game.Window.ClientBounds;
        return new XnaPoint(bounds.Width / 2, bounds.Height / 2);
    }

    private static void CenterMouse(RacingGameCasaEngineGame game)
    {
        XnaRectangle bounds = game.Window.ClientBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        XnaPoint center = GetMouseCenter(game);
        Mouse.SetPosition(center.X, center.Y);
    }
}