"""
QUAL-004: Extract magic numbers to named constants in CarPhysics.cs and Player.cs.
"""
import re, sys, pathlib

ROOT = pathlib.Path(__file__).parent.parent
CAR = ROOT / "RacingGame.Shared/GameLogic/CarPhysics.cs"
PLAYER = ROOT / "RacingGame.Shared/GameLogic/Player.cs"

# ---------------------------------------------------------------------------
# CarPhysics.cs
# ---------------------------------------------------------------------------
NEW_CONSTANTS = """
    // Spring physics for car pitch
    /// <summary>Friction coefficient for the car pitch spring simulation.</summary>
    private const float PitchSpringFriction = 1.5f;
    /// <summary>Spring constant (stiffness) for the car pitch simulation.</summary>
    private const int PitchSpringConstant = 120;

    // Steering feel
    /// <summary>Per-frame rotation damping factor (applied each update to bleed off rotation).</summary>
    private const float RotationFrictionFactor = 0.95f;
    /// <summary>Divisor applied to keyboard-input rotation to tune turning speed.</summary>
    private const float KeyboardRotationDivisor = 2.5f;
    /// <summary>Divisor that scales mouse-X delta into a rotation change.</summary>
    private const float MouseSteeringDivisor = 15.0f;
    /// <summary>Divisor for gamepad analog-stick rotation (tuned for analog feel).</summary>
    private const float GamePadAnalogStickDivisor = 1.12345f;
    /// <summary>Divisor for gamepad D-pad rotation (identical to keyboard feel).</summary>
    private const float GamePadDPadRotationDivisor = 1.5f;
    /// <summary>Multiplier that sets the maximum allowed rotation per update.</summary>
    private const float MaxRotationMultiplier = 1.25f;
    /// <summary>Speed (m/s) below which rotation is progressively reduced.</summary>
    private const float LowSpeedThreshold = 10.0f;
    /// <summary>Base rotation scale applied at zero speed (linear ramp starts here).</summary>
    private const float LowSpeedRotationBase = 0.67f;
    /// <summary>Slope of the linear rotation-scale ramp applied below LowSpeedThreshold.</summary>
    private const float LowSpeedRotationFactor = 0.33f;
    /// <summary>High-speed rotation divisor — reduces over-steer at top speeds.</summary>
    private const float HighSpeedRotationDivisor = 100.0f;
    /// <summary>Denominator used to smooth rotation interpolation over ~200 ms.</summary>
    private const float RotationInterpolationFactor = 0.225f;

    // View distance
    /// <summary>Rate (units/sec) at which view distance changes via page-up/down.</summary>
    private const float ViewDistanceChangeRate = 2.0f;
    /// <summary>Divisor that converts mouse-wheel delta into a view-distance change.</summary>
    private const float MouseWheelViewDivisor = 500.0f;

    // Physics application
    /// <summary>Scalar that converts acceleration force into the internal force units.</summary>
    private const float AccelerationForceFactor = 85.0f;
    /// <summary>Factor applied to speed when advancing car position each frame.</summary>
    private const float CarSpeedPositionFactor = 1.75f;
    /// <summary>Maximum allowed speed change per second (caps brake/acceleration).</summary>
    private const float MaxSpeedChangePerSec = 100.0f;

    // Collision response
    /// <summary>Speed retained by front wheels after a glancing left-rail collision.</summary>
    private const float FrontLeftGlanceSpeedFactor = 0.93f;
    /// <summary>Speed retained by front wheels after a glancing right-rail collision.</summary>
    private const float FrontRightGlanceSpeedFactor = 0.935f;
    /// <summary>Speed retained by rear wheels after a glancing collision.</summary>
    private const float RearGlanceSpeedFactor = 0.96f;
    /// <summary>Minimum viewDistance value before collision visual effects are applied.</summary>
    private const float CollisionViewDistanceMin = 0.75f;
    /// <summary>View-distance decrease for a front-wheel collision.</summary>
    private const float FrontCollisionViewDecrement = 0.1f;
    /// <summary>View-distance decrease for a rear-wheel collision.</summary>
    private const float RearCollisionViewDecrement = 0.05f;
    /// <summary>Camera wobble intensity for a glancing collision.</summary>
    private const float GlancingCollisionWobbleFactor = 0.00075f;
    /// <summary>Camera wobble intensity for a frontal collision.</summary>
    private const float FrontalCollisionWobbleFactor = 0.005f;
    /// <summary>Divisor applied to collision angle for large-angle crash rotation.</summary>
    private const float FrontalCollisionRotationDivisor = 3.0f;
"""

