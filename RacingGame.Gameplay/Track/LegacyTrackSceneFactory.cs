using System.Xml.Serialization;
using CasaEngine.Engine;
using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static RacingGame.Gameplay.Track.RaceTrackSpace;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Track;

internal static partial class LegacyTrackSceneFactory
{
    // The start light's models: red, yellow and green (RacingGame.Shared/Landscapes/TrackObjectManager.cs).
    internal const string StartLightEntityNamePrefix = "Track.Scenery.StartLight.";

    private static readonly Dictionary<string, LegacyTrackLayout> TrackCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<LegacyCombiObject>> CombiCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LegacyTerrainHeightSampler> TerrainCache = new(StringComparer.OrdinalIgnoreCase);

    internal static RaceTrackScene Create(string trackName, AssetContentManager assetContentManager)
    {
        Guid trackAssetId = GetCatalogAssetId(RaceBootstrapAssets.GetTrackDataAssetName(trackName));
        Guid heightmapAssetId = GetCatalogAssetId(RaceBootstrapAssets.LandscapeHeightsData);
        RaceTrackSource source = ResolveTrackSource(trackAssetId, heightmapAssetId);
        RaceTrackLayout layout = ComputeRaceLayout(trackName, source);
        Logs.WriteInfo($"Track placements {trackName}: {layout.Placements.Count} placement(s) ({layout.LeftPosts.Count} left posts, {layout.RightPosts.Count} right posts, {layout.ColumnSegments.Count} column segments, {layout.Scenery.Count} scenery), tunnels {layout.TunnelSpans.Count}, digest {layout.ComputePlacementDigest()}");

        // The track's geometry is rebuilt by generator components from the same catalogued assets; the layout above
        // only drives what stays assembled here: the scenery, the checkpoints and the start.
        var trackEntities = new List<Entity>
        {
            CreateGeneratorEntity($"Track.Ground.{trackName}", new RaceTerrainGeneratorComponent { Name = nameof(RaceTerrainGeneratorComponent) }, trackAssetId, heightmapAssetId),
            CreateGeneratorEntity($"Track.Road.{trackName}", new RaceRoadGeneratorComponent { Name = nameof(RaceRoadGeneratorComponent) }, trackAssetId, heightmapAssetId),
        };
        if (layout.HasGuardRails)
        {
            trackEntities.Add(CreateGeneratorEntity($"Track.GuardRail.Left.{trackName}", new RaceGuardRailGeneratorComponent { Name = nameof(RaceGuardRailGeneratorComponent), Side = RaceGuardRailSide.Left }, trackAssetId, heightmapAssetId));
            trackEntities.Add(CreateGeneratorEntity($"Track.GuardRail.Right.{trackName}", new RaceGuardRailGeneratorComponent { Name = nameof(RaceGuardRailGeneratorComponent), Side = RaceGuardRailSide.Right }, trackAssetId, heightmapAssetId));
        }

        if (layout.Columns.Count > 0)
        {
            trackEntities.Add(CreateGeneratorEntity($"Track.Columns.{trackName}", new RaceColumnGeneratorComponent { Name = nameof(RaceColumnGeneratorComponent) }, trackAssetId, heightmapAssetId));
        }

        var sceneryEntities = new List<Entity>();
        for (int index = 0; index < layout.Scenery.Count; index++)
        {
            RaceTrackModelPlacement placement = layout.Scenery[index];
            if (!TryGetStaticModelAssetId(placement.ModelName, out Guid modelAssetId))
            {
                continue;
            }

            sceneryEntities.Add(CreateStaticModelEntity($"Track.Scenery.{placement.ModelName}.{index:000}", modelAssetId, placement.Transform, placement.CastsShadow, placement.ReceivesShadow));
        }

        return new RaceTrackScene(trackEntities, sceneryEntities, layout.PlayerStartPose, layout.CheckpointTriggers, trackAssetId, heightmapAssetId);
    }

    /// <summary>
    /// The layout <see cref="Create"/> builds the scenery, the checkpoints and the start from: every placement, with the
    /// scenery. Also read by the reference capture, which measures what the race world holds against it.
    /// </summary>
    internal static RaceTrackLayout ComputeRaceLayout(string trackName, RaceTrackSource source)
    {
        return RaceTrackLayout.Compute(
            trackName,
            source.Track,
            source.Heights,
            LoadCombiModel,
            RaceTrackLayout.ScenerySeed);
    }

