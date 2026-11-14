using System.Xml.Serialization;
using CasaEngine.Core.Log;
using CasaEngine.Engine;
using CasaEngine.Engine.Primitives3D;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Graphics;
using CasaEngine.Framework.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Bootstrap;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Worlds;

internal static partial class LegacyTrackSceneFactory
{
    private const float WorldScale = 1.0f;
    private static readonly Quaternion LegacyXModelCorrection = Quaternion.CreateFromAxisAngle(Vector3.Right, MathHelper.Pi / 2.0f);
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
    private const float HelperActivationDistance = 25.0f;
    private const float PalmAndLaternGap = 20.0f;
    private const float CheckpointGap = 500.0f;
    private const float SignGap = 24.0f;
    private const float CheckpointTriggerWidthPadding = 1.5f;
    private const float CheckpointTriggerMinHalfWidth = 5.5f;
    private const float CheckpointTriggerHalfHeight = 4.0f;
    private const float CheckpointTriggerHalfDepth = 2.75f;
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
    internal const string StartLightEntityNamePrefix = "Track.Scenery.StartLight.";
    private static readonly string[] StartLightModelNames = ["StartLight", "StartLight2", "StartLight3"];

    private static readonly XmlSerializer TrackSerializer = new(typeof(LegacyTrackLayout));
    private static readonly XmlSerializer CombiSerializer = new(typeof(List<LegacyCombiObject>), new XmlRootAttribute("ArrayOfCombiObject"));
    private static readonly Texture2DLoader TextureLoader = new();
    private static readonly Dictionary<string, LegacyTrackLayout> TrackCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<LegacyCombiObject>> CombiCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, StaticModel?> ModelCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Texture2D?> TextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, TextureCube?> TextureCubeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LegacyTerrainHeightSampler> TerrainCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> ModelAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OilWell"] = "OilPump",
        ["PalmSmall"] = "AlphaPalmSmall",
        ["AlphaPalm4"] = "AlphaPalmSmall",
        ["Palm"] = "AlphaPalm",
        ["Casino"] = "Casino01",
        ["Combi"] = "CombiPalms",
    };
    private static readonly string[] CheckpointBannerCandidates = ["Banner", "Banner2", "Banner3", "Banner4", "Banner5", "Banner6"];
    private static readonly string[] RoadTextureCandidates = ["Road.tga", "track.tga", "RoadCement.tga"];

    internal static RaceTrackScene Create(string trackName, AssetContentManager assetContentManager)
    {
        LegacyTrackLayout layout = LoadTrackLayout(RaceBootstrapAssets.GetTrackDataAssetName(trackName));
        LegacyTerrainHeightSampler terrainSampler = LoadTerrainHeightSampler();
        List<LegacyTrackPoint> roadSplinePoints = BuildRoadSplinePoints(layout, terrainSampler);
        Vector3 origin = ComputeOrigin(layout.TrackPoints);
        BeginSceneDiagnostics();
        RaceTrackPhysicsProfile physicsProfile = CreateTrackPhysicsProfile(roadSplinePoints, origin);

        if (roadSplinePoints.Count < 3)
        {
            throw new InvalidOperationException($"Track '{trackName}' does not contain enough points to build a road.");
        }

        RaceTrackStartPose playerStartPose = CreatePlayerStartPose(roadSplinePoints[0], origin);
        List<RaceCheckpointTriggerDefinition> checkpointTriggers =
        [
            CreateCheckpointTriggerDefinition(roadSplinePoints, 0.20f, origin),
            CreateCheckpointTriggerDefinition(roadSplinePoints, 0.50f, origin),
            CreateCheckpointTriggerDefinition(roadSplinePoints, 0.80f, origin),
        ];

        var trackEntities = new List<Entity>();
        trackEntities.AddRange(LegacyTerrainMeshBuilder.CreateEntities(trackName, origin, assetContentManager));
        trackEntities.Add(CreateRoadEntity(trackName, roadSplinePoints, origin, assetContentManager));

        // The objects are added in the order of the original's Landscape/Track (so that its "skip an object too close
        // to an earlier one" rule drops the same ones): the track file's objects (TrackLine.Load), then the guard rail
        // holders and the column segments (Track.GenerateVerticesAndObjects), then the objects along the road.
        var landscape = new LegacyLandscape(origin, terrainSampler, assetContentManager, CreateSceneryRandom());
        var sceneryEntities = new LegacyEntityGroup(static (modelName, index) => $"Track.Scenery.{modelName}.{index:000}");
        foreach (LegacyNeutralObject neutralObject in layout.NeutralsObjects)
        {
            landscape.AddObjectToRender(neutralObject.modelName, neutralObject.matrix, sceneryEntities);
        }

        trackEntities.AddRange(LegacyTrackGuardRailBuilder.CreateEntities(trackName, roadSplinePoints, origin, terrainSampler, landscape, assetContentManager));
        AddHelperDrivenSceneryEntities(sceneryEntities, roadSplinePoints, layout.RoadHelpers, landscape);

        EndSceneDiagnostics(trackName, roadSplinePoints);

        // After the diagnostics, which record every object the landscape places: the city ground looks for the first
        // big building of the Beginner level with a landscape of its own.
        trackEntities.AddRange(LegacyRoadStructureBuilder.CreateEntities(trackName, roadSplinePoints, layout.RoadHelpers, origin, terrainSampler, assetContentManager));
        return new RaceTrackScene(trackEntities, sceneryEntities.Entities, playerStartPose, checkpointTriggers, physicsProfile);
    }

    /// <summary>
    /// Converts a point given in the original game's coordinates (Z up) to the race world of this track, exactly as
    /// <see cref="Create"/> places the track: same axis mapping, same origin. Used to replay the original's reference
    /// views (ADR-0015).
    /// </summary>
    internal static Vector3 ConvertLegacyPointToRaceWorld(string trackName, Vector3 legacyPoint)
    {
        LegacyTrackLayout layout = LoadTrackLayout(RaceBootstrapAssets.GetTrackDataAssetName(trackName));
        return ConvertLegacyPoint(legacyPoint) - ComputeOrigin(layout.TrackPoints);
    }

    private static LegacyTrackLayout LoadTrackLayout(string assetName)
    {
        if (TrackCache.TryGetValue(assetName, out LegacyTrackLayout? cachedLayout))
        {
            return cachedLayout;
        }

        string fileName = ResolveAssetPath(assetName);
        using var stream = File.OpenRead(fileName);
        if (TrackSerializer.Deserialize(stream) is not LegacyTrackLayout layout)
        {
            throw new InvalidOperationException($"Unable to deserialize track asset '{assetName}'.");
        }

        TrackCache[assetName] = layout;
        return layout;
    }

    private static List<LegacyCombiObject> LoadCombiModel(string combiName)
    {
        if (CombiCache.TryGetValue(combiName, out List<LegacyCombiObject>? cachedObjects))
        {
            return cachedObjects;
        }

        string fileName = Path.Combine(GetProjectContentPath(), "Models", $"{combiName}.CombiModel");
        if (!File.Exists(fileName))
        {
            CombiCache[combiName] = [];
            return CombiCache[combiName];
        }

        using var stream = File.OpenRead(fileName);
        List<LegacyCombiObject> combiObjects = CombiSerializer.Deserialize(stream) as List<LegacyCombiObject> ?? [];
        CombiCache[combiName] = combiObjects;
        return combiObjects;
    }

    private static StaticModel? LoadLegacyModel(string modelName, AssetContentManager assetContentManager)
    {
        modelName = NormalizeLegacyModelName(modelName);

        if (ModelCache.TryGetValue(modelName, out StaticModel? cachedModel))
        {
            return cachedModel;
        }

        string fileName = Path.Combine(GetProjectContentPath(), "Models", $"{modelName}.gltf");
        if (!File.Exists(fileName))
        {
            Logs.WriteWarning($"Legacy track model '{modelName}' not found at '{fileName}'.");
            ModelCache[modelName] = null;
            return null;
        }

        if (!LegacyGltfModelReader.IsFileSupported(fileName))
        {
            Logs.WriteWarning($"Legacy track model '{modelName}' uses an unsupported format for runtime import.");
            ModelCache[modelName] = null;
            return null;
        }

        StaticModelImportResult importResult = LegacyGltfModelReader.ReadWithMetadata(fileName, RacingGameImportProfiles.LegacyMaterialProfile);
        StaticModel model = importResult.Model;
        ApplyLegacyModelRootCorrection(model);
        ApplyImportedMaterials(model, importResult.Materials, modelName, assetContentManager);
        ApplyFallbackMaterials(model, modelName);
        ModelCache[modelName] = model;
        return model;
    }

    private static void ApplyLegacyModelRootCorrection(StaticModel model)
    {
        if (model.RootNode == null)
        {
            return;
        }

        var correctedRoot = new StaticModelNode
        {
            Name = $"{model.Name}.LegacyRootCorrection",
            Rotation = LegacyXModelCorrection,
        };
        correctedRoot.Children.Add(model.RootNode);
        model.RootNode = correctedRoot;
    }

    private static Entity CreateRoadEntity(string trackName, IReadOnlyList<LegacyTrackPoint> roadPoints, Vector3 origin, AssetContentManager assetContentManager)
    {
        var vertices = new VertexPositionNormalTextureTangent[roadPoints.Count * 5];

        for (int index = 0; index < roadPoints.Count; index++)
        {
            LegacyTrackPoint point = roadPoints[index];
            int vertexIndex = index * 5;
            vertices[vertexIndex + 0] = CreateRoadVertex(point, 0.50f, point.RoadWidth, origin);
            vertices[vertexIndex + 1] = CreateRoadVertex(point, 0.25f, point.RoadWidth * 0.75f, origin);
            vertices[vertexIndex + 2] = CreateRoadVertex(point, 0.00f, point.RoadWidth * 0.50f, origin);
            vertices[vertexIndex + 3] = CreateRoadVertex(point, -0.25f, point.RoadWidth * 0.25f, origin);
            vertices[vertexIndex + 4] = CreateRoadVertex(point, -0.50f, 0.0f, origin);
        }

        var indices = new uint[(roadPoints.Count - 1) * 8 * 3];
        int outputIndex = 0;
        int segmentStartVertex = 0;
        for (int segment = 0; segment < roadPoints.Count - 1; segment++)
        {
            for (int side = 0; side < 4; side++)
            {
                indices[outputIndex + 0] = (uint)(segmentStartVertex + side);
                indices[outputIndex + 1] = (uint)(segmentStartVertex + 6 + side);
                indices[outputIndex + 2] = (uint)(segmentStartVertex + 5 + side);
                indices[outputIndex + 3] = (uint)(segmentStartVertex + 6 + side);
                indices[outputIndex + 4] = (uint)(segmentStartVertex + side);
                indices[outputIndex + 5] = (uint)(segmentStartVertex + 1 + side);
                outputIndex += 6;
            }

            segmentStartVertex += 5;
        }

        var roadMesh = new StaticModelMesh { Name = $"{trackName}.Road" };
        roadMesh.SetData(vertices, indices);
        roadMesh.Material = CreateRoadMaterial(trackName, assetContentManager);

        var roadModel = new StaticModel { Name = $"{trackName}.Road" };
        roadModel.Meshes.Add(roadMesh);
        roadModel.RootNode = new StaticModelNode
        {
            Name = "RoadRoot",
            MeshIndex = 0,
        };

        return CreateStaticModelEntity($"Track.Road.{trackName}", roadModel, Matrix.Identity);
    }

    private static VertexPositionNormalTextureTangent CreateRoadVertex(LegacyTrackPoint point, float lateralFactor, float textureV, Vector3 origin)
    {
        Vector3 legacyPosition = point.Position + point.Right * (LegacyRoadWidthScale * point.RoadWidth * lateralFactor);
        Vector3 runtimePosition = ConvertLegacyPoint(legacyPosition) - origin;
        Vector3 runtimeNormal = ConvertLegacyDirection(point.Up);
        // TrackVertex.cs: TangentVertex(position, uv, normal = up, tangent = right).
        Vector3 runtimeTangent = ConvertLegacyDirection(point.Right);
        return new VertexPositionNormalTextureTangent(
            runtimePosition,
            runtimeNormal,
            new Microsoft.Xna.Framework.Vector2(point.TextureU, textureV),
            new Vector4(runtimeTangent, 1f));
    }

    private static RaceTrackStartPose CreatePlayerStartPose(LegacyTrackPoint point, Vector3 origin)
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

    private static RaceCheckpointTriggerDefinition CreateCheckpointTriggerDefinition(IReadOnlyList<LegacyTrackPoint> roadPoints, float progress, Vector3 origin)
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

    private static RaceTrackPhysicsProfile CreateTrackPhysicsProfile(IReadOnlyList<LegacyTrackPoint> roadPoints, Vector3 origin)
    {
        var points = new RaceTrackPhysicsPoint[roadPoints.Count];

        for (int index = 0; index < roadPoints.Count; index++)
        {
            points[index] = CreateRuntimePhysicsPoint(roadPoints[index], origin);
        }

        return new RaceTrackPhysicsProfile(points);
    }

    /// <summary>The start light's red, yellow and green models, loaded once (null for a missing one).</summary>
    internal static StaticModel?[] LoadStartLightModels(AssetContentManager assetContentManager)
    {
        return StartLightModelNames.Select(modelName => LoadLegacyModel(modelName, assetContentManager)).ToArray();
    }

    private static Entity CreateStaticModelEntity(string name, StaticModel model, Matrix transform, bool castShadows = true)
    {
        if (!transform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation))
        {
            scale = Vector3.One;
            rotation = Quaternion.Identity;
            translation = transform.Translation;
        }

        var entity = new Entity
        {
            Name = name,
        };
        entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);

        var component = new StaticModelComponent
        {
            StaticModel = model,
            CastShadows = castShadows,
        };
        component.LocalTransform.Position = translation;
        component.LocalTransform.Orientation = rotation;
        component.LocalTransform.Scale = scale;
        entity.RootComponent = component;
        return entity;
    }

    private static List<LegacyRoadHelperPosition> BuildRoadHelperPositions(
        IReadOnlyList<LegacyTrackPoint> roadPoints,
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
                    // As in the original (TrackLine.GenerateTunnelsAndLandscapeObjects): any helper starts a range,
                    // a Reset one too (a range of type Reset, which nothing uses).
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

    /// <summary>
    /// The original's right vector of a road point: the cross product of its direction and up vectors, never
    /// normalized (TrackLine.Load). The road points of the port keep it normalized, but the objects the original
    /// places along the road take their lateral offset and their scale from the raw vector.
    /// </summary>
    private static Vector3 GetLegacyRawRight(LegacyTrackPoint point)
    {
        return Vector3.Cross(point.Direction, point.Up);
    }

    private static Matrix CreateLegacyPointSpace(LegacyTrackPoint point)
    {
        Matrix pointSpace = Matrix.Identity;
        pointSpace.Right = GetLegacyRawRight(point);
        pointSpace.Up = point.Direction;
        pointSpace.Forward = -point.Up;
        return pointSpace;
    }

    private static bool IsTracksidePlacementAllowed(LegacyTrackPoint point)
    {
        bool upsideDown = point.Up.Z < 0.05f;
        bool movingUp = point.Direction.Z > 0.65f;
        bool movingDown = point.Direction.Z < -0.65f;
        return !upsideDown && !movingUp && !movingDown;
    }

    private static Vector3 InterpolateRoadPoint(IReadOnlyList<LegacyTrackPoint> roadPoints, int pointIndex, float amount)
    {
        Vector3 p1 = roadPoints[pointIndex - 1 < 0 ? roadPoints.Count - 1 : pointIndex - 1].Position;
        Vector3 p2 = roadPoints[pointIndex].Position;
        Vector3 p3 = roadPoints[(pointIndex + 1) % roadPoints.Count].Position;
        Vector3 p4 = roadPoints[(pointIndex + 2) % roadPoints.Count].Position;
        return Vector3.CatmullRom(p1, p2, p3, p4, amount);
    }

    private static float GetNearestWidthScale(IReadOnlyList<LegacyWidthHelper> widthHelpers, Vector3 legacyPoint)
    {
        if (widthHelpers.Count == 0)
        {
            return 1f;
        }

        float bestDistanceSquared = float.MaxValue;
        float scale = 1f;
        foreach (LegacyWidthHelper widthHelper in widthHelpers)
        {
            float distanceSquared = Vector3.DistanceSquared(widthHelper.pos, legacyPoint);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                scale = Math.Clamp(widthHelper.scale, 0.7f, 2.0f);
            }
        }

        return scale;
    }

    private static Vector3 ComputeOrigin(IReadOnlyList<Vector3> trackPoints)
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

    private static Vector3 SampleLoopPoint(IReadOnlyList<Vector3> roadPoints, float progress)
    {
        int index = (int)MathF.Round(progress * (roadPoints.Count - 1)) % roadPoints.Count;
        return roadPoints[Math.Clamp(index, 0, roadPoints.Count - 1)];
    }

    private static LegacyTrackPoint SampleLoopPoint(IReadOnlyList<LegacyTrackPoint> roadPoints, float progress)
    {
        int index = (int)MathF.Round(progress * (roadPoints.Count - 1)) % roadPoints.Count;
        return roadPoints[Math.Clamp(index, 0, roadPoints.Count - 1)];
    }

    private static Matrix ConvertLegacyTransform(Matrix legacyTransform, Vector3 origin)
    {
        Matrix runtimeTransform = Matrix.Identity;
        runtimeTransform.Right = ConvertLegacyVector(legacyTransform.Right);
        runtimeTransform.Up = ConvertLegacyVector(legacyTransform.Up);
        runtimeTransform.Forward = ConvertLegacyVector(legacyTransform.Forward);
        runtimeTransform.Translation = ConvertLegacyPoint(legacyTransform.Translation) - origin;
        return runtimeTransform;
    }

    private static string NormalizeLegacyModelName(string modelName)
    {
        return ModelAliases.TryGetValue(modelName, out string? alias)
            ? alias
            : modelName;
    }

    private static Vector3 ConvertLegacyPoint(Vector3 point)
    {
        return new Vector3(point.X, point.Z, -point.Y) * WorldScale;
    }

    private static Vector3 ConvertLegacyVector(Vector3 vector)
    {
        return new Vector3(vector.X, vector.Z, -vector.Y) * WorldScale;
    }

    private static Vector3 ConvertLegacyDirection(Vector3 direction)
    {
        Vector3 runtimeDirection = ConvertLegacyVector(direction);
        if (runtimeDirection.LengthSquared() < 0.0001f)
        {
            return Vector3.Up;
        }

        runtimeDirection.Normalize();
        return runtimeDirection;
    }

    private static RaceTrackPhysicsPoint CreateRuntimePhysicsPoint(LegacyTrackPoint point, Vector3 origin)
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

    private static LegacyTerrainHeightSampler LoadTerrainHeightSampler()
    {
        string filePath = Path.Combine(GetProjectContentPath(), "LandscapeHeights.data");
        if (TerrainCache.TryGetValue(filePath, out LegacyTerrainHeightSampler? cachedSampler))
        {
            return cachedSampler;
        }

        LegacyTerrainHeightSampler sampler = LegacyTerrainHeightSampler.Load(filePath);
        TerrainCache[filePath] = sampler;
        return sampler;
    }

    // The original lights a pixel as texture * (A + D * N.L) (NormalMapping.fx:216, 402). The race world has one white
    // light of intensity 1 and a flat ambient of 1 (RaceWorldFactory), and the engine renders
    // texture * DiffuseColor * (N.L * light + ambient * MaterialAmbientColor): DiffuseColor = D and
    // MaterialAmbientColor = A / D, per channel, give the same pixel. A channel without diffuse (D = 0) shows no
    // ambient either, because the engine multiplies the ambient by DiffuseColor: it gets 0.
    // Material.DefaultAmbientColor / DefaultDiffuseColor (Core/Graphics/Material.cs:30-32).
    internal const byte LegacyDefaultAmbient = 40;
    internal const byte LegacyDefaultDiffuse = 210;

    /// <summary>DiffuseColor (D) and MaterialAmbientColor (A / D) of a generated material the original builds with ambient A and diffuse D (0..255).</summary>
    /// <param name="baseColor">Tint of the material when its texture is missing; white when it has one.</param>
    private static (Color Diffuse, Vector3 Ambient) CreateLegacyLighting(string materialName, Color baseColor, byte ambient, byte diffuse)
    {
        var diffuseColor = new Color(
            (byte)((baseColor.R * diffuse + 127) / 255),
            (byte)((baseColor.G * diffuse + 127) / 255),
            (byte)((baseColor.B * diffuse + 127) / 255));
        return (diffuseColor, ComputeLegacyAmbient(materialName, new Vector3(ambient / 255f), diffuseColor));
    }

    /// <summary>
    /// The MaterialAmbientColor (A / D per channel) of an imported material: A is the ambient of its legacy effect (the
    /// raw value, not the engine presentation's, which caps it to 1), D its DiffuseColor as the engine will use it.
    /// </summary>
    internal static Vector3 ComputeLegacyImportedAmbient(string modelName, StaticModelImportedMaterial importedMaterial)
    {
        Vector3 ambient = importedMaterial.AmbientColor;
        return ComputeLegacyAmbient($"{modelName}.{importedMaterial.DisplayName}", ambient, importedMaterial.DiffuseColor);
    }

    private static Vector3 ComputeLegacyAmbient(string materialName, Vector3 ambient, Color diffuse)
    {
        Vector3 d = diffuse.ToVector3();
        if ((d.X <= 0f && ambient.X > 0f) || (d.Y <= 0f && ambient.Y > 0f) || (d.Z <= 0f && ambient.Z > 0f))
        {
            Logs.WriteWarning($"Legacy material '{materialName}' has an ambient without diffuse on a channel: the engine cannot render it.");
        }

        return new Vector3(
            d.X > 0f ? ambient.X / d.X : 0f,
            d.Y > 0f ? ambient.Y / d.Y : 0f,
            d.Z > 0f ? ambient.Z / d.Z : 0f);
    }

    private static LitDiffuseMaterial CreateRoadMaterial(string trackName, AssetContentManager assetContentManager)
    {
        Texture2D? roadTexture = LoadProjectTexture(assetContentManager, RoadTextureCandidates);
        string name = $"{trackName}.RoadMaterial";
        (Color diffuse, Vector3 ambient) = CreateLegacyLighting(name, roadTexture != null ? Color.White : new Color(60, 62, 66), LegacyDefaultAmbient, LegacyDefaultDiffuse);
        return new LitDiffuseMaterial
        {
            Name = name,
            BasColor = roadTexture,
            NormalMap = LoadProjectTexture(assetContentManager, "RoadNormal.png"),
            DiffuseColor = diffuse,
            AmbientColor = ambient,
            SpecularColor = new Vector3(0.14f),
            SpecularPower = 18f,
            RasterizerState = RasterizerState.CullCounterClockwise,
            SamplerState = SamplerState.AnisotropicWrap,
        };
    }

    private static void ApplyImportedMaterials(StaticModel model, IReadOnlyList<StaticModelImportedMaterial> importedMaterials, string modelName, AssetContentManager assetContentManager)
    {
        foreach (StaticModelMesh mesh in model.Meshes)
        {
            if (mesh.Material != null)
            {
                continue;
            }

            if (mesh.MaterialIndex < 0 || mesh.MaterialIndex >= importedMaterials.Count)
            {
                continue;
            }

            StaticModelImportedMaterial importedMaterial = importedMaterials[mesh.MaterialIndex];
            mesh.Material = CreateImportedRuntimeMaterial(modelName, importedMaterial, assetContentManager);
        }
    }

    private static LitDiffuseMaterial CreateImportedRuntimeMaterial(string modelName, StaticModelImportedMaterial importedMaterial, AssetContentManager assetContentManager)
    {
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning(modelName, importedMaterial);
        Texture2D? diffuseTexture = LoadTexture(importedMaterial.DiffuseTextureFilePath, assetContentManager);
        Texture2D? normalTexture = LoadTexture(importedMaterial.NormalTextureFilePath, assetContentManager);
        bool useSceneReflectionCube = tuning.EnableReflection
            && RaceSkySystem.ShouldUseSceneReflectionCube(importedMaterial.ReflectionTextureFilePath);
        TextureCube? reflectionCube = null;
        if (tuning.EnableReflection && !useSceneReflectionCube)
        {
            reflectionCube = LoadTextureCube(importedMaterial.ReflectionTextureFilePath, assetContentManager);
            useSceneReflectionCube = reflectionCube == null;
        }

        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        Vector3 specularColor = tuning.ApplySpecularColor(importedMaterial.SpecularColor);
        float specularPower = Math.Clamp(tuning.ApplySpecularPower(importedMaterial.SpecularPower), 2f, 48f);

        return new LitDiffuseMaterial
        {
            Name = $"{modelName}.{importedMaterial.DisplayName}",
            BasColor = diffuseTexture,
            NormalMap = normalTexture,
            ReflectionCube = reflectionCube,
            UseSceneReflectionCube = useSceneReflectionCube,
            DiffuseColor = importedMaterial.DiffuseColor,
            AmbientColor = ComputeLegacyImportedAmbient(modelName, importedMaterial),
            EmissiveColor = presentation.EmissiveColor,
            SpecularColor = specularColor,
            SpecularPower = specularPower,
            SamplerState = SamplerState.AnisotropicWrap,
            Queue = presentation.Queue,
            AlphaCutoff = presentation.AlphaCutoff,
            RasterizerState = presentation.DisableBackfaceCulling ? RasterizerState.CullNone : null,
        };
    }

    private static Texture2D? LoadProjectTexture(AssetContentManager assetContentManager, params string[] fileNames)
    {
        foreach (string fileName in fileNames)
        {
            string texturePath = Path.Combine(GetProjectContentPath(), "Textures", fileName);
            Texture2D? texture = LoadTexture(texturePath, assetContentManager);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Texture2D? LoadTexture(string? texturePath, AssetContentManager assetContentManager)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        string normalizedPath = Path.GetFullPath(texturePath);
        if (TextureCache.TryGetValue(normalizedPath, out Texture2D? cachedTexture))
        {
            return cachedTexture;
        }

        if (!File.Exists(normalizedPath) || !TextureLoader.IsFileSupported(normalizedPath))
        {
            TextureCache[normalizedPath] = null;
            return null;
        }

        try
        {
            var texture = (Texture2D)TextureLoader.LoadAsset(normalizedPath, assetContentManager);
            TextureCache[normalizedPath] = texture;
            return texture;
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            TextureCache[normalizedPath] = null;
            return null;
        }
    }

    private static TextureCube? LoadTextureCube(string? texturePath, AssetContentManager assetContentManager)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        string normalizedPath = Path.GetFullPath(texturePath);
        if (TextureCubeCache.TryGetValue(normalizedPath, out TextureCube? cachedTexture))
        {
            return cachedTexture;
        }

        if (!File.Exists(normalizedPath) || !TextureCubeLoader.IsTextureCubeFile(normalizedPath))
        {
            TextureCubeCache[normalizedPath] = null;
            return null;
        }

        try
        {
            var texture = TextureCubeLoader.LoadTextureCube(normalizedPath, assetContentManager.GraphicsDevice);
            TextureCubeCache[normalizedPath] = texture;
            return texture;
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            TextureCubeCache[normalizedPath] = null;
            return null;
        }
    }

    private static void ApplyFallbackMaterials(StaticModel model, string modelName)
    {
        Color diffuseColor = InferSceneryColor(modelName);
        foreach (StaticModelMesh mesh in model.Meshes)
        {
            if (mesh.Material != null)
            {
                continue;
            }

            mesh.Material = new LitDiffuseMaterial
            {
                DiffuseColor = diffuseColor,
                EmissiveColor = diffuseColor.ToVector3() * 0.015f,
                SpecularColor = new Vector3(0.05f),
                SpecularPower = 4f,
            };
        }
    }

    private static Color InferSceneryColor(string modelName)
    {
        if (modelName.Contains("Palm", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Kaktus", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Tree", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(86, 138, 76);
        }

        if (modelName.Contains("Hotel", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Building", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Casino", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(174, 168, 160);
        }

        if (modelName.Contains("Oil", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Train", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Road", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Sign", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(126, 126, 132);
        }

        if (modelName.Contains("Rock", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Stone", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Ruin", StringComparison.OrdinalIgnoreCase)
            || modelName.Contains("Sand", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(156, 140, 112);
        }

        return new Color(190, 190, 190);
    }

    private static string ResolveAssetPath(string assetName)
    {
        AssetInfo? assetInfo = AssetCatalog.Get(assetName);
        if (assetInfo == null)
        {
            throw new InvalidOperationException($"Asset '{assetName}' is not registered in AssetInfos.json.");
        }

        return Path.Combine(GetProjectContentPath(), assetInfo.FileName.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string GetProjectContentPath()
    {
        return !string.IsNullOrWhiteSpace(EngineEnvironment.ProjectPath)
            ? EngineEnvironment.ProjectPath
            : throw new InvalidOperationException("EngineEnvironment.ProjectPath must be configured before loading race content.");
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

    private sealed class LegacyTerrainHeightSampler
    {
        private const int GridWidth = 257;
        private const int GridHeight = 257;
        private const float MapWidthFactor = 10f;
        private const float MapHeightFactor = 10f;
        private const float MapZScale = 300f;

        private readonly float[,] _mapHeights;

        private LegacyTerrainHeightSampler(float[,] mapHeights)
        {
            _mapHeights = mapHeights;
        }

        public static LegacyTerrainHeightSampler Load(string filePath)
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
                    float heightPercent = heights[index] / 255f;
                    mapHeights[x, y] = heightPercent * MapZScale;
                }
            }

            return new LegacyTerrainHeightSampler(mapHeights);
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
}

internal sealed record RaceTrackScene(
    IReadOnlyList<Entity> TrackEntities,
    IReadOnlyList<Entity> SceneryEntities,
    RaceTrackStartPose PlayerStartPose,
    IReadOnlyList<RaceCheckpointTriggerDefinition> CheckpointTriggers,
    RaceTrackPhysicsProfile PhysicsProfile);

internal sealed record RaceCheckpointTriggerDefinition(
    Vector3 Position,
    Quaternion Orientation,
    float HalfWidth,
    float HalfHeight,
    float HalfDepth);

internal sealed record RaceTrackStartPose(
    Vector3 Position,
    Quaternion Orientation,
    Vector3 Forward,
    Vector3 Up);

[XmlRoot("TrackData")]
public sealed class LegacyTrackLayout
{
    public List<Vector3> TrackPoints { get; set; } = [];
    [XmlArrayItem("WidthHelper")]
    public List<LegacyWidthHelper> WidthHelpers { get; set; } = [];
    [XmlArrayItem("RoadHelper")]
    public List<LegacyRoadHelper> RoadHelpers { get; set; } = [];
    [XmlArray("NeutralsObjects")]
    [XmlArrayItem("NeutralObject")]
    public List<LegacyNeutralObject> NeutralsObjects { get; set; } = [];
}

public sealed class LegacyWidthHelper
{
    public Vector3 pos;
    public float scale;
}

public sealed class LegacyRoadHelper
{
    public string type = string.Empty;
    public Vector3 pos;
}

internal enum LegacyRoadHelperType
{
    Tunnel,
    Palms,
    Laterns,
    Reset,
}

internal sealed record LegacyRoadHelperPosition(LegacyRoadHelperType Type, int StartIndex, int EndIndex);

public sealed class LegacyNeutralObject
{
    public string modelName = string.Empty;
    public Matrix matrix;
}

[XmlType("CombiObject")]
public sealed class LegacyCombiObject
{
    public string modelName = string.Empty;
    public Matrix matrix;
}
