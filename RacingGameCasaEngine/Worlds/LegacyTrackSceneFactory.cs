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
using RacingGameCasaEngine.Bootstrap;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Worlds;

internal static class LegacyTrackSceneFactory
{
    private const float WorldScale = 0.04f;
    private const float BaseRoadWidth = 6.4f;
    private const float GroundMargin = 18f;

    private static readonly XmlSerializer TrackSerializer = new(typeof(LegacyTrackLayout));
    private static readonly XmlSerializer CombiSerializer = new(typeof(List<LegacyCombiObject>), new XmlRootAttribute("ArrayOfCombiObject"));
    private static readonly StaticModelImporter StaticModelImporter = new();
    private static readonly Dictionary<string, LegacyTrackLayout> TrackCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<LegacyCombiObject>> CombiCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, StaticModel?> ModelCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> ModelAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Casino"] = "Casino01",
    };

    internal static RaceTrackScene Create(string trackName)
    {
        LegacyTrackLayout layout = LoadTrackLayout(RaceBootstrapAssets.GetTrackDataAssetName(trackName));
        Vector3 origin = ComputeOrigin(layout.TrackPoints);
        List<Vector3> roadPoints = layout.TrackPoints.Select(point => ConvertLegacyPoint(point) - origin).ToList();

        if (roadPoints.Count < 3)
        {
            throw new InvalidOperationException($"Track '{trackName}' does not contain enough points to build a road.");
        }

        Vector3 playerStartPosition = roadPoints[0];
        List<Vector3> checkpoints =
        [
            SampleLoopPoint(roadPoints, 0.20f),
            SampleLoopPoint(roadPoints, 0.50f),
            SampleLoopPoint(roadPoints, 0.80f),
        ];

        var entities = new List<Entity>
        {
            CreateGroundEntity(trackName, roadPoints),
            CreateRoadEntity(trackName, layout, roadPoints),
        };

        int sceneryIndex = 0;
        foreach (LegacyNeutralObject neutralObject in layout.NeutralsObjects)
        {
            AddSceneryEntities(entities, neutralObject.modelName, neutralObject.matrix, origin, ref sceneryIndex);
        }

        return new RaceTrackScene(entities, playerStartPosition, checkpoints);
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

        string fileName = Path.Combine(EngineEnvironment.ProjectPath, "Models", $"{combiName}.CombiModel");
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

    private static void AddSceneryEntities(List<Entity> entities, string modelName, Matrix legacyTransform, Vector3 origin, ref int sceneryIndex)
    {
        if (string.IsNullOrWhiteSpace(modelName) || modelName.StartsWith("Track", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (modelName.StartsWith("Combi", StringComparison.OrdinalIgnoreCase))
        {
            foreach (LegacyCombiObject combiObject in LoadCombiModel(modelName))
            {
                Matrix combinedTransform = combiObject.matrix * legacyTransform;
                AddSceneryEntities(entities, combiObject.modelName, combinedTransform, origin, ref sceneryIndex);
            }

            return;
        }

        StaticModel? staticModel = LoadLegacyModel(modelName);
        if (staticModel == null)
        {
            return;
        }

        Matrix runtimeTransform = ConvertLegacyTransform(legacyTransform, origin);
        entities.Add(CreateStaticModelEntity($"Scenery.{modelName}.{sceneryIndex:000}", staticModel, runtimeTransform));
        sceneryIndex++;
    }

    private static StaticModel? LoadLegacyModel(string modelName)
    {
        if (ModelAliases.TryGetValue(modelName, out string? alias))
        {
            modelName = alias;
        }

        if (ModelCache.TryGetValue(modelName, out StaticModel? cachedModel))
        {
            return cachedModel;
        }

        string fileName = Path.Combine(EngineEnvironment.ProjectPath, "Models", $"{modelName}.X");
        if (!File.Exists(fileName))
        {
            Logs.WriteWarning($"Legacy track model '{modelName}' not found at '{fileName}'.");
            ModelCache[modelName] = null;
            return null;
        }

        if (!StaticModelImporter.IsFileSupported(fileName))
        {
            Logs.WriteWarning($"Legacy track model '{modelName}' uses an unsupported format for runtime import.");
            ModelCache[modelName] = null;
            return null;
        }

        StaticModel model = StaticModelImporter.Import(fileName);
        ApplyFallbackMaterials(model, modelName);
        ModelCache[modelName] = model;
        return model;
    }

    private static Entity CreateRoadEntity(string trackName, LegacyTrackLayout layout, IReadOnlyList<Vector3> roadPoints)
    {
        var vertices = new List<Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture>(roadPoints.Count * 2);
        var indices = new List<uint>(roadPoints.Count * 6);

        for (int index = 0; index < roadPoints.Count; index++)
        {
            Vector3 previous = roadPoints[(index - 1 + roadPoints.Count) % roadPoints.Count];
            Vector3 current = roadPoints[index];
            Vector3 next = roadPoints[(index + 1) % roadPoints.Count];

            Vector3 tangent = next - previous;
            if (tangent.LengthSquared() < 0.0001f)
            {
                tangent = Vector3.Forward;
            }
            tangent.Normalize();

            Vector3 right = Vector3.Cross(Vector3.Up, tangent);
            if (right.LengthSquared() < 0.0001f)
            {
                right = Vector3.Right;
            }
            right.Normalize();

            float width = BaseRoadWidth * GetNearestWidthScale(layout.WidthHelpers, layout.TrackPoints[index]);
            Vector3 leftPoint = current - right * (width * 0.5f);
            Vector3 rightPoint = current + right * (width * 0.5f);
            float v = index / (float)Math.Max(1, roadPoints.Count - 1) * 10f;

            vertices.Add(new Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture(leftPoint, Vector3.Up, new Microsoft.Xna.Framework.Vector2(0f, v)));
            vertices.Add(new Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture(rightPoint, Vector3.Up, new Microsoft.Xna.Framework.Vector2(1f, v)));
        }

        for (int index = 0; index < roadPoints.Count; index++)
        {
            int nextIndex = (index + 1) % roadPoints.Count;
            uint leftCurrent = (uint)(index * 2);
            uint rightCurrent = leftCurrent + 1;
            uint leftNext = (uint)(nextIndex * 2);
            uint rightNext = leftNext + 1;

            indices.Add(leftCurrent);
            indices.Add(rightCurrent);
            indices.Add(leftNext);
            indices.Add(rightCurrent);
            indices.Add(rightNext);
            indices.Add(leftNext);
        }

        var roadMesh = new StaticModelMesh { Name = $"{trackName}.Road" };
        roadMesh.SetData(vertices.ToArray(), indices.ToArray());
        roadMesh.Material = new LitDiffuseMaterial
        {
            DiffuseColor = new Color(60, 62, 66),
            EmissiveColor = new Vector3(0.03f, 0.03f, 0.03f),
            SpecularColor = new Vector3(0.08f),
            SpecularPower = 6f,
        };

        var roadModel = new StaticModel { Name = $"{trackName}.Road" };
        roadModel.Meshes.Add(roadMesh);
        roadModel.RootNode = new StaticModelNode
        {
            Name = "RoadRoot",
            MeshIndex = 0,
        };

        return CreateStaticModelEntity($"TrackRoot.{trackName}", roadModel, Matrix.Identity);
    }

    private static Entity CreateGroundEntity(string trackName, IReadOnlyList<Vector3> roadPoints)
    {
        float minX = roadPoints.Min(static point => point.X) - GroundMargin;
        float maxX = roadPoints.Max(static point => point.X) + GroundMargin;
        float minZ = roadPoints.Min(static point => point.Z) - GroundMargin;
        float maxZ = roadPoints.Max(static point => point.Z) + GroundMargin;
        float minY = roadPoints.Min(static point => point.Y) - 0.8f;
        float width = Math.Max(10f, maxX - minX);
        float depth = Math.Max(10f, maxZ - minZ);

        StaticModel groundModel = StaticModel.CreateFromPrimitive(new BoxPrimitive(width, 1.0f, depth), $"{trackName}.Ground");
        groundModel.Meshes[0].Material = new LitDiffuseMaterial
        {
            DiffuseColor = new Color(182, 166, 118),
            EmissiveColor = new Vector3(0.02f, 0.02f, 0.01f),
            SpecularColor = new Vector3(0.03f),
            SpecularPower = 2f,
        };

        Matrix transform = Matrix.CreateTranslation(new Vector3((minX + maxX) * 0.5f, minY, (minZ + maxZ) * 0.5f));
        return CreateStaticModelEntity($"SceneryRoot.{trackName}", groundModel, transform);
    }

    private static Entity CreateStaticModelEntity(string name, StaticModel model, Matrix transform)
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

        var component = new StaticModelComponent
        {
            StaticModel = model,
        };
        component.Coordinates.Position = translation;
        component.Coordinates.Orientation = rotation;
        component.Coordinates.Scale = scale;
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

    private static Matrix ConvertLegacyTransform(Matrix legacyTransform, Vector3 origin)
    {
        Vector3 right = ConvertLegacyBasisVector(new Vector3(legacyTransform.M11, legacyTransform.M12, legacyTransform.M13));
        Vector3 up = ConvertLegacyBasisVector(new Vector3(legacyTransform.M21, legacyTransform.M22, legacyTransform.M23));
        Vector3 backward = ConvertLegacyBasisVector(new Vector3(legacyTransform.M31, legacyTransform.M32, legacyTransform.M33));
        Vector3 translation = ConvertLegacyPoint(new Vector3(legacyTransform.M41, legacyTransform.M42, legacyTransform.M43)) - origin;

        return new Matrix(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            backward.X, backward.Y, backward.Z, 0f,
            translation.X, translation.Y, translation.Z, 1f);
    }

    private static Vector3 ConvertLegacyPoint(Vector3 point)
    {
        return new Vector3(point.X, point.Z, point.Y) * WorldScale;
    }

    private static Vector3 ConvertLegacyBasisVector(Vector3 basisVector)
    {
        if (basisVector.LengthSquared() < 0.000001f)
        {
            return basisVector;
        }

        return new Vector3(basisVector.X, basisVector.Z, basisVector.Y) * WorldScale;
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

        return Path.Combine(EngineEnvironment.ProjectPath, assetInfo.FileName.Replace('/', Path.DirectorySeparatorChar));
    }
}

internal sealed record RaceTrackScene(IReadOnlyList<Entity> Entities, Vector3 PlayerStartPosition, IReadOnlyList<Vector3> CheckpointPositions);

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