    private static Entity CreateGeneratorEntity(string name, RaceTrackGeneratorComponent generator, Guid trackAssetId, Guid heightmapAssetId)
    {
        generator.TrackAssetId = trackAssetId;
        generator.HeightmapAssetId = heightmapAssetId;

        var entity = new Entity
        {
            Name = name,
        };
        entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);
        entity.RootComponent = generator;
        return entity;
    }

    private static Guid GetCatalogAssetId(string assetName)
    {
        AssetInfo? assetInfo = AssetCatalog.Get(assetName);
        return assetInfo?.Id ?? throw new InvalidOperationException($"Asset '{assetName}' is not registered in AssetInfos.json.");
    }

    private static LegacyTrackLayout LoadTrackLayout(string assetName, string fileName)
    {
        if (TrackCache.TryGetValue(assetName, out LegacyTrackLayout? cachedLayout))
        {
            return cachedLayout;
        }

        using var stream = File.OpenRead(fileName);
        LegacyTrackLayout layout = LegacyTrackFileReader.ReadTrack(stream);
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
        List<LegacyCombiObject> combiObjects = LegacyTrackFileReader.ReadCombiObjects(stream);
        CombiCache[combiName] = combiObjects;
        return combiObjects;
    }

    internal static RaceGeneratedModel CreateRoadModel(string trackName, IReadOnlyList<RaceTrackRoadPoint> roadPoints, Vector3 origin, Guid materialAssetId)
    {
        var vertices = new VertexPositionNormalTextureTangent[roadPoints.Count * 5];

        for (int index = 0; index < roadPoints.Count; index++)
        {
            RaceTrackRoadPoint point = roadPoints[index];
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
        roadMesh.MaterialAssetId = materialAssetId;

        var roadModel = new StaticModel { Name = $"{trackName}.Road" };
        roadModel.Meshes.Add(roadMesh);
        roadModel.RootNode = new StaticModelNode
        {
            Name = "RoadRoot",
            MeshIndex = 0,
        };

        return new RaceGeneratedModel($"{trackName}.Road", roadModel, Matrix.Identity, CastShadows: true, OwnsBuffers: true);
    }

    // RacingGame's road underside (TrackVertex.RoadThickness, Track.RoadBackHullTextureWidthFactor and RoadBackSideTextureHeight).
    private const float LegacyRoadThickness = 1.0f;
    private const float LegacyRoadBackHullTextureWidthFactor = 1.0f;
    private const float LegacyRoadBackSideTextureHeight = 0.135f;

    /// <summary>
    /// The underside of the road, built as RacingGame builds it (Track.cs, "We need 4 vertices per cross-section edge of the road
    /// back hull"). Each road point gives 4 vertices: the left road edge, the left edge lowered by the road thickness times the
    /// road width, the lowered right edge and the right road edge, with U scaled by 1 and V at 0, 0.135, 0.865 and 1. The upper
    /// vertices are those of the road mesh (same edge positions, normal up, tangent right), the lower ones have the normal and the
    /// tangent negated. Each segment has 6 triangles, in the pattern of the tunnel, drawn with the culling of the road. The
    /// original neither casts nor receives a shadow on it (Track.GenerateShadow: "ignore the road back").
    /// </summary>
    internal static RaceGeneratedModel CreateRoadBackModel(string trackName, IReadOnlyList<RaceTrackRoadPoint> roadPoints, Vector3 origin, Guid materialAssetId)
    {
        var vertices = new VertexPositionNormalTextureTangent[roadPoints.Count * 4];
        for (int index = 0; index < roadPoints.Count; index++)
        {
            RaceTrackRoadPoint point = roadPoints[index];
            float textureU = point.TextureU * LegacyRoadBackHullTextureWidthFactor;
            int vertexIndex = index * 4;
            vertices[vertexIndex + 0] = CreateRoadBackVertex(point, -0.5f, false, new Vector2(textureU, 0f), origin);
            vertices[vertexIndex + 1] = CreateRoadBackVertex(point, -0.5f, true, new Vector2(textureU, LegacyRoadBackSideTextureHeight), origin);
            vertices[vertexIndex + 2] = CreateRoadBackVertex(point, 0.5f, true, new Vector2(textureU, 1f - LegacyRoadBackSideTextureHeight), origin);
            vertices[vertexIndex + 3] = CreateRoadBackVertex(point, 0.5f, false, new Vector2(textureU, 1f), origin);
        }

        // Track.cs: 6 polygons per segment, 3 sides of 2; the last point is not duplicated here, so there are Count - 1 segments.
        var indices = new uint[(roadPoints.Count - 1) * 18];
        int outputIndex = 0;
        int segmentStartVertex = 0;
        for (int segment = 0; segment < roadPoints.Count - 1; segment++)
        {
            for (int side = 0; side < 3; side++)
            {
                indices[outputIndex + 0] = (uint)(segmentStartVertex + side);
                indices[outputIndex + 1] = (uint)(segmentStartVertex + 5 + side);
                indices[outputIndex + 2] = (uint)(segmentStartVertex + 4 + side);
                indices[outputIndex + 3] = (uint)(segmentStartVertex + 5 + side);
                indices[outputIndex + 4] = (uint)(segmentStartVertex + side);
                indices[outputIndex + 5] = (uint)(segmentStartVertex + 1 + side);
                outputIndex += 6;
            }

            segmentStartVertex += 4;
        }

        var roadBackMesh = new StaticModelMesh { Name = $"{trackName}.RoadBack" };
        roadBackMesh.SetData(vertices, indices);
        roadBackMesh.MaterialAssetId = materialAssetId;

        var roadBackModel = new StaticModel { Name = $"{trackName}.RoadBack" };
        roadBackModel.Meshes.Add(roadBackMesh);
        roadBackModel.RootNode = new StaticModelNode
        {
            Name = "RoadBackRoot",
            MeshIndex = 0,
        };

        return new RaceGeneratedModel($"{trackName}.RoadBack", roadBackModel, Matrix.Identity, CastShadows: false, OwnsBuffers: true, ReceiveShadows: false);
    }

    // TrackVertex.cs: LeftTangentVertex and RightTangentVertex (normal up, tangent right) and BottomLeftSideTangentVertex and
    // BottomRightSideTangentVertex (the edge moved down by up * RoadThickness * roadWidth, normal -up, tangent -right). The top edges
    // are the edges of the road mesh (CreateRoadVertex), so that the two meshes touch.
    private static VertexPositionNormalTextureTangent CreateRoadBackVertex(RaceTrackRoadPoint point, float lateralFactor, bool bottom, Vector2 textureCoordinate, Vector3 origin)
    {
        Vector3 legacyPosition = point.Position + point.Right * (LegacyRoadWidthScale * point.RoadWidth * lateralFactor);
        if (bottom)
        {
            legacyPosition -= point.Up * (LegacyRoadThickness * point.RoadWidth);
        }

        Vector3 runtimePosition = ConvertLegacyPoint(legacyPosition) - origin;
        Vector3 runtimeNormal = ConvertLegacyDirection(point.Up);
        Vector3 runtimeTangent = ConvertLegacyDirection(point.Right);
        if (bottom)
        {
            runtimeNormal = -runtimeNormal;
            runtimeTangent = -runtimeTangent;
        }

        return new VertexPositionNormalTextureTangent(runtimePosition, runtimeNormal, textureCoordinate, new Vector4(runtimeTangent, 1f));
    }

    // RacingGame's road tunnel (TrackVertex.RoadTunnelHeight, Track.RoadTunnelTextureWidthFactor and RoadTunnelSideTextureHeight).
    private const float LegacyRoadTunnelHeight = 7.125f;
    private const float LegacyRoadTunnelTextureWidthFactor = 0.25f;
    private const float LegacyRoadTunnelSideTextureHeight = 0.235f;

    /// <summary>
    /// The road tunnels, built as RacingGame builds them (Track.cs, "Generate the road tunnel vertices"). Each road point
    /// of a span gives 4 vertices: the left road edge, the same edge raised by the tunnel height, the raised right edge
    /// and the right road edge, with U scaled by 0.25 and V at 0, 0.235, 0.765 and 1. Every normal points down (the
    /// original flips the two lower ones). Each segment has 6 triangles, drawn without culling by the material.
    /// Returns null when the track has no tunnel.
    /// </summary>
    internal static RaceGeneratedModel? CreateTunnelModel(
        string trackName,
        IReadOnlyList<RaceTrackRoadPoint> roadPoints,
        IReadOnlyList<RaceTrackTunnelSpan> tunnelSpans,
        Vector3 origin,
        Guid materialAssetId)
    {
        if (tunnelSpans.Count == 0)
        {
            return null;
        }

        int vertexCount = 0;
        int segmentCount = 0;
        foreach (RaceTrackTunnelSpan span in tunnelSpans)
        {
            vertexCount += (span.EndIndex - span.StartIndex + 1) * 4;
            segmentCount += span.EndIndex - span.StartIndex;
        }

        var vertices = new VertexPositionNormalTextureTangent[vertexCount];
        var indices = new uint[segmentCount * 18];
        int vertexIndex = 0;
        int outputIndex = 0;
        foreach (RaceTrackTunnelSpan span in tunnelSpans)
        {
            int spanFirstVertex = vertexIndex;
            for (int pointIndex = span.StartIndex; pointIndex <= span.EndIndex; pointIndex++)
            {
                RaceTrackRoadPoint point = roadPoints[pointIndex];
                float textureU = point.TextureU * LegacyRoadTunnelTextureWidthFactor;
                vertices[vertexIndex + 0] = CreateTunnelVertex(point, -0.5f, 0f, new Vector2(textureU, 0f), origin);
                vertices[vertexIndex + 1] = CreateTunnelVertex(point, -0.5f, LegacyRoadTunnelHeight, new Vector2(textureU, LegacyRoadTunnelSideTextureHeight), origin);
                vertices[vertexIndex + 2] = CreateTunnelVertex(point, 0.5f, LegacyRoadTunnelHeight, new Vector2(textureU, 1f - LegacyRoadTunnelSideTextureHeight), origin);
                vertices[vertexIndex + 3] = CreateTunnelVertex(point, 0.5f, 0f, new Vector2(textureU, 1f), origin);
                vertexIndex += 4;
            }

            for (int segment = 0; segment < span.EndIndex - span.StartIndex; segment++)
            {
                int segmentFirstVertex = spanFirstVertex + segment * 4;
                for (int side = 0; side < 3; side++)
                {
                    indices[outputIndex + 0] = (uint)(segmentFirstVertex + side);
                    indices[outputIndex + 1] = (uint)(segmentFirstVertex + 5 + side);
                    indices[outputIndex + 2] = (uint)(segmentFirstVertex + 4 + side);
                    indices[outputIndex + 3] = (uint)(segmentFirstVertex + 5 + side);
                    indices[outputIndex + 4] = (uint)(segmentFirstVertex + side);
                    indices[outputIndex + 5] = (uint)(segmentFirstVertex + 1 + side);
                    outputIndex += 6;
                }
            }
        }

        var tunnelMesh = new StaticModelMesh { Name = $"{trackName}.Tunnel" };
        tunnelMesh.SetData(vertices, indices);
        tunnelMesh.MaterialAssetId = materialAssetId;

        var tunnelModel = new StaticModel { Name = $"{trackName}.Tunnel" };
        tunnelModel.Meshes.Add(tunnelMesh);
        tunnelModel.RootNode = new StaticModelNode
        {
            Name = "TunnelRoot",
            MeshIndex = 0,
        };

        return new RaceGeneratedModel($"{trackName}.Tunnel", tunnelModel, Matrix.Identity, CastShadows: true, OwnsBuffers: true);
    }

    private static VertexPositionNormalTextureTangent CreateTunnelVertex(RaceTrackRoadPoint point, float lateralFactor, float height, Vector2 textureCoordinate, Vector3 origin)
    {
        Vector3 legacyPosition = point.Position
            + point.Right * (LegacyRoadWidthScale * point.RoadWidth * lateralFactor)
            + point.Up * height;
        Vector3 runtimePosition = ConvertLegacyPoint(legacyPosition) - origin;
        Vector3 runtimeNormal = -ConvertLegacyDirection(point.Up);
        // Track.cs:515-549 and TrackVertex.cs:203-231: the two lower vertices are LeftTangentVertex and RightTangentVertex (normal
        // up, tangent right) with normal and tangent negated, the two upper ones TunnelTop...SideTangentVertex (normal -up, tangent
        // -right): the four vertices of a tunnel point have the normal -up and the tangent -right.
        Vector3 runtimeTangent = -ConvertLegacyDirection(point.Right);
        return new VertexPositionNormalTextureTangent(runtimePosition, runtimeNormal, textureCoordinate, new Vector4(runtimeTangent, 1f));
    }

    private static VertexPositionNormalTextureTangent CreateRoadVertex(RaceTrackRoadPoint point, float lateralFactor, float textureV, Vector3 origin)
    {
        Vector3 legacyPosition = point.Position + point.Right * (LegacyRoadWidthScale * point.RoadWidth * lateralFactor);
        Vector3 runtimePosition = ConvertLegacyPoint(legacyPosition) - origin;
        Vector3 runtimeNormal = ConvertLegacyDirection(point.Up);
        // Track.cs, TrackVertex.cs: TangentVertex(position, uv, normal = up, tangent = right). With the tangent, the engine
        // enables the normal map of the road material.
        Vector3 runtimeTangent = ConvertLegacyDirection(point.Right);
        return new VertexPositionNormalTextureTangent(
            runtimePosition,
            runtimeNormal,
            new Microsoft.Xna.Framework.Vector2(point.TextureU, textureV),
            new Vector4(runtimeTangent, 1f));
    }

    private static Entity CreateStaticModelEntity(string name, Guid staticModelAssetId, Matrix transform, bool castShadows, bool receiveShadows)
    {
        if (!TryDecomposePlacement(transform, out Vector3 scale, out Quaternion rotation, out Vector3 translation))
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
            Name = nameof(StaticModelComponent),
            StaticModelAssetId = staticModelAssetId,
            CastShadows = castShadows,
            ReceiveShadows = receiveShadows,
        };
        component.LocalTransform.Position = translation;
        component.LocalTransform.Orientation = rotation;
        component.LocalTransform.Scale = scale;
        entity.RootComponent = component;
        return entity;
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

    private static Vector3 SampleLoopPoint(IReadOnlyList<Vector3> roadPoints, float progress)
    {
        int index = (int)MathF.Round(progress * (roadPoints.Count - 1)) % roadPoints.Count;
        return roadPoints[Math.Clamp(index, 0, roadPoints.Count - 1)];
    }

    private static LegacyTerrainHeightSampler LoadTerrainHeightSampler(string filePath)
    {
        if (TerrainCache.TryGetValue(filePath, out LegacyTerrainHeightSampler? cachedSampler))
        {
            return cachedSampler;
        }

        LegacyTerrainHeightSampler sampler = LegacyTerrainHeightSampler.Load(filePath);
        TerrainCache[filePath] = sampler;
        return sampler;
    }

    private static string ResolveAssetFilePath(AssetInfo assetInfo)
    {
        return Path.Combine(GetProjectContentPath(), assetInfo.FileName.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string GetProjectContentPath()
    {
        return !string.IsNullOrWhiteSpace(EngineEnvironment.ProjectPath)
            ? EngineEnvironment.ProjectPath
            : throw new InvalidOperationException("EngineEnvironment.ProjectPath must be configured before loading race content.");
    }

}

internal sealed record RaceTrackScene(
    IReadOnlyList<Entity> TrackEntities,
    IReadOnlyList<Entity> SceneryEntities,
    RaceTrackStartPose PlayerStartPose,
    IReadOnlyList<RaceCheckpointTriggerDefinition> CheckpointTriggers,
    Guid TrackAssetId,
    Guid HeightmapAssetId);

[XmlRoot("TrackData")]
public sealed class LegacyTrackLayout
{
    public List<Vector3> TrackPoints { get; set; } = [];
    public List<LegacyWidthHelper> WidthHelpers { get; set; } = [];
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

public sealed class LegacyCombiObject
{
    public string modelName = string.Empty;
    public Matrix matrix;
}