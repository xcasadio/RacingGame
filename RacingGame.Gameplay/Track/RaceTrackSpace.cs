using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Track;

/// <summary>The legacy-to-runtime coordinate conversion shared by the track layout and the mesh builders.</summary>
internal static class RaceTrackSpace
{
    internal const float WorldScale = 1.0f;
    internal const float LegacyRoadWidthScale = 13.25f;

    internal static Matrix ConvertLegacyTransform(Matrix legacyTransform, Vector3 origin)
    {
        Matrix runtimeTransform = Matrix.Identity;
        runtimeTransform.Right = ConvertLegacyVector(legacyTransform.Right);
        runtimeTransform.Up = ConvertLegacyVector(legacyTransform.Up);
        runtimeTransform.Forward = ConvertLegacyVector(legacyTransform.Forward);
        runtimeTransform.Translation = ConvertLegacyPoint(legacyTransform.Translation) - origin;
        return runtimeTransform;
    }

    internal static Vector3 ConvertLegacyPoint(Vector3 point)
    {
        return new Vector3(point.X, point.Z, -point.Y) * WorldScale;
    }

    internal static Vector3 ConvertLegacyVector(Vector3 vector)
    {
        return new Vector3(vector.X, vector.Z, -vector.Y) * WorldScale;
    }

    internal static Vector3 ConvertLegacyDirection(Vector3 direction)
    {
        Vector3 runtimeDirection = ConvertLegacyVector(direction);
        if (runtimeDirection.LengthSquared() < 0.0001f)
        {
            return Vector3.Up;
        }

        runtimeDirection.Normalize();
        return runtimeDirection;
    }

    internal static Matrix CreatePointSpaceMatrix(Vector3 right, Vector3 direction, Vector3 up)
    {
        if (right.LengthSquared() > 0.0001f)
        {
            right.Normalize();
        }
        else
        {
            right = Vector3.Right;
        }

        if (direction.LengthSquared() > 0.0001f)
        {
            direction.Normalize();
        }
        else
        {
            direction = Vector3.UnitY;
        }

        if (up.LengthSquared() > 0.0001f)
        {
            up.Normalize();
        }
        else
        {
            up = Vector3.UnitZ;
        }

        Matrix pointSpace = Matrix.Identity;
        pointSpace.M11 = right.X;
        pointSpace.M12 = right.Y;
        pointSpace.M13 = right.Z;
        pointSpace.M21 = direction.X;
        pointSpace.M22 = direction.Y;
        pointSpace.M23 = direction.Z;
        pointSpace.M31 = up.X;
        pointSpace.M32 = up.Y;
        pointSpace.M33 = up.Z;
        return pointSpace;
    }
}
