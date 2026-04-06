using System.Text.Json;
using Microsoft.Xna.Framework;

public static partial class Program
{
    private const float GroundMargin = 18f;
    private const float HelperActivationDistance = 25.0f;
    private const float PalmAndLaternGap = 20.0f;
    private const float CheckpointGap = 500.0f;
    private const float SignGap = 24.0f;

    private static readonly string[] PalmModelCandidates = ["AlphaPalm", "AlphaPalm2", "AlphaPalm3", "AlphaPalmSmall"];
    private static readonly string[] CheckpointBannerCandidates = ["Banner", "Banner2", "Banner3", "Banner4", "Banner5", "Banner6"];

    private static RuntimeSceneTrackRecord ExportExpectedRuntimeTrack(
        string trackFileName,
        TrackLayout layout,
        Vector3 comparisonOrigin,
        TerrainHeightSampler terrain,
        ModelSizeProvider modelSizeProvider,
        CombiLoader combiLoader,
        string modelRoot)
    {
        string runtimeTrackName = NormalizeRuntimeTrackName(trackFileName);
        var state = new RuntimeSceneExpectationState(runtimeTrackName, comparisonOrigin, terrain, modelSizeProvider, combiLoader, modelRoot);
        TrackSplineComputation trackSpline = BuildTrackSplinePoints(layout.TrackPoints, layout.WidthHelpers, terrain);
        List<Vector3> roadPoints = trackSpline.Points.Select(point => ConvertLegacyPoint(point.Position) - comparisonOrigin).ToList();

        if (roadPoints.Count > 0)
        {
            state.Entities.Add(CreateExpectedGroundEntity(runtimeTrackName, roadPoints));
            state.Entities.Add(CreateExpectedRoadEntity(runtimeTrackName));
        }

        for (int index = 0; index < layout.NeutralsObjects.Count; index++)
        {
            NeutralObject neutralObject = layout.NeutralsObjects[index];
            AddExpectedSceneryEntities(state, neutralObject.modelName, neutralObject.matrix);
        }

        if (trackSpline.Points.Count > 0)
        {
            AddExpectedHelperDrivenSceneryEntities(runtimeTrackName, state, trackSpline.Points, layout.RoadHelpers);

            List<Float3> checkpointPositions = BuildCheckpointRecords(trackSpline.Points, comparisonOrigin);
            for (int index = 0; index < checkpointPositions.Count; index++)
            {
                state.Entities.Add(CreateExpectedCheckpointEntity(index, checkpointPositions[index]));
            }

            state.Entities.Add(CreateExpectedPlayerStartEntity(CreateTrackStartPose(trackSpline.Points[0], comparisonOrigin)));
        }

        state.Entities.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
        return new RuntimeSceneTrackRecord
        {
            TrackName = runtimeTrackName,
            WorldName = $"RacingGameCasaEngine.RaceWorld.{runtimeTrackName}",
            EntityCount = state.Entities.Count,
            VisibleEntityCount = state.Entities.Count(static entity => entity.IsVisible),
            ComparisonTargetCount = state.Entities.Count(static entity => entity.IncludeInDeterministicComparison),
            Entities = state.Entities,
        };
    }

    private static RuntimeSceneTrackRecord BuildAuthoredRuntimeTrack(TrackExport casaTrack)
    {
        string runtimeTrackName = NormalizeRuntimeTrackName(casaTrack.TrackName);
        var entities = new List<RuntimeSceneEntityRecord>();

        if (casaTrack.RoadSamples.Count > 0)
        {
            List<Vector3> roadPoints = casaTrack.RoadSamples.Select(static sample => sample.Position.ToVector3()).ToList();
            entities.Add(CreateExpectedGroundEntity(runtimeTrackName, roadPoints));
            entities.Add(CreateExpectedRoadEntity(runtimeTrackName));
        }

        for (int index = 0; index < casaTrack.Placements.Count; index++)
        {
            PlacementRecord placement = casaTrack.Placements[index];
            entities.Add(CreateExpectedRuntimeEntity(
                $"Track.Scenery.{placement.ResolvedModelName}.{index:000}",
                "track-scenery",
                isVisible: true,
                isEnabled: true,
                includeInDeterministicComparison: true,
                staticModelName: placement.ResolvedModelName,
                FromMatrixRows(placement.ComparisonMatrix)));
        }

        for (int index = 0; index < casaTrack.CheckpointPositions.Count; index++)
        {
            entities.Add(CreateExpectedCheckpointEntity(index, casaTrack.CheckpointPositions[index]));
        }

        if (casaTrack.StartPose != null)
        {
            entities.Add(CreateExpectedPlayerStartEntity(casaTrack.StartPose));
        }

        entities.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
        return new RuntimeSceneTrackRecord
        {
            TrackName = runtimeTrackName,
            WorldName = $"RacingGameCasaEngine.RaceWorld.{runtimeTrackName}",
            EntityCount = entities.Count,
            VisibleEntityCount = entities.Count(static entity => entity.IsVisible),
            ComparisonTargetCount = entities.Count(static entity => entity.IncludeInDeterministicComparison),
            Entities = entities,
        };
    }

