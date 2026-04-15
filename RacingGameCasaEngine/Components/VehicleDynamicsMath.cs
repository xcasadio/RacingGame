using System.Text;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Components;

internal static class VehicleDynamicsMath
{
    public static Vector3 GetForward(Quaternion orientation)
    {
        return NormalizeOrFallback(Vector3.Transform(Vector3.Forward, orientation), Vector3.Forward);
    }

    public static Vector3 GetUp(Quaternion orientation)
    {
        return NormalizeOrFallback(Vector3.Transform(Vector3.Up, orientation), Vector3.Up);
    }

    public static Vector3 TransformLocalOffset(Quaternion orientation, Vector3 localOffset)
    {
        return Vector3.Transform(localOffset, orientation);
    }

    public static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
    {
        if (value.LengthSquared() < 0.0001f)
        {
            value = fallback;
        }

        if (value.LengthSquared() < 0.0001f)
        {
            return Vector3.Forward;
        }

        value.Normalize();
        return value;
    }

    public static Vector3 ProjectDirectionOntoSurface(Vector3 direction, Vector3 surfaceUp, Vector3 fallbackDirection)
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

    public static Vector3 RotateDirectionAroundAxis(Vector3 direction, Vector3 axis, float angle, Vector3 fallbackDirection)
    {
        Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angle);
        return ProjectDirectionOntoSurface(Vector3.Transform(direction, rotation), axis, fallbackDirection);
    }

    public static Quaternion CreateSurfaceOrientation(Vector3 forward, Vector3 surfaceUp)
    {
        Vector3 normalizedForward = ProjectDirectionOntoSurface(forward, surfaceUp, Vector3.Forward);
        Vector3 normalizedUp = NormalizeOrFallback(surfaceUp, Vector3.Up);
        Matrix orientation = Matrix.CreateWorld(Vector3.Zero, normalizedForward, normalizedUp);
        return Quaternion.CreateFromRotationMatrix(orientation);
    }

    public static Quaternion IntegrateAngularVelocity(Quaternion orientation, Vector3 angularVelocity, float elapsedTime)
    {
        float angularSpeed = angularVelocity.Length();
        if (angularSpeed <= 0.0001f || elapsedTime <= 0f)
        {
            return Quaternion.Normalize(orientation);
        }

        Vector3 axis = angularVelocity / angularSpeed;
        Quaternion delta = Quaternion.CreateFromAxisAngle(axis, angularSpeed * elapsedTime);
        return Quaternion.Normalize(delta * orientation);
    }

    public static float MoveToward(float value, float target, float maxDelta)
    {
        if (Math.Abs(target - value) <= maxDelta)
        {
            return target;
        }

        return value + MathF.Sign(target - value) * maxDelta;
    }

    public static Vector3 ClampMagnitude(Vector3 value, float maxLength)
    {
        float lengthSquared = value.LengthSquared();
        if (lengthSquared <= maxLength * maxLength || lengthSquared < 0.0001f)
        {
            return value;
        }

        return value / MathF.Sqrt(lengthSquared) * maxLength;
    }

    public static void ClearWheelState(VehicleWheelDefinition definition, VehicleWheelRuntimeState state)
    {
        state.HasContact = false;
        state.IsFallbackContact = false;
        state.ContactPointWorld = state.AttachmentPointWorld;
        state.ContactNormal = Vector3.Up;
        state.ContactForward = Vector3.Forward;
        state.SuspensionLength = definition.SuspensionRestLength;
        state.SuspensionCompression = 0f;
        state.SuspensionCompressionVelocity = 0f;
        state.NormalizedCompression = 0f;
        state.SteeringAngleRadians = 0f;
        state.RotationSpeedRadiansPerSecond = 0f;
        state.SlipRatio = 0f;
        state.SlipAngleRadians = 0f;
        state.ApproximateLoad = 0f;
    }

    public static string BuildWheelDebugSummary(IReadOnlyList<VehicleWheelRuntimeState> wheelStates)
    {
        if (wheelStates.Count == 0)
        {
            return "none";
        }

        var builder = new StringBuilder();
        for (int index = 0; index < wheelStates.Count; index++)
        {
            VehicleWheelRuntimeState state = wheelStates[index];
            if (index > 0)
            {
                builder.Append(" | ");
            }

            builder.Append(GetWheelLabel(state.Slot));
            builder.Append(state.HasContact ? ":C" : ":A");
            builder.Append(" k=");
            builder.Append(state.NormalizedCompression.ToString("0.00"));
            builder.Append(" slip=");
            builder.Append(state.SlipRatio.ToString("0.00"));
        }

        return builder.ToString();
    }

    public static string GetWheelLabel(VehicleWheelSlot slot)
    {
        return slot switch
        {
            VehicleWheelSlot.FrontLeft => "FL",
            VehicleWheelSlot.FrontRight => "FR",
            VehicleWheelSlot.RearLeft => "RL",
            VehicleWheelSlot.RearRight => "RR",
            _ => slot.ToString(),
        };
    }
}