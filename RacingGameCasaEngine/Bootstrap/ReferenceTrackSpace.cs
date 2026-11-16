using CasaEngine.Framework.Entities;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Race;
using RacingGame.Gameplay.Track;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// The conversion between the original game's coordinates (Z up) and the race world of a track, as the scene is placed:
/// <see cref="RaceTrackSpace.ConvertLegacyPoint"/>, then the track origin (<see cref="RaceTrackLayout.ComputeOrigin"/>, the
/// layout's <c>Origin</c>), then the world matrix of the road generator that the track entities hang under. The reverse
/// methods are the exact inverse of the forward ones, so a point converted and brought back is the same point to float
/// rounding.
/// </summary>
internal sealed class ReferenceTrackSpace
{
    private readonly Matrix _roadWorldInverse;

    private ReferenceTrackSpace(RaceTrackSource source, Vector3 origin, Matrix roadWorld)
    {
        Source = source;
        Origin = origin;
        RoadWorld = roadWorld;
        _roadWorldInverse = Matrix.Invert(roadWorld);
    }

    /// <summary>The track and height map assets of the road generator.</summary>
    public RaceTrackSource Source { get; }

    /// <summary>The runtime-space point the track is centred on (the layout's origin).</summary>
    public Vector3 Origin { get; }

    /// <summary>The world matrix of the road generator of the race world.</summary>
    public Matrix RoadWorld { get; }

    /// <summary>Reads the origin from the track assets of the road generator of <paramref name="world"/>.</summary>
    public static bool TryCreate(World world, out ReferenceTrackSpace? space)
    {
        space = null;
        Entity? road = world.Entities.FirstOrDefault(entity => entity.Name?.StartsWith(RaceWorldNames.TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true);
        if (road?.RootComponent is not RaceRoadGeneratorComponent generator
            || !LegacyTrackSceneFactory.TryResolveTrackSource("Reference view capture", generator.TrackAssetId, generator.HeightmapAssetId, out RaceTrackSource? source))
        {
            return false;
        }

        space = new ReferenceTrackSpace(source!, RaceTrackLayout.ComputeOrigin(source!.Track.TrackPoints), generator.WorldMatrixWithScale);
        return true;
    }

    /// <summary>A point of the original's coordinates, in the race world.</summary>
    public Vector3 ToRaceWorld(Vector3 legacyPoint)
    {
        return Vector3.Transform(RaceTrackSpace.ConvertLegacyPoint(legacyPoint) - Origin, RoadWorld);
    }

    /// <summary>A point of the race world, in the original's coordinates: the inverse of <see cref="ToRaceWorld"/>.</summary>
    public Vector3 ToLegacy(Vector3 worldPoint)
    {
        return ConvertRuntimeToLegacyPoint(Vector3.Transform(worldPoint, _roadWorldInverse) + Origin);
    }

    /// <summary>
    /// A transform placed by <see cref="RaceTrackSpace.ConvertLegacyTransform"/> (relative to the track origin, as the layout
    /// gives them), in the original's coordinates: the inverse of that conversion.
    /// </summary>
    public Matrix ToLegacyTransform(Matrix runtimeTransform)
    {
        Matrix legacyTransform = Matrix.Identity;
        legacyTransform.Right = ConvertRuntimeToLegacyVector(runtimeTransform.Right);
        legacyTransform.Up = ConvertRuntimeToLegacyVector(runtimeTransform.Up);
        legacyTransform.Forward = ConvertRuntimeToLegacyVector(runtimeTransform.Forward);
        legacyTransform.Translation = ConvertRuntimeToLegacyPoint(runtimeTransform.Translation + Origin);
        return legacyTransform;
    }

    /// <summary>
    /// The transform of a component read from the race world (its world matrix), in the original's coordinates: brought back
    /// from the road generator's world, then by <see cref="ToLegacyTransform"/>.
    /// </summary>
    public Matrix ToLegacyWorldTransform(Matrix worldTransform)
    {
        return ToLegacyTransform(worldTransform * _roadWorldInverse);
    }

    /// <summary>
    /// The largest distance between a point of the original's coordinates and the same point converted to the race world and
    /// brought back: float rounding only when the two conversions are inverse of each other.
    /// </summary>
    public float MaxPointRoundTripError(IEnumerable<Vector3> legacyPoints)
    {
        float maxError = 0f;
        foreach (Vector3 point in legacyPoints)
        {
            maxError = Math.Max(maxError, Vector3.Distance(ToLegacy(ToRaceWorld(point)), point));
        }

        return maxError;
    }

    /// <summary>
    /// The largest difference of one element between a placed transform and the same transform brought back to the
    /// original's coordinates and placed again by <see cref="RaceTrackSpace.ConvertLegacyTransform"/>.
    /// </summary>
    public float MaxTransformRoundTripError(IEnumerable<Matrix> runtimeTransforms)
    {
        float maxError = 0f;
        foreach (Matrix runtimeTransform in runtimeTransforms)
        {
            Matrix placedAgain = RaceTrackSpace.ConvertLegacyTransform(ToLegacyTransform(runtimeTransform), Origin);
            maxError = Math.Max(maxError, MaxElementDifference(runtimeTransform, placedAgain));
        }

        return maxError;
    }

    private static float MaxElementDifference(Matrix a, Matrix b)
    {
        float maxDifference = 0f;
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M11 - b.M11));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M12 - b.M12));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M13 - b.M13));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M21 - b.M21));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M22 - b.M22));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M23 - b.M23));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M31 - b.M31));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M32 - b.M32));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M33 - b.M33));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M41 - b.M41));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M42 - b.M42));
        maxDifference = Math.Max(maxDifference, Math.Abs(a.M43 - b.M43));
        return maxDifference;
    }

    // RaceTrackSpace.ConvertLegacyVector is (x, z, -y) * WorldScale: this is (x, -z, y) / WorldScale.
    private static Vector3 ConvertRuntimeToLegacyVector(Vector3 vector)
    {
        return new Vector3(vector.X, -vector.Z, vector.Y) / RaceTrackSpace.WorldScale;
    }

    private static Vector3 ConvertRuntimeToLegacyPoint(Vector3 point)
    {
        return ConvertRuntimeToLegacyVector(point);
    }
}
