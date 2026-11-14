using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace RacingGameCasaEngine.Worlds;

/// <summary>
/// The objects the original game places in a track (Landscape.AddObjectToRender and the generation code of Track):
/// same add path, same order, same random draws, so that the port ends up with the same objects.
/// </summary>
internal static partial class LegacyTrackSceneFactory
{
    /// <summary>
    /// Seed of the random scenery of the next <see cref="Create"/>. The original draws it from a generator it
    /// re-creates before each level: <c>new Random(1)</c> in its reference capture, a time-based seed in the game.
    /// <c>null</c> (the default) is the time-based seed.
    /// </summary>
    internal static int? SceneryRandomSeed { get; set; }

    // The models the original preloads for AddObjectToRender (Landscape.landscapeModels): an object whose model is
    // not in this list is not added.
    private static readonly HashSet<string> LandscapeModelNames = new(StringComparer.Ordinal)
    {
        "StartLight", "StartLight2", "StartLight3", "Blockade", "Blockade2", "Hydrant", "Kaktus", "Kaktus2",
        "KaktusBenny", "KaktusSeg", "AlphaDeadTree", "AlphaPalm", "AlphaPalm2", "AlphaPalm3", "AlphaPalmSmall",
        "Laterne", "Laterne2Sides", "Trashcan", "Roadsign", "Roadsign2", "Goal", "Building", "Building2", "Building3",
        "Building4", "Building5", "OilPump", "OilTanks", "RoadColumnSegment", "Windmill", "Ruin", "RuinHouse",
        "SandCastle", "Banner", "Banner2", "Banner3", "Banner4", "Banner5", "Banner6", "Sign", "Sign2", "SignWarning",
        "SignCurveLeft", "SignCurveRight", "SharpRock", "SharpRock2", "Stone4", "Stone5", "AlphaTrain",
        "GuardRailHolder", "Hotel01", "Hotel02", "Casino01",
    };

    // The combos of the original (Landscape.combos) and their size (TrackCombiModels.Size).
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

    // Landscape.autoGenerationNames: the first 6 entries are picked with more probability.
    private static readonly string[] AutoGenerationNames =
    [
        "CombiPalms", "CombiPalms2", "CombiRuins", "CombiRuins2", "CombiStones", "CombiStones2",
        "Kaktus", "Kaktus2", "KaktusBenny", "KaktusSeg", "AlphaDeadTree",
        "AlphaPalm", "AlphaPalm2", "AlphaPalm3", "AlphaPalmSmall",
        "Laterne2Sides", "Trashcan", "OilPump", "OilTanks", "RoadColumnSegment", "Windmill", "Ruin", "RuinHouse",
        "Sign", "Sign2", "SharpRock", "SharpRock2", "Stone4", "Stone5", "Casino01",
    ];

    // Model.Size of the original (Meshes[0].BoundingSphere.Radius * the root bone's scale of the XNA model), which
    // decides how far from an earlier object a new one has to be. The values are read from the original's compiled
    // models (RacingGameOriginal/Desktop/bin/Debug/net10.0/Content/Models/*.xnb) with a small throwaway MonoGame
    // program: the port's glTF conversions cannot give them (the meshes are in another order and some models are
    // stored in another unit), and one of them wrongly kept or dropped changes which objects the original places.
    private static readonly Dictionary<string, float> LegacyModelSizes = new(StringComparer.Ordinal)
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

    private static float GetLegacyModelSize(string modelName)
    {
        return LegacyModelSizes.TryGetValue(modelName, out float size) ? size : 1f;
    }

    private static Random CreateSceneryRandom()
    {
        return new Random(SceneryRandomSeed ?? (int)DateTime.Now.Ticks);
    }

    // The renamed models of Landscape.AddObjectToRender (exact, case sensitive names, as in the original).
    private static string FixLegacyModelName(string modelName)
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

    private static bool IsDuplicateCheckExempt(string modelName)
    {
        return modelName.StartsWith("Banner", StringComparison.Ordinal)
            || modelName.StartsWith("Sign", StringComparison.Ordinal)
            || modelName.StartsWith("StartLight", StringComparison.Ordinal);
    }

