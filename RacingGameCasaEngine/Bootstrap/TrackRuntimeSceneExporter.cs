using System.Text.Json;
using System.Text.Json.Serialization;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Materials;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Worlds;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;

namespace RacingGameCasaEngine.Bootstrap;

internal static class TrackRuntimeSceneExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static TrackRuntimeSceneSnapshot CaptureTrack(string trackName, World world)
    {
        List<TrackRuntimeSceneEntityRecord> entities = world.Entities
            .Where(RaceWorldFactory.IsRaceRenderableEntity)
            .OrderBy(static entity => entity.Name, StringComparer.Ordinal)
            .Select(CaptureEntity)
            .ToList();

        return new TrackRuntimeSceneSnapshot
        {
            TrackName = trackName,
            WorldName = world.Name ?? string.Empty,
            EntityCount = entities.Count,
            VisibleEntityCount = entities.Count(static entity => entity.IsVisible),
            ComparisonTargetCount = entities.Count(static entity => entity.IncludeInDeterministicComparison),
            Entities = entities,
        };
    }

    public static void WriteFile(string filePath, IReadOnlyList<TrackRuntimeSceneSnapshot> trackSnapshots)
    {
        string fullFilePath = Path.GetFullPath(filePath);
        string? directory = Path.GetDirectoryName(fullFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var exportFile = new TrackRuntimeSceneExportFile
        {
            Generator = "RacingGameCasaEngine",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Scope = "Live CasaEngine race-world snapshot including final entity transforms, visibility state, bounding boxes, and resolved static-model mesh/material metadata for deterministic circuit migration verification.",
            Tracks = trackSnapshots.OrderBy(static track => track.TrackName, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        string json = JsonSerializer.Serialize(exportFile, JsonOptions);
        File.WriteAllText(fullFilePath, json);
    }

    private static TrackRuntimeSceneEntityRecord CaptureEntity(Entity entity)
    {
        SceneComponent? rootComponent = entity.RootComponent;
        Matrix worldMatrix = rootComponent?.WorldMatrixWithScale ?? Matrix.Identity;
        DecomposeTransform(worldMatrix, out Vector3 position, out XnaQuaternion orientation, out Vector3 scale);

        BoundingBox boundingBox = entity.GetBoundingBox();
        StaticModelComponent? staticModelComponent = entity.GetComponent<StaticModelComponent>();
        List<TrackRuntimeSceneSubMeshRecord> subMeshes = [];
        if (rootComponent != null)
        {
            CollectSubMeshes(rootComponent, subMeshes);
        }

        return new TrackRuntimeSceneEntityRecord
        {
            Name = entity.Name,
            Kind = ClassifyEntityKind(entity.Name),
            IsVisible = entity.IsVisible,
            IsEnabled = entity.IsEnabled,
            IncludeInDeterministicComparison = !string.Equals(entity.Name, RaceWorldFactory.PlayerCarEntityName, StringComparison.Ordinal),
            StaticModelName = staticModelComponent?.StaticModel?.Name,
            Position = Float3.FromVector3(position),
            Orientation = Float4.FromQuaternion(orientation),
            Scale = Float3.FromVector3(scale),
            WorldMatrix = ToMatrixRows(worldMatrix),
            BoundingBoxMin = Float3.FromVector3(boundingBox.Min),
            BoundingBoxMax = Float3.FromVector3(boundingBox.Max),
            BoundingBoxSize = Float3.FromVector3(boundingBox.Max - boundingBox.Min),
            SubMeshes = subMeshes,
        };
    }

    private static void CollectSubMeshes(SceneComponent component, List<TrackRuntimeSceneSubMeshRecord> subMeshes)
    {
        if (component is StaticModelSubMeshComponent staticModelSubMeshComponent && staticModelSubMeshComponent.ModelMesh != null)
        {
            BoundingBox bounds = staticModelSubMeshComponent.BoundingBox;
            MaterialBase? material = staticModelSubMeshComponent.ModelMesh.Material;
            subMeshes.Add(new TrackRuntimeSceneSubMeshRecord
            {
                ComponentName = staticModelSubMeshComponent.Name,
                MeshName = staticModelSubMeshComponent.ModelMesh.Name,
                MaterialName = material?.Name,
                MaterialType = material?.GetType().Name,
                BoundingBoxMin = Float3.FromVector3(bounds.Min),
                BoundingBoxMax = Float3.FromVector3(bounds.Max),
                BoundingBoxSize = Float3.FromVector3(bounds.Max - bounds.Min),
            });
        }

        foreach (SceneComponent child in component.Children)
        {
            CollectSubMeshes(child, subMeshes);
        }
    }

    private static string ClassifyEntityKind(string? entityName)
    {
        if (entityName?.StartsWith(RaceWorldFactory.TrackGroundEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-ground";
        }

        if (entityName?.StartsWith(RaceWorldFactory.TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-road";
        }

        if (entityName?.StartsWith(RaceWorldFactory.TrackGuardRailEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-guardrail";
        }

        if (entityName?.StartsWith(RaceWorldFactory.TrackGuardRailHolderEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-guardrail-holder";
        }

        if (entityName?.StartsWith(RaceWorldFactory.TrackColumnsEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-columns";
        }

        if (entityName?.StartsWith(RaceWorldFactory.TrackColumnSegmentEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-column-segment";
        }

        if (entityName?.StartsWith(RaceWorldFactory.TrackSceneryEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "track-scenery";
        }

        if (entityName?.StartsWith(RaceWorldFactory.CheckpointEntityNamePrefix, StringComparison.Ordinal) == true)
        {
            return "checkpoint";
        }

        if (string.Equals(entityName, RaceWorldFactory.PlayerStartEntityName, StringComparison.Ordinal))
        {
            return "player-start";
        }

        if (string.Equals(entityName, RaceWorldFactory.PlayerCarEntityName, StringComparison.Ordinal))
        {
            return "player-car";
        }

        return "other";
    }

    private static void DecomposeTransform(Matrix matrix, out Vector3 position, out XnaQuaternion orientation, out Vector3 scale)
    {
        if (!matrix.Decompose(out scale, out orientation, out position))
        {
            position = matrix.Translation;
            scale = new Vector3(matrix.Right.Length(), matrix.Up.Length(), matrix.Backward.Length());
            orientation = XnaQuaternion.Identity;
        }

        if (orientation.LengthSquared() > 0.000001f)
        {
            orientation.Normalize();
        }
        else
        {
            orientation = XnaQuaternion.Identity;
        }
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

    internal sealed class TrackRuntimeSceneExportFile
    {
        public string Generator { get; set; } = string.Empty;

        public DateTimeOffset GeneratedAtUtc { get; set; }

        public string Scope { get; set; } = string.Empty;

        public List<TrackRuntimeSceneSnapshot> Tracks { get; set; } = [];
    }

    internal sealed class TrackRuntimeSceneSnapshot
    {
        public string TrackName { get; set; } = string.Empty;

        public string WorldName { get; set; } = string.Empty;

        public int EntityCount { get; set; }

        public int VisibleEntityCount { get; set; }

        public int ComparisonTargetCount { get; set; }

        public List<TrackRuntimeSceneEntityRecord> Entities { get; set; } = [];
    }

    internal sealed class TrackRuntimeSceneEntityRecord
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

        public Float3 BoundingBoxMin { get; set; }

        public Float3 BoundingBoxMax { get; set; }

        public Float3 BoundingBoxSize { get; set; }

        public List<TrackRuntimeSceneSubMeshRecord> SubMeshes { get; set; } = [];
    }

    internal sealed class TrackRuntimeSceneSubMeshRecord
    {
        public string ComponentName { get; set; } = string.Empty;

        public string? MeshName { get; set; }

        public string? MaterialName { get; set; }

        public string? MaterialType { get; set; }

        public Float3 BoundingBoxMin { get; set; }

        public Float3 BoundingBoxMax { get; set; }

        public Float3 BoundingBoxSize { get; set; }
    }

    internal readonly record struct Float3(float X, float Y, float Z)
    {
        public static Float3 FromVector3(Vector3 value)
        {
            return new Float3(Round(value.X), Round(value.Y), Round(value.Z));
        }
    }

    internal readonly record struct Float4(float X, float Y, float Z, float W)
    {
        public static Float4 FromQuaternion(XnaQuaternion value)
        {
            return new Float4(Round(value.X), Round(value.Y), Round(value.Z), Round(value.W));
        }
    }
}