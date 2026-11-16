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

    /// <summary>
    /// Splits a placement into the scale, rotation and translation an entity's local transform holds. The point spaces the
    /// original builds are not made of vectors at a right angle (the up vector of a road point is not perpendicular to its
    /// direction on a slope), so a placement can be sheared, which a scale, a rotation and a translation cannot hold, and
    /// Matrix.Decompose then gives a quaternion that is not a unit one, which scales and bends the model. The scale here is
    /// the length of each row and the rotation is the one closest to the rows (the polar factor of the matrix whose rows are
    /// the normalized rows): the shear is shared between the axes instead of landing on one, and a matrix that has none
    /// is split exactly as Matrix.Decompose splits it.
    /// </summary>
    internal static bool TryDecomposePlacement(Matrix transform, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
    {
        translation = transform.Translation;
        scale = new Vector3(transform.Right.Length(), transform.Up.Length(), transform.Backward.Length());
        rotation = Quaternion.Identity;
        if (scale.X < 1e-6f || scale.Y < 1e-6f || scale.Z < 1e-6f)
        {
            return false;
        }

        Matrix closest = Matrix.Identity;
        closest.Right = transform.Right / scale.X;
        closest.Up = transform.Up / scale.Y;
        closest.Backward = transform.Backward / scale.Z;
        for (int iteration = 0; iteration < 10; iteration++)
        {
            Matrix inverseTranspose = Matrix.Transpose(Matrix.Invert(closest));
            closest = (closest + inverseTranspose) * 0.5f;
            closest.M44 = 1f;
        }

        rotation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(closest));
        return true;
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