    /// <summary>The entities of one kind of object (scenery, guard rail holders...) and how they are named.</summary>
    private sealed class LegacyEntityGroup(Func<string, int, string> nameFactory)
    {
        public List<Entity> Entities { get; } = [];

        public string NextName(string modelName) => nameFactory(modelName, Entities.Count);
    }

    /// <summary>
    /// The port of Landscape.AddObjectToRender: the one way every object enters the scene. It keeps the position of
    /// all the objects added so far, because the original skips an object that is too close to an earlier one.
    /// </summary>
    private sealed class LegacyLandscape(
        Vector3 origin,
        LegacyTerrainHeightSampler terrainSampler,
        AssetContentManager assetContentManager,
        Random random)
    {
        private readonly List<Vector3> _positions = [];

        public Random Random { get; } = random;

        /// <summary>
        /// Position of the first Hotel or Building added (LandscapeObject.IsBigBuilding), in the original's coordinates:
        /// where Landscape.CreateCityPlane puts the city ground.
        /// </summary>
        public Vector3? FirstBigBuildingPosition { get; private set; }

        public float GetMapHeight(float x, float y) => terrainSampler.GetMapHeight(x, y);

        public void AddObjectToRender(string modelName, Matrix renderMatrix, LegacyEntityGroup group)
        {
            modelName = FixLegacyModelName(modelName);

            if (CombiSizes.ContainsKey(modelName))
            {
                // Adds all the models of the combo, each one through this method
                foreach (LegacyCombiObject combiObject in LoadCombiModel(modelName))
                {
                    AddObjectToRender(combiObject.modelName, combiObject.matrix * renderMatrix, group);
                }

                return;
            }

            if (!LandscapeModelNames.Contains(modelName))
            {
                return;
            }

            StaticModel? model = LoadLegacyModel(modelName, assetContentManager);
            if (model == null)
            {
                return;
            }

            // Fix z position to be always above the landscape
            Vector3 modelPosition = renderMatrix.Translation;
            float landscapeHeight = terrainSampler.GetMapHeight(modelPosition.X, modelPosition.Y);
            if (modelPosition.Z < landscapeHeight)
            {
                modelPosition.Z = landscapeHeight;
                renderMatrix.Translation = modelPosition;
            }

            // Skip the object if another one is nearby, except signs and banners
            if (!IsDuplicateCheckExempt(modelName))
            {
                float modelSize = GetLegacyModelSize(modelName);
                float threshold = modelSize * modelSize / 4;
                for (int index = 0; index < _positions.Count; index++)
                {
                    if (Vector3.DistanceSquared(_positions[index], modelPosition) < threshold)
                    {
                        return;
                    }
                }
            }

            // Scale all objects up a little (else the world is not filled enough)
            Matrix placedTransform = Matrix.CreateScale(1.2f) * renderMatrix;
            _positions.Add(placedTransform.Translation);
            if (FirstBigBuildingPosition == null
                && (modelName.Contains("hotel", StringComparison.OrdinalIgnoreCase)
                    || modelName.Contains("building", StringComparison.OrdinalIgnoreCase)))
            {
                FirstBigBuildingPosition = placedTransform.Translation;
            }

            RecordPlacedObject(modelName, placedTransform);
            group.Entities.Add(CreateStaticModelEntity(
                group.NextName(modelName),
                model,
                ConvertLegacyTransform(placedTransform, origin)));
        }

        /// <summary>The overload of the original that puts an object beside the road, away from it.</summary>
        public void AddObjectToRender(string modelName, float rotation, Vector3 trackPosition, Vector3 trackRight, float distance, LegacyEntityGroup group)
        {
            // Find out the size
            float objectSize = 1;
            if (CombiSizes.TryGetValue(modelName, out float combiSize))
            {
                objectSize = combiSize;
            }

            if (LandscapeModelNames.Contains(modelName) && LoadLegacyModel(modelName, assetContentManager) != null)
            {
                objectSize = GetLegacyModelSize(modelName);
            }

            // Make sure it is away from the road.
            if (distance > 0 && distance - 10 < objectSize)
            {
                distance += objectSize;
            }

            if (distance < 0 && distance + 10 > -objectSize)
            {
                distance -= objectSize;
            }

            AddObjectToRender(
                modelName,
                Matrix.CreateRotationZ(rotation) * Matrix.CreateTranslation(trackPosition + trackRight * distance + new Vector3(0, 0, -100)),
                group);
        }
    }

