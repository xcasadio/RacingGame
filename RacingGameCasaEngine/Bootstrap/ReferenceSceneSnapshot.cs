using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Scene.Entities.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Components;
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
    IReadOnlyList<ReferencePlacedObject> PlacedObjects,
    IReadOnlyList<ReferenceGeneratorChildren> GeneratorChildren)
{
    /// <summary>
    /// Snapshots <paramref name="layout"/>, the layout the race world was built from. A scenery object is kept only when the
    /// race world holds the entity built for it; its transform is the layout's, brought back to the original's coordinates by
    /// the inverse of the placing conversion. A guard rail post or a column segment is a child component of a generator: every
    /// child the generator holds is an object, whether the layout has a placement for it or not, and its transform is that
    /// of the child itself, read from the entity (its world matrix, brought back to the original's coordinates). The shadow
    /// flags are read from the entities' components, not from the rule that sets them.
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
        var generatorChildren = new List<ReferenceGeneratorChildren>();
        notBuiltCount = 0;

        for (int index = 0; index < layout.Scenery.Count; index++)
        {
            RaceTrackModelPlacement placement = layout.Scenery[index];
            string entityName = $"{RaceWorldNames.TrackSceneryEntityNamePrefix}{placement.ModelName}.{index:000}";
            if (!entitiesByName.TryGetValue(entityName, out Entity? sceneryEntity) || sceneryEntity.RootComponent == null)
            {
                notBuiltCount++;
                continue;
            }

            AddPlacedObject(placedObjects, placement.ModelName, space.ToLegacyTransform(placement.Transform), sceneryEntity.RootComponent);
        }

        AddGeneratedChildren(placedObjects, generatorChildren, space, world, $"{RaceWorldNames.TrackGuardRailEntityNamePrefix}Left.", "Left rail posts", "Post", RaceTrackLayout.GuardRailPostModelName, layout.LeftPosts, ref notBuiltCount);
        AddGeneratedChildren(placedObjects, generatorChildren, space, world, $"{RaceWorldNames.TrackGuardRailEntityNamePrefix}Right.", "Right rail posts", "Post", RaceTrackLayout.GuardRailPostModelName, layout.RightPosts, ref notBuiltCount);
        AddGeneratedChildren(placedObjects, generatorChildren, space, world, RaceWorldNames.TrackColumnsEntityNamePrefix, "Column segments", "Segment", RaceTrackLayout.ColumnSegmentModelName, layout.ColumnSegments, ref notBuiltCount);

        return new ReferenceSceneSnapshot(layout.TrackName, roadPoints, placedObjects, generatorChildren);
    }

    // The posts and the column segments are children of their generator, named <childPrefix>.<index:000> by their index in
    // the layout's list. Each child the generator holds is an object of the snapshot, at the transform of the child itself;
    // one whose name has no placement in the layout is an object the layout does not account for, and the measure shows it as
    // an object in the port the original does not have.
    private static void AddGeneratedChildren(
        List<ReferencePlacedObject> placedObjects,
        List<ReferenceGeneratorChildren> generatorChildren,
        ReferenceTrackSpace space,
        World world,
        string generatorEntityNamePrefix,
        string label,
        string childPrefix,
        string defaultModelName,
        IReadOnlyList<RaceTrackModelPlacement> placements,
        ref int notBuiltCount)
    {
        var placementsWithChild = new HashSet<int>();
        int childCount = 0;
        int childrenWithoutPlacement = 0;
        Entity? generator = world.Entities.FirstOrDefault(entity => entity.Name?.StartsWith(generatorEntityNamePrefix, StringComparison.Ordinal) == true);
        if (generator?.RootComponent is { } root)
        {
            foreach (SceneComponent child in root.Children)
            {
                // The other children of a generator are its generated meshes (the rail, the columns), not placed objects
                if (child is not RaceGeneratedModelComponent
                    || child.Name is not { } name
                    || !name.StartsWith($"{childPrefix}.", StringComparison.Ordinal))
                {
                    continue;
                }

                childCount++;
                bool hasPlacement = int.TryParse(name.AsSpan(childPrefix.Length + 1), out int index)
                    && index >= 0
                    && index < placements.Count
                    && placementsWithChild.Add(index);
                childrenWithoutPlacement += hasPlacement ? 0 : 1;
                AddPlacedObject(
                    placedObjects,
                    hasPlacement ? placements[index].ModelName : defaultModelName,
                    space.ToLegacyWorldTransform(child.WorldMatrixWithScale),
                    child);
            }
        }

        int placementsWithoutChild = placements.Count - placementsWithChild.Count;
        notBuiltCount += placementsWithoutChild;
        generatorChildren.Add(new ReferenceGeneratorChildren(label, childCount, placements.Count, childrenWithoutPlacement, placementsWithoutChild));
    }

    private static void AddPlacedObject(List<ReferencePlacedObject> placedObjects, string modelName, Matrix legacyTransform, SceneComponent component)
    {
        PrimitiveComponent? primitive = component as PrimitiveComponent;
        placedObjects.Add(new ReferencePlacedObject(modelName, legacyTransform, primitive?.CastShadows, primitive?.ReceiveShadows));
    }
}

internal readonly record struct ReferenceRoadPoint(Vector3 Position, Vector3 Right, Vector3 Up, Vector3 Direction, float RoadWidth);

/// <summary>
/// A placed model. <paramref name="CastsShadow"/> and <paramref name="ReceivesShadow"/> are the flags of the component the
/// game created (<c>null</c>: not known, as in an original scene.json written before the flags were exported).
/// </summary>
internal readonly record struct ReferencePlacedObject(string ModelName, Matrix LegacyTransform, bool? CastsShadow = null, bool? ReceivesShadow = null);

/// <summary>
/// The children of one kind a generator holds (<paramref name="ChildCount"/>) against the placements the layout has for it
/// (<paramref name="PlacementCount"/>), among them the children without a placement and the placements without a child.
/// </summary>
internal readonly record struct ReferenceGeneratorChildren(string Label, int ChildCount, int PlacementCount, int ChildrenWithoutPlacement, int PlacementsWithoutChild);
