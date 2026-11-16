using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Scene.Entities.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Race;
using RacingGame.Gameplay.Track;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// What the game built for a race, in the original's coordinates (Z up): the road points of the track layout and every
/// placed model (scenery, guard rail posts, column segments). Measured against the original's scene.json by
/// <see cref="ReferenceSceneMeasure"/>.
/// </summary>
internal sealed record ReferenceSceneSnapshot(
    string TrackName,
    IReadOnlyList<ReferenceRoadPoint> RoadPoints,
    IReadOnlyList<ReferencePlacedObject> PlacedObjects)
{
    /// <summary>
    /// Snapshots <paramref name="layout"/>, the layout the race world was built from. A placed object is kept only when the
    /// race world holds the entity (scenery) or the generated component (post, column segment) built for it; its transform is
    /// the layout's, brought back to the original's coordinates by the inverse of the placing conversion, and its shadow
    /// flags are read from that entity's component, not from the rule that sets them.
    /// <paramref name="notBuiltCount"/> is the number of placements without such an entity or component.
    /// </summary>
    internal static ReferenceSceneSnapshot Capture(World world, ReferenceTrackSpace space, RaceTrackLayout layout, out int notBuiltCount)
    {
        var roadPoints = new List<ReferenceRoadPoint>(layout.RoadPoints.Count);
        foreach (RaceTrackRoadPoint point in layout.RoadPoints)
        {
            roadPoints.Add(new ReferenceRoadPoint(point.Position, point.Right, point.Up, point.Direction, point.RoadWidth));
        }

        var entitiesByName = new Dictionary<string, Entity>(StringComparer.Ordinal);
        foreach (Entity entity in world.Entities)
        {
            if (entity.Name != null)
            {
                entitiesByName.TryAdd(entity.Name, entity);
            }
        }

        var placedObjects = new List<ReferencePlacedObject>();
        notBuiltCount = 0;

        for (int index = 0; index < layout.Scenery.Count; index++)
        {
            RaceTrackModelPlacement placement = layout.Scenery[index];
            string entityName = $"{RaceWorldNames.TrackSceneryEntityNamePrefix}{placement.ModelName}.{index:000}";
            AddPlacedObject(
                placedObjects,
                space,
                placement,
                entitiesByName.TryGetValue(entityName, out Entity? sceneryEntity) ? sceneryEntity.RootComponent : null,
                ref notBuiltCount);
        }

        AddGeneratedPlacements(placedObjects, space, world, $"{RaceWorldNames.TrackGuardRailEntityNamePrefix}Left.", "Post", layout.LeftPosts, ref notBuiltCount);
        AddGeneratedPlacements(placedObjects, space, world, $"{RaceWorldNames.TrackGuardRailEntityNamePrefix}Right.", "Post", layout.RightPosts, ref notBuiltCount);
        AddGeneratedPlacements(placedObjects, space, world, RaceWorldNames.TrackColumnsEntityNamePrefix, "Segment", layout.ColumnSegments, ref notBuiltCount);

        return new ReferenceSceneSnapshot(layout.TrackName, roadPoints, placedObjects);
    }

    // The posts and the column segments are children of their generator, named <childPrefix>.<index:000> by their index in
    // the layout's list.
    private static void AddGeneratedPlacements(
        List<ReferencePlacedObject> placedObjects,
        ReferenceTrackSpace space,
        World world,
        string generatorEntityNamePrefix,
        string childPrefix,
        IReadOnlyList<RaceTrackModelPlacement> placements,
        ref int notBuiltCount)
    {
        var childrenByName = new Dictionary<string, SceneComponent>(StringComparer.Ordinal);
        Entity? generator = world.Entities.FirstOrDefault(entity => entity.Name?.StartsWith(generatorEntityNamePrefix, StringComparison.Ordinal) == true);
        if (generator?.RootComponent is { } root)
        {
            foreach (SceneComponent child in root.Children)
            {
                if (child.Name != null)
                {
                    childrenByName.TryAdd(child.Name, child);
                }
            }
        }

        for (int index = 0; index < placements.Count; index++)
        {
            childrenByName.TryGetValue($"{childPrefix}.{index:000}", out SceneComponent? component);
            AddPlacedObject(placedObjects, space, placements[index], component, ref notBuiltCount);
        }
    }

    private static void AddPlacedObject(
        List<ReferencePlacedObject> placedObjects,
        ReferenceTrackSpace space,
        RaceTrackModelPlacement placement,
        SceneComponent? component,
        ref int notBuiltCount)
    {
        if (component == null)
        {
            notBuiltCount++;
            return;
        }

        PrimitiveComponent? primitive = component as PrimitiveComponent;
        placedObjects.Add(new ReferencePlacedObject(
            placement.ModelName,
            space.ToLegacyTransform(placement.Transform),
            primitive?.CastShadows,
            primitive?.ReceiveShadows));
    }
}

internal readonly record struct ReferenceRoadPoint(Vector3 Position, Vector3 Right, Vector3 Up, Vector3 Direction, float RoadWidth);

/// <summary>
/// A placed model. <paramref name="CastsShadow"/> and <paramref name="ReceivesShadow"/> are the flags of the component the
/// game created (<c>null</c>: not known, as in an original scene.json written before the flags were exported).
/// </summary>
internal readonly record struct ReferencePlacedObject(string ModelName, Matrix LegacyTransform, bool? CastsShadow = null, bool? ReceivesShadow = null);
