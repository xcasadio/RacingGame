using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;
using static RacingGame.Gameplay.Track.RaceTrackSpace;

namespace RacingGame.Gameplay.Track;

/// <summary>
/// Every placement of a legacy track, computed from its .Track layout and the landscape height map:
/// the road spline, the guard rails and their posts, the columns and their segments, the scenery,
/// the checkpoints, the start pose and the physics profile.
/// It builds no mesh, material or entity and touches no GPU object, so the scene factory, the generator
/// components and the export of worlds can all reuse it. The operations, their order and their constants are
/// those of the original game (RacingGameOriginal/Core/Tracks, Core/Landscapes): the float results must stay bit-identical.
/// </summary>
internal sealed class RaceTrackLayout
{
    internal const float HolderGap = 15.0f;
    internal const float ColumnsDistance = 33.0f;
    internal const string GuardRailPostModelName = "GuardRailHolder";
    internal const string ColumnSegmentModelName = "RoadColumnSegment";

    private const int NumberOfIterationsPer100Meters = 40;
    private const float CurveFactor = 0.25f;
    private const float UpFactorCorrector = 0.6f;
    private const float RoadTextureStretchFactor = 0.125f;
    private const int NumberOfUpSmoothValues = 10;
    private const float MinimumLandscapeDistance = 2.0f;
    private const float LegacyMinRoadWidth = 0.25f;
    private const float LegacyDefaultRoadWidth = 1.0f;
    private const float LegacyMaxRoadWidth = 2.0f;
    private const float HelperActivationDistance = 25.0f;
    private const float PalmAndLaternGap = 20.0f;
    private const float CheckpointGap = 500.0f;
    private const float SignGap = 24.0f;
    private const float CheckpointTriggerWidthPadding = 1.5f;
    private const float CheckpointTriggerMinHalfWidth = 5.5f;
    private const float CheckpointTriggerHalfHeight = 4.0f;
    private const float CheckpointTriggerHalfDepth = 2.75f;
    private const float InsideRoadDistance = 0.5f;
    private const float MinimumColumnHeight = 2.5f;
    private const float ColumnGroundHeight = 1.0f;

    private static readonly Vector3 HolderPileCorrectionVector = new(0.225f, 0f, 0f);
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

    // The start light's models: red, yellow and green (RacingGame.Shared/Landscapes/TrackObjectManager.cs).
    internal static readonly string[] StartLightModelNames = ["StartLight", "StartLight2", "StartLight3"];

    // Track.cs:786-788: the palms the generation picks from.
    private static readonly string[] PalmModelCandidates = ["AlphaPalm", "AlphaPalm2", "AlphaPalm3", "AlphaPalmSmall"];

    // Track.cs:938-942: the banners of the checkpoints.
    private static readonly string[] CheckpointBannerCandidates = ["Banner", "Banner2", "Banner3", "Banner4", "Banner5", "Banner6"];

    private RaceTrackLayout(
        string trackName,
        Vector3 origin,
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        IReadOnlyList<RaceTrackRailPoint> leftRailPoints,
        IReadOnlyList<RaceTrackRailPoint> rightRailPoints,
        IReadOnlyList<RaceTrackModelPlacement> leftPosts,
        IReadOnlyList<RaceTrackModelPlacement> rightPosts,
        IReadOnlyList<RaceTrackColumnPlacement> columns,
        IReadOnlyList<RaceTrackModelPlacement> columnSegments,
        IReadOnlyList<RaceTrackTunnelSpan> tunnelSpans,
        IReadOnlyList<RaceTrackModelPlacement> scenery,
        IReadOnlyList<RaceTrackModelPlacement> placements,
        IReadOnlyList<RaceCheckpointTriggerDefinition> checkpointTriggers,
        RaceTrackStartPose playerStartPose,
        RaceTrackPhysicsProfile physicsProfile,
        RaceTrackLayoutSettings settings)
    {
        Settings = settings;
        TrackName = trackName;
        Origin = origin;
        RoadPoints = roadPoints;
        LeftRailPoints = leftRailPoints;
        RightRailPoints = rightRailPoints;
        LeftPosts = leftPosts;
        RightPosts = rightPosts;
        Columns = columns;
        ColumnSegments = columnSegments;
        TunnelSpans = tunnelSpans;
        Scenery = scenery;
        Placements = placements;
        CheckpointTriggers = checkpointTriggers;
        PlayerStartPose = playerStartPose;
        PhysicsProfile = physicsProfile;
    }

    public string TrackName { get; }

    /// <summary>The post and column spacing this layout was computed with.</summary>
    public RaceTrackLayoutSettings Settings { get; }

    /// <summary>The runtime-space point the track is centered on: every runtime position is relative to it.</summary>
    public Vector3 Origin { get; }

    /// <summary>The road spline in legacy space, with its orientations, widths and texture coordinates; the last point closes the loop.</summary>
    public IReadOnlyList<RaceTrackRoadPoint> RoadPoints { get; }

    /// <summary>True when the track has enough road points for guard rails and columns; otherwise their lists are empty.</summary>
    public bool HasGuardRails => RoadPoints.Count >= 4;

    public IReadOnlyList<RaceTrackRailPoint> LeftRailPoints { get; }

    public IReadOnlyList<RaceTrackRailPoint> RightRailPoints { get; }

    /// <summary>The guard rail posts of the left rail, in runtime space, in placement order.</summary>
    public IReadOnlyList<RaceTrackModelPlacement> LeftPosts { get; }

    public IReadOnlyList<RaceTrackModelPlacement> RightPosts { get; }

    public IReadOnlyList<RaceTrackColumnPlacement> Columns { get; }

    /// <summary>One column segment per column, in runtime space, in the order of <see cref="Columns"/>.</summary>
    public IReadOnlyList<RaceTrackModelPlacement> ColumnSegments { get; }