    // Track.GenerateObjectsForTrack: palms and lanterns, the start banner and light, the checkpoint banners, the signs
    // and the random scenery, in the original's order.
    private static void AddHelperDrivenSceneryEntities(
        LegacyEntityGroup sceneryEntities,
        IReadOnlyList<LegacyTrackPoint> roadPoints,
        IReadOnlyList<LegacyRoadHelper> roadHelpers,
        LegacyLandscape landscape)
    {
        if (roadPoints.Count == 0)
        {
            return;
        }

        List<LegacyRoadHelperPosition> helperPositions = BuildRoadHelperPositions(roadPoints, roadHelpers);

        AddPalmAndLaternEntities(sceneryEntities, roadPoints, helperPositions, landscape);
        AddStartObjects(sceneryEntities, roadPoints, landscape);
        AddCheckpointAndSignEntities(sceneryEntities, roadPoints, landscape);
        AddRandomSceneryEntities(sceneryEntities, roadPoints, landscape);
    }

    private static void AddPalmAndLaternEntities(
        LegacyEntityGroup sceneryEntities,
        IReadOnlyList<LegacyTrackPoint> roadPoints,
        IReadOnlyList<LegacyRoadHelperPosition> helperPositions,
        LegacyLandscape landscape)
    {
        Random random = landscape.Random;
        float lastGap = 0.0f;
        int generatedNum = 0;

        for (int pointIndex = 0; pointIndex < roadPoints.Count; pointIndex++)
        {
            bool palms = false;
            bool laterns = false;
            for (int helperIndex = 0; helperIndex < helperPositions.Count; helperIndex++)
            {
                LegacyRoadHelperPosition helper = helperPositions[helperIndex];
                if (pointIndex < helper.StartIndex || pointIndex > helper.EndIndex)
                {
                    continue;
                }

                if (helper.Type == LegacyRoadHelperType.Palms)
                {
                    palms = true;
                }
                else if (helper.Type == LegacyRoadHelperType.Laterns)
                {
                    laterns = true;
                }
            }

            if (!palms && !laterns)
            {
                continue;
            }

            LegacyTrackPoint point = roadPoints[pointIndex];
            float distance = Vector3.Distance(roadPoints[(pointIndex + 1) % roadPoints.Count].Position, point.Position);
            if (lastGap - distance <= 0.0f)
            {
                if (!IsTracksidePlacementAllowed(point))
                {
                    continue;
                }

                Vector3 right = GetLegacyRawRight(point);
                Matrix pointSpace = CreateLegacyPointSpace(point);
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, lastGap / distance);
                generatedNum++;

                if (palms)
                {
                    // Skip the palm if the road is more than 11 m above the ground
                    if (objectPoint.Z - landscape.GetMapHeight(objectPoint.X, objectPoint.Y) < 11.0f)
                    {
                        int randomNum = random.Next(4);
                        // Less probability for the small palm
                        if (randomNum == 3)
                        {
                            randomNum = random.Next(4);
                        }

                        string palmName = randomNum == 0 ? "AlphaPalm"
                            : randomNum == 1 ? "AlphaPalm2"
                            : randomNum == 2 ? "AlphaPalm3" : "AlphaPalmSmall";
                        landscape.AddObjectToRender(
                            palmName,
                            Matrix.CreateScale(1.25f)
                            * Matrix.CreateRotationZ(NextFloat(random, 0, MathHelper.Pi * 2))
                            * Matrix.CreateTranslation(right * (generatedNum % 2 == 0 ? 0.6f : -0.6f) * point.RoadWidth * LegacyRoadWidthScale)
                            * Matrix.CreateTranslation(new Vector3(0, 0, -50))
                            * Matrix.CreateTranslation(objectPoint),
                            sceneryEntities);
                    }
                }
                else
                {
                    landscape.AddObjectToRender(
                        "Laterne",
                        Matrix.CreateRotationZ(generatedNum % 2 == 0 ? MathHelper.Pi : 0.0f)
                        * Matrix.CreateTranslation(new Vector3(
                            (generatedNum % 2 == 0 ? 0.5f : -0.5f) * point.RoadWidth * LegacyRoadWidthScale - 0.35f,
                            0,
                            -0.2f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        sceneryEntities);
                }

                lastGap += PalmAndLaternGap;
            }

            lastGap -= distance;
        }
    }

    // The goal and start light models are always added.
    private static void AddStartObjects(LegacyEntityGroup sceneryEntities, IReadOnlyList<LegacyTrackPoint> roadPoints, LegacyLandscape landscape)
    {
        LegacyTrackPoint startPoint = roadPoints[0];
        Matrix startPointSpace = CreateLegacyPointSpace(startPoint);

        landscape.AddObjectToRender(
            "Banner6",
            Matrix.CreateScale(startPoint.RoadWidth)
            // Scale up 1.1, but compensate the 1.2 done in AddObjectToRender
            * Matrix.CreateScale(1.051f)
            * Matrix.CreateTranslation(new Vector3(0, -5.1f, 0))
            * startPointSpace
            * Matrix.CreateTranslation(startPoint.Position),
            sceneryEntities);

        // The original adds StartLight3 and turns it into the red StartLight once the level is loaded
        // (Landscape.FinishTrackLoad); RaceStartLight then turns it yellow, then green.
        landscape.AddObjectToRender(
            StartLightModelNames[0],
            Matrix.CreateScale(1.1f)
            * Matrix.CreateTranslation(new Vector3(startPoint.RoadWidth * LegacyRoadWidthScale * 0.50f - 0.3f, 6, -0.2f))
            * startPointSpace
            * Matrix.CreateTranslation(startPoint.Position),
            sceneryEntities);
    }

    private static void AddCheckpointAndSignEntities(LegacyEntityGroup sceneryEntities, IReadOnlyList<LegacyTrackPoint> roadPoints, LegacyLandscape landscape)
    {
        Random random = landscape.Random;
        float checkpointGap = CheckpointGap;
        float signGap = SignGap;

        // Don't add another one near the end!
        for (int pointIndex = 0; pointIndex < roadPoints.Count - 24; pointIndex++)
        {
            LegacyTrackPoint point = roadPoints[pointIndex];
            float distance = Vector3.Distance(roadPoints[(pointIndex + 1) % roadPoints.Count].Position, point.Position);
            if (!IsTracksidePlacementAllowed(point))
            {
                continue;
            }

            Vector3 right = GetLegacyRawRight(point);
            Matrix pointSpace = CreateLegacyPointSpace(point);
            if (checkpointGap - distance <= 0)
            {
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, checkpointGap / distance);
                string bannerName = CheckpointBannerCandidates[random.Next(6)];
                landscape.AddObjectToRender(
                    bannerName,
                    Matrix.CreateScale(point.RoadWidth)
                    * Matrix.CreateTranslation(new Vector3(0, 0, -0.1f))
                    * pointSpace
                    * Matrix.CreateTranslation(objectPoint),
                    sceneryEntities);

                checkpointGap += CheckpointGap;
            }
            else if (signGap - distance <= 0 && pointIndex >= 25)
            {
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, signGap / distance);

                // Find out how curvy this point is by going back 25 points.
                Vector3 backPosition = roadPoints[(pointIndex - 25) % roadPoints.Count].Position;
                bool loopingAhead = roadPoints[(pointIndex + 60) % roadPoints.Count].Up.Z < 0.15f;
                Vector3 angleVector = Vector3.Normalize(backPosition - point.Position);
                float roadAngle = GetAngleBetweenNormalizedVectors(angleVector, Vector3.Normalize(-point.Direction));
                // If the road goes to the left, use a negative angle value!
                if (Vector3.Distance(right, angleVector) < Vector3.Distance(-right, angleVector))
                {
                    roadAngle = -roadAngle;
                }

                float halfRoadWidth = point.RoadWidth * LegacyRoadWidthScale * 0.5f;
                if (loopingAhead)
                {
                    landscape.AddObjectToRender(
                        "SignWarning",
                        Matrix.CreateTranslation(new Vector3(halfRoadWidth - 0.1f, 0, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        sceneryEntities);
                }
                else if (roadAngle < -MathHelper.Pi / 7.5f)
                {
                    landscape.AddObjectToRender(
                        "SignCurveRight",
                        Matrix.CreateRotationZ(MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(-point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.15f, 0, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        sceneryEntities);
                }
                else if (roadAngle > MathHelper.Pi / 7.5f)
                {
                    landscape.AddObjectToRender(
                        "SignCurveLeft",
                        Matrix.CreateRotationZ(-MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(halfRoadWidth - 0.15f, 0, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        sceneryEntities);
                }
                else if (roadAngle < -MathHelper.Pi / 10.0f
                    || roadAngle > MathHelper.Pi / 10.0f
                    || random.Next(9) == 4)
                {
                    // Also mix in random curve signs, this is still a curve
                    int randomVariant = random.Next(3);
                    // Randomize again if not that curvy here
                    if (randomVariant == 0 && Math.Abs(roadAngle) < MathHelper.Pi / 24)
                    {
                        randomVariant = random.Next(3);
                    }
                    else if (Math.Abs(roadAngle) < MathHelper.Pi / 20 && random.Next(2) == 1)
                    {
                        roadAngle *= -1;
                    }

                    string signName = randomVariant == 0
                        ? (roadAngle > 0 ? "SignCurveLeft" : "SignCurveRight")
                        : (randomVariant == 1 ? "Sign" : "Sign2");
                    landscape.AddObjectToRender(
                        signName,
                        Matrix.CreateRotationZ((roadAngle > 0 ? -1 : 1) * MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(
                            (roadAngle > 0 ? 1 : -1) * point.RoadWidth * LegacyRoadWidthScale * 0.5f - (randomVariant == 0 ? 0.15f : 0.005f),
                            0,
                            -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        sceneryEntities);
                }

                // We have just set a sign (or not), check for the next sign after the gap.
                signGap += SignGap;
            }

            checkpointGap -= distance;
            signGap -= distance;
        }
    }

    // Randomly generated scenery beside the road, away from the road and from the existing objects.
    private static void AddRandomSceneryEntities(LegacyEntityGroup sceneryEntities, IReadOnlyList<LegacyTrackPoint> roadPoints, LegacyLandscape landscape)
    {
        Random random = landscape.Random;
        const int randomMaxProbability = 5; // high detail: twice as many objects as the low detail's 10
        int lastNameIndex = AutoGenerationNames.Length - 1;

        for (int pointIndex = 0; pointIndex < roadPoints.Count; pointIndex += 2)
        {
            LegacyTrackPoint point = roadPoints[pointIndex];

            // Skip the generation at great heights
            if (point.Position.Z - landscape.GetMapHeight(point.Position.X, point.Position.Y) > 60.0f)
            {
                continue;
            }

            if (!IsTracksidePlacementAllowed(point))
            {
                continue;
            }

            // Generate stuff in 20% of the cases
            if (random.Next(randomMaxProbability) != 0)
            {
                continue;
            }

            int randomObjectNum = random.Next(AutoGenerationNames.Length);

            // If above 6, generate again
            if (randomObjectNum >= 6)
            {
                randomObjectNum = random.Next(AutoGenerationNames.Length);
            }

            // Don't generate so many casinos
            if (randomObjectNum == lastNameIndex && random.Next(3) < 2)
            {
                randomObjectNum = random.Next(AutoGenerationNames.Length);
            }

            float distance = NextFloat(random, 26, 88);
            // For casinos make sure the object is far enough away.
            if (randomObjectNum == lastNameIndex)
            {
                distance += 20;
            }

            bool side = random.Next(2) == 0;
            float rotation = NextFloat(random, 0, MathHelper.Pi * 2);
            landscape.AddObjectToRender(
                AutoGenerationNames[randomObjectNum],
                rotation,
                point.Position,
                GetLegacyRawRight(point),
                distance * (side ? 1 : -1),
                sceneryEntities);
        }
    }

    // RandomHelper.GetRandomFloat
    private static float NextFloat(Random random, float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }

    // Vector3Helper.GetAngleBetweenVectors: no clamp, so a dot product a bit above 1 gives NaN like in the original,
    // which changes which comparisons (and so which random draws) follow.
    private static float GetAngleBetweenNormalizedVectors(Vector3 first, Vector3 second)
    {
        return (float)Math.Acos(Vector3.Dot(first, second));
    }
}