CAR_REPLACEMENTS = [
    # Spring physics constructor calls (two of them — different context lines)
    (
        "new SpringPhysicsObject(\n            carMass, 1.5f, 120, 0);",
        "new SpringPhysicsObject(\n            carMass, PitchSpringFriction, PitchSpringConstant, 0);"
    ),
    (
        "new SpringPhysicsObject(\n        DefaultCarMass, 1.5f, 120, 0);",
        "new SpringPhysicsObject(\n        DefaultCarMass, PitchSpringFriction, PitchSpringConstant, 0);"
    ),
    # Rotation friction
    (
        "rotationChange *= 0.95f;",
        "rotationChange *= RotationFrictionFactor;"
    ),
    # Keyboard rotation divisor (two occurrences — left and right)
    (
        "MaxRotationPerSec * moveFactor / 2.5f;",
        "MaxRotationPerSec * moveFactor / KeyboardRotationDivisor;"
    ),
    # Mouse steering divisor
    (
        "(Input.MouseXMovement / 15.0f) *",
        "(Input.MouseXMovement / MouseSteeringDivisor) *"
    ),
    # GamePad analog stick divisor
    (
        "MaxRotationPerSec * moveFactor / 1.12345f;",
        "MaxRotationPerSec * moveFactor / GamePadAnalogStickDivisor;"
    ),
    # GamePad D-pad rotation divisor (two occurrences — left and right)
    (
        "MaxRotationPerSec * moveFactor / 1.5f;",
        "MaxRotationPerSec * moveFactor / GamePadDPadRotationDivisor;"
    ),
    # Max rotation multiplier
    (
        "float maxRot = MaxRotationPerSec * moveFactor * 1.25f;",
        "float maxRot = MaxRotationPerSec * moveFactor * MaxRotationMultiplier;"
    ),
    # Low-speed threshold and factors
    (
        "if (speed < 10.0f)\n            {\n                rotationChange *= 0.67f + 0.33f * speed / 10.0f;",
        "if (speed < LowSpeedThreshold)\n            {\n                rotationChange *= LowSpeedRotationBase + LowSpeedRotationFactor * speed / LowSpeedThreshold;"
    ),
    # High-speed rotation divisor
    (
        "rotationChange *= 1.0f + (speed - 10) / 100.0f;",
        "rotationChange *= 1.0f + (speed - LowSpeedThreshold) / HighSpeedRotationDivisor;"
    ),
    # Rotation interpolation factor
    (
        "moveFactor / 0.225f;",
        "moveFactor / RotationInterpolationFactor;"
    ),
    # View distance change rate (two occurrences)
    (
        "viewDistance -= moveFactor * 2.0f;",
        "viewDistance -= moveFactor * ViewDistanceChangeRate;"
    ),
    (
        "viewDistance += moveFactor * 2.0f;",
        "viewDistance += moveFactor * ViewDistanceChangeRate;"
    ),
    # Mouse wheel view divisor
    (
        "viewDistance -= Input.MouseWheelDelta / 500.0f;",
        "viewDistance -= Input.MouseWheelDelta / MouseWheelViewDivisor;"
    ),
    # Acceleration force factor
    (
        "carForce +=\n                carDir * newAccelerationForce * (moveFactor * 85);",
        "carForce +=\n                carDir * newAccelerationForce * (moveFactor * AccelerationForceFactor);"
    ),
    # Car speed position factor
    (
        "carPos += speed * carDir * moveFactor * 1.75f;",
        "carPos += speed * carDir * moveFactor * CarSpeedPositionFactor;"
    ),
    # Max speed change per sec (two occurrences in braking)
    (
        "if (speed > oldSpeed + 100 * moveFactor)",
        "if (speed > oldSpeed + MaxSpeedChangePerSec * moveFactor)"
    ),
    (
        "speed = (oldSpeed + 100 * moveFactor);",
        "speed = (oldSpeed + MaxSpeedChangePerSec * moveFactor);"
    ),
    (
        "if (speed < oldSpeed - 100 * moveFactor)",
        "if (speed < oldSpeed - MaxSpeedChangePerSec * moveFactor)"
    ),
    (
        "speed = (oldSpeed - 100 * moveFactor);",
        "speed = (oldSpeed - MaxSpeedChangePerSec * moveFactor);"
    ),
    # Glancing collision — left rail, front wheels
    (
        "speed *= 0.93f;\n                        if (viewDistance > 0.75f)\n                        {\n                            viewDistance -= 0.1f;",
        "speed *= FrontLeftGlanceSpeedFactor;\n                        if (viewDistance > CollisionViewDistanceMin)\n                        {\n                            viewDistance -= FrontCollisionViewDecrement;"
    ),
    # Glancing collision — left rail, rear wheels (0.96f / 0.05f)
    (
        "speed *= 0.96f;\n                        if (viewDistance > 0.75f)\n                        {\n                            viewDistance -= 0.05f;\n                        }\n                    }\n                    ChaseCamera.WobbleCamera(0.00075f * speed);\n                }\n\n                // If 90-45 degrees (in either direction), make frontal crash\n                // + stop car + wobble camera\n                else if (Math.Abs(collisionAngle) < MathHelper.Pi * 3.0f / 4.0f)\n                {\n                    // Also rotate car if less than 60 degrees\n                    if (Math.Abs(collisionAngle) < MathHelper.Pi / 3.0f)\n                    {\n                        rotateCarAfterCollision = -collisionAngle / 3.0f;\n                    }\n\n                    // Play crash sound\n                    Sound.PlayCrashSound(true);\n\n                    // Shake camera\n                    ChaseCamera.WobbleCamera(0.005f * speed);",
        "speed *= RearGlanceSpeedFactor;\n                        if (viewDistance > CollisionViewDistanceMin)\n                        {\n                            viewDistance -= RearCollisionViewDecrement;\n                        }\n                    }\n                    ChaseCamera.WobbleCamera(GlancingCollisionWobbleFactor * speed);\n                }\n\n                // If 90-45 degrees (in either direction), make frontal crash\n                // + stop car + wobble camera\n                else if (Math.Abs(collisionAngle) < MathHelper.Pi * 3.0f / 4.0f)\n                {\n                    // Also rotate car if less than 60 degrees\n                    if (Math.Abs(collisionAngle) < MathHelper.Pi / 3.0f)\n                    {\n                        rotateCarAfterCollision = -collisionAngle / FrontalCollisionRotationDivisor;\n                    }\n\n                    // Play crash sound\n                    Sound.PlayCrashSound(true);\n\n                    // Shake camera\n                    ChaseCamera.WobbleCamera(FrontalCollisionWobbleFactor * speed);"
    ),
    # Glancing collision — right rail, front wheels
    (
        "speed *= 0.935f;\n                        if (viewDistance > 0.75f)\n                        {\n                            viewDistance -= 0.1f;",
        "speed *= FrontRightGlanceSpeedFactor;\n                        if (viewDistance > CollisionViewDistanceMin)\n                        {\n                            viewDistance -= FrontCollisionViewDecrement;"
    ),
    # Glancing collision — right rail, rear wheels
    (
        "speed *= 0.96f;\n                        if (viewDistance > 0.75f)\n                        {\n                            viewDistance -= 0.05f;\n                        }\n                    }\n                    ChaseCamera.WobbleCamera(0.00075f * speed);\n                }\n\n                // If 90-45 degrees (in either direction), make frontal crash\n                // + stop car + wobble camera\n                else if (Math.Abs(collisionAngle) < MathHelper.Pi * 3.0f / 4.0f)\n                {\n                    // Also rotate car if less than 60 degrees\n                    if (Math.Abs(collisionAngle) < MathHelper.Pi / 3.0f)\n                    {\n                        rotateCarAfterCollision = +collisionAngle / 3.0f;\n                    }\n\n                    // Play crash sound\n                    Sound.PlayCrashSound(true);\n\n                    // Shake camera\n                    ChaseCamera.WobbleCamera(0.005f * speed);",
        "speed *= RearGlanceSpeedFactor;\n                        if (viewDistance > CollisionViewDistanceMin)\n                        {\n                            viewDistance -= RearCollisionViewDecrement;\n                        }\n                    }\n                    ChaseCamera.WobbleCamera(GlancingCollisionWobbleFactor * speed);\n                }\n\n                // If 90-45 degrees (in either direction), make frontal crash\n                // + stop car + wobble camera\n                else if (Math.Abs(collisionAngle) < MathHelper.Pi * 3.0f / 4.0f)\n                {\n                    // Also rotate car if less than 60 degrees\n                    if (Math.Abs(collisionAngle) < MathHelper.Pi / 3.0f)\n                    {\n                        rotateCarAfterCollision = +collisionAngle / FrontalCollisionRotationDivisor;\n                    }\n\n                    // Play crash sound\n                    Sound.PlayCrashSound(true);\n\n                    // Shake camera\n                    ChaseCamera.WobbleCamera(FrontalCollisionWobbleFactor * speed);"
    ),
]

