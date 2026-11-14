using CasaEngine.Framework.Scene.Entities.Components;
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

    private static List<(string ModelName, Matrix LegacyTransform, PrimitiveComponent? Component)>? _placedObjectsBeingBuilt;

    private static void BeginSceneDiagnostics()
    {
        _placedObjectsBeingBuilt = [];
    }

    /// <summary>
    /// Keeps the component created for a placed object: its shadow flags are read when the scene is complete, from the
    /// component itself and not from the rule that set them, so that the measure proves the state of the entities.
    /// </summary>
    private static void RecordPlacedObject(string modelName, Matrix legacyTransform, Entity entity)
    {
        _placedObjectsBeingBuilt?.Add((modelName, legacyTransform, entity.RootComponent as PrimitiveComponent));
    }

    private static void EndSceneDiagnostics(string trackName, IReadOnlyList<LegacyTrackPoint> roadPoints)
    {
        var points = new List<LegacyRoadPointSnapshot>(roadPoints.Count);
        foreach (LegacyTrackPoint point in roadPoints)
        {
            points.Add(new LegacyRoadPointSnapshot(point.Position, point.Right, point.Up, point.Direction, point.RoadWidth));
        }

        var placedObjects = new List<LegacyPlacedObject>(_placedObjectsBeingBuilt?.Count ?? 0);
        foreach ((string modelName, Matrix legacyTransform, PrimitiveComponent? component) in _placedObjectsBeingBuilt ?? [])
        {
            placedObjects.Add(new LegacyPlacedObject(modelName, legacyTransform, component?.CastShadows, component?.ReceiveShadows));
        }

        LastSceneDiagnostics = new LegacySceneDiagnostics(trackName, points, placedObjects);
        _placedObjectsBeingBuilt = null;
    }
}

internal sealed record LegacySceneDiagnostics(
    string TrackName,
    IReadOnlyList<LegacyRoadPointSnapshot> RoadPoints,
    IReadOnlyList<LegacyPlacedObject> PlacedObjects);

internal readonly record struct LegacyRoadPointSnapshot(Vector3 Position, Vector3 Right, Vector3 Up, Vector3 Direction, float RoadWidth);

/// <summary>
/// A placed model. <paramref name="CastsShadow"/> and <paramref name="ReceivesShadow"/> are the flags of the component
/// the port created (<c>null</c>: not known, as in an original scene.json written before the flags were exported).
/// </summary>
internal readonly record struct LegacyPlacedObject(string ModelName, Matrix LegacyTransform, bool? CastsShadow = null, bool? ReceivesShadow = null);
