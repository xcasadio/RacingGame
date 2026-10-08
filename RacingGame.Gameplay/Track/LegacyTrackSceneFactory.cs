using System.Xml.Serialization;
using CasaEngine.Engine;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Assets.Loaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using static RacingGame.Gameplay.Track.RaceTrackSpace;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Import;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Track;

internal static partial class LegacyTrackSceneFactory
{
    private const float GroundMargin = 18f;
    private static readonly Quaternion LegacyXModelCorrection = Quaternion.CreateFromAxisAngle(Vector3.Right, MathHelper.Pi / 2.0f);

    // The start light's models: red, yellow and green (RacingGame.Shared/Landscapes/TrackObjectManager.cs).
    internal const string StartLightEntityNamePrefix = "Track.Scenery.StartLight.";

    private static readonly Texture2DLoader TextureLoader = new();
    private static readonly Dictionary<string, LegacyTrackLayout> TrackCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<LegacyCombiObject>> CombiCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, StaticModel?> ModelCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, float> ModelSizeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Texture2D?> TextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, TextureCube?> TextureCubeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LegacyTerrainHeightSampler> TerrainCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] RoadTextureCandidates = ["Road.png", "track.png", "RoadCement.png"];
    private static readonly string[] GroundTextureCandidates = ["Landscape.png", "CityGround.png"];

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
            modelName => LoadLegacyModel(modelName, assetContentManager) != null ? GetLegacyModelSize(modelName) : (float?)null);

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
            StaticModel? staticModel = LoadLegacyModel(placement.ModelName, assetContentManager);
            if (staticModel == null)
            {
                continue;
            }

            sceneryEntities.Add(CreateStaticModelEntity($"Track.Scenery.{placement.ModelName}.{index:000}", staticModel, placement.Transform));
        }

        LoadStartLightModels(assetContentManager);

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

    private static StaticModel? LoadLegacyModel(string modelName, AssetContentManager assetContentManager)
    {
        modelName = RaceTrackLayout.NormalizeLegacyModelName(modelName);

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
        ModelSizeCache[modelName] = ComputeLegacyModelSize(model);
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

    internal static RaceGeneratedModel CreateRoadModel(string trackName, IReadOnlyList<RaceTrackRoadPoint> roadPoints, Vector3 origin, AssetContentManager assetContentManager)
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
        roadMesh.Material = CreateRoadMaterial(trackName, assetContentManager);

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

    private static Entity CreateGroundEntity(string trackName, IReadOnlyList<Vector3> roadPoints, AssetContentManager assetContentManager)
    {
        float minX = roadPoints.Min(static point => point.X) - GroundMargin;
        float maxX = roadPoints.Max(static point => point.X) + GroundMargin;
        float minZ = roadPoints.Min(static point => point.Z) - GroundMargin;
        float maxZ = roadPoints.Max(static point => point.Z) + GroundMargin;
        float minY = roadPoints.Min(static point => point.Y) - 0.8f;
        float width = Math.Max(10f, maxX - minX);
        float depth = Math.Max(10f, maxZ - minZ);

        StaticModel groundModel = StaticModel.CreateFromPrimitive(new BoxPrimitive(width, 1.0f, depth), $"{trackName}.Ground");
        groundModel.Meshes[0].Material = CreateGroundMaterial(trackName, assetContentManager);

        Matrix transform = Matrix.CreateTranslation(new Vector3((minX + maxX) * 0.5f, minY, (minZ + maxZ) * 0.5f));
        // As in RacingGame, the ground only receives shadows (Landscape.GenerateShadow).
        return CreateStaticModelEntity($"Track.Ground.{trackName}", groundModel, transform, castShadows: false);
    }

    /// <summary>The start light's red, yellow and green models, loaded once (null for a missing one).</summary>
    internal static StaticModel?[] LoadStartLightModels(AssetContentManager assetContentManager)
    {
        return RaceTrackLayout.StartLightModelNames.Select(modelName => LoadLegacyModel(modelName, assetContentManager)).ToArray();
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

    private static float GetLegacyModelSize(string modelName)
    {
        modelName = RaceTrackLayout.NormalizeLegacyModelName(modelName);
        return ModelSizeCache.TryGetValue(modelName, out float cachedSize)
            ? cachedSize
            : 1f;
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

    private static LitDiffuseMaterial CreateRoadMaterial(string trackName, AssetContentManager assetContentManager)
    {
        Texture2D? roadTexture = LoadProjectTexture(assetContentManager, RoadTextureCandidates);
        return new LitDiffuseMaterial
        {
            Name = $"{trackName}.RoadMaterial",
            BasColor = roadTexture,
            DiffuseColor = roadTexture != null ? Color.White : new Color(60, 62, 66),
            EmissiveColor = new Vector3(0.015f, 0.015f, 0.015f),
            SpecularColor = new Vector3(0.14f),
            SpecularPower = 18f,
            RasterizerState = RasterizerState.CullCounterClockwise,
            SamplerState = SamplerState.AnisotropicWrap,
        };
    }

    private static LitDiffuseMaterial CreateGroundMaterial(string trackName, AssetContentManager assetContentManager)
    {
        Texture2D? groundTexture = LoadProjectTexture(assetContentManager, GroundTextureCandidates);
        return new LitDiffuseMaterial
        {
            Name = $"{trackName}.GroundMaterial",
            BasColor = groundTexture,
            DiffuseColor = groundTexture != null ? Color.White : new Color(182, 166, 118),
            EmissiveColor = new Vector3(0.01f, 0.01f, 0.008f),
            SpecularColor = new Vector3(0.05f),
            SpecularPower = 6f,
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
            AmbientColor = presentation.AmbientColor,
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