using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaPoint = Microsoft.Xna.Framework.Point;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Components;

public sealed class DebugFreeCameraComponent : EntityComponent
{
    private const float PitchLimitRadians = 1.45f;

    private float _yaw;
    private float _pitch;
    private Vector3 _up = Vector3.Up;
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
        if (Owner?.RootComponent is not PerspectiveCameraComponent camera)
        {
            return;
        }

        if (Owner.World?.Game is not IRaceGameHost game || !game.IsActive)
        {
            return;
        }

        RuntimeRaceSession session = game.RaceSession;
        if (!session.IsActive || !session.IsDebugCameraEnabled || session.IsCameraPosedByAutomation)
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

        // The camera is posed by its transform (it has no parent). The up vector is the one the camera had when the
        // free camera was switched on: it does not follow the orientation written here.
        camera.LocalPosition = camera.Position + displacement * (MoveSpeedUnitsPerSecond * elapsedTime);
        camera.LocalOrientation = Quaternion.CreateFromRotationMatrix(Matrix.CreateWorld(Vector3.Zero, forward, _up));
        CenterMouse(game);
    }

    private void InitializeFromCamera(PerspectiveCameraComponent camera)
    {
        _up = camera.Up;
        Vector3 forward = camera.Forward;
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

    private static XnaPoint GetMouseCenter(IRaceGameHost game)
    {
        XnaRectangle bounds = game.Window.ClientBounds;
        return new XnaPoint(bounds.Width / 2, bounds.Height / 2);
    }

    private static void CenterMouse(IRaceGameHost game)
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