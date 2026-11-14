using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Worlds;

internal static partial class LegacyTrackSceneFactory
{
    /// <summary>
    /// What <see cref="Create"/> last built, in the original game's coordinates (Z up, before the conversion to the
    /// race world): the generated road points and every placed model. Read by the reference capture to measure the
    /// port against the original's scene.json (ADR-0015).
    /// </summary>
    internal static LegacySceneDiagnostics? LastSceneDiagnostics { get; private set; }

    private static List<LegacyPlacedObject>? _placedObjectsBeingBuilt;

    private static void BeginSceneDiagnostics()
    {
        _placedObjectsBeingBuilt = [];
    }

    private static void RecordPlacedObject(string modelName, Matrix legacyTransform)
    {
        _placedObjectsBeingBuilt?.Add(new LegacyPlacedObject(modelName, legacyTransform));
    }

    private static void EndSceneDiagnostics(string trackName, IReadOnlyList<LegacyTrackPoint> roadPoints)
    {
        var points = new List<LegacyRoadPointSnapshot>(roadPoints.Count);
        foreach (LegacyTrackPoint point in roadPoints)
        {
            points.Add(new LegacyRoadPointSnapshot(point.Position, point.Right, point.Up, point.Direction, point.RoadWidth));
        }

        LastSceneDiagnostics = new LegacySceneDiagnostics(trackName, points, _placedObjectsBeingBuilt ?? []);
        _placedObjectsBeingBuilt = null;
    }
}

internal sealed record LegacySceneDiagnostics(
    string TrackName,
    IReadOnlyList<LegacyRoadPointSnapshot> RoadPoints,
    IReadOnlyList<LegacyPlacedObject> PlacedObjects);

internal readonly record struct LegacyRoadPointSnapshot(Vector3 Position, Vector3 Right, Vector3 Up, Vector3 Direction, float RoadWidth);

internal readonly record struct LegacyPlacedObject(string ModelName, Matrix LegacyTransform);