    private static RuntimeSceneEntityRecord CreateExpectedGroundEntity(string runtimeTrackName, IReadOnlyList<Vector3> roadPoints)
    {
        float minX = roadPoints.Min(static point => point.X) - GroundMargin;
        float maxX = roadPoints.Max(static point => point.X) + GroundMargin;
        float minY = roadPoints.Min(static point => point.Y) - 0.8f;
        float minZ = roadPoints.Min(static point => point.Z) - GroundMargin;
        float maxZ = roadPoints.Max(static point => point.Z) + GroundMargin;

        Matrix transform = Matrix.CreateTranslation(new Vector3((minX + maxX) * 0.5f, minY, (minZ + maxZ) * 0.5f));
        return CreateExpectedRuntimeEntity(
            $"Track.Ground.{runtimeTrackName}",
            "track-ground",
            isVisible: true,
            isEnabled: true,
            includeInDeterministicComparison: true,
            staticModelName: $"{runtimeTrackName}.Ground",
            transform);
    }

    private static RuntimeSceneEntityRecord CreateExpectedRoadEntity(string runtimeTrackName)
    {
        return CreateExpectedRuntimeEntity(
            $"Track.Road.{runtimeTrackName}",
            "track-road",
            isVisible: true,
            isEnabled: true,
            includeInDeterministicComparison: true,
            staticModelName: $"{runtimeTrackName}.Road",
            Matrix.Identity);
    }

    private static RuntimeSceneEntityRecord CreateExpectedCheckpointEntity(int checkpointIndex, Float3 checkpointPosition)
    {
        Matrix transform = Matrix.CreateTranslation(checkpointPosition.ToVector3());
        return CreateExpectedRuntimeEntity(
            $"Checkpoint.{checkpointIndex + 1:00}",
            "checkpoint",
            isVisible: true,
            isEnabled: true,
            includeInDeterministicComparison: true,
            staticModelName: null,
            transform);
    }

    private static RuntimeSceneEntityRecord CreateExpectedPlayerStartEntity(TrackStartPoseRecord startPose)
    {
        Matrix transform = Matrix.CreateScale(Vector3.One)
            * Matrix.CreateFromQuaternion(startPose.Orientation.ToQuaternion())
            * Matrix.CreateTranslation(startPose.Position.ToVector3());
        return CreateExpectedRuntimeEntity(
            "PlayerStart",
            "player-start",
            isVisible: true,
            isEnabled: true,
            includeInDeterministicComparison: true,
            staticModelName: null,
            transform);
    }

    private static RuntimeSceneEntityRecord CreateExpectedRuntimeEntity(
        string name,
        string kind,
        bool isVisible,
        bool isEnabled,
        bool includeInDeterministicComparison,
        string? staticModelName,
        Matrix worldMatrix)
    {
        DecomposeMatrix(worldMatrix, out Vector3 position, out Quaternion orientation, out Vector3 scale);
        return new RuntimeSceneEntityRecord
        {
            Name = name,
            Kind = kind,
            IsVisible = isVisible,
            IsEnabled = isEnabled,
            IncludeInDeterministicComparison = includeInDeterministicComparison,
            StaticModelName = staticModelName,
            Position = Float3.FromVector3(position),
            Orientation = Float4.FromQuaternion(orientation),
            Scale = Float3.FromVector3(scale),
            WorldMatrix = ToMatrixRows(worldMatrix),
        };
    }

