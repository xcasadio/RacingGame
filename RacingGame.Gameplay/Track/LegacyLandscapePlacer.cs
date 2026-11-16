using Microsoft.Xna.Framework;
using static RacingGame.Gameplay.Track.RaceTrackSpace;

namespace RacingGame.Gameplay.Track;

/// <summary>
/// The one way an object enters a legacy scene: a port of the original's <c>Landscape.AddObjectToRender</c>
/// (RacingGameOriginal/Core/Landscapes/Landscape.cs). It keeps the position of every object added so far, because the
/// original skips an object that is too close to an earlier one, whatever the earlier object is (a neutral object of the
/// track file, a guard rail post, a column segment, a palm...). The guard rail posts, the column segments, the objects of
/// the track file and the generated scenery all go through it, in the order the original adds them.
/// </summary>
internal sealed class LegacyLandscapePlacer
{
    // Model.Size of every model the original preloads for AddObjectToRender (Landscape.cs:271-326, landscapeModels), which
    // is the model's realScaling: Meshes[0].BoundingSphere.Radius * the Right length of the root bone transform
    // (RacingGameOriginal/Core/Graphics/Model.cs:185-190). It decides how far from an earlier object a new one has to be
    // (Landscape.cs:459-473). The values were read from the original's compiled models, the .xnb files of
    // RacingGameOriginal/Desktop/bin/Debug/net10.0/Content/Models, by a throwaway MonoGame program run on the other PC (see
    // ai-agent/tasks/rgce-3d-geometry-fidelity-tasks.md, T4 and open point O1): the cooked glTF models cannot give them
    // (their meshes are in another order, and some are stored in another unit). An object whose model is not in this table
    // is not added, as in the original.
    private static readonly Dictionary<string, float> ModelSizes = new(StringComparer.Ordinal)
    {
        ["StartLight"] = 140.67923f,
        ["StartLight2"] = 140.67923f,
        ["StartLight3"] = 140.67923f,
        ["Blockade"] = 2.6232657f,
        ["Blockade2"] = 2.6584532f,
        ["Hydrant"] = 0.73886937f,
        ["Kaktus"] = 2.1352797f,
        ["Kaktus2"] = 1.970255f,
        ["KaktusBenny"] = 2.149308f,
        ["KaktusSeg"] = 2.1352413f,
        ["AlphaDeadTree"] = 29.411222f,
        ["AlphaPalm"] = 7.4883313f,
        ["AlphaPalm2"] = 7.100757f,
        ["AlphaPalm3"] = 7.530296f,
        ["AlphaPalmSmall"] = 3.1228712f,
        ["Laterne"] = 4.5709515f,
        ["Laterne2Sides"] = 3.8274932f,
        ["Trashcan"] = 1.3335549f,
        ["Roadsign"] = 1.8391635f,
        ["Roadsign2"] = 1.8049462f,
        ["Goal"] = 5.447921f,
        ["Building"] = 17.918053f,
        ["Building2"] = 5.420309f,
        ["Building3"] = 15.755752f,
        ["Building4"] = 18.454165f,
        ["Building5"] = 20.615528f,
        ["OilPump"] = 5.861919f,
        ["OilTanks"] = 22.500195f,
        ["RoadColumnSegment"] = 49.04808f,
        ["Windmill"] = 5.4170876f,
        ["Ruin"] = 8.834589f,
        ["RuinHouse"] = 13.21373f,
        ["SandCastle"] = 22.54499f,
        ["Banner"] = 7.503903f,
        ["Banner2"] = 7.503903f,
        ["Banner3"] = 7.503903f,
        ["Banner4"] = 7.503903f,
        ["Banner5"] = 7.503903f,
        ["Banner6"] = 7.503903f,
        ["Sign"] = 5.0179577f,
        ["Sign2"] = 5.0179577f,
        ["SignWarning"] = 5.1938734f,
        ["SignCurveLeft"] = 3.9329133f,
        ["SignCurveRight"] = 3.9327815f,
        ["SharpRock"] = 3.1633534f,
        ["SharpRock2"] = 4.178886f,
        ["Stone4"] = 2.784325f,
        ["Stone5"] = 2.2884557f,
        ["AlphaTrain"] = 73.57597f,
        ["GuardRailHolder"] = 0.4194772f,
        ["Hotel01"] = 4.6980696f,
        ["Hotel02"] = 87.41026f,
        ["Casino01"] = 6.360404f,
    };

    // The combos of the original (Landscape.cs:333-345, combos) and their size (TrackCombiModels.cs:125-131).
    private static readonly Dictionary<string, float> CombiSizes = new(StringComparer.Ordinal)
    {
        ["CombiPalms"] = 10f,
        ["CombiPalms2"] = 10f,
        ["CombiRuins"] = 10f,
        ["CombiRuins2"] = 10f,
        ["CombiStones"] = 10f,
        ["CombiStones2"] = 10f,
        ["CombiOilTanks"] = 50f,
        ["CombiSandCastle"] = 50f,
        ["CombiBuildings"] = 50f,
        ["CombiHotels"] = 50f,
    };

    /// <summary>
    /// Landscape.cs:351-384, autoGenerationNames: the objects of the random scenery. The first 6 entries are drawn with
    /// more probability.
    /// </summary>
    internal static readonly string[] AutoGenerationNames =
    [
        "CombiPalms", "CombiPalms2", "CombiRuins", "CombiRuins2", "CombiStones", "CombiStones2",
        "Kaktus", "Kaktus2", "KaktusBenny", "KaktusSeg", "AlphaDeadTree",
        "AlphaPalm", "AlphaPalm2", "AlphaPalm3", "AlphaPalmSmall",
        "Laterne2Sides", "Trashcan", "OilPump", "OilTanks", "RoadColumnSegment", "Windmill", "Ruin", "RuinHouse",
        "Sign", "Sign2", "SharpRock", "SharpRock2", "Stone4", "Stone5", "Casino01",
    ];

