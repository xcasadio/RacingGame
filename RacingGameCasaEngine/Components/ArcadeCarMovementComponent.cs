using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.GameFramework;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Components;

public sealed class ArcadeCarMovementComponent : EntityComponent
{
    private float _speedUnitsPerSecond;

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

        float throttle = GetThrottle(input);
        float steering = GetSteering(input);

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

        if (Math.Abs(_speedUnitsPerSecond) > 0.05f && Math.Abs(steering) > 0f)
        {
            float steeringScale = Math.Clamp(_speedUnitsPerSecond / MaxForwardSpeedUnitsPerSecond, -0.75f, 1f);
            float turnAmount = steering * controller.SteeringSensitivityScale * TurnRateRadiansPerSecond * steeringScale * elapsedTime;
            Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, turnAmount);
            pawn.RootComponent.LocalOrientation = Quaternion.Normalize(rotation * pawn.RootComponent.LocalOrientation);
        }

        Vector3 nextPosition = pawn.RootComponent.LocalPosition + pawn.RootComponent.Forward * (_speedUnitsPerSecond * elapsedTime);
        pawn.RootComponent.LocalPosition = new Vector3(nextPosition.X, pawn.RootComponent.LocalPosition.Y, nextPosition.Z);

        float normalizedSpeed = Math.Clamp(Math.Abs(_speedUnitsPerSecond) / MaxForwardSpeedUnitsPerSecond, 0f, 1f);
        pawn.CurrentSpeedMph = normalizedSpeed * pawn.TargetTopSpeedMph;
        pawn.SteeringInput = steering;
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