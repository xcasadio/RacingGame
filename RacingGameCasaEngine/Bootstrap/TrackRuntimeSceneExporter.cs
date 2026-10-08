using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Materials;
using CasaEngine.Framework.Materials.Compilation;
using CasaEngine.Framework.Materials.Runtime;
using CasaEngine.Framework.Rendering.Models;
using CasaEngine.Framework.Rendering.Shaders;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGameCasaEngine.Worlds;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Import;

namespace RacingGameCasaEngine.Bootstrap;

internal static class TrackRuntimeSceneExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static TrackRuntimeSceneSnapshot CaptureTrack(string trackName, World world, MaterialCache? materialCache)
    {
        List<TrackRuntimeSceneEntityRecord> entities = world.Entities
            .Where(RaceWorldFactory.IsRaceRenderableEntity)
            .OrderBy(static entity => entity.Name, StringComparer.Ordinal)
            .Select(CaptureEntity)
            .ToList();

        List<TrackRuntimeSceneDrawnMeshRecord> drawnMeshes = [];
        foreach (Entity entity in world.Entities
                     .Where(RaceWorldFactory.IsRaceRenderableEntity)
                     .OrderBy(static entity => entity.Name, StringComparer.Ordinal))
        {
            if (entity.RootComponent != null)
            {
                CollectDrawnMeshes(entity, entity.RootComponent, string.Empty, materialCache, drawnMeshes);
            }
        }

        return new TrackRuntimeSceneSnapshot
        {
            TrackName = trackName,
            WorldName = world.Name ?? string.Empty,
            EntityCount = entities.Count,
            VisibleEntityCount = entities.Count(static entity => entity.IsVisible),
            ComparisonTargetCount = entities.Count(static entity => entity.IncludeInDeterministicComparison),
            Entities = entities,
            DrawnMeshes = drawnMeshes,
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

    private static void CollectDrawnMeshes(Entity entity, SceneComponent component, string parentPath, MaterialCache? materialCache, List<TrackRuntimeSceneDrawnMeshRecord> drawnMeshes)
    {
        string path = parentPath.Length == 0 ? component.Name : parentPath + "/" + component.Name;
        if (component is StaticModelSubMeshComponent subMesh && subMesh.ModelMesh != null)
        {
            CaptureDrawnMesh(entity, subMesh, path, materialCache, drawnMeshes);
        }

        foreach (SceneComponent child in component.Children)
        {
            CollectDrawnMeshes(entity, child, path, materialCache, drawnMeshes);
        }
    }

    /// <summary>
    /// Adds one record per drawn SubMesh (index range) of the mesh, or one record for a mesh without SubMesh, as
    /// StaticMeshRendererComponent builds its render items.
    /// </summary>
    private static void CaptureDrawnMesh(Entity entity, StaticModelSubMeshComponent subMesh, string path, MaterialCache? materialCache, List<TrackRuntimeSceneDrawnMeshRecord> drawnMeshes)
    {
        StaticModelMesh mesh = subMesh.ModelMesh;
        IReadOnlyList<VertexPositionNormalTexture> vertices = mesh.GetVertices();
        IReadOnlyList<uint> indices = mesh.GetIndices();

        // Same rule as StaticModelSubMeshComponent.Draw: a generated sub-mesh takes the shadow flags of its StaticModelComponent.
        PrimitiveComponent shadowFlagSource = subMesh;
        if (subMesh.IsGeneratedFromModel)
        {
            for (SceneComponent? current = subMesh.Parent; current != null; current = current.Parent)
            {
                if (current is StaticModelComponent staticModelComponent)
                {
                    shadowFlagSource = staticModelComponent;
                    break;
                }
            }
        }

        int primitiveCount = mesh.PrimitiveType switch
        {
            PrimitiveType.TriangleList => indices.Count / 3,
            PrimitiveType.TriangleStrip => Math.Max(0, indices.Count - 2),
            PrimitiveType.LineList => indices.Count / 2,
            PrimitiveType.LineStrip => Math.Max(0, indices.Count - 1),
            _ => 0,
        };

        string vertexHash = HashVertices(vertices);
        string indexHash = HashIndices(indices);

        if (mesh.SubMeshes.Count == 0)
        {
            MaterialBase? meshMaterial = GetMaterialOverride(subMesh.MaterialOverridesBySlotIndex, mesh.MaterialSlotIndex) ?? mesh.Material;
            drawnMeshes.Add(CreateRecord(meshMaterial, null));
            return;
        }

        for (int subMeshIndex = 0; subMeshIndex < mesh.SubMeshes.Count; subMeshIndex++)
        {
            SubMesh drawnRange = mesh.SubMeshes[subMeshIndex];
            // Same order as StaticMeshRendererComponent: instance override, SubMesh material, mesh material.
            MaterialBase? drawnMaterial = GetMaterialOverride(subMesh.MaterialOverridesBySlotIndex, drawnRange.MaterialSlotIndex)
                ?? drawnRange.Material
                ?? mesh.Material;
            drawnMeshes.Add(CreateRecord(drawnMaterial, subMeshIndex));
        }

        TrackRuntimeSceneDrawnMeshRecord CreateRecord(MaterialBase? material, int? subMeshIndex)
        {
            SortedDictionary<string, string> parameters = new(StringComparer.Ordinal);
            SortedDictionary<string, string> textures = new(StringComparer.Ordinal);
            if (material != null)
            {
                CaptureMaterialProperties(material, parameters, textures);
            }

            SubMesh? range = subMeshIndex.HasValue ? mesh.SubMeshes[subMeshIndex.Value] : null;
            return new TrackRuntimeSceneDrawnMeshRecord
            {
                EntityName = entity.Name,
                ComponentPath = path,
                MeshName = mesh.Name,
                WorldMatrix = ToMatrixRows(subMesh.WorldMatrixWithScale),
                MaterialType = material?.GetType().Name,
                MaterialParameters = parameters,
                TextureHashes = textures,
                EntityIsVisible = entity.IsVisible,
                CastShadows = shadowFlagSource.CastShadows,
                ReceiveShadows = shadowFlagSource.ReceiveShadows,
                PrimitiveType = mesh.PrimitiveType.ToString(),
                HasTangents = mesh.HasTangents,
                VertexCount = vertices.Count,
                IndexCount = indices.Count,
                PrimitiveCount = primitiveCount,
                VertexHash = vertexHash,
                IndexHash = indexHash,
                SubMeshIndex = subMeshIndex,
                SubMeshIndexStart = range?.IndexStart,
                SubMeshPrimitiveCount = range?.PrimitiveCount,
                RenderFeatures = material == null ? null : CaptureRenderFeatures(material, mesh, materialCache),
            };
        }
    }

    private static MaterialBase? GetMaterialOverride(IReadOnlyDictionary<int, MaterialBase>? overridesBySlotIndex, int slotIndex)
    {
        if (overridesBySlotIndex == null || slotIndex < 0)
        {
            return null;
        }

        return overridesBySlotIndex.TryGetValue(slotIndex, out MaterialBase? materialOverride) ? materialOverride : null;
    }

    /// <summary>
    /// The features StaticMeshRendererComponent puts on the render item: the compiled material's when its id is in
    /// the compiled-material cache, adjusted to the mesh by the engine (RenderFeatureResolver.ResolveForMesh). Sorted flag names.
    /// </summary>
    private static List<string> CaptureRenderFeatures(MaterialBase material, StaticModelMesh mesh, MaterialCache? materialCache)
    {
        // Same lookup and same engine function as StaticMeshRendererComponent, so this records what is drawn.
        CompiledMaterial? compiledMaterial = null;
        if (material.Id != Guid.Empty && materialCache != null && materialCache.TryGet(material.Id, out var cached))
        {
            compiledMaterial = cached;
        }

        ShaderFeature features = RenderFeatureResolver.ResolveForMesh(material, compiledMaterial!, mesh);

        return Enum.GetValues<ShaderFeature>()
            .Where(flag => flag != ShaderFeature.None && features.HasFlag(flag))
            .Select(static flag => flag.ToString())
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static void CaptureMaterialProperties(MaterialBase material, SortedDictionary<string, string> parameters, SortedDictionary<string, string> textures)
    {
        foreach (PropertyInfo property in material.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || property.GetMethod?.IsPublic != true)
            {
                continue;
            }

            Type type = property.PropertyType;
            if (type == typeof(Guid) || property.Name.EndsWith("AssetId", StringComparison.Ordinal))
            {
                continue;
            }

            object? value = property.GetValue(material);
            if (typeof(Texture).IsAssignableFrom(type))
            {
                textures[property.Name] = value is Texture2D or TextureCube ? HashTexture((Texture)value) : "none";
                continue;
            }

            string? formatted = FormatSimpleValue(value, type);
            if (formatted != null)
            {
                parameters[property.Name] = formatted;
            }
        }
    }

    private static string? FormatSimpleValue(object? value, Type type)
    {
        if (value is float single)
        {
            return single.ToString("R", CultureInfo.InvariantCulture);
        }

        if (value is double or decimal or bool or byte or sbyte or short or ushort or int or uint or long or ulong || type.IsEnum)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        return value switch
        {
            Vector2 v2 => Join(v2.X, v2.Y),
            Vector3 v3 => Join(v3.X, v3.Y, v3.Z),
            Vector4 v4 => Join(v4.X, v4.Y, v4.Z, v4.W),
            Microsoft.Xna.Framework.Color color => $"{color.R},{color.G},{color.B},{color.A}",
            XnaQuaternion quaternion => Join(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W),
            _ => FormatStateObject(value, type),
        };
    }

    private static string? FormatStateObject(object? value, Type type)
    {
        if (type != typeof(BlendState) && type != typeof(DepthStencilState) && type != typeof(RasterizerState) && type != typeof(SamplerState))
        {
            return null;
        }

        return value == null ? "null" : ((GraphicsResource)value).Name ?? "<unnamed " + value.GetType().Name + ">";
    }

    private static string Join(params float[] values)
    {
        return string.Join(",", values.Select(static v => v.ToString("R", CultureInfo.InvariantCulture)));
    }

    private static readonly ConditionalWeakTable<Texture, string> TextureHashCache = new();

    private static string HashTexture(Texture texture)
    {
        if (TextureHashCache.TryGetValue(texture, out string? cached))
        {
            return cached;
        }

        string hash;
        try
        {
            using IncrementalHash sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            if (texture is Texture2D texture2D)
            {
                AppendTextureLevel(sha1, texture2D.Width, texture2D.Height, texture2D.Format, data => texture2D.GetData(data));
            }
            else
            {
                var cube = (TextureCube)texture;
                foreach (CubeMapFace face in Enum.GetValues<CubeMapFace>())
                {
                    CubeMapFace currentFace = face;
                    AppendTextureLevel(sha1, cube.Size, cube.Size, cube.Format, data => cube.GetData(currentFace, data));
                }
            }

            hash = Convert.ToHexString(sha1.GetHashAndReset()).ToLowerInvariant();
        }
        catch (Exception exception)
        {
            hash = "unreadable:" + exception.GetType().Name;
        }

        TextureHashCache.Add(texture, hash);
        return hash;
    }

    private static void AppendTextureLevel(IncrementalHash sha1, int width, int height, SurfaceFormat format, Action<byte[]> getData)
    {
        byte[] data = new byte[GetLevelByteCount(width, height, format)];
        getData(data);
        Span<byte> header = stackalloc byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(header, width);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], height);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..], (int)format);
        sha1.AppendData(header);
        sha1.AppendData(data);
    }

    private static int GetLevelByteCount(int width, int height, SurfaceFormat format)
    {
        switch (format)
        {
            case SurfaceFormat.Color:
            case SurfaceFormat.Bgra32:
            case SurfaceFormat.Rgba1010102:
            case SurfaceFormat.Single:
                return width * height * 4;
            case SurfaceFormat.Alpha8:
                return width * height;
            case SurfaceFormat.Bgr565:
            case SurfaceFormat.Bgra5551:
            case SurfaceFormat.Bgra4444:
                return width * height * 2;
            case SurfaceFormat.HalfVector4:
            case SurfaceFormat.Rgba64:
                return width * height * 8;
            case SurfaceFormat.Vector4:
                return width * height * 16;
            case SurfaceFormat.Dxt1:
                return (width + 3) / 4 * ((height + 3) / 4) * 8;
            case SurfaceFormat.Dxt3:
            case SurfaceFormat.Dxt5:
                return (width + 3) / 4 * ((height + 3) / 4) * 16;
            default:
                throw new NotSupportedException("Surface format " + format);
        }
    }

    private static string HashVertices(IReadOnlyList<VertexPositionNormalTexture> vertices)
    {
        // Positions, normals, texture coordinates. StaticModelMesh does not expose its tangent array.
        byte[] buffer = new byte[vertices.Count * 8 * sizeof(float)];
        int offset = 0;
        foreach (VertexPositionNormalTexture vertex in vertices)
        {
            WriteFloat(buffer, ref offset, vertex.Position.X);
            WriteFloat(buffer, ref offset, vertex.Position.Y);
            WriteFloat(buffer, ref offset, vertex.Position.Z);
            WriteFloat(buffer, ref offset, vertex.Normal.X);
            WriteFloat(buffer, ref offset, vertex.Normal.Y);
            WriteFloat(buffer, ref offset, vertex.Normal.Z);
            WriteFloat(buffer, ref offset, vertex.TextureCoordinate.X);
            WriteFloat(buffer, ref offset, vertex.TextureCoordinate.Y);
        }

        return Convert.ToHexString(SHA1.HashData(buffer)).ToLowerInvariant();
    }

    private static string HashIndices(IReadOnlyList<uint> indices)
    {
        byte[] buffer = new byte[indices.Count * sizeof(uint)];
        for (int i = 0; i < indices.Count; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(i * sizeof(uint)), indices[i]);
        }

        return Convert.ToHexString(SHA1.HashData(buffer)).ToLowerInvariant();
    }

    private static void WriteFloat(byte[] buffer, ref int offset, float value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), BitConverter.SingleToInt32Bits(value));
        offset += sizeof(float);
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

        public List<TrackRuntimeSceneDrawnMeshRecord> DrawnMeshes { get; set; } = [];
    }

    internal sealed class TrackRuntimeSceneDrawnMeshRecord
    {
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Component names from the root component, joined with '/'.</summary>
        public string ComponentPath { get; set; } = string.Empty;

        public string? MeshName { get; set; }

        public float[][] WorldMatrix { get; set; } = [];

        public string? MaterialType { get; set; }

        public SortedDictionary<string, string> MaterialParameters { get; set; } = new(StringComparer.Ordinal);

        /// <summary>SHA-1 of the pixel data of every texture property of the material, by property name.</summary>
        public SortedDictionary<string, string> TextureHashes { get; set; } = new(StringComparer.Ordinal);

        public bool EntityIsVisible { get; set; }

        public bool CastShadows { get; set; }

        public bool ReceiveShadows { get; set; }

        public string PrimitiveType { get; set; } = string.Empty;

        public bool HasTangents { get; set; }

        public int VertexCount { get; set; }

        public int IndexCount { get; set; }

        public int PrimitiveCount { get; set; }

        public string VertexHash { get; set; } = string.Empty;

        public string IndexHash { get; set; } = string.Empty;

        /// <summary>Index in StaticModelMesh.SubMeshes; absent for a mesh without SubMesh.</summary>
        public int? SubMeshIndex { get; set; }

        public int? SubMeshIndexStart { get; set; }

        public int? SubMeshPrimitiveCount { get; set; }

        /// <summary>Names of the ShaderFeature flags the renderer draws this record with, sorted; absent without material.</summary>
        public List<string>? RenderFeatures { get; set; }
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