    /// <summary>The tunnel spans of the road helpers, in road order. Computed with or without the scenery.</summary>
    public IReadOnlyList<RaceTrackTunnelSpan> TunnelSpans { get; }

    /// <summary>
    /// The scenery in runtime space, in placement order: the neutral objects of the track file, then what the generation adds
    /// (palms, laterns, start objects, banners, signs, random scenery). A placement's index is the one its entity name carries.
    /// </summary>
    public IReadOnlyList<RaceTrackModelPlacement> Scenery { get; }

    /// <summary>Every object the original's add path accepted (posts, column segments and scenery), in the order it added them.</summary>
    public IReadOnlyList<RaceTrackModelPlacement> Placements { get; }

    public IReadOnlyList<RaceCheckpointTriggerDefinition> CheckpointTriggers { get; }

    public RaceTrackStartPose PlayerStartPose { get; }

    public RaceTrackPhysicsProfile PhysicsProfile { get; }

    /// <summary>
    /// Computes the layout of a track. The objects go through <see cref="LegacyLandscapePlacer"/>, in the order the original
    /// adds them: the neutral objects of the track file (TrackLine.cs:664-676), the posts of the left then of the right
    /// guard rail (Track.cs:637-638), the column segments (Track.cs:639), then what Track.GenerateObjectsForTrack adds
    /// (Track.cs:704-1161). Nothing before the generation draws a random number, so the posts and the column segments
    /// do not depend on the seed.
    /// </summary>
    /// <param name="trackName">The track's name.</param>
    /// <param name="track">The track's .Track layout.</param>
    /// <param name="terrainSampler">The landscape height map.</param>
    /// <param name="loadCombiModel">Returns the objects of a CombiModel (empty for an unknown one).</param>
    /// <param name="seed">The seed of the one random generator the generation draws from; see <see cref="CreateTrackSeed"/>.</param>
    /// <param name="settings">The spacing of the guard rail posts and of the columns; null for the legacy values.</param>
    /// <param name="includeScenery">
    /// False skips the generation of the scenery (a generator component does not use it, and it is the only part that draws
    /// from the seed); the neutral objects, the posts and the column segments are always placed.
    /// </param>
    internal static RaceTrackLayout Compute(
        string trackName,
        LegacyTrackLayout track,
        LegacyTerrainHeightSampler terrainSampler,
        Func<string, IReadOnlyList<LegacyCombiObject>> loadCombiModel,
        int seed,
        RaceTrackLayoutSettings? settings = null,
        bool includeScenery = true)
    {
        RaceTrackLayoutSettings layoutSettings = settings ?? RaceTrackLayoutSettings.Default;
        List<LegacyTrackPoint> splinePoints = BuildRoadSplinePoints(track, terrainSampler);
        var roadPoints = new List<RaceTrackRoadPoint>(splinePoints.Count);
        foreach (LegacyTrackPoint splinePoint in splinePoints)
        {
            roadPoints.Add(new RaceTrackRoadPoint(
                splinePoint.Position,
                splinePoint.Right,
                splinePoint.Up,
                splinePoint.Direction,
                splinePoint.TextureU,
                splinePoint.RoadWidth));
        }

        Vector3 origin = ComputeOrigin(track.TrackPoints);
        var placer = new LegacyLandscapePlacer(origin, terrainSampler, loadCombiModel);
        RaceTrackPhysicsProfile physicsProfile = CreateTrackPhysicsProfile(roadPoints, origin);

        if (roadPoints.Count < 3)
        {
            throw new InvalidOperationException($"Track '{trackName}' does not contain enough points to build a road.");
        }

        RaceTrackStartPose playerStartPose = CreatePlayerStartPose(roadPoints[0], origin);
        List<RaceCheckpointTriggerDefinition> checkpointTriggers =
        [
            CreateCheckpointTriggerDefinition(roadPoints, 0.20f, origin),
            CreateCheckpointTriggerDefinition(roadPoints, 0.50f, origin),
            CreateCheckpointTriggerDefinition(roadPoints, 0.80f, origin),
        ];

        // The road helper spans drive both the tunnels, which the road generator builds without the scenery, and the
        // palms and laterns.
        List<LegacyRoadHelperPosition> helperPositions = roadPoints.Count == 0 ? [] : BuildRoadHelperPositions(roadPoints, track.RoadHelpers);
        var tunnelSpans = new List<RaceTrackTunnelSpan>();
        foreach (LegacyRoadHelperPosition helperPosition in helperPositions)
        {
            if (helperPosition.Type == LegacyRoadHelperType.Tunnel)
            {
                tunnelSpans.Add(new RaceTrackTunnelSpan(helperPosition.StartIndex, helperPosition.EndIndex));
            }
        }

        // TrackLine.cs:664-676: the neutral objects of the track file come first.
        var scenery = new List<RaceTrackModelPlacement>();
        foreach (LegacyNeutralObject neutralObject in track.NeutralsObjects)
        {
            placer.AddObject(neutralObject.modelName, neutralObject.matrix, false, scenery);
        }

        List<RaceTrackRailPoint> leftRailPoints = [];
        List<RaceTrackRailPoint> rightRailPoints = [];
        List<RaceTrackModelPlacement> leftPosts = [];
        List<RaceTrackModelPlacement> rightPosts = [];
        List<RaceTrackColumnPlacement> columns = [];
        List<RaceTrackModelPlacement> columnSegments = [];
        if (roadPoints.Count >= 4)
        {
            leftRailPoints = BuildRailPoints(roadPoints, leftSide: true);
            rightRailPoints = BuildRailPoints(roadPoints, leftSide: false);
            AddPostPlacements(placer, BuildPostRailPoints(roadPoints, leftSide: true), layoutSettings.HolderGap, leftPosts);
            AddPostPlacements(placer, BuildPostRailPoints(roadPoints, leftSide: false), layoutSettings.HolderGap, rightPosts);
            columns = BuildColumnPlacements(roadPoints, terrainSampler, layoutSettings.ColumnsDistance);
            AddColumnSegmentPlacements(placer, columns, columnSegments);
        }

        if (includeScenery)
        {
            AddHelperDrivenSceneryPlacements(seed, roadPoints, helperPositions, terrainSampler, placer, scenery);
        }

        return new RaceTrackLayout(
            trackName,
            origin,
            roadPoints,
            leftRailPoints,
            rightRailPoints,
            leftPosts,
            rightPosts,
            columns,
            columnSegments,
            tunnelSpans,
            scenery,
            placer.Placements,
            checkpointTriggers,
            playerStartPose,
            physicsProfile,
            layoutSettings);
    }