    private static void AddExpectedSceneryEntities(RuntimeSceneExpectationState state, string rawModelName, Matrix sourceMatrix)
    {
        if (string.IsNullOrWhiteSpace(rawModelName))
        {
            return;
        }

        string resolvedModelName = ApplyAlias(rawModelName, CasaAliases, []);
        if (resolvedModelName.StartsWith("Track", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (resolvedModelName.StartsWith("Combi", StringComparison.OrdinalIgnoreCase))
        {
            if (!state.CombiLoader.TryLoadCombi(resolvedModelName, out IReadOnlyList<CombiObject> combiObjects))
            {
                return;
            }

            for (int index = 0; index < combiObjects.Count; index++)
            {
                CombiObject child = combiObjects[index];
                AddExpectedSceneryEntities(state, child.modelName, child.matrix * sourceMatrix);
            }

            return;
        }

        string modelPath = Path.Combine(state.ModelRoot, resolvedModelName + ".X");
        if (!File.Exists(modelPath))
        {
            return;
        }

        Matrix adjustedMatrix = sourceMatrix;
        Vector3 legacyPosition = adjustedMatrix.Translation;
        float terrainHeight = state.Terrain.GetMapHeight(legacyPosition.X, legacyPosition.Y);
        if (legacyPosition.Z < terrainHeight)
        {
            legacyPosition.Z = terrainHeight;
            adjustedMatrix.Translation = legacyPosition;
        }

        if (!IsDuplicateExempt(resolvedModelName))
        {
            float modelSize = state.ModelSizeProvider.GetModelSize(resolvedModelName);
            float threshold = modelSize * modelSize / 4f;
            for (int index = 0; index < state.CreatedLegacyPositions.Count; index++)
            {
                if (Vector3.DistanceSquared(state.CreatedLegacyPositions[index], legacyPosition) < threshold)
                {
                    return;
                }
            }
        }

        Matrix comparisonMatrix = ConvertLegacyTransform(Matrix.CreateScale(1.2f) * adjustedMatrix, state.ComparisonOrigin);
        state.Entities.Add(CreateExpectedRuntimeEntity(
            $"Track.Scenery.{resolvedModelName}.{state.SceneryIndex:000}",
            "track-scenery",
            isVisible: true,
            isEnabled: true,
            includeInDeterministicComparison: true,
            staticModelName: resolvedModelName,
            comparisonMatrix));
        state.CreatedLegacyPositions.Add(legacyPosition);
        state.SceneryIndex++;
    }

    private static void AddExpectedHelperDrivenSceneryEntities(
        string runtimeTrackName,
        RuntimeSceneExpectationState state,
        IReadOnlyList<ComparisonTrackPoint> roadPoints,
        IReadOnlyList<RoadHelper> roadHelpers)
    {
        if (roadPoints.Count == 0)
        {
            return;
        }

        List<LegacyRoadHelperPosition> helperPositions = BuildRoadHelperPositions(roadPoints, roadHelpers);
        Random random = CreateDeterministicTrackRandom(runtimeTrackName);
        AddPalmAndLaternExpectedEntities(state, roadPoints, helperPositions, random);
        AddSignAndCheckpointExpectedEntities(state, roadPoints, random);
    }

    private static List<LegacyRoadHelperPosition> BuildRoadHelperPositions(
        IReadOnlyList<ComparisonTrackPoint> roadPoints,
        IReadOnlyList<RoadHelper> roadHelpers)
    {
        var helperPositions = new List<LegacyRoadHelperPosition>();
        var remainingHelpers = new List<RoadHelper>(roadHelpers.Count);
        for (int index = 0; index < roadHelpers.Count; index++)
        {
            remainingHelpers.Add(new RoadHelper
            {
                type = roadHelpers[index].type,
                pos = roadHelpers[index].pos,
            });
        }

        int helperStartedNum = -1;
        LegacyRoadHelperType currentType = LegacyRoadHelperType.Reset;
        for (int pointIndex = 0; pointIndex < roadPoints.Count; pointIndex++)
        {
            Vector3 position = roadPoints[pointIndex].Position;
            for (int helperIndex = 0; helperIndex < remainingHelpers.Count; helperIndex++)
            {
                RoadHelper roadHelper = remainingHelpers[helperIndex];
                if (Vector3.Distance(roadHelper.pos, position) >= HelperActivationDistance)
                {
                    continue;
                }

                LegacyRoadHelperType helperType = ParseRoadHelperType(roadHelper.type);
                if (helperStartedNum >= 0)
                {
                    helperPositions.Add(new LegacyRoadHelperPosition(currentType, helperStartedNum, pointIndex));
                    if (helperType == LegacyRoadHelperType.Reset)
                    {
                        helperStartedNum = -1;
                    }
                    else
                    {
                        helperStartedNum = pointIndex;
                        currentType = helperType;
                    }
                }
                else if (helperType != LegacyRoadHelperType.Reset)
                {
                    helperStartedNum = pointIndex;
                    currentType = helperType;
                }

                remainingHelpers.RemoveAt(helperIndex);
                break;
            }
        }

        if (helperStartedNum > 0)
        {
            helperPositions.Add(new LegacyRoadHelperPosition(currentType, helperStartedNum, Math.Max(0, roadPoints.Count - 3)));
        }

        return helperPositions;
    }

    private static LegacyRoadHelperType ParseRoadHelperType(string helperType)
    {
        return Enum.TryParse(helperType, ignoreCase: true, out LegacyRoadHelperType parsedType)
            ? parsedType
            : LegacyRoadHelperType.Reset;
    }

    private static void AddPalmAndLaternExpectedEntities(
        RuntimeSceneExpectationState state,
        IReadOnlyList<ComparisonTrackPoint> roadPoints,
        IReadOnlyList<LegacyRoadHelperPosition> helperPositions,
        Random random)
    {
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

            ComparisonTrackPoint point = roadPoints[pointIndex];
            float distance = Vector3.Distance(roadPoints[(pointIndex + 1) % roadPoints.Count].Position, point.Position);
            if (lastGap - distance <= 0.0f)
            {
                if (!IsTracksidePlacementAllowed(point))
                {
                    continue;
                }

                Matrix pointSpace = CreateLegacyPointSpace(point);
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, lastGap / distance);
                generatedNum++;

                if (palms)
                {
                    float terrainHeight = state.Terrain.GetMapHeight(objectPoint.X, objectPoint.Y);
                    if (objectPoint.Z - terrainHeight < 11.0f)
                    {
                        int randomNum = random.Next(PalmModelCandidates.Length);
                        if (randomNum == PalmModelCandidates.Length - 1)
                        {
                            randomNum = random.Next(PalmModelCandidates.Length);
                        }

                        string modelName = PalmModelCandidates[Math.Clamp(randomNum, 0, PalmModelCandidates.Length - 1)];
                        Matrix legacyTransform =
                            Matrix.CreateScale(1.25f)
                            * Matrix.CreateRotationZ(NextFloat(random, 0.0f, MathHelper.TwoPi))
                            * Matrix.CreateTranslation(point.Right * (generatedNum % 2 == 0 ? 0.6f : -0.6f) * point.RoadWidth * LegacyRoadWidthScale)
                            * Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, -50.0f))
                            * Matrix.CreateTranslation(objectPoint);

                        AddExpectedSceneryEntities(state, modelName, legacyTransform);
                    }
                }
                else
                {
                    Matrix legacyTransform =
                        Matrix.CreateRotationZ(generatedNum % 2 == 0 ? MathHelper.Pi : 0.0f)
                        * Matrix.CreateTranslation(new Vector3(
                            (generatedNum % 2 == 0 ? 0.5f : -0.5f) * point.RoadWidth * LegacyRoadWidthScale - 0.35f,
                            0.0f,
                            -0.2f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint);

                    AddExpectedSceneryEntities(state, "Laterne", legacyTransform);
                }

                lastGap += PalmAndLaternGap;
            }

            lastGap -= distance;
        }
    }

    private static void AddSignAndCheckpointExpectedEntities(
        RuntimeSceneExpectationState state,
        IReadOnlyList<ComparisonTrackPoint> roadPoints,
        Random random)
    {
        ComparisonTrackPoint startPoint = roadPoints[0];
        Matrix startPointSpace = CreateLegacyPointSpace(startPoint);

        Matrix startBannerTransform =
            Matrix.CreateScale(startPoint.RoadWidth)
            * Matrix.CreateScale(1.051f)
            * Matrix.CreateTranslation(new Vector3(0.0f, -5.1f, 0.0f))
            * startPointSpace
            * Matrix.CreateTranslation(startPoint.Position);
        AddExpectedSceneryEntities(state, "Banner6", startBannerTransform);

        Matrix startLightTransform =
            Matrix.CreateScale(1.1f)
            * Matrix.CreateTranslation(new Vector3(startPoint.RoadWidth * LegacyRoadWidthScale * 0.50f - 0.3f, 6.0f, -0.2f))
            * startPointSpace
            * Matrix.CreateTranslation(startPoint.Position);
        AddExpectedSceneryEntities(state, "StartLight3", startLightTransform);

        float checkpointGap = CheckpointGap;
        float signGap = SignGap;
        for (int pointIndex = 0; pointIndex < roadPoints.Count - 24; pointIndex++)
        {
            ComparisonTrackPoint point = roadPoints[pointIndex];
            float distance = Vector3.Distance(roadPoints[(pointIndex + 1) % roadPoints.Count].Position, point.Position);
            if (!IsTracksidePlacementAllowed(point))
            {
                continue;
            }

            Matrix pointSpace = CreateLegacyPointSpace(point);
            if (checkpointGap - distance <= 0.0f)
            {
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, checkpointGap / distance);
                string bannerName = CheckpointBannerCandidates[random.Next(CheckpointBannerCandidates.Length)];
                Matrix checkpointTransform =
                    Matrix.CreateScale(point.RoadWidth)
                    * Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, -0.1f))
                    * pointSpace
                    * Matrix.CreateTranslation(objectPoint);
                AddExpectedSceneryEntities(state, bannerName, checkpointTransform);

                checkpointGap += CheckpointGap;
            }
            else if (signGap - distance <= 0.0f && pointIndex >= 25)
            {
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, signGap / distance);
                Vector3 backPosition = roadPoints[pointIndex - 25].Position;
                bool loopingAhead = roadPoints[(pointIndex + 60) % roadPoints.Count].Up.Z < 0.15f;
                Vector3 angleVector = Vector3.Normalize(backPosition - point.Position);
                float roadAngle = AngleBetweenVectors(angleVector, Vector3.Normalize(-point.Direction));
                if (Vector3.Distance(point.Right, angleVector) < Vector3.Distance(-point.Right, angleVector))
                {
                    roadAngle = -roadAngle;
                }

                if (loopingAhead)
                {
                    Matrix signTransform =
                        Matrix.CreateTranslation(new Vector3(point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.1f, 0.0f, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint);
                    AddExpectedSceneryEntities(state, "SignWarning", signTransform);
                }
                else if (roadAngle < -MathHelper.Pi / 7.5f)
                {
                    Matrix signTransform =
                        Matrix.CreateRotationZ(MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(-point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.15f, 0.0f, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint);
                    AddExpectedSceneryEntities(state, "SignCurveRight", signTransform);
                }
                else if (roadAngle > MathHelper.Pi / 7.5f)
                {
                    Matrix signTransform =
                        Matrix.CreateRotationZ(-MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.15f, 0.0f, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint);
                    AddExpectedSceneryEntities(state, "SignCurveLeft", signTransform);
                }
                else if (roadAngle < -MathHelper.Pi / 10.0f || roadAngle > MathHelper.Pi / 10.0f || random.Next(9) == 4)
                {
                    int randomVariant = random.Next(3);
                    if (randomVariant == 0 && Math.Abs(roadAngle) < MathHelper.Pi / 24.0f)
                    {
                        randomVariant = random.Next(3);
                    }
                    else if (Math.Abs(roadAngle) < MathHelper.Pi / 20.0f && random.Next(2) == 1)
                    {
                        roadAngle *= -1.0f;
                    }

                    string modelName = randomVariant == 0
                        ? (roadAngle > 0.0f ? "SignCurveLeft" : "SignCurveRight")
                        : (randomVariant == 1 ? "Sign" : "Sign2");
                    Matrix signTransform =
                        Matrix.CreateRotationZ((roadAngle > 0.0f ? -1.0f : 1.0f) * MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(
                            (roadAngle > 0.0f ? 1.0f : -1.0f) * point.RoadWidth * LegacyRoadWidthScale * 0.5f - (randomVariant == 0 ? 0.15f : 0.005f),
                            0.0f,
                            -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint);
                    AddExpectedSceneryEntities(state, modelName, signTransform);
                }

                signGap += SignGap;
            }

            checkpointGap -= distance;
            signGap -= distance;
        }
    }

    private static Matrix CreateLegacyPointSpace(ComparisonTrackPoint point)
    {
        Matrix pointSpace = Matrix.Identity;
        pointSpace.Right = point.Right;
        pointSpace.Up = point.Direction;
        pointSpace.Forward = -point.Up;
        return pointSpace;
    }

    private static bool IsTracksidePlacementAllowed(ComparisonTrackPoint point)
    {
        bool upsideDown = point.Up.Z < 0.05f;
        bool movingUp = point.Direction.Z > 0.65f;
        bool movingDown = point.Direction.Z < -0.65f;
        return !upsideDown && !movingUp && !movingDown;
    }

    private static Vector3 InterpolateRoadPoint(IReadOnlyList<ComparisonTrackPoint> roadPoints, int pointIndex, float amount)
    {
        Vector3 p1 = roadPoints[pointIndex - 1 < 0 ? roadPoints.Count - 1 : pointIndex - 1].Position;
        Vector3 p2 = roadPoints[pointIndex].Position;
        Vector3 p3 = roadPoints[(pointIndex + 1) % roadPoints.Count].Position;
        Vector3 p4 = roadPoints[(pointIndex + 2) % roadPoints.Count].Position;
        return Vector3.CatmullRom(p1, p2, p3, p4, amount);
    }

    private static Random CreateDeterministicTrackRandom(string runtimeTrackName)
    {
        var seed = new HashCode();
        seed.Add(runtimeTrackName, StringComparer.OrdinalIgnoreCase);
        seed.Add("TrackPlacementExporter.RuntimeScene");
        return new Random(seed.ToHashCode());
    }

    private static float NextFloat(Random random, float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }

    private static float AngleBetweenVectors(Vector3 first, Vector3 second)
    {
        if (first.LengthSquared() < 0.0001f || second.LengthSquared() < 0.0001f)
        {
            return 0.0f;
        }

        first.Normalize();
        second.Normalize();
        float dot = Math.Clamp(Vector3.Dot(first, second), -1.0f, 1.0f);
        return MathF.Acos(dot);
    }

    private static void DecomposeMatrix(Matrix worldMatrix, out Vector3 position, out Quaternion orientation, out Vector3 scale)
    {
        if (!worldMatrix.Decompose(out scale, out orientation, out position))
        {
            position = worldMatrix.Translation;
            scale = new Vector3(worldMatrix.Right.Length(), worldMatrix.Up.Length(), worldMatrix.Backward.Length());
            orientation = Quaternion.Identity;
        }

        if (orientation.LengthSquared() > 0.000001f)
        {
            orientation.Normalize();
        }
        else
        {
            orientation = Quaternion.Identity;
        }
    }

    private static string NormalizeRuntimeTrackName(string trackFileName)
    {
        return trackFileName.StartsWith("Track", StringComparison.OrdinalIgnoreCase)
            ? trackFileName["Track".Length..]
            : trackFileName;
    }

    private static RuntimeSceneExportFile LoadRuntimeSceneExport(string liveRuntimeScenePath)
    {
        string fullPath = Path.GetFullPath(liveRuntimeScenePath);
        string json = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<RuntimeSceneExportFile>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize runtime scene export '{fullPath}'.");
    }

    private static void PrintRuntimeSceneComparisonSummary(
        IReadOnlyList<RuntimeSceneTrackRecord> expectedTracks,
        IReadOnlyList<RuntimeSceneTrackRecord> liveTracks)
    {
        var liveTracksByName = liveTracks.ToDictionary(
            static track => NormalizeRuntimeTrackName(track.TrackName),
            StringComparer.OrdinalIgnoreCase);

        foreach (RuntimeSceneTrackRecord expectedTrack in expectedTracks.OrderBy(static track => track.TrackName, StringComparer.OrdinalIgnoreCase))
        {
            if (!liveTracksByName.TryGetValue(NormalizeRuntimeTrackName(expectedTrack.TrackName), out RuntimeSceneTrackRecord? liveTrack))
            {
                Console.WriteLine($"{expectedTrack.TrackName}: no live runtime scene export available.");
                continue;
            }

            var expectedEntities = expectedTrack.Entities
                .Where(static entity => entity.IncludeInDeterministicComparison)
                .ToList();
            var expectedNamedEntities = expectedEntities
                .Where(static entity => !string.Equals(entity.Kind, "track-scenery", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(static entity => entity.Name, StringComparer.OrdinalIgnoreCase);
            List<RuntimeSceneEntityRecord> expectedSceneryEntities = expectedEntities
                .Where(static entity => string.Equals(entity.Kind, "track-scenery", StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<RuntimeSceneEntityRecord> liveEntities = liveTrack.Entities
                .Where(static entity => entity.IncludeInDeterministicComparison)
                .ToList();
            var liveNamedEntities = liveEntities
                .Where(static entity => !string.Equals(entity.Kind, "track-scenery", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(static entity => entity.Name, StringComparer.OrdinalIgnoreCase);
            List<RuntimeSceneEntityRecord> liveSceneryEntities = liveEntities
                .Where(static entity => string.Equals(entity.Kind, "track-scenery", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var matchedLiveEntityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var expectedEntityNames = expectedNamedEntities.Keys
                .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase);

            int missingInLive = 0;
            int modelMismatch = 0;
            int visibilityMismatch = 0;
            int transformMismatch = 0;
            var samples = new List<string>();

            foreach (string entityName in expectedEntityNames)
            {
                RuntimeSceneEntityRecord expectedEntity = expectedNamedEntities[entityName];
                if (!liveNamedEntities.TryGetValue(entityName, out RuntimeSceneEntityRecord? liveEntity))
                {
                    missingInLive++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"missing-in-live {entityName} -> {expectedEntity.StaticModelName ?? expectedEntity.Kind}");
                    }

                    continue;
                }

                matchedLiveEntityNames.Add(liveEntity.Name);

                if (!string.Equals(expectedEntity.StaticModelName, liveEntity.StaticModelName, StringComparison.OrdinalIgnoreCase))
                {
                    modelMismatch++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"model-mismatch {entityName} -> expected={expectedEntity.StaticModelName ?? "<none>"}, live={liveEntity.StaticModelName ?? "<none>"}");
                    }
                }

                if (expectedEntity.IsVisible != liveEntity.IsVisible)
                {
                    visibilityMismatch++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"visibility-mismatch {entityName} -> expected={expectedEntity.IsVisible}, live={liveEntity.IsVisible}");
                    }
                }

                float positionDelta = Vector3.Distance(expectedEntity.Position.ToVector3(), liveEntity.Position.ToVector3());
                float rotationDeltaDegrees = QuaternionDeltaDegrees(expectedEntity.Orientation.ToQuaternion(), liveEntity.Orientation.ToQuaternion());
                float scaleDelta = MaxAbsDelta(expectedEntity.Scale.ToVector3(), liveEntity.Scale.ToVector3());
                if (positionDelta > 0.01f || rotationDeltaDegrees > 0.5f || scaleDelta > 0.01f)
                {
                    transformMismatch++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"transform-delta {entityName} -> pos={positionDelta:0.###}, rot={rotationDeltaDegrees:0.###}deg, scale={scaleDelta:0.###}");
                    }
                }
            }

            MatchExpectedSceneryEntities(
                expectedSceneryEntities,
                liveSceneryEntities,
                matchedLiveEntityNames,
                ref missingInLive,
                ref transformMismatch,
                samples);

            List<RuntimeSceneEntityRecord> additionalLiveEntities = liveEntities
                .Where(entity => !matchedLiveEntityNames.Contains(entity.Name))
                .OrderBy(static entity => entity.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            string additionalLiveSummary = additionalLiveEntities.Count == 0
                ? "none"
                : string.Join(", ",
                    additionalLiveEntities
                        .GroupBy(static entity => entity.StaticModelName ?? entity.Kind, StringComparer.OrdinalIgnoreCase)
                        .OrderByDescending(static group => group.Count())
                        .Take(4)
                        .Select(static group => $"{group.Key}={group.Count()}"));

            Console.WriteLine($"{expectedTrack.TrackName}: authoredExpected={expectedEntities.Count}, liveComparable={liveEntities.Count}, missingInLive={missingInLive}, modelMismatch={modelMismatch}, visibilityMismatch={visibilityMismatch}, transformMismatch={transformMismatch}, additionalLiveEntities={additionalLiveEntities.Count} ({additionalLiveSummary})");
            foreach (string sample in samples)
            {
                Console.WriteLine($"  {sample}");
            }
        }
    }

    private static void MatchExpectedSceneryEntities(
        IReadOnlyList<RuntimeSceneEntityRecord> expectedSceneryEntities,
        IReadOnlyList<RuntimeSceneEntityRecord> liveSceneryEntities,
        HashSet<string> matchedLiveEntityNames,
        ref int missingInLive,
        ref int transformMismatch,
        List<string> samples)
    {
        var expectedByModel = expectedSceneryEntities
            .GroupBy(static entity => entity.StaticModelName ?? entity.Kind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var liveByModel = liveSceneryEntities
            .GroupBy(static entity => entity.StaticModelName ?? entity.Kind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach ((string modelName, List<RuntimeSceneEntityRecord> expectedGroup) in expectedByModel.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            List<RuntimeSceneEntityRecord> liveGroup = liveByModel.GetValueOrDefault(modelName, []);
            var candidates = new List<SceneryMatchCandidate>();

            for (int expectedIndex = 0; expectedIndex < expectedGroup.Count; expectedIndex++)
            {
                RuntimeSceneEntityRecord expectedEntity = expectedGroup[expectedIndex];
                for (int liveIndex = 0; liveIndex < liveGroup.Count; liveIndex++)
                {
                    RuntimeSceneEntityRecord liveEntity = liveGroup[liveIndex];
                    float positionDelta = Vector3.Distance(expectedEntity.Position.ToVector3(), liveEntity.Position.ToVector3());
                    float rotationDeltaDegrees = QuaternionDeltaDegrees(expectedEntity.Orientation.ToQuaternion(), liveEntity.Orientation.ToQuaternion());
                    float scaleDelta = MaxAbsDelta(expectedEntity.Scale.ToVector3(), liveEntity.Scale.ToVector3());

                    if (positionDelta > 0.25f || rotationDeltaDegrees > 5.0f || scaleDelta > 0.05f)
                    {
                        continue;
                    }

                    float score = positionDelta + rotationDeltaDegrees * 0.001f + scaleDelta * 0.01f;
                    candidates.Add(new SceneryMatchCandidate(expectedIndex, liveIndex, score, positionDelta, rotationDeltaDegrees, scaleDelta));
                }
            }

            candidates.Sort(static (left, right) => left.Score.CompareTo(right.Score));

            var matchedExpectedIndices = new HashSet<int>();
            var matchedLiveIndices = new HashSet<int>();

            foreach (SceneryMatchCandidate candidate in candidates)
            {
                if (!matchedExpectedIndices.Add(candidate.ExpectedIndex) || !matchedLiveIndices.Add(candidate.LiveIndex))
                {
                    continue;
                }

                RuntimeSceneEntityRecord liveEntity = liveGroup[candidate.LiveIndex];
                matchedLiveEntityNames.Add(liveEntity.Name);

                if (candidate.PositionDelta > 0.01f || candidate.RotationDeltaDegrees > 0.5f || candidate.ScaleDelta > 0.01f)
                {
                    transformMismatch++;
                    if (samples.Count < 6)
                    {
                        RuntimeSceneEntityRecord expectedEntity = expectedGroup[candidate.ExpectedIndex];
                        samples.Add($"transform-delta {expectedEntity.Name} -> pos={candidate.PositionDelta:0.###}, rot={candidate.RotationDeltaDegrees:0.###}deg, scale={candidate.ScaleDelta:0.###}");
                    }
                }
            }

            for (int expectedIndex = 0; expectedIndex < expectedGroup.Count; expectedIndex++)
            {
                if (matchedExpectedIndices.Contains(expectedIndex))
                {
                    continue;
                }

                RuntimeSceneEntityRecord expectedEntity = expectedGroup[expectedIndex];
                missingInLive++;
                if (samples.Count < 6)
                {
                    samples.Add($"missing-in-live {expectedEntity.Name} -> {expectedEntity.StaticModelName ?? expectedEntity.Kind}");
                }
            }
        }
    }

    private static Matrix FromMatrixRows(float[][] rows)
    {
        return new Matrix(
            rows[0][0], rows[0][1], rows[0][2], rows[0][3],
            rows[1][0], rows[1][1], rows[1][2], rows[1][3],
            rows[2][0], rows[2][1], rows[2][2], rows[2][3],
            rows[3][0], rows[3][1], rows[3][2], rows[3][3]);
    }

    private static float MaxAbsDelta(Vector3 left, Vector3 right)
    {
        Vector3 delta = left - right;
        return Math.Max(Math.Abs(delta.X), Math.Max(Math.Abs(delta.Y), Math.Abs(delta.Z)));
    }

    private sealed class RuntimeSceneExpectationState
    {
        public RuntimeSceneExpectationState(
            string runtimeTrackName,
            Vector3 comparisonOrigin,
            TerrainHeightSampler terrain,
            ModelSizeProvider modelSizeProvider,
            CombiLoader combiLoader,
            string modelRoot)
        {
            RuntimeTrackName = runtimeTrackName;
            ComparisonOrigin = comparisonOrigin;
            Terrain = terrain;
            ModelSizeProvider = modelSizeProvider;
            CombiLoader = combiLoader;
            ModelRoot = modelRoot;
        }

        public string RuntimeTrackName { get; }

        public Vector3 ComparisonOrigin { get; }

        public TerrainHeightSampler Terrain { get; }

        public ModelSizeProvider ModelSizeProvider { get; }

        public CombiLoader CombiLoader { get; }

        public string ModelRoot { get; }

        public List<Vector3> CreatedLegacyPositions { get; } = [];

        public List<RuntimeSceneEntityRecord> Entities { get; } = [];

        public int SceneryIndex { get; set; }
    }

    private sealed class RuntimeSceneExportFile
    {
        public string Generator { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; }

        public string Scope { get; set; } = string.Empty;

        public List<RuntimeSceneTrackRecord> Tracks { get; set; } = [];
    }

    private sealed class RuntimeSceneTrackRecord
    {
        public string TrackName { get; set; } = string.Empty;

        public string WorldName { get; set; } = string.Empty;

        public int EntityCount { get; set; }

        public int VisibleEntityCount { get; set; }

        public int ComparisonTargetCount { get; set; }

        public List<RuntimeSceneEntityRecord> Entities { get; set; } = [];
    }

    private sealed class RuntimeSceneEntityRecord
    {
        public string Name { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public bool IsVisible { get; set; }

        public bool IsEnabled { get; set; }

        public bool IncludeInDeterministicComparison { get; set; }

        public string? StaticModelName { get; set; }

        public Float3 Position { get; set; }

        public Float4 Orientation { get; set; }

        public Float3 Scale { get; set; }

        public float[][] WorldMatrix { get; set; } = [];

        public Float3? BoundingBoxMin { get; set; }

        public Float3? BoundingBoxMax { get; set; }

        public Float3? BoundingBoxSize { get; set; }

        public List<RuntimeSceneSubMeshRecord> SubMeshes { get; set; } = [];
    }

    private sealed class RuntimeSceneSubMeshRecord
    {
        public string ComponentName { get; set; } = string.Empty;

        public string? MeshName { get; set; }

        public string? MaterialName { get; set; }

        public string? MaterialType { get; set; }

        public Float3? BoundingBoxMin { get; set; }

        public Float3? BoundingBoxMax { get; set; }

        public Float3? BoundingBoxSize { get; set; }
    }

    private readonly record struct SceneryMatchCandidate(
        int ExpectedIndex,
        int LiveIndex,
        float Score,
        float PositionDelta,
        float RotationDeltaDegrees,
        float ScaleDelta);

    private enum LegacyRoadHelperType
    {
        Tunnel,
        Palms,
        Laterns,
        Reset,
    }

    private sealed record LegacyRoadHelperPosition(LegacyRoadHelperType Type, int StartIndex, int EndIndex);
}