# ---------------------------------------------------------------------------
# Player.cs
# ---------------------------------------------------------------------------
PLAYER_CONSTANT = (
    "    // Game over camera\n"
    "    /// <summary>Period (ms) of one full orbit of the game-over camera around the car.</summary>\n"
    "    private const float GameOverCameraRotationPeriodMs = 2593.0f;\n\n"
)

PLAYER_REPLACEMENT = (
    "Matrix.CreateRotationZ(BaseGame.TotalTimeMilliseconds / 2593.0f)",
    "Matrix.CreateRotationZ(BaseGame.TotalTimeMilliseconds / GameOverCameraRotationPeriodMs)"
)

def apply_car_physics():
    text = CAR.read_text(encoding="utf-8")

    # 1. Inject new constants just before the closing #endregion of #region Constants
    marker = "    private const float MaxViewDistance = 1.8f;\n    #endregion"
    if marker not in text:
        print("ERROR: Cannot find MaxViewDistance constant anchor.", file=sys.stderr)
        sys.exit(1)
    text = text.replace(
        marker,
        "    private const float MaxViewDistance = 1.8f;\n" + NEW_CONSTANTS + "    #endregion",
        1
    )

    # 2. Apply each body replacement
    for old, new in CAR_REPLACEMENTS:
        count = text.count(old)
        if count == 0:
            print(f"WARNING: Pattern not found:\n  {repr(old[:80])}", file=sys.stderr)
        else:
            text = text.replace(old, new)

    CAR.write_text(text, encoding="utf-8")
    print(f"CarPhysics.cs updated.")

def apply_player():
    text = PLAYER.read_text(encoding="utf-8")

    # Find the #region Variables (or first "private const") in Player.cs to inject the constant
    # Player.cs has LapCount and InAirTimeoutMilliseconds already defined; add after them.
    marker = "    private const float InAirTimeoutMilliseconds = 3000.0f;"
    if marker not in text:
        print("ERROR: Cannot find InAirTimeoutMilliseconds anchor in Player.cs.", file=sys.stderr)
        sys.exit(1)
    text = text.replace(
        marker,
        marker + "\n\n" + PLAYER_CONSTANT.rstrip("\n"),
        1
    )

    old, new = PLAYER_REPLACEMENT
    count = text.count(old)
    if count == 0:
        print("ERROR: Cannot find 2593.0f usage in Player.cs.", file=sys.stderr)
        sys.exit(1)
    text = text.replace(old, new)
    PLAYER.write_text(text, encoding="utf-8")
    print("Player.cs updated.")

if __name__ == "__main__":
    apply_car_physics()
    apply_player()
    print("Done.")
