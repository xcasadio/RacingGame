using Microsoft.Xna.Framework;
using CasaEngine.Framework.GameFramework;
using RacingGameCasaEngine.GameFramework;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace RacingGameCasaEngine.Components;

internal static class VehicleInputReader
{
    private const float GamePadStickDeadZone = 0.12f;
    private const float GamePadTriggerDeadZone = 0.08f;

    public static VehicleControlInput Read(CasaEngine.Framework.Input.InputComponent input, RacingPlayerController controller)
    {
        return new VehicleControlInput(GetThrottle(input, controller), GetSteering(input, controller));
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

        return Math.Abs(keyboardThrottle) >= Math.Abs(gamePadThrottle)
            ? keyboardThrottle
            : gamePadThrottle;
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
        return Math.Abs(keyboardSteering) >= Math.Abs(gamePadSteering)
            ? keyboardSteering
            : gamePadSteering;
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
}