    /// <summary>
    /// The seed of the random scenery of a track when nothing sets one: derived from the track's name, so that a track has
    /// the same scenery on every launch. System.HashCode is randomized per process, so the seed uses a stable FNV-1a hash.
    /// </summary>
    internal static int CreateTrackSeed(string trackName)
    {
        const uint FnvOffsetBasis = 2166136261u;
        const uint FnvPrime = 16777619u;

        uint hash = FnvOffsetBasis;
        foreach (byte value in Encoding.UTF8.GetBytes($"{trackName.ToUpperInvariant()}|LegacyTrackSceneFactory.Scenery"))
        {
            hash = unchecked((hash ^ value) * FnvPrime);
        }

        return unchecked((int)hash);
    }

    /// <summary>
    /// A digest of the placements (model name, then the 16 elements of the transform, in the order the objects were added):
    /// the first 8 bytes of a SHA-256, in hexadecimal. Two launches of a track with the same seed give the same digest.
    /// </summary>
    internal string ComputePlacementDigest()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<float> elements = stackalloc float[16];
        foreach (RaceTrackModelPlacement placement in Placements)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(placement.ModelName));
            Matrix transform = placement.Transform;
            elements[0] = transform.M11;
            elements[1] = transform.M12;
            elements[2] = transform.M13;
            elements[3] = transform.M14;
            elements[4] = transform.M21;
            elements[5] = transform.M22;
            elements[6] = transform.M23;
            elements[7] = transform.M24;
            elements[8] = transform.M31;
            elements[9] = transform.M32;
            elements[10] = transform.M33;
            elements[11] = transform.M34;
            elements[12] = transform.M41;
            elements[13] = transform.M42;
            elements[14] = transform.M43;
            elements[15] = transform.M44;
            hash.AppendData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(elements));
        }

        return Convert.ToHexString(hash.GetHashAndReset(), 0, 8);
    }

    /// <summary>The runtime-space point a track is centered on, from its .Track points alone.</summary>
    internal static Vector3 ComputeOrigin(IReadOnlyList<Vector3> trackPoints)
    {
        float minX = trackPoints.Min(static point => point.X);
        float maxX = trackPoints.Max(static point => point.X);
        float minY = trackPoints.Min(static point => point.Y);
        float maxY = trackPoints.Max(static point => point.Y);
        float minZ = trackPoints.Min(static point => point.Z);
        float maxZ = trackPoints.Max(static point => point.Z);
        Vector3 center = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f);
        return ConvertLegacyPoint(center);
    }

    private static RaceTrackStartPose CreatePlayerStartPose(RaceTrackRoadPoint point, Vector3 origin)
    {
        Vector3 runtimePosition = ConvertLegacyPoint(point.Position) - origin;
        Vector3 runtimeForward = ConvertLegacyDirection(point.Direction);
        Vector3 runtimeUp = ConvertLegacyDirection(point.Up);
        Vector3 runtimeRight = ConvertLegacyDirection(point.Right);

        if (runtimeRight.LengthSquared() < 0.0001f)
        {
            runtimeRight = Vector3.Cross(runtimeForward, runtimeUp);
        }

        if (runtimeRight.LengthSquared() < 0.0001f)
        {
            runtimeRight = Vector3.Right;
        }

        runtimeRight.Normalize();
        runtimeUp = Vector3.Cross(runtimeRight, runtimeForward);
        if (runtimeUp.LengthSquared() < 0.0001f)
        {
            runtimeUp = Vector3.Up;
        }
        else
        {
            runtimeUp.Normalize();
        }

        runtimeRight = Vector3.Cross(runtimeForward, runtimeUp);
        if (runtimeRight.LengthSquared() < 0.0001f)
        {
            runtimeRight = Vector3.Right;
        }
        else
        {
            runtimeRight.Normalize();
        }

        Matrix rotation = Matrix.Identity;
        rotation.Right = runtimeRight;
        rotation.Up = runtimeUp;
        rotation.Forward = runtimeForward;

        return new RaceTrackStartPose(
            runtimePosition,
            Quaternion.CreateFromRotationMatrix(rotation),
            runtimeForward,
            runtimeUp);
    }

    private static RaceCheckpointTriggerDefinition CreateCheckpointTriggerDefinition(IReadOnlyList<RaceTrackRoadPoint> roadPoints, float progress, Vector3 origin)
    {
        RaceTrackPhysicsPoint physicsPoint = CreateRuntimePhysicsPoint(SampleLoopPoint(roadPoints, progress), origin);

        Matrix rotation = Matrix.Identity;
        rotation.Right = physicsPoint.Right;
        rotation.Up = physicsPoint.Up;
        rotation.Forward = physicsPoint.Forward;

        float halfWidth = Math.Max(CheckpointTriggerMinHalfWidth, physicsPoint.HalfWidth + CheckpointTriggerWidthPadding);
        return new RaceCheckpointTriggerDefinition(
            physicsPoint.Center,
            Quaternion.CreateFromRotationMatrix(rotation),
            halfWidth,
            CheckpointTriggerHalfHeight,
            CheckpointTriggerHalfDepth);
    }

    private static RaceTrackPhysicsProfile CreateTrackPhysicsProfile(IReadOnlyList<RaceTrackRoadPoint> roadPoints, Vector3 origin)
    {
        var points = new RaceTrackPhysicsPoint[roadPoints.Count];

        for (int index = 0; index < roadPoints.Count; index++)
        {
            points[index] = CreateRuntimePhysicsPoint(roadPoints[index], origin);
        }

        return new RaceTrackPhysicsProfile(points);
    }

    private static RaceTrackPhysicsPoint CreateRuntimePhysicsPoint(RaceTrackRoadPoint point, Vector3 origin)
    {
        Vector3 center = ConvertLegacyPoint(point.Position) - origin;
        Vector3 forward = ConvertLegacyDirection(point.Direction);
        Vector3 up = ConvertLegacyDirection(point.Up);
        Vector3 right = ConvertLegacyDirection(point.Right);

        if (right.LengthSquared() < 0.0001f)
        {
            right = Vector3.Cross(up, forward);
        }

        if (right.LengthSquared() < 0.0001f)
        {
            right = Vector3.Right;
        }
        else
        {
            right.Normalize();
        }

        up = Vector3.Cross(right, forward);
        if (up.LengthSquared() < 0.0001f)
        {
            up = Vector3.Up;
        }
        else
        {
            up.Normalize();
        }

        forward = Vector3.Cross(up, right);
        if (forward.LengthSquared() < 0.0001f)
        {
            forward = Vector3.Forward;
        }
        else
        {
            forward.Normalize();
        }

        float halfWidth = Math.Max(LegacyMinRoadWidth, point.RoadWidth) * LegacyRoadWidthScale * 0.5f;
        return new RaceTrackPhysicsPoint(center, forward, up, right, halfWidth);
    }

    private static RaceTrackRoadPoint SampleLoopPoint(IReadOnlyList<RaceTrackRoadPoint> roadPoints, float progress)
    {
        int index = (int)MathF.Round(progress * (roadPoints.Count - 1)) % roadPoints.Count;
        return roadPoints[Math.Clamp(index, 0, roadPoints.Count - 1)];
    }

    private static List<LegacyTrackPoint> BuildRoadSplinePoints(LegacyTrackLayout layout, LegacyTerrainHeightSampler terrainSampler)
    {
        Vector3[] inputPoints = layout.TrackPoints.ToArray();
        EnsureTrackPointsStayAboveLandscape(inputPoints, terrainSampler);
        inputPoints = InsertLoopingSegments(inputPoints);

        var points = new List<LegacyTrackPoint>();
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
                points.Add(new LegacyTrackPoint(Vector3.CatmullRom(p1, p2, p3, p4, iteration / (float)numberOfIterations)));
            }
        }

        if (points.Count == 0)
        {
            return points;
        }

        GenerateOrientationVectors(points, terrainSampler);
        AdjustRoadWidths(points, layout.WidthHelpers);
        GenerateRoadTextureCoordinates(points);
        return points;
    }

    private static void EnsureTrackPointsStayAboveLandscape(Vector3[] inputPoints, LegacyTerrainHeightSampler terrainSampler)
    {
        for (int index = 0; index < inputPoints.Length; index++)
        {
            float landscapeHeight = terrainSampler.GetMapHeight(inputPoints[index].X, inputPoints[index].Y) + MinimumLandscapeDistance * 2.25f;
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
                        float landscapeHeight = terrainSampler.GetMapHeight(
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

    private static void GenerateOrientationVectors(List<LegacyTrackPoint> points, LegacyTerrainHeightSampler terrainSampler)
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

            float landscapeHeight = terrainSampler.GetMapHeight(points[index].Position.X, points[index].Position.Y);
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

    private static void AdjustRoadWidths(List<LegacyTrackPoint> points, List<LegacyWidthHelper> widthHelpers)
    {
        float currentWidth = LegacyDefaultRoadWidth;
        float widthInfluence = currentWidth;
        for (int index = 0; index < points.Count; index++)
        {
            Vector3 position = points[index].Position;
            foreach (LegacyWidthHelper widthHelper in widthHelpers)
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
                    index == points.Count - 2 ? 0.25f : 0.175f;
                currentWidth = influence * points[0].RoadWidth + (1 - influence) * currentWidth;
            }

            currentWidth = Math.Clamp(currentWidth, LegacyMinRoadWidth, LegacyMaxRoadWidth);
            points[index].RoadWidth = currentWidth;
        }
    }

    private static void GenerateRoadTextureCoordinates(List<LegacyTrackPoint> points)
    {
        float currentRoadTextureU = 0.0f;
        for (int index = 0; index < points.Count; index++)
        {
            points[index].TextureU = currentRoadTextureU;
            currentRoadTextureU += RoadTextureStretchFactor * (points[(index + 1) % points.Count].Position - points[index % points.Count].Position).Length();
        }

        points.Add(new LegacyTrackPoint(points[0])
        {
            TextureU = currentRoadTextureU,
        });
    }

    private static List<RaceTrackRailPoint> BuildRailPoints(IReadOnlyList<RaceTrackRoadPoint> roadPoints, bool leftSide)
    {
        var railPoints = new List<RaceTrackRailPoint>(roadPoints.Count / 2 + 1);
        int railPointCount = roadPoints.Count / 2 + 1;

        for (int index = 0; index < railPointCount; index++)
        {
            int pointIndex = index * 2;
            if (pointIndex >= roadPoints.Count - 1)
            {
                pointIndex = roadPoints.Count - 1;
            }

            RaceTrackRoadPoint roadPoint = roadPoints[pointIndex];
            Vector3 right = roadPoint.Right;
            Vector3 direction = roadPoint.Direction;
            Vector3 position = roadPoint.Position + (leftSide ? -1f : 1f) * (LegacyRoadWidthScale * roadPoint.RoadWidth * right * 0.5f);

            if (leftSide)
            {
                right = -right;
                direction = -direction;
            }

            position -= right * InsideRoadDistance;
            railPoints.Add(new RaceTrackRailPoint(position, right, direction, roadPoint.Up));
        }

        return railPoints;
    }

    /// <summary>
    /// The rail points GuardRail.cs:169-199 builds, from which it places its posts: the road point's offset by half the road
    /// width along the road point's right vector as it is, not normalized (TrackVertex.cs:64-90; the vector is the cross
    /// product of the direction and the up vector, a bit shorter than 1 where the two are not at a right angle), then moved
    /// 0.5 units inside the road. The rail's mesh uses <see cref="BuildRailPoints"/>.
    /// </summary>
    private static List<RaceTrackRailPoint> BuildPostRailPoints(IReadOnlyList<RaceTrackRoadPoint> roadPoints, bool leftSide)
    {
        int railPointCount = roadPoints.Count / 2 + 1;
        var railPoints = new List<RaceTrackRailPoint>(railPointCount);

        for (int index = 0; index < railPointCount; index++)
        {
            int pointIndex = index * 2;
            if (pointIndex >= roadPoints.Count - 1)
            {
                pointIndex = roadPoints.Count - 1;
            }

            RaceTrackRoadPoint roadPoint = roadPoints[pointIndex];
            Vector3 right = GetLegacyRawRight(roadPoint);
            Vector3 direction = roadPoint.Direction;
            Vector3 position;
            if (leftSide)
            {
                position = roadPoint.Position - LegacyRoadWidthScale * roadPoint.RoadWidth * right / 2;

                // Invert the direction and right vector for the left side (GuardRail.cs:186-187)
                right = -right;
                direction = -direction;
            }
            else
            {
                position = roadPoint.Position + LegacyRoadWidthScale * roadPoint.RoadWidth * right / 2;
            }

            position -= right * InsideRoadDistance;
            railPoints.Add(new RaceTrackRailPoint(position, right, direction, roadPoint.Up));
        }

        return railPoints;
    }

    /// <summary>
    /// GuardRail.cs:206-315: a post every <paramref name="holderGap"/> units along the rail, each added the way
    /// GuardRail.cs:289-304 adds it (scale 1.125, shifted 0.225 along the rail's right, in the rail point's own space,
    /// not marked as generating shadows), so it is also scaled by 1.2, put above the terrain and skipped near an earlier object.
    /// </summary>
    private static void AddPostPlacements(
        LegacyLandscapePlacer placer,
        IReadOnlyList<RaceTrackRailPoint> railPoints,
        float holderGap,
        List<RaceTrackModelPlacement> target)
    {
        float lastHolderGap = 0f;

        for (int index = 0; index < railPoints.Count; index++)
        {
            RaceTrackRailPoint railPoint = railPoints[index];
            Vector3 nextPosition = railPoints[(index + 1) % railPoints.Count].Position;
            float distance = Vector3.Distance(nextPosition, railPoint.Position);

            if (distance > 0.0001f && lastHolderGap - distance <= 0f)
            {
                Vector3 p1 = railPoints[index - 1 < 0 ? railPoints.Count - 1 : index - 1].Position;
                Vector3 p2 = railPoint.Position;
                Vector3 p3 = railPoints[(index + 1) % railPoints.Count].Position;
                Vector3 p4 = railPoints[(index + 2) % railPoints.Count].Position;
                Vector3 holderPoint = Vector3.CatmullRom(p1, p2, p3, p4, lastHolderGap / distance);

                Matrix pointSpace = CreateLegacyPointSpace(railPoint.Right, railPoint.Direction, railPoint.Up);
                placer.AddObject(
                    GuardRailPostModelName,
                    Matrix.CreateScale(1.125f)
                    * Matrix.CreateTranslation(HolderPileCorrectionVector)
                    * pointSpace
                    * Matrix.CreateTranslation(holderPoint),
                    false,
                    target);
                lastHolderGap += holderGap;
            }

            lastHolderGap -= distance;
        }
    }

    private static List<RaceTrackColumnPlacement> BuildColumnPlacements(IReadOnlyList<RaceTrackRoadPoint> roadPoints, LegacyTerrainHeightSampler terrainSampler, float columnsDistance)
    {
        var columns = new List<RaceTrackColumnPlacement>();
        float lastColumnsDistance = columnsDistance;

        for (int index = 0; index < roadPoints.Count; index++)
        {
            RaceTrackRoadPoint currentPoint = roadPoints[index];
            RaceTrackRoadPoint nextPoint = roadPoints[(index + 1) % roadPoints.Count];
            float distance = Vector3.Distance(nextPoint.Position, currentPoint.Position);

            if (distance > 0.0001f && lastColumnsDistance - distance <= 0f)
            {
                Vector3 p1 = roadPoints[index - 1 < 0 ? roadPoints.Count - 1 : index - 1].Position;
                Vector3 p2 = currentPoint.Position;
                Vector3 p3 = nextPoint.Position;
                Vector3 p4 = roadPoints[(index + 2) % roadPoints.Count].Position;
                Vector3 columnPoint = Vector3.CatmullRom(p1, p2, p3, p4, lastColumnsDistance / distance);

                float draft = Vector3.Dot(currentPoint.Up, new Vector3(0f, 0f, 1f));
                float groundHeight = terrainSampler.GetMapHeight(columnPoint.X, columnPoint.Y);
                float columnHeight = columnPoint.Z - groundHeight;
                if (draft > 0.3f && columnHeight > MinimumColumnHeight)
                {
                    Matrix topSpace = CreatePointSpaceMatrix(currentPoint.Right, currentPoint.Direction, currentPoint.Up);

                    Vector3 upVector = new(0f, 0f, 1f);
                    Vector3 rightVector = Vector3.Cross(currentPoint.Direction, upVector);
                    if (rightVector.LengthSquared() < 0.0001f)
                    {
                        rightVector = Vector3.Right;
                    }
                    else
                    {
                        rightVector.Normalize();
                    }

                    Matrix bottomSpace = CreatePointSpaceMatrix(rightVector, currentPoint.Direction, upVector);
                    columns.Add(new RaceTrackColumnPlacement(columnPoint, topSpace, bottomSpace, groundHeight));
                }

                lastColumnsDistance += columnsDistance;
            }

            lastColumnsDistance -= distance;
        }

        return columns;
    }

    /// <summary>
    /// TrackColumns.cs:237-243: a column segment at the foot of each column, put on the terrain. The foot is the terrain
    /// height plus ColumnGroundHeight (TrackColumns.cs:195-197), and the segment is placed ColumnGroundHeight below it.
    /// </summary>
    private static void AddColumnSegmentPlacements(
        LegacyLandscapePlacer placer,
        IReadOnlyList<RaceTrackColumnPlacement> columns,
        List<RaceTrackModelPlacement> target)
    {
        for (int index = 0; index < columns.Count; index++)
        {
            Vector3 columnPoint = columns[index].Position;
            Vector3 bottomPosition = new(columnPoint.X, columnPoint.Y, columns[index].GroundHeight + ColumnGroundHeight);
            placer.AddObject(
                ColumnSegmentModelName,
                new Vector3(bottomPosition.X, bottomPosition.Y, bottomPosition.Z - ColumnGroundHeight),
                target);
        }
    }

    /// <summary>
    /// Track.GenerateObjectsForTrack (Track.cs:704-1161) with the objects it adds, in its order, drawing from one random
    /// generator the way the original draws from its one generator, RandomHelper (RandomHelper.cs:23-59, reseeded once per
    /// level): palms and laterns, the start banner and light, the checkpoint banners and the signs, the random scenery.
    /// </summary>
    private static void AddHelperDrivenSceneryPlacements(
        int seed,
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        IReadOnlyList<LegacyRoadHelperPosition> helperPositions,
        LegacyTerrainHeightSampler terrainSampler,
        LegacyLandscapePlacer placer,
        List<RaceTrackModelPlacement> target)
    {
        if (roadPoints.Count == 0)
        {
            return;
        }

        var random = new Random(seed);
        AddPalmAndLaternPlacements(roadPoints, helperPositions, terrainSampler, placer, random, target);
        AddStartPlacements(roadPoints, placer, target);
        AddCheckpointAndSignPlacements(roadPoints, placer, random, target);
        AddRandomSceneryPlacements(roadPoints, terrainSampler, placer, random, target);
    }

    /// <summary>
    /// TrackLine.cs:612-660: the helpers (tunnels, palms, laterns, reset) of the track file become ranges of road points.
    /// A helper is a track file marker that a road point within 25 units reaches, each marker used once. The first marker
    /// opens a range, of any type (a reset included); each later marker closes the open range and, unless it is a reset,
    /// opens the next one.
    /// </summary>
    private static List<LegacyRoadHelperPosition> BuildRoadHelperPositions(
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        IReadOnlyList<LegacyRoadHelper> roadHelpers)
    {
        var helperPositions = new List<LegacyRoadHelperPosition>();
        var remainingHelpers = new List<LegacyRoadHelper>(roadHelpers.Count);
        for (int index = 0; index < roadHelpers.Count; index++)
        {
            remainingHelpers.Add(new LegacyRoadHelper
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
                LegacyRoadHelper roadHelper = remainingHelpers[helperIndex];
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
                else
                {
                    helperStartedNum = pointIndex;
                    currentType = helperType;
                }

                remainingHelpers.RemoveAt(helperIndex);
                break;
            }
        }

        // Still a helper open? Then close it close before the end (TrackLine.cs:657-660).
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

    // Track.cs:704-841: a palm (or a lantern) every PalmAndLaternGap units along the ranges of the palms (or laterns).
    private static void AddPalmAndLaternPlacements(
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        IReadOnlyList<LegacyRoadHelperPosition> helperPositions,
        LegacyTerrainHeightSampler terrainSampler,
        LegacyLandscapePlacer placer,
        Random random,
        List<RaceTrackModelPlacement> target)
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

            RaceTrackRoadPoint point = roadPoints[pointIndex];
            float distance = Vector3.Distance(roadPoints[(pointIndex + 1) % roadPoints.Count].Position, point.Position);
            if (lastGap - distance <= 0.0f)
            {
                // Skip generation here when the road is a looping (Track.cs:743-749)
                if (!IsTracksidePlacementAllowed(point))
                {
                    continue;
                }

                Vector3 right = GetLegacyRawRight(point);
                Matrix pointSpace = CreateLegacyPointSpace(right, point.Direction, point.Up);
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, lastGap / distance);
                generatedNum++;

                if (palms)
                {
                    // Skip the palm if the road is more than 11 m above the ground (Track.cs:775-778)
                    if (objectPoint.Z - terrainSampler.GetMapHeight(objectPoint.X, objectPoint.Y) < 11.0f)
                    {
                        int randomNum = random.Next(4);

                        // Less propability for small palm
                        if (randomNum == 3)
                        {
                            randomNum = random.Next(4);
                        }

                        // The rotation is drawn after the model, as the arguments of the original's call are evaluated
                        placer.AddObject(
                            PalmModelCandidates[randomNum],
                            Matrix.CreateScale(1.25f)
                            * Matrix.CreateRotationZ(NextFloat(random, 0, MathHelper.Pi * 2))
                            * Matrix.CreateTranslation(right * (generatedNum % 2 == 0 ? 0.6f : -0.6f) * point.RoadWidth * LegacyRoadWidthScale)
                            * Matrix.CreateTranslation(new Vector3(0, 0, -50))
                            * Matrix.CreateTranslation(objectPoint),
                            true,
                            target);
                    }
                }
                else
                {
                    placer.AddObject(
                        "Laterne",
                        Matrix.CreateRotationZ(generatedNum % 2 == 0 ? MathHelper.Pi : 0.0f)
                        * Matrix.CreateTranslation(new Vector3(
                            (generatedNum % 2 == 0 ? 0.5f : -0.5f) * point.RoadWidth * LegacyRoadWidthScale - 0.35f,
                            0,
                            -0.2f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        true,
                        target);
                }

                // We have just set a pile, the next pile will be set after reaching the next holder gap.
                lastGap += PalmAndLaternGap;
            }

            lastGap -= distance;
        }
    }

    // Track.cs:842-880: the goal banner and the start light are always added.
    private static void AddStartPlacements(
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        LegacyLandscapePlacer placer,
        List<RaceTrackModelPlacement> target)
    {
        RaceTrackRoadPoint startPoint = roadPoints[0];
        Matrix startPointSpace = CreateLegacyPointSpace(GetLegacyRawRight(startPoint), startPoint.Direction, startPoint.Up);

        placer.AddObject(
            "Banner6",
            Matrix.CreateScale(startPoint.RoadWidth)
            // Scale up 1.1, but compensate the 1.2 done in the add path
            * Matrix.CreateScale(1.051f)
            * Matrix.CreateTranslation(new Vector3(0, -5.1f, 0))
            * startPointSpace
            * Matrix.CreateTranslation(startPoint.Position),
            true,
            target);

        // The original adds StartLight3 and turns it into the red StartLight once the level is loaded
        // (Landscape.cs:1135-1140); RaceStartLight then turns it yellow, then green.
        placer.AddObject(
            StartLightModelNames[0],
            Matrix.CreateScale(1.1f)
            * Matrix.CreateTranslation(new Vector3(startPoint.RoadWidth * LegacyRoadWidthScale * 0.50f - 0.3f, 6, -0.2f))
            * startPointSpace
            * Matrix.CreateTranslation(startPoint.Position),
            true,
            target);
    }

    // Track.cs:882-1086: a banner at each checkpoint gap and, between them, the road signs.
    private static void AddCheckpointAndSignPlacements(
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        LegacyLandscapePlacer placer,
        Random random,
        List<RaceTrackModelPlacement> target)
    {
        float checkpointGap = CheckpointGap;
        float signGap = SignGap;

        // Don't add another one near the end! (Track.cs:893)
        for (int pointIndex = 0; pointIndex < roadPoints.Count - 24; pointIndex++)
        {
            RaceTrackRoadPoint point = roadPoints[pointIndex];
            float distance = Vector3.Distance(roadPoints[(pointIndex + 1) % roadPoints.Count].Position, point.Position);
            if (!IsTracksidePlacementAllowed(point))
            {
                continue;
            }

            Vector3 right = GetLegacyRawRight(point);
            Matrix pointSpace = CreateLegacyPointSpace(right, point.Direction, point.Up);
            if (checkpointGap - distance <= 0.0f)
            {
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, checkpointGap / distance);
                string bannerName = CheckpointBannerCandidates[random.Next(6)];
                placer.AddObject(
                    bannerName,
                    Matrix.CreateScale(point.RoadWidth)
                    * Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, -0.1f))
                    * pointSpace
                    * Matrix.CreateTranslation(objectPoint),
                    true,
                    target);

                checkpointGap += CheckpointGap;
            }
            else if (signGap - distance <= 0.0f && pointIndex >= 25)
            {
                Vector3 objectPoint = InterpolateRoadPoint(roadPoints, pointIndex, signGap / distance);

                // Find out how curvy this point is by going back 25 points.
                Vector3 backPosition = roadPoints[(pointIndex - 25) % roadPoints.Count].Position;
                bool loopingAhead = roadPoints[(pointIndex + 60) % roadPoints.Count].Up.Z < 0.15f;
                Vector3 angleVector = Vector3.Normalize(backPosition - point.Position);
                float roadAngle = GetAngleBetweenVectors(angleVector, Vector3.Normalize(-point.Direction));

                // If road goes to the left, use negative angle value!
                if (Vector3.Distance(right, angleVector) < Vector3.Distance(-right, angleVector))
                {
                    roadAngle = -roadAngle;
                }

                if (loopingAhead)
                {
                    placer.AddObject(
                        "SignWarning",
                        Matrix.CreateTranslation(new Vector3(point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.1f, 0.0f, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        true,
                        target);
                }
                else if (roadAngle < -MathHelper.Pi / 7.5f)
                {
                    placer.AddObject(
                        "SignCurveRight",
                        Matrix.CreateRotationZ(MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(-point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.15f, 0.0f, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        true,
                        target);
                }
                else if (roadAngle > MathHelper.Pi / 7.5f)
                {
                    placer.AddObject(
                        "SignCurveLeft",
                        Matrix.CreateRotationZ(-MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(point.RoadWidth * LegacyRoadWidthScale * 0.5f - 0.15f, 0.0f, -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        true,
                        target);
                }
                else if (roadAngle < -MathHelper.Pi / 10.0f
                    || roadAngle > MathHelper.Pi / 10.0f
                    // Randomly generate sign
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

                    placer.AddObject(
                        randomVariant == 0
                            ? (roadAngle > 0 ? "SignCurveLeft" : "SignCurveRight")
                            : (randomVariant == 1 ? "Sign" : "Sign2"),
                        Matrix.CreateRotationZ((roadAngle > 0 ? -1 : 1) * MathHelper.Pi / 2.0f)
                        * Matrix.CreateTranslation(new Vector3(
                            (roadAngle > 0 ? 1 : -1) * point.RoadWidth * LegacyRoadWidthScale * 0.5f - (randomVariant == 0 ? 0.15f : 0.005f),
                            0,
                            -0.25f))
                        * pointSpace
                        * Matrix.CreateTranslation(objectPoint),
                        true,
                        target);
                }

                // We have just set a sign (or not), check for next sign after gap.
                signGap += SignGap;
            }

            checkpointGap -= distance;
            signGap -= distance;
        }
    }

    // Track.cs:1087-1161: scenery away from the road, drawn from the objects of Landscape.autoGenerationNames. The original's
    // high detail mode is the one the reference scene was exported in (scene.json, highDetail true).
    private static void AddRandomSceneryPlacements(
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        LegacyTerrainHeightSampler terrainSampler,
        LegacyLandscapePlacer placer,
        Random random,
        List<RaceTrackModelPlacement> target)
    {
        string[] names = LegacyLandscapePlacer.AutoGenerationNames;
        for (int pointIndex = 0; pointIndex < roadPoints.Count; pointIndex += 2)
        {
            RaceTrackRoadPoint point = roadPoints[pointIndex];

            // Skip object generation at great heights!
            if (point.Position.Z - terrainSampler.GetMapHeight(point.Position.X, point.Position.Y) > 60.0f)
            {
                continue;
            }

            if (!IsTracksidePlacementAllowed(point))
            {
                continue;
            }

            // Show twice as many objects in high details mode
            const int randomMaxPropability = 5;

            // Generate stuff in 20% of the cases
            if (random.Next(randomMaxPropability) != 0)
            {
                continue;
            }

            // Get random name
            int randomObjNum = random.Next(names.Length);

            // If above 6, generate again
            if (randomObjNum >= 6)
            {
                randomObjNum = random.Next(names.Length);
            }

            // Don't generate so many casinos
            if (randomObjNum == names.Length - 1 && random.Next(3) < 2)
            {
                randomObjNum = random.Next(names.Length);
            }

            // Ok, generate
            float distance = NextFloat(random, 26, 88);

            // For casinos make sure the object is far enough away.
            if (randomObjNum == names.Length - 1)
            {
                distance += 20;
            }

            bool side = random.Next(2) == 0;
            float rotation = NextFloat(random, 0, MathHelper.Pi * 2);
            placer.AddObject(
                names[randomObjNum],
                rotation,
                point.Position,
                GetLegacyRawRight(point),
                distance * (side ? 1 : -1),
                target);
        }
    }

    // The road point's right vector as the original keeps and uses it (TrackLine.cs:462-464 and 490): the cross product of
    // the direction and the up vector, not normalized. The offsets the generation computes from it are those of the original.
    private static Vector3 GetLegacyRawRight(RaceTrackRoadPoint point)
    {
        return Vector3.Cross(point.Direction, point.Up);
    }

    // The point space the generation builds from the three vectors (Track.cs:751-756): right, direction, then up.
    private static Matrix CreateLegacyPointSpace(Vector3 right, Vector3 direction, Vector3 up)
    {
        Matrix pointSpace = Matrix.Identity;
        pointSpace.Right = right;
        pointSpace.Up = direction;
        pointSpace.Forward = -up;
        return pointSpace;
    }

    // Track.cs:743-749: no generation where the road is upside down or a looping goes up or down.
    private static bool IsTracksidePlacementAllowed(RaceTrackRoadPoint point)
    {
        bool upsideDown = point.Up.Z < 0.05f;
        bool movingUp = point.Direction.Z > 0.65f;
        bool movingDown = point.Direction.Z < -0.65f;
        return !upsideDown && !movingUp && !movingDown;
    }

    // Vector3.CatmullRom between the road point and the next one, as the original interpolates its positions (Track.cs:758-766).
    private static Vector3 InterpolateRoadPoint(IReadOnlyList<RaceTrackRoadPoint> roadPoints, int pointIndex, float amount)
    {
        Vector3 p1 = roadPoints[pointIndex - 1 < 0 ? roadPoints.Count - 1 : pointIndex - 1].Position;
        Vector3 p2 = roadPoints[pointIndex].Position;
        Vector3 p3 = roadPoints[(pointIndex + 1) % roadPoints.Count].Position;
        Vector3 p4 = roadPoints[(pointIndex + 2) % roadPoints.Count].Position;
        return Vector3.CatmullRom(p1, p2, p3, p4, amount);
    }

    // RandomHelper.GetRandomFloat (RandomHelper.cs:56-59)
    private static float NextFloat(Random random, float min, float max)
    {
        return (float)random.NextDouble() * (max - min) + min;
    }

    // Vector3Helper.GetAngleBetweenVectors (Vector3Helper.cs:33-40): the dot product of two normalized vectors is not clamped,
    // so one a bit above 1 gives NaN, as in the original, which makes the comparisons that follow false and so changes which
    // random numbers are drawn after.
    private static float GetAngleBetweenVectors(Vector3 first, Vector3 second)
    {
        return (float)Math.Acos(Vector3.Dot(first, second));
    }

    private sealed class LegacyTrackPoint
    {
        public LegacyTrackPoint(Vector3 position)
        {
            Position = position;
        }

        public LegacyTrackPoint(LegacyTrackPoint other)
        {
            Position = other.Position;
            Right = other.Right;
            Up = other.Up;
            Direction = other.Direction;
            TextureU = other.TextureU;
            RoadWidth = other.RoadWidth;
        }

        public Vector3 Position { get; set; }

        public Vector3 Right { get; set; } = Vector3.Right;

        public Vector3 Up { get; set; } = new(0f, 0f, 1f);

        public Vector3 Direction { get; set; } = Vector3.UnitY;

        public float TextureU { get; set; }

        public float RoadWidth { get; set; } = LegacyDefaultRoadWidth;
    }
}
