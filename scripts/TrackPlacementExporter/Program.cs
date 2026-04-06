using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Assimp;
using Microsoft.Xna.Framework;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;

public static partial class Program
{
    private const float ComparisonWorldScale = 1.0f;
    private const int NumberOfIterationsPer100Meters = 40;
    private const float CurveFactor = 0.25f;
    private const float UpFactorCorrector = 0.6f;
    private const float RoadTextureStretchFactor = 0.125f;
    private const int NumberOfUpSmoothValues = 10;
    private const float MinimumLandscapeDistance = 2.0f;
    private const float LegacyRoadWidthScale = 13.25f;
    private const float LegacyMinRoadWidth = 0.25f;
    private const float LegacyDefaultRoadWidth = 1.0f;
    private const float LegacyMaxRoadWidth = 2.0f;
    private static readonly Vector3[] LoopingPoints =
    [
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 0.353553f, 0.146447f),
        new Vector3(0f, 0.5f, 0.5f),
        new Vector3(0f, 0.353553f, 1.0f - 0.146447f),
        new Vector3(0f, 0f, 1.0f),
        new Vector3(0f, -0.353553f, 1.0f - 0.146447f),
        new Vector3(0f, -0.5f, 0.5f),
        new Vector3(0f, -0.353553f, 0.146447f),
        new Vector3(0f, 0f, 0f),
    ];

    private static readonly XmlSerializer TrackSerializer = new(typeof(TrackLayout));
    private static readonly XmlSerializer CombiSerializer = new(typeof(List<CombiObject>), new XmlRootAttribute("ArrayOfCombiObject"));

    private static readonly Dictionary<string, string> OriginalAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OilWell"] = "OilPump",
        ["PalmSmall"] = "AlphaPalmSmall",
        ["AlphaPalm4"] = "AlphaPalmSmall",
        ["Palm"] = "AlphaPalm",
        ["Casino"] = "Casino01",
        ["Combi"] = "CombiPalms",
    };

    private static readonly Dictionary<string, string> CasaAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OilWell"] = "OilPump",
        ["PalmSmall"] = "AlphaPalmSmall",
        ["AlphaPalm4"] = "AlphaPalmSmall",
        ["Palm"] = "AlphaPalm",
        ["Casino"] = "Casino01",
        ["Combi"] = "CombiPalms",
    };

    private static readonly HashSet<string> OriginalSupportedModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "StartLight",
        "StartLight2",
        "StartLight3",
        "Blockade",
        "Blockade2",
        "Hydrant",
        "Kaktus",
        "Kaktus2",
        "KaktusBenny",
        "KaktusSeg",
        "AlphaDeadTree",
        "AlphaPalm",
        "AlphaPalm2",
        "AlphaPalm3",
        "AlphaPalmSmall",
        "Laterne",
        "Laterne2Sides",
        "Trashcan",
        "Roadsign",
        "Roadsign2",
        "Goal",
        "Building",
        "Building2",
        "Building3",
        "Building4",
        "Building5",
        "OilPump",
        "OilTanks",
        "RoadColumnSegment",
        "Windmill",
        "Ruin",
        "RuinHouse",
        "SandCastle",
        "Banner",
        "Banner2",
        "Banner3",
        "Banner4",
        "Banner5",
        "Banner6",
        "Sign",
        "Sign2",
        "SignWarning",
        "SignCurveLeft",
        "SignCurveRight",
        "SharpRock",
        "SharpRock2",
        "Stone4",
        "Stone5",
        "AlphaTrain",
        "GuardRailHolder",
        "Hotel01",
        "Hotel02",
        "Casino01",
    };

    private static readonly HashSet<string> OriginalSupportedCombis = new(StringComparer.OrdinalIgnoreCase)
    {
        "CombiPalms",
        "CombiPalms2",
        "CombiRuins",
        "CombiRuins2",
        "CombiStones",
        "CombiStones2",
        "CombiOilTanks",
        "CombiSandCastle",
        "CombiBuildings",
        "CombiHotels",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static int Main(string[] args)
    {
        try
        {
            Options options = Options.Parse(args);
            Run(options);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Run(Options options)
    {
        string repoRoot = Path.GetFullPath(options.RepoRoot ?? Directory.GetCurrentDirectory());
        string contentRoot = Path.Combine(repoRoot, "RacingGame", "Content");
        string modelRoot = Path.Combine(contentRoot, "Models");
        string outputDirectory = Path.GetFullPath(options.OutputDirectory ?? Path.Combine(repoRoot, "artifacts", "track-placement"));
        Directory.CreateDirectory(outputDirectory);

        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Unable to locate RacingGame content directory at '{contentRoot}'.");
        }

        TerrainHeightSampler terrain = TerrainHeightSampler.Load(Path.Combine(contentRoot, "LandscapeHeights.data"));
        using var modelSizeProvider = new ModelSizeProvider(modelRoot);
        var combiLoader = new CombiLoader(contentRoot);

        string[] trackFiles = Directory.GetFiles(contentRoot, "*.Track", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var originalTracks = new List<TrackExport>();
        var casaTracks = new List<TrackExport>();
        var authoredRuntimeTracks = new List<RuntimeSceneTrackRecord>();

        foreach (string trackFile in trackFiles)
        {
            string trackName = Path.GetFileNameWithoutExtension(trackFile);
            TrackLayout layout = LoadTrack(trackFile);
            Vector3 comparisonOrigin = ComputeComparisonOrigin(layout.TrackPoints);

            originalTracks.Add(ExportOriginalTrack(trackName, layout, comparisonOrigin, terrain, modelSizeProvider, combiLoader));
            TrackExport casaTrack = ExportCasaTrack(trackName, layout, comparisonOrigin, terrain, modelSizeProvider, combiLoader, modelRoot);
            casaTracks.Add(casaTrack);
            authoredRuntimeTracks.Add(BuildAuthoredRuntimeTrack(casaTrack));
        }

        var originalFile = new ExportFile
        {
            Generator = "TrackPlacementExporter",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Scope = "Deterministic authored scenery placements coming from .Track neutral objects and .CombiModel expansions only. This excludes runtime/procedural objects generated by Track.cs and GuardRail.cs such as random decoration, checkpoints, banners, guardrail holders and other helper-driven track objects.",
            ComparisonSpace = "CasaEngine runtime space: legacy (X,Y,Z) is mapped to (X,Z,-Y), kept in legacy world units (scale 1.0), and centered using the track bounds midpoint.",
            Tracks = originalTracks,
        };

        var casaFile = new ExportFile
        {
            Generator = "TrackPlacementExporter",
            GeneratedAtUtc = originalFile.GeneratedAtUtc,
            Scope = originalFile.Scope,
            ComparisonSpace = originalFile.ComparisonSpace,
            Tracks = casaTracks,
        };

        string originalOutputPath = Path.Combine(outputDirectory, "racinggame-placements.json");
        string casaOutputPath = Path.Combine(outputDirectory, "racinggame-casaengine-placements.json");
        string expectedRuntimeOutputPath = Path.Combine(outputDirectory, "racinggame-casaengine-authored-runtime-scene.json");
        WriteJson(originalOutputPath, originalFile);
        WriteJson(casaOutputPath, casaFile);
        WriteJson(expectedRuntimeOutputPath, new RuntimeSceneExportFile
        {
            Generator = "TrackPlacementExporter",
            GeneratedAtUtc = originalFile.GeneratedAtUtc,
            Scope = "Deterministic authored-runtime CasaEngine scene baseline built from exported authored placements plus runtime track, ground, checkpoint, and player-start transforms. Helper-driven runtime entities are reported separately during comparison.",
            Tracks = authoredRuntimeTracks,
        });

        Console.WriteLine($"Generated: {originalOutputPath}");
        Console.WriteLine($"Generated: {casaOutputPath}");
        Console.WriteLine($"Generated: {expectedRuntimeOutputPath}");
        Console.WriteLine();
        PrintComparisonSummary(originalTracks, casaTracks);

        if (!string.IsNullOrWhiteSpace(options.CompareLiveRuntimeScenePath))
        {
            Console.WriteLine();
            RuntimeSceneExportFile liveRuntimeFile = LoadRuntimeSceneExport(options.CompareLiveRuntimeScenePath);
            PrintRuntimeSceneComparisonSummary(authoredRuntimeTracks, liveRuntimeFile.Tracks);
        }
    }

    private static TrackExport ExportOriginalTrack(
        string trackName,
        TrackLayout layout,
        Vector3 comparisonOrigin,
        TerrainHeightSampler terrain,
        ModelSizeProvider modelSizeProvider,
        CombiLoader combiLoader)
    {
        var state = new OriginalExportState(trackName, comparisonOrigin, terrain, modelSizeProvider, combiLoader);
        TrackSplineComputation trackSpline = BuildTrackSplinePoints(layout.TrackPoints, layout.WidthHelpers, terrain);
        if (trackSpline.Points.Count > 0)
        {
            state.SetStartPose(CreateTrackStartPose(trackSpline.Points[0], comparisonOrigin));
            state.SetRoadGeometry(
                BuildRoadSampleRecords(trackSpline.Points, comparisonOrigin),
                BuildCheckpointRecords(trackSpline.Points, comparisonOrigin),
                trackSpline.LoopInsertionsCount);
        }

        for (int i = 0; i < layout.NeutralsObjects.Count; i++)
        {
            NeutralObject neutralObject = layout.NeutralsObjects[i];
            string sourcePath = $"neutral[{i:000}]";
            ExpandOriginal(state, neutralObject.modelName, neutralObject.matrix, sourcePath, [neutralObject.modelName], []);
        }

        return state.ToExport();
    }

    private static TrackExport ExportCasaTrack(
        string trackName,
        TrackLayout layout,
        Vector3 comparisonOrigin,
        TerrainHeightSampler terrain,
        ModelSizeProvider modelSizeProvider,
        CombiLoader combiLoader,
        string modelRoot)
    {
        var state = new CasaExportState(trackName, comparisonOrigin, terrain, modelSizeProvider, combiLoader, modelRoot);
        TrackSplineComputation trackSpline = BuildTrackSplinePoints(layout.TrackPoints, layout.WidthHelpers, terrain);
        if (trackSpline.Points.Count > 0)
        {
            state.SetStartPose(CreateTrackStartPose(trackSpline.Points[0], comparisonOrigin));
            state.SetRoadGeometry(
                BuildRoadSampleRecords(trackSpline.Points, comparisonOrigin),
                BuildCheckpointRecords(trackSpline.Points, comparisonOrigin),
                trackSpline.LoopInsertionsCount);
        }

        for (int i = 0; i < layout.NeutralsObjects.Count; i++)
        {
            NeutralObject neutralObject = layout.NeutralsObjects[i];
            string sourcePath = $"neutral[{i:000}]";
            ExpandCasa(state, neutralObject.modelName, neutralObject.matrix, sourcePath, [neutralObject.modelName], []);
        }

        return state.ToExport();
    }

    private static void ExpandOriginal(
        OriginalExportState state,
        string rawModelName,
        Matrix sourceMatrix,
        string sourcePath,
        List<string> lineage,
        List<string> inheritedNotes)
    {
        var notes = new List<string>(inheritedNotes);
        string resolvedModelName = ApplyAlias(rawModelName, OriginalAliases, notes);

        if (OriginalSupportedCombis.Contains(resolvedModelName))
        {
            if (!state.CombiLoader.TryLoadCombi(resolvedModelName, out IReadOnlyList<CombiObject> combiObjects))
            {
                state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "missing-combi", notes, sourceMatrix);
                return;
            }

            for (int i = 0; i < combiObjects.Count; i++)
            {
                CombiObject child = combiObjects[i];
                string childPath = $"{sourcePath}/child[{i:000}]";
                var childLineage = new List<string>(lineage) { child.modelName };
                ExpandOriginal(state, child.modelName, child.matrix * sourceMatrix, childPath, childLineage, notes);
            }

            return;
        }

        if (!OriginalSupportedModels.Contains(resolvedModelName))
        {
            state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "unsupported-model", notes, sourceMatrix);
            return;
        }

        Matrix adjustedMatrix = sourceMatrix;
        Vector3 legacyPosition = adjustedMatrix.Translation;
        float terrainHeight = state.Terrain.GetMapHeight(legacyPosition.X, legacyPosition.Y);
        if (legacyPosition.Z < terrainHeight)
        {
            legacyPosition.Z = terrainHeight;
            adjustedMatrix.Translation = legacyPosition;
            notes.Add("terrain-clamped");
        }

        if (!IsDuplicateExempt(resolvedModelName))
        {
            float modelSize = state.ModelSizeProvider.GetModelSize(resolvedModelName);
            float threshold = modelSize * modelSize / 4f;

            for (int i = 0; i < state.CreatedLegacyPositions.Count; i++)
            {
                if (Vector3.DistanceSquared(state.CreatedLegacyPositions[i], legacyPosition) < threshold)
                {
                    state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "duplicate-filtered", notes, adjustedMatrix);
                    return;
                }
            }
        }

        notes.Add("pipeline-scale:1.2");
        Matrix finalLegacyMatrix = Matrix.CreateScale(1.2f) * adjustedMatrix;
        Matrix comparisonMatrix = ConvertLegacyTransform(finalLegacyMatrix, state.ComparisonOrigin);
        state.AddPlacement(sourcePath, lineage, rawModelName, resolvedModelName, notes, sourceMatrix, comparisonMatrix);
        state.CreatedLegacyPositions.Add(legacyPosition);
    }

    private static void ExpandCasa(
        CasaExportState state,
        string rawModelName,
        Matrix sourceMatrix,
        string sourcePath,
        List<string> lineage,
        List<string> inheritedNotes)
    {
        if (string.IsNullOrWhiteSpace(rawModelName))
        {
            state.AddSkip(sourcePath, lineage, rawModelName, rawModelName, "empty-model-name", inheritedNotes, sourceMatrix);
            return;
        }

        var notes = new List<string>(inheritedNotes);
        string resolvedModelName = ApplyAlias(rawModelName, CasaAliases, notes);

        if (resolvedModelName.StartsWith("Track", StringComparison.OrdinalIgnoreCase))
        {
            state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "ignored-track-marker", inheritedNotes, sourceMatrix);
            return;
        }

        if (resolvedModelName.StartsWith("Combi", StringComparison.OrdinalIgnoreCase))
        {
            if (!state.CombiLoader.TryLoadCombi(resolvedModelName, out IReadOnlyList<CombiObject> combiObjects))
            {
                state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "missing-combi", notes, sourceMatrix);
                return;
            }

            for (int i = 0; i < combiObjects.Count; i++)
            {
                CombiObject child = combiObjects[i];
                string childPath = $"{sourcePath}/child[{i:000}]";
                var childLineage = new List<string>(lineage) { child.modelName };
                ExpandCasa(state, child.modelName, child.matrix * sourceMatrix, childPath, childLineage, notes);
            }

            return;
        }

        string modelPath = Path.Combine(state.ModelRoot, resolvedModelName + ".X");
        if (!File.Exists(modelPath))
        {
            state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "missing-model", notes, sourceMatrix);
            return;
        }

        Matrix adjustedMatrix = sourceMatrix;
        Vector3 legacyPosition = adjustedMatrix.Translation;
        float terrainHeight = state.Terrain.GetMapHeight(legacyPosition.X, legacyPosition.Y);
        if (legacyPosition.Z < terrainHeight)
        {
            legacyPosition.Z = terrainHeight;
            adjustedMatrix.Translation = legacyPosition;
            notes.Add("terrain-clamped");
        }

        if (!IsDuplicateExempt(resolvedModelName))
        {
            float modelSize = state.ModelSizeProvider.GetModelSize(resolvedModelName);
            float threshold = modelSize * modelSize / 4f;

            for (int i = 0; i < state.CreatedLegacyPositions.Count; i++)
            {
                if (Vector3.DistanceSquared(state.CreatedLegacyPositions[i], legacyPosition) < threshold)
                {
                    state.AddSkip(sourcePath, lineage, rawModelName, resolvedModelName, "duplicate-filtered", notes, adjustedMatrix);
                    return;
                }
            }
        }

        notes.Add("pipeline-scale:1.2");
        Matrix comparisonMatrix = ConvertLegacyTransform(Matrix.CreateScale(1.2f) * adjustedMatrix, state.ComparisonOrigin);
        state.AddPlacement(sourcePath, lineage, rawModelName, resolvedModelName, notes, sourceMatrix, comparisonMatrix);
        state.CreatedLegacyPositions.Add(legacyPosition);
    }

    private static string ApplyAlias(string rawModelName, IReadOnlyDictionary<string, string> aliases, List<string> notes)
    {
        if (aliases.TryGetValue(rawModelName, out string? alias))
        {
            notes.Add($"alias:{rawModelName}->{alias}");
            return alias;
        }

        return rawModelName;
    }

    private static bool IsDuplicateExempt(string modelName)
    {
        return modelName.StartsWith("Banner", StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith("Sign", StringComparison.OrdinalIgnoreCase)
            || modelName.StartsWith("StartLight", StringComparison.OrdinalIgnoreCase);
    }

    private static TrackLayout LoadTrack(string trackFilePath)
    {
        using var stream = File.OpenRead(trackFilePath);
        return (TrackLayout)(TrackSerializer.Deserialize(stream)
            ?? throw new InvalidOperationException($"Unable to deserialize track file '{trackFilePath}'."));
    }

    private static Matrix ConvertLegacyTransform(Matrix legacyTransform, Vector3 comparisonOrigin)
    {
        Matrix comparisonMatrix = Matrix.Identity;
        comparisonMatrix.Right = ConvertLegacyVector(legacyTransform.Right);
        comparisonMatrix.Up = ConvertLegacyVector(legacyTransform.Up);
        comparisonMatrix.Forward = ConvertLegacyVector(legacyTransform.Forward);
        comparisonMatrix.Translation = ConvertLegacyPoint(legacyTransform.Translation) - comparisonOrigin;
        return comparisonMatrix;
    }

    private static Vector3 ComputeComparisonOrigin(IReadOnlyList<Vector3> trackPoints)
    {
        float minX = trackPoints.Min(static point => point.X);
        float maxX = trackPoints.Max(static point => point.X);
        float minY = trackPoints.Min(static point => point.Y);
        float maxY = trackPoints.Max(static point => point.Y);
        float minZ = trackPoints.Min(static point => point.Z);
        float maxZ = trackPoints.Max(static point => point.Z);

        Vector3 center = new(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            (minZ + maxZ) * 0.5f);

        return ConvertLegacyPoint(center);
    }

    private static Vector3 ConvertLegacyPoint(Vector3 point)
    {
        return new Vector3(point.X, point.Z, -point.Y) * ComparisonWorldScale;
    }

    private static Vector3 ConvertLegacyVector(Vector3 vector)
    {
        return new Vector3(vector.X, vector.Z, -vector.Y) * ComparisonWorldScale;
    }

    private static Vector3 ConvertLegacyDirection(Vector3 direction)
    {
        Vector3 comparisonDirection = ConvertLegacyVector(direction);
        if (comparisonDirection.LengthSquared() < 0.0001f)
        {
            return Vector3.Up;
        }

        comparisonDirection.Normalize();
        return comparisonDirection;
    }

    private static TrackSplineComputation BuildTrackSplinePoints(IReadOnlyList<Vector3> rawTrackPoints, IReadOnlyList<WidthHelper> widthHelpers, TerrainHeightSampler terrain)
    {
        Vector3[] inputPoints = rawTrackPoints.ToArray();
        EnsureTrackPointsStayAboveLandscape(inputPoints, terrain);
        inputPoints = InsertLoopingSegments(inputPoints);
        int loopInsertionsCount = Math.Max(0, (inputPoints.Length - rawTrackPoints.Count) / 7);

        var points = new List<ComparisonTrackPoint>();
        for (int index = 0; index < inputPoints.Length; index++)
        {
            Vector3 p1 = inputPoints[index - 1 < 0 ? inputPoints.Length - 1 : index - 1];
            Vector3 p2 = inputPoints[index];
            Vector3 p3 = inputPoints[(index + 1) % inputPoints.Length];
            Vector3 p4 = inputPoints[(index + 2) % inputPoints.Length];

            float distance = Vector3.Distance(p2, p3);
            int numberOfIterations = (int)(NumberOfIterationsPer100Meters * (distance / 100.0f));
            if (numberOfIterations <= 0)
            {
                numberOfIterations = 1;
            }

            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                points.Add(new ComparisonTrackPoint(Vector3.CatmullRom(p1, p2, p3, p4, iteration / (float)numberOfIterations)));
            }
        }

        if (points.Count == 0)
        {
            return new TrackSplineComputation(points, loopInsertionsCount);
        }

        GenerateOrientationVectors(points, terrain);
        AdjustRoadWidths(points, widthHelpers);
        GenerateRoadTextureCoordinates(points);
        return new TrackSplineComputation(points, loopInsertionsCount);
    }

    private static void AdjustRoadWidths(List<ComparisonTrackPoint> points, IReadOnlyList<WidthHelper> widthHelpers)
    {
        float currentWidth = LegacyDefaultRoadWidth;
        float widthInfluence = currentWidth;
        for (int index = 0; index < points.Count; index++)
        {
            Vector3 position = points[index].Position;
            foreach (WidthHelper widthHelper in widthHelpers)
            {
                float distance = Vector3.Distance(widthHelper.pos, position);
                if (distance < 25.0f)
                {
                    float influence = 1 - (distance / 25.0f);
                    widthInfluence = (1 - influence) * widthInfluence + influence * widthHelper.scale;
                }
            }

            currentWidth = currentWidth * 0.9f + widthInfluence * 0.1f;
            if (index > points.Count - 7)
            {
                float influence =
                    index == points.Count - 1 ? 0.75f :
                    index == points.Count - 2 ? 0.5f :
                    index == points.Count - 3 ? 0.25f : 0.175f;
                currentWidth = influence * points[0].RoadWidth + (1 - influence) * currentWidth;
            }

            currentWidth = Math.Clamp(currentWidth, LegacyMinRoadWidth, LegacyMaxRoadWidth);
            points[index].RoadWidth = currentWidth;
        }
    }

    private static void GenerateRoadTextureCoordinates(List<ComparisonTrackPoint> points)
    {
        float currentRoadTextureU = 0.0f;
        for (int index = 0; index < points.Count; index++)
        {
            points[index].TextureU = currentRoadTextureU;
            currentRoadTextureU += RoadTextureStretchFactor * (points[(index + 1) % points.Count].Position - points[index % points.Count].Position).Length();
        }

        points.Add(new ComparisonTrackPoint(points[0])
        {
            Right = points[0].Right,
            Up = points[0].Up,
            Direction = points[0].Direction,
            RoadWidth = points[0].RoadWidth,
            TextureU = currentRoadTextureU,
        });
    }

    private static void EnsureTrackPointsStayAboveLandscape(Vector3[] inputPoints, TerrainHeightSampler terrain)
    {
        for (int index = 0; index < inputPoints.Length; index++)
        {
            float landscapeHeight = terrain.GetMapHeight(inputPoints[index].X, inputPoints[index].Y) + MinimumLandscapeDistance * 2.25f;
            if (inputPoints[index].Z < landscapeHeight)
            {
                inputPoints[index].Z = landscapeHeight;
            }
        }

        for (int index = 0; index < inputPoints.Length; index++)
        {
            for (int iteration = 1; iteration < 25; iteration++)
            {
                float iterationPercent = iteration / 25.0f;
                float interpolatedHeight = inputPoints[index].Z * (1 - iterationPercent)
                    + inputPoints[(index + 1) % inputPoints.Length].Z * iterationPercent;

                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        float landscapeHeight = terrain.GetMapHeight(
                                -5.0f + 10.0f * x + inputPoints[index].X * (1 - iterationPercent) + inputPoints[(index + 1) % inputPoints.Length].X * iterationPercent,
                                -5.0f + 10.0f * y + inputPoints[index].Y * (1 - iterationPercent) + inputPoints[(index + 1) % inputPoints.Length].Y * iterationPercent)
                            + MinimumLandscapeDistance * 1.6f;

                        if (interpolatedHeight < landscapeHeight)
                        {
                            float increaseHeight = landscapeHeight - interpolatedHeight;
                            inputPoints[index].Z += increaseHeight;
                            inputPoints[(index + 1) % inputPoints.Length].Z += increaseHeight;
                        }
                    }
                }
            }
        }
    }

    private static Vector3[] InsertLoopingSegments(Vector3[] inputPoints)
    {
        for (int index = 1; index < inputPoints.Length - 3; index++)
        {
            Vector3 distanceVector = inputPoints[index + 1] - inputPoints[index];
            float xyDistance = MathF.Sqrt(distanceVector.X * distanceVector.X + distanceVector.Y * distanceVector.Y);
            float zDistance = Math.Abs(distanceVector.Z);
            Vector3 nextDistanceVector = inputPoints[index + 2] - inputPoints[index + 1];
            if (zDistance / 2 <= xyDistance || Math.Abs(distanceVector.Z + nextDistanceVector.Z) >= zDistance / 2)
            {
                continue;
            }

            Vector3 direction = inputPoints[index] - inputPoints[index - 1];
            direction.Normalize();
            Vector3 upVector = new(0f, 0f, 1f);
            Vector3 rightVector = Vector3.Cross(direction, upVector);
            Matrix rotationMatrix = new(
                rightVector.X, rightVector.Y, rightVector.Z, 0f,
                direction.X, direction.Y, direction.Z, 0f,
                upVector.X, upVector.Y, upVector.Z, 0f,
                0f, 0f, 0f, 1f);

            Vector3 startLoopPosition = inputPoints[index];
            Vector3 endLoopPosition = inputPoints[index + 2];
            Vector3[] previousPoints = (Vector3[])inputPoints.Clone();
            inputPoints = new Vector3[inputPoints.Length + 7];
            for (int copyIndex = 0; copyIndex < previousPoints.Length; copyIndex++)
            {
                inputPoints[copyIndex < index ? copyIndex : copyIndex + 7] = previousPoints[copyIndex];
            }

            for (int loopIndex = 0; loopIndex < LoopingPoints.Length; loopIndex++)
            {
                float loopPercent = loopIndex / (float)(LoopingPoints.Length - 1);
                inputPoints[index + loopIndex] = startLoopPosition * (1 - loopPercent)
                    + endLoopPosition * loopPercent
                    + zDistance * Vector3.Transform(LoopingPoints[loopIndex], rotationMatrix);
            }

            Vector3 newRoadDirection = inputPoints[index + 10] - inputPoints[index + 8];
            if (newRoadDirection.Length() > zDistance * 2)
            {
                newRoadDirection.Normalize();
                newRoadDirection *= zDistance;
                inputPoints[index + 9] = inputPoints[index + 8] + newRoadDirection;
            }
            else
            {
                inputPoints[index + 9] = (inputPoints[index + 8] + inputPoints[index + 10]) / 2.0f;
            }

            index += 10;
        }

        return inputPoints;
    }

    private static void GenerateOrientationVectors(List<ComparisonTrackPoint> points, TerrainHeightSampler terrain)
    {
        var preUpVectors = new List<Vector3>(points.Count);
        Vector3 defaultUpVector = new(0f, 0f, 1f);
        Vector3 lastUpVector = defaultUpVector;

        for (int index = 0; index < points.Count; index++)
        {
            Vector3 direction = points[(index + 1) % points.Count].Position - points[index - 1 < 0 ? points.Count - 1 : index - 1].Position;
            direction.Normalize();

            Vector3 middlePoint = (points[(index + 1) % points.Count].Position + points[index - 1 < 0 ? points.Count - 1 : index - 1].Position) / 2.0f;
            Vector3 optimalUpVector = middlePoint - points[index].Position;
            if (optimalUpVector.Length() < 0.0001f)
            {
                optimalUpVector = lastUpVector;
            }

            optimalUpVector.Normalize();
            preUpVectors.Add(optimalUpVector);
            points[index].Direction = direction;
            lastUpVector = optimalUpVector;
        }

        preUpVectors[0] = preUpVectors[preUpVectors.Count - 1] + preUpVectors[1];
        preUpVectors[0].Normalize();

        lastUpVector = Vector3.Lerp(defaultUpVector, preUpVectors[0], 1.5f * CurveFactor * UpFactorCorrector);
        Vector3 lastUnmodifiedUpVector = lastUpVector;
        for (int index = 0; index < points.Count; index++)
        {
            Vector3 direction = points[index].Direction;
            Vector3 upVector = Vector3.Zero;
            for (int smoothIndex = -NumberOfUpSmoothValues / 2; smoothIndex <= NumberOfUpSmoothValues / 2; smoothIndex++)
            {
                upVector += preUpVectors[(index + points.Count + smoothIndex) % points.Count];
            }

            upVector.Normalize();

            bool upsideDown = upVector.Z < -0.25f && lastUnmodifiedUpVector.Z < -0.05f;
            bool movingUp = direction.Z > 0.75f;
            bool movingDown = direction.Z < -0.75f;

            upVector = Vector3.Lerp(lastUpVector, upVector, CurveFactor);
            upVector.Normalize();
            lastUnmodifiedUpVector = upVector;

            if (movingUp)
            {
                lastUpVector = Vector3.Lerp(upVector, -defaultUpVector, UpFactorCorrector);
            }
            else if (movingDown)
            {
                lastUpVector = Vector3.Lerp(upVector, defaultUpVector, UpFactorCorrector);
            }
            else if (upsideDown)
            {
                lastUpVector = Vector3.Lerp(upVector, -defaultUpVector, UpFactorCorrector);
            }
            else
            {
                lastUpVector = Vector3.Lerp(upVector, defaultUpVector, UpFactorCorrector);
            }

            float landscapeHeight = terrain.GetMapHeight(points[index].Position.X, points[index].Position.Y);
            if (points[index].Position.Z - landscapeHeight < MinimumLandscapeDistance * 4)
            {
                lastUpVector = Vector3.Lerp(upVector, defaultUpVector, 1.75f * UpFactorCorrector);
            }

            Vector3 rightVector = Vector3.Cross(direction, upVector);
            rightVector.Normalize();
            points[index].Right = rightVector;

            upVector = Vector3.Cross(rightVector, direction);
            upVector.Normalize();
            points[index].Up = upVector;
        }

        for (int index = 0; index < points.Count; index++)
        {
            preUpVectors[index] = points[index].Up;
        }

        for (int index = 0; index < points.Count; index++)
        {
            Vector3 upVector = Vector3.Zero;
            for (int smoothIndex = -NumberOfUpSmoothValues; smoothIndex <= NumberOfUpSmoothValues; smoothIndex++)
            {
                upVector += preUpVectors[(index + points.Count + smoothIndex) % points.Count];
            }

            upVector.Normalize();
            points[index].Up = upVector;

            Vector3 direction = points[index].Direction;
            points[index].Right = Vector3.Cross(direction, upVector);
            if (points[index].Right.LengthSquared() < 0.0001f)
            {
                points[index].Right = Vector3.Right;
            }
            else
            {
                points[index].Right.Normalize();
            }
        }
    }

    private static TrackStartPoseRecord CreateTrackStartPose(ComparisonTrackPoint point, Vector3 comparisonOrigin)
    {
        Vector3 comparisonPosition = ConvertLegacyPoint(point.Position) - comparisonOrigin;
        Vector3 comparisonForward = ConvertLegacyDirection(point.Direction);
        Vector3 comparisonUp = ConvertLegacyDirection(point.Up);
        Vector3 comparisonRight = ConvertLegacyDirection(point.Right);

        if (comparisonRight.LengthSquared() < 0.0001f)
        {
            comparisonRight = Vector3.Cross(comparisonForward, comparisonUp);
        }

        if (comparisonRight.LengthSquared() < 0.0001f)
        {
            comparisonRight = Vector3.Right;
        }

        comparisonRight.Normalize();
        comparisonUp = Vector3.Cross(comparisonRight, comparisonForward);
        if (comparisonUp.LengthSquared() < 0.0001f)
        {
            comparisonUp = Vector3.Up;
        }
        else
        {
            comparisonUp.Normalize();
        }

        comparisonRight = Vector3.Cross(comparisonForward, comparisonUp);
        if (comparisonRight.LengthSquared() < 0.0001f)
        {
            comparisonRight = Vector3.Right;
        }
        else
        {
            comparisonRight.Normalize();
        }

        Matrix rotation = Matrix.Identity;
        rotation.Right = comparisonRight;
        rotation.Up = comparisonUp;
        rotation.Forward = comparisonForward;
        XnaQuaternion orientation = XnaQuaternion.CreateFromRotationMatrix(rotation);
        if (orientation.LengthSquared() > 0.000001f)
        {
            orientation.Normalize();
        }
        else
        {
            orientation = XnaQuaternion.Identity;
        }

        return new TrackStartPoseRecord
        {
            Position = Float3.FromVector3(comparisonPosition),
            Orientation = Float4.FromQuaternion(orientation),
            Forward = Float3.FromVector3(comparisonForward),
            Up = Float3.FromVector3(comparisonUp),
        };
    }

    private static List<TrackRoadSampleRecord> BuildRoadSampleRecords(IReadOnlyList<ComparisonTrackPoint> points, Vector3 comparisonOrigin)
    {
        var samples = new List<TrackRoadSampleRecord>(points.Count);
        for (int index = 0; index < points.Count; index++)
        {
            ComparisonTrackPoint point = points[index];
            samples.Add(new TrackRoadSampleRecord
            {
                SampleIndex = index,
                Position = Float3.FromVector3(ConvertLegacyPoint(point.Position) - comparisonOrigin),
                Forward = Float3.FromVector3(ConvertLegacyDirection(point.Direction)),
                Up = Float3.FromVector3(ConvertLegacyDirection(point.Up)),
                Right = Float3.FromVector3(ConvertLegacyDirection(point.Right)),
                RoadWidth = Round(point.RoadWidth * LegacyRoadWidthScale * ComparisonWorldScale),
                TextureU = Round(point.TextureU),
            });
        }

        return samples;
    }

    private static List<Float3> BuildCheckpointRecords(IReadOnlyList<ComparisonTrackPoint> points, Vector3 comparisonOrigin)
    {
        List<Vector3> roadPoints = points.Select(point => ConvertLegacyPoint(point.Position) - comparisonOrigin).ToList();
        return
        [
            Float3.FromVector3(SampleLoopPoint(roadPoints, 0.20f)),
            Float3.FromVector3(SampleLoopPoint(roadPoints, 0.50f)),
            Float3.FromVector3(SampleLoopPoint(roadPoints, 0.80f)),
        ];
    }

    private static Vector3 SampleLoopPoint(IReadOnlyList<Vector3> roadPoints, float progress)
    {
        int index = (int)MathF.Round(progress * (roadPoints.Count - 1)) % roadPoints.Count;
        return roadPoints[Math.Clamp(index, 0, roadPoints.Count - 1)];
    }

    private static void WriteJson<T>(string outputPath, T exportFile)
    {
        string json = JsonSerializer.Serialize(exportFile, JsonOptions);
        File.WriteAllText(outputPath, json);
    }

    private static void PrintComparisonSummary(IReadOnlyList<TrackExport> originalTracks, IReadOnlyList<TrackExport> casaTracks)
    {
        var casaByTrack = casaTracks.ToDictionary(static track => track.TrackName, StringComparer.OrdinalIgnoreCase);

        foreach (TrackExport originalTrack in originalTracks)
        {
            if (!casaByTrack.TryGetValue(originalTrack.TrackName, out TrackExport? casaTrack))
            {
                Console.WriteLine($"{originalTrack.TrackName}: no CasaEngine export available.");
                continue;
            }

            var originalByPath = originalTrack.Placements.ToDictionary(static placement => placement.SourcePath, StringComparer.OrdinalIgnoreCase);
            var casaByPath = casaTrack.Placements.ToDictionary(static placement => placement.SourcePath, StringComparer.OrdinalIgnoreCase);
            var allPaths = originalByPath.Keys.Union(casaByPath.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(static path => path, StringComparer.OrdinalIgnoreCase);

            int missingInCasa = 0;
            int missingInOriginal = 0;
            int nameMismatch = 0;
            int transformMismatch = 0;
            var samples = new List<string>();
            float? startPositionDelta = null;
            float? startRotationDeltaDegrees = null;
            float checkpointMaxDelta = 0.0f;
            float roadPositionMaxDelta = 0.0f;
            float roadForwardMaxDeltaDegrees = 0.0f;
            float roadUpMaxDeltaDegrees = 0.0f;
            float roadWidthMaxDelta = 0.0f;
            float roadTextureUMaxDelta = 0.0f;
            int roadSampleCountDelta = Math.Abs(originalTrack.RoadSamples.Count - casaTrack.RoadSamples.Count);
            int loopInsertionsDelta = Math.Abs(originalTrack.LoopInsertionsCount - casaTrack.LoopInsertionsCount);

            if (originalTrack.StartPose != null && casaTrack.StartPose != null)
            {
                startPositionDelta = Vector3.Distance(originalTrack.StartPose.Position.ToVector3(), casaTrack.StartPose.Position.ToVector3());
                startRotationDeltaDegrees = QuaternionDeltaDegrees(originalTrack.StartPose.Orientation.ToQuaternion(), casaTrack.StartPose.Orientation.ToQuaternion());
            }

            int checkpointCount = Math.Min(originalTrack.CheckpointPositions.Count, casaTrack.CheckpointPositions.Count);
            for (int checkpointIndex = 0; checkpointIndex < checkpointCount; checkpointIndex++)
            {
                checkpointMaxDelta = Math.Max(
                    checkpointMaxDelta,
                    Vector3.Distance(
                        originalTrack.CheckpointPositions[checkpointIndex].ToVector3(),
                        casaTrack.CheckpointPositions[checkpointIndex].ToVector3()));
            }

            int roadSampleCount = Math.Min(originalTrack.RoadSamples.Count, casaTrack.RoadSamples.Count);
            for (int sampleIndex = 0; sampleIndex < roadSampleCount; sampleIndex++)
            {
                TrackRoadSampleRecord originalRoadSample = originalTrack.RoadSamples[sampleIndex];
                TrackRoadSampleRecord casaRoadSample = casaTrack.RoadSamples[sampleIndex];
                roadPositionMaxDelta = Math.Max(roadPositionMaxDelta, Vector3.Distance(originalRoadSample.Position.ToVector3(), casaRoadSample.Position.ToVector3()));
                roadForwardMaxDeltaDegrees = Math.Max(roadForwardMaxDeltaDegrees, VectorAngleDeltaDegrees(originalRoadSample.Forward.ToVector3(), casaRoadSample.Forward.ToVector3()));
                roadUpMaxDeltaDegrees = Math.Max(roadUpMaxDeltaDegrees, VectorAngleDeltaDegrees(originalRoadSample.Up.ToVector3(), casaRoadSample.Up.ToVector3()));
                roadWidthMaxDelta = Math.Max(roadWidthMaxDelta, Math.Abs(originalRoadSample.RoadWidth - casaRoadSample.RoadWidth));
                roadTextureUMaxDelta = Math.Max(roadTextureUMaxDelta, Math.Abs(originalRoadSample.TextureU - casaRoadSample.TextureU));
            }

            foreach (string path in allPaths)
            {
                bool hasOriginal = originalByPath.TryGetValue(path, out PlacementRecord? originalPlacement);
                bool hasCasa = casaByPath.TryGetValue(path, out PlacementRecord? casaPlacement);

                if (!hasOriginal)
                {
                    missingInOriginal++;
                    if (samples.Count < 6 && casaPlacement != null)
                    {
                        samples.Add($"only-in-casa {path} -> {casaPlacement.ResolvedModelName}");
                    }

                    continue;
                }

                if (!hasCasa)
                {
                    missingInCasa++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"missing-in-casa {path} -> {originalPlacement!.ResolvedModelName}");
                    }

                    continue;
                }

                if (!string.Equals(originalPlacement!.ResolvedModelName, casaPlacement!.ResolvedModelName, StringComparison.OrdinalIgnoreCase))
                {
                    nameMismatch++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"name-mismatch {path} -> legacy={originalPlacement.ResolvedModelName}, casa={casaPlacement.ResolvedModelName}");
                    }
                }

                float positionDelta = Vector3.Distance(originalPlacement.Position.ToVector3(), casaPlacement.Position.ToVector3());
                float rotationDeltaDegrees = QuaternionDeltaDegrees(originalPlacement.Orientation.ToQuaternion(), casaPlacement.Orientation.ToQuaternion());
                if (positionDelta > 0.01f || rotationDeltaDegrees > 0.5f)
                {
                    transformMismatch++;
                    if (samples.Count < 6)
                    {
                        samples.Add($"transform-delta {path} -> pos={positionDelta:0.###}, rot={rotationDeltaDegrees:0.###}deg");
                    }
                }
            }

            string startPoseSummary = startPositionDelta.HasValue && startRotationDeltaDegrees.HasValue
                ? $", startPosDelta={startPositionDelta.Value:0.###}, startRotDelta={startRotationDeltaDegrees.Value:0.###}deg"
                : ", startPose=n/a";
            Console.WriteLine($"{originalTrack.TrackName}: legacy={originalTrack.PlacementCount}, casa={casaTrack.PlacementCount}, missingInCasa={missingInCasa}, missingInLegacy={missingInOriginal}, nameMismatch={nameMismatch}, transformMismatch={transformMismatch}{startPoseSummary}, checkpointMaxDelta={checkpointMaxDelta:0.###}, roadPosMaxDelta={roadPositionMaxDelta:0.###}, roadForwardMaxDelta={roadForwardMaxDeltaDegrees:0.###}deg, roadUpMaxDelta={roadUpMaxDeltaDegrees:0.###}deg, roadWidthMaxDelta={roadWidthMaxDelta:0.###}, roadTextureUMaxDelta={roadTextureUMaxDelta:0.###}, roadSampleCountDelta={roadSampleCountDelta}, loopInsertionsDelta={loopInsertionsDelta}");

            if (casaTrack.Skipped.Count > 0)
            {
                string skipSummary = string.Join(", ",
                    casaTrack.Skipped
                        .GroupBy(static skip => skip.Reason, StringComparer.OrdinalIgnoreCase)
                        .OrderByDescending(static group => group.Count())
                        .Select(static group => $"{group.Key}={group.Count()}"));
                Console.WriteLine($"  Casa skips: {skipSummary}");
            }

            if (samples.Count > 0)
            {
                foreach (string sample in samples)
                {
                    Console.WriteLine($"  {sample}");
                }
            }
        }
    }

    private static float QuaternionDeltaDegrees(XnaQuaternion first, XnaQuaternion second)
    {
        if (Math.Abs(first.X - second.X) <= 0.000001f
            && Math.Abs(first.Y - second.Y) <= 0.000001f
            && Math.Abs(first.Z - second.Z) <= 0.000001f
            && Math.Abs(first.W - second.W) <= 0.000001f)
        {
            return 0f;
        }

        if (first.LengthSquared() > 0.000001f)
        {
            first.Normalize();
        }
        else
        {
            first = XnaQuaternion.Identity;
        }

        if (second.LengthSquared() > 0.000001f)
        {
            second.Normalize();
        }
        else
        {
            second = XnaQuaternion.Identity;
        }

        float dot = Math.Abs(XnaQuaternion.Dot(first, second));
        dot = Math.Clamp(dot, -1f, 1f);
        return MathHelper.ToDegrees(2f * MathF.Acos(dot));
    }

    private static float VectorAngleDeltaDegrees(Vector3 first, Vector3 second)
    {
        if (first.LengthSquared() <= 0.000001f || second.LengthSquared() <= 0.000001f)
        {
            return 0f;
        }

        first.Normalize();
        second.Normalize();
        float dot = Math.Clamp(Vector3.Dot(first, second), -1f, 1f);
        return MathHelper.ToDegrees(MathF.Acos(dot));
    }

    private static PlacementRecord BuildPlacementRecord(
        string sourcePath,
        IReadOnlyList<string> lineage,
        string rawModelName,
        string resolvedModelName,
        IReadOnlyList<string> notes,
        Matrix sourceMatrix,
        Matrix comparisonMatrix)
    {
        if (!comparisonMatrix.Decompose(out Vector3 scale, out XnaQuaternion orientation, out Vector3 position))
        {
            scale = new Vector3(
                comparisonMatrix.Right.Length(),
                comparisonMatrix.Up.Length(),
                comparisonMatrix.Backward.Length());
            orientation = XnaQuaternion.Identity;
            position = comparisonMatrix.Translation;
        }

        orientation = orientation.LengthSquared() > 0.000001f
            ? XnaQuaternion.Normalize(orientation)
            : XnaQuaternion.Identity;

        Vector3 right = Vector3.Normalize(Vector3.Transform(Vector3.Right, orientation));
        Vector3 up = Vector3.Normalize(Vector3.Transform(Vector3.Up, orientation));
        Vector3 forward = Vector3.Normalize(Vector3.Transform(Vector3.Forward, orientation));

        return new PlacementRecord
        {
            SourcePath = sourcePath,
            SourceLineage = lineage.ToArray(),
            RawModelName = rawModelName,
            ResolvedModelName = resolvedModelName,
            Notes = notes.ToArray(),
            SourceMatrix = ToMatrixRows(sourceMatrix),
            ComparisonMatrix = ToMatrixRows(comparisonMatrix),
            Position = Float3.FromVector3(position),
            Orientation = Float4.FromQuaternion(orientation),
            Scale = Float3.FromVector3(scale),
            Right = Float3.FromVector3(right),
            Up = Float3.FromVector3(up),
            Forward = Float3.FromVector3(forward),
        };
    }

    private static SkippedPlacementRecord BuildSkippedRecord(
        string sourcePath,
        IReadOnlyList<string> lineage,
        string rawModelName,
        string resolvedModelName,
        string reason,
        IReadOnlyList<string> notes,
        Matrix sourceMatrix,
        Vector3 comparisonOrigin)
    {
        return new SkippedPlacementRecord
        {
            SourcePath = sourcePath,
            SourceLineage = lineage.ToArray(),
            RawModelName = rawModelName,
            ResolvedModelName = resolvedModelName,
            Reason = reason,
            Notes = notes.ToArray(),
            SourceMatrix = ToMatrixRows(sourceMatrix),
            ComparisonPreviewPosition = Float3.FromVector3(ConvertLegacyTransform(sourceMatrix, comparisonOrigin).Translation),
        };
    }

    private static float[][] ToMatrixRows(Matrix matrix)
    {
        return
        [
            [Round(matrix.M11), Round(matrix.M12), Round(matrix.M13), Round(matrix.M14)],
            [Round(matrix.M21), Round(matrix.M22), Round(matrix.M23), Round(matrix.M24)],
            [Round(matrix.M31), Round(matrix.M32), Round(matrix.M33), Round(matrix.M34)],
            [Round(matrix.M41), Round(matrix.M42), Round(matrix.M43), Round(matrix.M44)],
        ];
    }

    private static float Round(float value)
    {
        return MathF.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    [XmlRoot("TrackData")]
    public sealed class TrackLayout
    {
        public List<Vector3> TrackPoints { get; set; } = [];

        public List<WidthHelper> WidthHelpers { get; set; } = [];

        public List<RoadHelper> RoadHelpers { get; set; } = [];

        public List<NeutralObject> NeutralsObjects { get; set; } = [];
    }

    public sealed class WidthHelper
    {
        public Vector3 pos { get; set; }

        public float scale { get; set; }
    }

    public sealed class RoadHelper
    {
        public string type { get; set; } = string.Empty;

        public Vector3 pos { get; set; }
    }

    public sealed class NeutralObject
    {
        public string modelName { get; set; } = string.Empty;

        public Matrix matrix { get; set; }
    }

    public sealed class CombiObject
    {
        public string modelName { get; set; } = string.Empty;

        public Matrix matrix { get; set; }
    }

    private sealed class Options
    {
        public string? RepoRoot { get; private set; }

        public string? OutputDirectory { get; private set; }

        public string? CompareLiveRuntimeScenePath { get; private set; }

        public static Options Parse(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--repo-root":
                        options.RepoRoot = GetNextValue(args, ref i, "--repo-root");
                        break;

                    case "--output-dir":
                        options.OutputDirectory = GetNextValue(args, ref i, "--output-dir");
                        break;

                    case "--compare-live-runtime-scene":
                        options.CompareLiveRuntimeScenePath = GetNextValue(args, ref i, "--compare-live-runtime-scene");
                        break;

                    default:
                        throw new ArgumentException($"Unknown argument '{args[i]}'. Supported arguments are --repo-root, --output-dir, and --compare-live-runtime-scene.");
                }
            }

            return options;
        }

        private static string GetNextValue(string[] args, ref int index, string optionName)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {optionName}.");
            }

            index++;
            return args[index];
        }
    }

    private sealed class CombiLoader
    {
        private readonly string _contentRoot;
        private readonly Dictionary<string, CombiLoadResult> _cache = new(StringComparer.OrdinalIgnoreCase);

        public CombiLoader(string contentRoot)
        {
            _contentRoot = contentRoot;
        }

        public bool TryLoadCombi(string combiName, out IReadOnlyList<CombiObject> combiObjects)
        {
            if (_cache.TryGetValue(combiName, out CombiLoadResult? cached))
            {
                combiObjects = cached.Objects;
                return cached.Exists;
            }

            string filePath = Path.Combine(_contentRoot, combiName + ".CombiModel");
            if (!File.Exists(filePath))
            {
                _cache[combiName] = new CombiLoadResult(false, Array.Empty<CombiObject>());
                combiObjects = Array.Empty<CombiObject>();
                return false;
            }

            using var stream = File.OpenRead(filePath);
            IReadOnlyList<CombiObject> loaded = CombiSerializer.Deserialize(stream) as List<CombiObject> ?? [];
            _cache[combiName] = new CombiLoadResult(true, loaded);
            combiObjects = loaded;
            return true;
        }

        private sealed record CombiLoadResult(bool Exists, IReadOnlyList<CombiObject> Objects);
    }

    private sealed class TerrainHeightSampler
    {
        private const int GridWidth = 257;
        private const int GridHeight = 257;
        private const float MapWidthFactor = 10f;
        private const float MapHeightFactor = 10f;
        private const float MapZScale = 300f;

        private readonly float[,] _mapHeights;

        private TerrainHeightSampler(float[,] mapHeights)
        {
            _mapHeights = mapHeights;
        }

        public static TerrainHeightSampler Load(string filePath)
        {
            byte[] heights = File.ReadAllBytes(filePath);
            if (heights.Length < GridWidth * GridHeight)
            {
                throw new InvalidOperationException($"Landscape height data '{filePath}' is incomplete.");
            }

            var mapHeights = new float[GridWidth, GridHeight];
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int index = x + y * GridWidth;
                    float heightPercent = heights[index] / 255.0f;
                    mapHeights[x, y] = heightPercent * MapZScale;
                }
            }

            return new TerrainHeightSampler(mapHeights);
        }

        public float GetMapHeight(float x, float y)
        {
            x /= MapWidthFactor;
            y /= MapHeightFactor;

            int ix = ModulateValueInRange(x, GridWidth - 1);
            int iy = ModulateValueInRange(y, GridHeight - 1);
            float fractionX = x - (int)x;
            float fractionY = y - (int)y;

            int ix2 = (ix + 1) % (GridWidth - 1);
            int iy2 = (iy + 1) % (GridHeight - 1);

            if (fractionX + fractionY < 1f)
            {
                return _mapHeights[ix, iy]
                    + fractionX * (_mapHeights[ix2, iy] - _mapHeights[ix, iy])
                    + fractionY * (_mapHeights[ix, iy2] - _mapHeights[ix, iy]);
            }

            return _mapHeights[ix2, iy2]
                + (1f - fractionY) * (_mapHeights[ix2, iy] - _mapHeights[ix2, iy2])
                + (1f - fractionX) * (_mapHeights[ix, iy2] - _mapHeights[ix2, iy2]);
        }

        private static int ModulateValueInRange(float value, int max)
        {
            if (value < 0f)
            {
                return (max - 1) - ((int)(-value) % max);
            }

            return (int)value % max;
        }
    }

    private sealed class ModelSizeProvider : IDisposable
    {
        private readonly string _modelRoot;
        private readonly AssimpContext _assimpContext = new();
        private readonly Dictionary<string, float> _cache = new(StringComparer.OrdinalIgnoreCase);

        public ModelSizeProvider(string modelRoot)
        {
            _modelRoot = modelRoot;
        }

        public float GetModelSize(string modelName)
        {
            if (_cache.TryGetValue(modelName, out float cachedValue))
            {
                return cachedValue;
            }

            string modelPath = Path.Combine(_modelRoot, modelName + ".X");
            if (!File.Exists(modelPath))
            {
                _cache[modelName] = 1f;
                return 1f;
            }

            try
            {
                Scene scene = _assimpContext.ImportFile(modelPath,
                    PostProcessSteps.Triangulate
                    | PostProcessSteps.FlipUVs
                    | PostProcessSteps.JoinIdenticalVertices
                    | PostProcessSteps.GenerateSmoothNormals
                    | PostProcessSteps.FlipWindingOrder
                    | PostProcessSteps.GlobalScale);

                if (scene.MeshCount == 0 || scene.Meshes[0].VertexCount == 0)
                {
                    _cache[modelName] = 1f;
                    return 1f;
                }

                var points = scene.Meshes[0].Vertices
                    .Select(static vertex => new Vector3(vertex.X, vertex.Y, vertex.Z))
                    .ToArray();

                BoundingSphere sphere = BoundingSphere.CreateFromPoints(points);
                float absoluteScale = 1f;
                if (scene.RootNode != null && TryGetMeshAbsoluteTransform(scene.RootNode, Assimp.Matrix4x4.Identity, 0, out Assimp.Matrix4x4 absoluteTransform))
                {
                    absoluteScale = EstimateScale(absoluteTransform);
                }

                float size = sphere.Radius * absoluteScale;
                if (size <= 0.0001f)
                {
                    size = 1f;
                }

                _cache[modelName] = size;
                return size;
            }
            catch
            {
                _cache[modelName] = 1f;
                return 1f;
            }
        }

        public void Dispose()
        {
            _assimpContext.Dispose();
        }

        private static float EstimateScale(Assimp.Matrix4x4 matrix)
        {
            Vector3 axisX = new(matrix.A1, matrix.B1, matrix.C1);
            Vector3 axisY = new(matrix.A2, matrix.B2, matrix.C2);
            Vector3 axisZ = new(matrix.A3, matrix.B3, matrix.C3);
            float scale = MathF.Max(axisX.Length(), MathF.Max(axisY.Length(), axisZ.Length()));
            return scale > 0.0001f ? scale : 1f;
        }

        private static bool TryGetMeshAbsoluteTransform(Assimp.Node node, Assimp.Matrix4x4 parentTransform, int meshIndex, out Assimp.Matrix4x4 absoluteTransform)
        {
            absoluteTransform = node.Transform * parentTransform;
            if (node.MeshIndices.Contains(meshIndex))
            {
                return true;
            }

            foreach (Assimp.Node child in node.Children)
            {
                if (TryGetMeshAbsoluteTransform(child, absoluteTransform, meshIndex, out Assimp.Matrix4x4 childTransform))
                {
                    absoluteTransform = childTransform;
                    return true;
                }
            }

            absoluteTransform = Assimp.Matrix4x4.Identity;
            return false;
        }
    }

    private abstract class ExportStateBase
    {
        private readonly List<PlacementRecord> _placements = [];
        private readonly List<SkippedPlacementRecord> _skipped = [];
        private TrackStartPoseRecord? _startPose;
        private List<TrackRoadSampleRecord> _roadSamples = [];
        private List<Float3> _checkpointPositions = [];
        private int _loopInsertionsCount;

        protected ExportStateBase(string trackName, Vector3 comparisonOrigin, CombiLoader combiLoader)
        {
            TrackName = trackName;
            ComparisonOrigin = comparisonOrigin;
            CombiLoader = combiLoader;
        }

        protected string TrackName { get; }

        public Vector3 ComparisonOrigin { get; }

        public CombiLoader CombiLoader { get; }

        public void AddPlacement(
            string sourcePath,
            IReadOnlyList<string> lineage,
            string rawModelName,
            string resolvedModelName,
            IReadOnlyList<string> notes,
            Matrix sourceMatrix,
            Matrix comparisonMatrix)
        {
            _placements.Add(BuildPlacementRecord(sourcePath, lineage, rawModelName, resolvedModelName, notes, sourceMatrix, comparisonMatrix));
        }

        public void AddSkip(
            string sourcePath,
            IReadOnlyList<string> lineage,
            string rawModelName,
            string resolvedModelName,
            string reason,
            IReadOnlyList<string> notes,
            Matrix sourceMatrix)
        {
            _skipped.Add(BuildSkippedRecord(sourcePath, lineage, rawModelName, resolvedModelName, reason, notes, sourceMatrix, ComparisonOrigin));
        }

        public void SetStartPose(TrackStartPoseRecord startPose)
        {
            _startPose = startPose;
        }

        public void SetRoadGeometry(List<TrackRoadSampleRecord> roadSamples, List<Float3> checkpointPositions, int loopInsertionsCount)
        {
            _roadSamples = roadSamples;
            _checkpointPositions = checkpointPositions;
            _loopInsertionsCount = loopInsertionsCount;
        }

        public TrackExport ToExport()
        {
            return new TrackExport
            {
                TrackName = TrackName,
                StartPose = _startPose,
                RoadSamples = _roadSamples,
                CheckpointPositions = _checkpointPositions,
                LoopInsertionsCount = _loopInsertionsCount,
                PlacementCount = _placements.Count,
                SkippedCount = _skipped.Count,
                Placements = _placements,
                Skipped = _skipped,
            };
        }
    }

    private sealed class OriginalExportState : ExportStateBase
    {
        public OriginalExportState(
            string trackName,
            Vector3 comparisonOrigin,
            TerrainHeightSampler terrain,
            ModelSizeProvider modelSizeProvider,
            CombiLoader combiLoader)
            : base(trackName, comparisonOrigin, combiLoader)
        {
            Terrain = terrain;
            ModelSizeProvider = modelSizeProvider;
        }

        public TerrainHeightSampler Terrain { get; }

        public ModelSizeProvider ModelSizeProvider { get; }

        public List<Vector3> CreatedLegacyPositions { get; } = [];
    }

    private sealed class CasaExportState : ExportStateBase
    {
        public CasaExportState(
            string trackName,
            Vector3 comparisonOrigin,
            TerrainHeightSampler terrain,
            ModelSizeProvider modelSizeProvider,
            CombiLoader combiLoader,
            string modelRoot)
            : base(trackName, comparisonOrigin, combiLoader)
        {
            Terrain = terrain;
            ModelSizeProvider = modelSizeProvider;
            ModelRoot = modelRoot;
        }

        public TerrainHeightSampler Terrain { get; }

        public ModelSizeProvider ModelSizeProvider { get; }

        public string ModelRoot { get; }

        public List<Vector3> CreatedLegacyPositions { get; } = [];
    }

    private sealed class ExportFile
    {
        public string Generator { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; }

        public string Scope { get; set; } = string.Empty;

        public string ComparisonSpace { get; set; } = string.Empty;

        public List<TrackExport> Tracks { get; set; } = [];
    }

    private sealed class TrackExport
    {
        public string TrackName { get; set; } = string.Empty;

        public TrackStartPoseRecord? StartPose { get; set; }

        public List<TrackRoadSampleRecord> RoadSamples { get; set; } = [];

        public List<Float3> CheckpointPositions { get; set; } = [];

        public int LoopInsertionsCount { get; set; }

        public int PlacementCount { get; set; }

        public int SkippedCount { get; set; }

        public List<PlacementRecord> Placements { get; set; } = [];

        public List<SkippedPlacementRecord> Skipped { get; set; } = [];
    }

    private sealed class TrackStartPoseRecord
    {
        public Float3 Position { get; set; }

        public Float4 Orientation { get; set; }

        public Float3 Forward { get; set; }

        public Float3 Up { get; set; }
    }

    private sealed class TrackRoadSampleRecord
    {
        public int SampleIndex { get; set; }

        public Float3 Position { get; set; }

        public Float3 Forward { get; set; }

        public Float3 Up { get; set; }

        public Float3 Right { get; set; }

        public float RoadWidth { get; set; }

        public float TextureU { get; set; }
    }

    private sealed class PlacementRecord
    {
        public string SourcePath { get; set; } = string.Empty;

        public string[] SourceLineage { get; set; } = Array.Empty<string>();

        public string RawModelName { get; set; } = string.Empty;

        public string ResolvedModelName { get; set; } = string.Empty;

        public string[] Notes { get; set; } = Array.Empty<string>();

        public float[][] SourceMatrix { get; set; } = Array.Empty<float[]>();

        public float[][] ComparisonMatrix { get; set; } = Array.Empty<float[]>();

        public Float3 Position { get; set; }

        public Float4 Orientation { get; set; }

        public Float3 Scale { get; set; }

        public Float3 Right { get; set; }

        public Float3 Up { get; set; }

        public Float3 Forward { get; set; }
    }

    private sealed class SkippedPlacementRecord
    {
        public string SourcePath { get; set; } = string.Empty;

        public string[] SourceLineage { get; set; } = Array.Empty<string>();

        public string RawModelName { get; set; } = string.Empty;

        public string ResolvedModelName { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string[] Notes { get; set; } = Array.Empty<string>();

        public float[][] SourceMatrix { get; set; } = Array.Empty<float[]>();

        public Float3 ComparisonPreviewPosition { get; set; }
    }

    private readonly record struct Float3(float X, float Y, float Z)
    {
        public static Float3 FromVector3(Vector3 value)
        {
            return new Float3(Round(value.X), Round(value.Y), Round(value.Z));
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }

    private readonly record struct Float4(float X, float Y, float Z, float W)
    {
        public static Float4 FromQuaternion(XnaQuaternion value)
        {
            return new Float4(Round(value.X), Round(value.Y), Round(value.Z), Round(value.W));
        }

        public XnaQuaternion ToQuaternion()
        {
            return new XnaQuaternion(X, Y, Z, W);
        }
    }

    private sealed class ComparisonTrackPoint
    {
        public ComparisonTrackPoint(Vector3 position)
        {
            Position = position;
        }

        public ComparisonTrackPoint(ComparisonTrackPoint other)
        {
            Position = other.Position;
            Right = other.Right;
            Up = other.Up;
            Direction = other.Direction;
            RoadWidth = other.RoadWidth;
            TextureU = other.TextureU;
        }

        public Vector3 Position { get; set; }

        public Vector3 Right { get; set; } = Vector3.Right;

        public Vector3 Up { get; set; } = new(0f, 0f, 1f);

        public Vector3 Direction { get; set; } = Vector3.UnitY;

        public float RoadWidth { get; set; } = LegacyDefaultRoadWidth;

        public float TextureU { get; set; }
    }

    private sealed record TrackSplineComputation(List<ComparisonTrackPoint> Points, int LoopInsertionsCount);
}