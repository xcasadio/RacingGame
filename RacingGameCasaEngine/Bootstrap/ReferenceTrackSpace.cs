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

    private ReferenceTrackSpace(string trackName, Vector3 origin, Matrix roadWorld)
    {
        TrackName = trackName;
        Origin = origin;
        RoadWorld = roadWorld;
        _roadWorldInverse = Matrix.Invert(roadWorld);
    }

    public string TrackName { get; }

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

        space = new ReferenceTrackSpace(source!.TrackName, RaceTrackLayout.ComputeOrigin(source.Track.TrackPoints), generator.WorldMatrixWithScale);
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