    private readonly Vector3 _origin;
    private readonly LegacyTerrainHeightSampler _terrainSampler;
    private readonly Func<string, IReadOnlyList<LegacyCombiObject>> _loadCombiModel;
    private readonly List<Vector3> _positions = [];

    public LegacyLandscapePlacer(
        Vector3 origin,
        LegacyTerrainHeightSampler terrainSampler,
        Func<string, IReadOnlyList<LegacyCombiObject>> loadCombiModel)
    {
        _origin = origin;
        _terrainSampler = terrainSampler;
        _loadCombiModel = loadCombiModel;
    }

    /// <summary>Every object added, in the order it was added, whatever list it went to.</summary>
    public List<RaceTrackModelPlacement> Placements { get; } = [];

    /// <summary>
    /// Landscape.AddObjectToRender(string, Matrix, bool), Landscape.cs:392-497. An added object is appended to
    /// <paramref name="target"/> (and to <see cref="Placements"/>) with its transform in runtime space.
    /// </summary>
    /// <param name="modelName">The model, or a combo, which adds each of its models through this method.</param>
    /// <param name="renderMatrix">The object's transform in legacy space, without the scale of 1.2 this method adds.</param>
    /// <param name="target">The list the placement is appended to.</param>
    public void AddObject(string modelName, Matrix renderMatrix, List<RaceTrackModelPlacement> target)
    {
        // Fix wrong model names (Landscape.cs:395-407)
        modelName = FixModelName(modelName);

        // Search for combos (Landscape.cs:416-428)
        if (CombiSizes.ContainsKey(modelName))
        {
            // Add all combi objects (TrackCombiModels.cs:139-145): each one goes through this method
            foreach (LegacyCombiObject combiObject in _loadCombiModel(modelName))
            {
                AddObject(combiObject.modelName, combiObject.matrix * renderMatrix, target);
            }

            return;
        }

        // Only add if the model is one the original preloads (Landscape.cs:430-444)
        if (!ModelSizes.TryGetValue(modelName, out float modelSize))
        {
            return;
        }

        // Fix z position to be always above the landscape (Landscape.cs:446-457)
        Vector3 modelPosition = renderMatrix.Translation;
        float landscapeHeight = _terrainSampler.GetMapHeight(modelPosition.X, modelPosition.Y);
        if (modelPosition.Z < landscapeHeight)
        {
            modelPosition.Z = landscapeHeight;
            renderMatrix.Translation = modelPosition;
        }

        // Check if another object is nearby, then skip this one. Signs, banners and start lights are never skipped
        // (Landscape.cs:459-473).
        if (!modelName.StartsWith("Banner", StringComparison.Ordinal)
            && !modelName.StartsWith("Sign", StringComparison.Ordinal)
            && !modelName.StartsWith("StartLight", StringComparison.Ordinal))
        {
            for (int index = 0; index < _positions.Count; index++)
            {
                if (Vector3.DistanceSquared(_positions[index], modelPosition) < modelSize * modelSize / 4)
                {
                    return;
                }
            }
        }

        // Scale all objects up a little (Landscape.cs:475-479)
        Matrix placedTransform = Matrix.CreateScale(1.2f) * renderMatrix;
        _positions.Add(placedTransform.Translation);

        var placement = new RaceTrackModelPlacement(modelName, ConvertLegacyTransform(placedTransform, _origin));
        target.Add(placement);
        Placements.Add(placement);
    }

    /// <summary>Landscape.AddObjectToRender(string, Vector3), Landscape.cs:557-560.</summary>
    public void AddObject(string modelName, Vector3 renderPosition, List<RaceTrackModelPlacement> target)
    {
        AddObject(modelName, Matrix.CreateTranslation(renderPosition), target);
    }

    /// <summary>
    /// Landscape.AddObjectToRender(string, float, Vector3, Vector3, float), Landscape.cs:507-550: puts an object beside the
    /// road, away from it. <paramref name="trackRight"/> is the road point's right vector as it is, not normalized.
    /// </summary>
    public void AddObject(
        string modelName,
        float rotation,
        Vector3 trackPosition,
        Vector3 trackRight,
        float distance,
        List<RaceTrackModelPlacement> target)
    {
        // Find out size (Landscape.cs:511-536): the combo's, then the model's
        float objectSize = 1;
        if (CombiSizes.TryGetValue(modelName, out float combiSize))
        {
            objectSize = combiSize;
        }

        if (ModelSizes.TryGetValue(modelName, out float modelSize))
        {
            objectSize = modelSize;
        }

        // Make sure it is away from the road (Landscape.cs:538-544).
        if (distance > 0 && distance - 10 < objectSize)
        {
            distance += objectSize;
        }

        if (distance < 0 && distance + 10 > -objectSize)
        {
            distance -= objectSize;
        }

        AddObject(
            modelName,
            Matrix.CreateRotationZ(rotation) * Matrix.CreateTranslation(trackPosition + trackRight * distance + new Vector3(0, 0, -100)),
            target);
    }

    // Landscape.cs:395-407. Exact, case sensitive names, as in the original.
    private static string FixModelName(string modelName)
    {
        return modelName switch
        {
            "OilWell" => "OilPump",
            "PalmSmall" => "AlphaPalmSmall",
            "AlphaPalm4" => "AlphaPalmSmall",
            "Palm" => "AlphaPalm",
            "Casino" => "Casino01",
            "Combi" => "CombiPalms",
            _ => modelName,
        };
    }
}
