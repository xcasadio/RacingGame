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
    private static readonly Dictionary<string, float> ModelSizeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LegacyTerrainHeightSampler> TerrainCache = new(StringComparer.OrdinalIgnoreCase);

    internal static RaceTrackScene Create(string trackName, AssetContentManager assetContentManager)
    {
        Guid trackAssetId = GetCatalogAssetId(RaceBootstrapAssets.GetTrackDataAssetName(trackName));
        Guid heightmapAssetId = GetCatalogAssetId(RaceBootstrapAssets.LandscapeHeightsData);
        RaceTrackSource source = ResolveTrackSource(trackAssetId, heightmapAssetId);
        RaceTrackLayout layout = RaceTrackLayout.Compute(
            trackName,
            source.Track,
            source.Heights,
            LoadCombiModel,
            modelName => GetLegacyModelSize(modelName, assetContentManager));

        // The track's geometry is rebuilt by generator components from the same catalogued assets; the layout above
        // only drives what stays assembled here: the scenery, the checkpoints and the start.
        var trackEntities = new List<Entity>
        {
            CreateGeneratorEntity($"Track.Ground.{trackName}", new RaceTerrainGeneratorComponent(), trackAssetId, heightmapAssetId),
            CreateGeneratorEntity($"Track.Road.{trackName}", new RaceRoadGeneratorComponent(), trackAssetId, heightmapAssetId),
        };
        if (layout.HasGuardRails)
        {
            trackEntities.Add(CreateGeneratorEntity($"Track.GuardRail.Left.{trackName}", new RaceGuardRailGeneratorComponent { Side = RaceGuardRailSide.Left }, trackAssetId, heightmapAssetId));
            trackEntities.Add(CreateGeneratorEntity($"Track.GuardRail.Right.{trackName}", new RaceGuardRailGeneratorComponent { Side = RaceGuardRailSide.Right }, trackAssetId, heightmapAssetId));
        }

        if (layout.Columns.Count > 0)
        {
            trackEntities.Add(CreateGeneratorEntity($"Track.Columns.{trackName}", new RaceColumnGeneratorComponent(), trackAssetId, heightmapAssetId));
        }

        var sceneryEntities = new List<Entity>();
        for (int index = 0; index < layout.Scenery.Count; index++)
        {
            RaceTrackModelPlacement placement = layout.Scenery[index];
            if (!TryGetStaticModelAssetId(placement.ModelName, out Guid modelAssetId))
            {
                continue;
            }

            sceneryEntities.Add(CreateStaticModelEntity($"Track.Scenery.{placement.ModelName}.{index:000}", modelAssetId, placement.Transform));
        }

        return new RaceTrackScene(trackEntities, sceneryEntities, layout.PlayerStartPose, layout.CheckpointTriggers, trackAssetId, heightmapAssetId);
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
        var vertices = new Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture[roadPoints.Count * 5];

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

    private static Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture CreateRoadVertex(RaceTrackRoadPoint point, float lateralFactor, float textureV, Vector3 origin)
    {
        Vector3 legacyPosition = point.Position + point.Right * (LegacyRoadWidthScale * point.RoadWidth * lateralFactor);
        Vector3 runtimePosition = ConvertLegacyPoint(legacyPosition) - origin;
        Vector3 runtimeNormal = ConvertLegacyDirection(point.Up);
        return new Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture(
            runtimePosition,
            runtimeNormal,
            new Microsoft.Xna.Framework.Vector2(point.TextureU, textureV));
    }

    private static Entity CreateStaticModelEntity(string name, Guid staticModelAssetId, Matrix transform)
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
            StaticModelAssetId = staticModelAssetId,
            CastShadows = true,
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

    private static float ComputeLegacyModelSize(StaticModel model)
    {
        if (model.Meshes.Count == 0)
        {
            return 1f;
        }

        IReadOnlyList<VertexPositionNormalTexture> vertices = model.Meshes[0].GetVertices();
        if (vertices.Count == 0)
        {
            return 1f;
        }

        var points = new Vector3[vertices.Count];
        for (int index = 0; index < vertices.Count; index++)
        {
            points[index] = vertices[index].Position;
        }

        BoundingSphere boundingSphere = BoundingSphere.CreateFromPoints(points);
        float scale = 1f;
        if (model.RootNode != null && TryGetMeshAbsoluteTransform(model.RootNode, Matrix.Identity, 0, out Matrix absoluteTransform))
        {
            scale = Math.Max(
                absoluteTransform.Right.Length(),
                Math.Max(
                    absoluteTransform.Up.Length(),
                    absoluteTransform.Backward.Length()));
        }

        float size = boundingSphere.Radius * scale;
        return size > 0.0001f ? size : 1f;
    }

    private static bool TryGetMeshAbsoluteTransform(StaticModelNode node, Matrix parentTransform, int meshIndex, out Matrix absoluteTransform)
    {
        absoluteTransform = node.LocalTransform * parentTransform;
        if (node.MeshIndex == meshIndex)
        {
            return true;
        }

        foreach (StaticModelNode child in node.Children)
        {
            if (TryGetMeshAbsoluteTransform(child, absoluteTransform, meshIndex, out Matrix childTransform))
            {
                absoluteTransform = childTransform;
                return true;
            }
        }

        absoluteTransform = Matrix.Identity;
        return false;
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