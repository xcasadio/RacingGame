using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Components;

internal static class VehicleTransmissionLogic
{
    private const float RadiansPerSecondToRpm = 60f / (MathF.PI * 2f);

    public static VehicleTransmissionDefinition CreateDefaultFiveSpeedDefinition()
    {
        return new VehicleTransmissionDefinition(
            forwardGearRatios: [3.18f, 2.14f, 1.56f, 1.21f, 0.97f],
            reverseGearRatio: 3.04f,
            finalDriveRatio: 3.46f,
            idleRpm: 950f,
            upshiftRpm: 6500f,
            downshiftRpm: 3150f,
            redlineRpm: 7800f,
            shiftDurationSeconds: 0.22f);
    }

    public static void Reset(VehicleTransmissionRuntimeState state, VehicleTransmissionDefinition definition)
    {
        state.CurrentGear = 1;
        state.EngineRpm = definition.IdleRpm;
        state.ShiftTimerSeconds = 0f;
        state.NormalizedRpm = 0f;
    }

    public static VehicleTransmissionFrame UpdateAutomaticForward(
        VehicleTransmissionRuntimeState state,
        VehicleTransmissionDefinition definition,
        float drivenWheelAngularSpeedRadiansPerSecond,
        float throttle,
        float elapsedTime)
    {
        state.ShiftTimerSeconds = Math.Max(0f, state.ShiftTimerSeconds - elapsedTime);
        int currentGear = Math.Clamp(state.CurrentGear, 1, definition.ForwardGearCount);
        float clampedThrottle = Math.Clamp(throttle, 0f, 1f);
        float currentRpm = ComputeForwardEngineRpm(definition, currentGear, drivenWheelAngularSpeedRadiansPerSecond);
        bool gearChanged = false;

        if (state.ShiftTimerSeconds <= 0f)
        {
            int targetGear = DetermineTargetGear(definition, currentGear, currentRpm, drivenWheelAngularSpeedRadiansPerSecond, clampedThrottle);
            if (targetGear != currentGear)
            {
                currentGear = targetGear;
                state.ShiftTimerSeconds = definition.ShiftDurationSeconds;
                currentRpm = ComputeForwardEngineRpm(definition, currentGear, drivenWheelAngularSpeedRadiansPerSecond);
                gearChanged = true;
            }
        }

        float normalizedRpm = NormalizeRpm(definition, currentRpm);
        float torqueCurveScale = EvaluateTorqueCurve(normalizedRpm);
        float gearForceScale = EvaluateGearForceScale(definition, currentGear);
        float shiftTorqueScale = EvaluateShiftTorqueScale(state.ShiftTimerSeconds, definition.ShiftDurationSeconds);
        float driveForceScale = gearForceScale * torqueCurveScale * MathHelper.Lerp(0.72f, 1f, clampedThrottle) * shiftTorqueScale;

        state.CurrentGear = currentGear;
        state.EngineRpm = currentRpm;
        state.NormalizedRpm = normalizedRpm;

        return new VehicleTransmissionFrame(
            currentGear,
            currentRpm,
            normalizedRpm,
            driveForceScale,
            shiftTorqueScale,
            gearChanged,
            state.IsShifting);
    }

    public static float ComputeForwardEngineRpm(
        VehicleTransmissionDefinition definition,
        int gear,
        float drivenWheelAngularSpeedRadiansPerSecond)
    {
        float wheelRpm = Math.Abs(drivenWheelAngularSpeedRadiansPerSecond) * RadiansPerSecondToRpm;
        float engineRpm = wheelRpm * definition.GetForwardGearRatio(gear) * definition.FinalDriveRatio;
        return Math.Clamp(engineRpm, definition.IdleRpm, definition.RedlineRpm);
    }

    public static float ComputeReverseEngineRpm(
        VehicleTransmissionDefinition definition,
        float drivenWheelAngularSpeedRadiansPerSecond)
    {
        float wheelRpm = Math.Abs(drivenWheelAngularSpeedRadiansPerSecond) * RadiansPerSecondToRpm;
        float engineRpm = wheelRpm * definition.ReverseGearRatio * definition.FinalDriveRatio;
        return Math.Clamp(engineRpm, definition.IdleRpm, definition.RedlineRpm * 0.72f);
    }

    private static int DetermineTargetGear(
        VehicleTransmissionDefinition definition,
        int currentGear,
        float currentRpm,
        float drivenWheelAngularSpeedRadiansPerSecond,
        float throttle)
    {
        if (currentGear < definition.ForwardGearCount && currentRpm >= definition.UpshiftRpm && throttle > 0.15f)
        {
            return currentGear + 1;
        }

        if (currentGear > 1 && currentRpm <= definition.DownshiftRpm)
        {
            float downshiftRpm = ComputeForwardEngineRpm(definition, currentGear - 1, drivenWheelAngularSpeedRadiansPerSecond);
            if (downshiftRpm <= definition.RedlineRpm * 0.96f)
            {
                return currentGear - 1;
            }
        }

        return currentGear;
    }

    private static float NormalizeRpm(VehicleTransmissionDefinition definition, float engineRpm)
    {
        return Math.Clamp((engineRpm - definition.IdleRpm) / Math.Max(1f, definition.RedlineRpm - definition.IdleRpm), 0f, 1f);
    }

    private static float EvaluateTorqueCurve(float normalizedRpm)
    {
        if (normalizedRpm < 0.22f)
        {
            return MathHelper.Lerp(0.62f, 0.88f, normalizedRpm / 0.22f);
        }

        if (normalizedRpm < 0.58f)
        {
            return MathHelper.Lerp(0.88f, 1.04f, (normalizedRpm - 0.22f) / 0.36f);
        }

        if (normalizedRpm < 0.82f)
        {
            return MathHelper.Lerp(1.04f, 0.94f, (normalizedRpm - 0.58f) / 0.24f);
        }

        return MathHelper.Lerp(0.94f, 0.72f, (normalizedRpm - 0.82f) / 0.18f);
    }

    private static float EvaluateGearForceScale(VehicleTransmissionDefinition definition, int gear)
    {
        float gearRatio = definition.GetForwardGearRatio(gear);
        float topGearRatio = definition.TopGearRatio;
        float firstGearRatio = definition.FirstGearRatio;
        float normalizedMechanicalAdvantage = Math.Clamp((gearRatio - topGearRatio) / Math.Max(0.0001f, firstGearRatio - topGearRatio), 0f, 1f);
        return MathHelper.Lerp(1f, 1.45f, normalizedMechanicalAdvantage);
    }

    private static float EvaluateShiftTorqueScale(float shiftTimerSeconds, float shiftDurationSeconds)
    {
        if (shiftDurationSeconds <= 0.0001f || shiftTimerSeconds <= 0f)
        {
            return 1f;
        }

        float progress = 1f - Math.Clamp(shiftTimerSeconds / shiftDurationSeconds, 0f, 1f);
        return MathHelper.Lerp(0.28f, 1f, progress);
    }
}