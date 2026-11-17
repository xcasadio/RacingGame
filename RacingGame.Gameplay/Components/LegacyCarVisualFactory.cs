using System;
using System.Collections.Generic;
using System.IO;
using CasaEngine.Engine.Environment;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Materials.Runtime;
using CasaEngine.Framework.Rendering.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using RacingGame.Gameplay.Entities;
using RacingGame.Gameplay.Import;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Components;

/// <summary>
/// Builds RacingGame's car for the race and the car selection: one model per car type, a private copy of the cooked
/// <c>StaticModel.Car</c> with the car's texture (RacerCar, RacerCar2, RacerCar3). The selected colour is painted into
/// the texture as RacingGame's shader painted it (<c>NormalMapping.fx</c>, SpecularWithReflectionForCar20:
/// <c>lerp(rgb, carHueColor, texture alpha)</c>), on the CPU since CasaEngine's shaders have no such tint (ADR-0009).
/// <para/>
/// The paint is a run-time state of each car index, never written to a shared asset: in the private copy, each slot whose
/// cooked material has <c>Texture.RacerCar</c> for base texture gets, on the mesh and on each of its sub-meshes, a copy
/// of the compiled material with a new id (so it is not in the compiled-material cache, and the renderer draws its own
/// texture) whose base texture is the car's painted texture (the paint slot, <see cref="IsPaintSlot"/>) or its unpainted
/// one (the other slots). The texture's alpha, the paint mask, is kept: RacingGame's shader also scaled the specular by
/// it, as CasaEngine's does. Every other value (tuning, reflection blend, normal map) comes from the cooked material.
/// </summary>
internal static class LegacyCarVisualFactory
{
    private const string CarModelAssetName = "StaticModel.Car";
    private const string CarTextureAssetName = "Texture.RacerCar";
    private const float LegacyCarCollisionLength = RaceCarEntityFactory.CollisionLength;
    private const float LegacyCarCollisionWidth = RaceCarEntityFactory.CollisionWidth;
    private const float LegacyCarCollisionHeight = RaceCarEntityFactory.CollisionHeight;
    private static readonly Color LegacyTextureColorKey = new(255, 0, 255, 0);
    private static readonly string[] CarDiffuseTextureNames = RaceMaterialRecipes.CarDiffuseTextureNames;

    private static readonly Dictionary<int, StaticModel?> ConfiguredCarModelCache = new();
    private static readonly Dictionary<int, CarPaint?> CarPaints = new();
    private static readonly object CacheLock = new();

    private static BoundingBox _cachedCarBounds;
    private static bool _hasCachedCarBounds;
    private static bool _hasLoggedMissingCarModel;

    internal static readonly Quaternion LegacyCarFacingCorrection = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi);

    internal static StaticModel? LoadConfiguredCarModel(AssetContentManager assetContentManager, int selectedCarIndex, int selectedCarColorIndex)
    {
        ArgumentNullException.ThrowIfNull(assetContentManager);

        int normalizedCarIndex = NormalizeModulo(selectedCarIndex, CarDiffuseTextureNames.Length);
        int normalizedColorIndex = NormalizeModulo(selectedCarColorIndex, Math.Max(1, RaceFrontEndCatalog.CarColors.Count));

        lock (CacheLock)
        {
            // Paint first: the materials of a new model take the car's textures as they are.
            GetCarPaint(normalizedCarIndex, assetContentManager)?.Paint(normalizedColorIndex);
            if (ConfiguredCarModelCache.TryGetValue(normalizedCarIndex, out StaticModel? cachedModel))
            {
                return cachedModel;
            }

            int key = normalizedCarIndex;

            AssetInfo? carModelInfo = AssetCatalog.Get(CarModelAssetName);
            if (carModelInfo == null)
            {
                if (!_hasLoggedMissingCarModel)
                {
                    Logs.WriteWarning($"The catalog has no '{CarModelAssetName}' asset.");
                    _hasLoggedMissingCarModel = true;
                }

                ConfiguredCarModelCache[key] = null;
                return null;
            }

            // A private copy, not the shared asset: its car-texture slots are repainted for this car index.
            StaticModel model = assetContentManager.LoadCopy<StaticModel>(carModelInfo.Id);
            model.Initialize(assetContentManager);
            ApplyCarPaint(model, GetCarPaint(normalizedCarIndex, assetContentManager));

            if (!_hasCachedCarBounds)
            {
                _cachedCarBounds = ComputeModelBounds(model);
                _hasCachedCarBounds = true;
            }

            ConfiguredCarModelCache[key] = model;
            return model;
        }
    }

    internal static BoundingBox GetCarBounds(AssetContentManager assetContentManager)
    {
        if (_hasCachedCarBounds)
        {
            return _cachedCarBounds;
        }

        _ = LoadConfiguredCarModel(assetContentManager, 0, 0);
        return _hasCachedCarBounds
            ? _cachedCarBounds
            : GetDefaultCarBounds();
    }

    internal static float ComputeUniformScale(BoundingBox bounds)
    {
        Vector3 size = bounds.Max - bounds.Min;
        float horizontalLength = Math.Max(size.X, size.Z);
        float horizontalWidth = Math.Min(size.X, size.Z);

        float lengthScale = horizontalLength > 0.0001f ? LegacyCarCollisionLength / horizontalLength : 1f;
        float widthScale = horizontalWidth > 0.0001f ? LegacyCarCollisionWidth / horizontalWidth : lengthScale;
        float heightScale = size.Y > 0.0001f ? LegacyCarCollisionHeight / size.Y : lengthScale;

        float scale = Math.Min(lengthScale, Math.Min(widthScale, heightScale));
        return Math.Clamp(scale, 0.01f, 10f);
    }

    internal static float ComputeGroundLiftY(BoundingBox bounds, float scale)
    {
        return (bounds.Max.Y - bounds.Min.Y) * 0.5f * scale;
    }

    internal static Vector3 ComputeGroundedModelOffset(BoundingBox bounds, float scale, float parentLiftY)
    {
        Vector3 boundsCenter = (bounds.Min + bounds.Max) * 0.5f;
        float groundedOffsetY = -boundsCenter.Y * scale + ComputeGroundLiftY(bounds, scale) - parentLiftY;
        return new Vector3(-boundsCenter.X * scale, groundedOffsetY, -boundsCenter.Z * scale);
    }

    /// <summary>
    /// Whether the car-texture slot named <paramref name="slotName"/> (<c>"Car [lack]"</c>: the mesh, then the material in
    /// brackets) takes the painted texture: RacingGame's paint rule, on the material's name (<c>IsPaintMaterial</c> of the
    /// legacy import, now on the slot name). A chrome, rubber or glass material is never painted, a "lack" or "paint"
    /// material always; any other one is when the cooked material reflects (<paramref name="reflects"/>).
    /// </summary>
    internal static bool IsPaintSlot(string slotName, bool reflects)
    {
        string materialName = slotName;
        int open = slotName.LastIndexOf('[');
        if (open >= 0 && slotName.EndsWith(']'))
        {
            materialName = slotName.Substring(open + 1, slotName.Length - open - 2);
        }

        if (materialName.Contains("chrome", StringComparison.OrdinalIgnoreCase)
            || materialName.Contains("gummi", StringComparison.OrdinalIgnoreCase)
            || materialName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            || materialName.Contains("fenster", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (materialName.Contains("lack", StringComparison.OrdinalIgnoreCase)
            || materialName.Contains("paint", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return reflects;
    }

    /// <summary>Whether the cooked material reflects the scene or a cube, for <see cref="IsPaintSlot"/>.</summary>
    internal static bool Reflects(LitDiffuseMaterial material)
    {
        return material.UseSceneReflectionCube || material.ReflectionCube != null || material.ReflectionCubeAssetId != Guid.Empty;
    }

    // Replaces, in the private copy of the cooked model, the material of every car-texture slot (mesh and sub-meshes)
    // by a copy that samples the car's painted or unpainted texture. Without a paint (texture unreadable) the cooked
    // materials stay as they are.
    private static void ApplyCarPaint(StaticModel model, CarPaint? paint)
    {
        AssetInfo? carTextureInfo = AssetCatalog.Get(CarTextureAssetName);
        if (paint == null || carTextureInfo == null)
        {
            return;
        }

        foreach (StaticModelMesh mesh in model.Meshes)
        {
            mesh.Material = CreateCarTextureCopy(mesh.Material, mesh.SlotName, carTextureInfo.Id, paint) ?? mesh.Material;
            foreach (SubMesh subMesh in mesh.SubMeshes)
            {
                string slotName = string.IsNullOrEmpty(subMesh.SlotName) ? mesh.SlotName : subMesh.SlotName;
                subMesh.Material = CreateCarTextureCopy(subMesh.Material, slotName, carTextureInfo.Id, paint) ?? subMesh.Material;
            }
        }
    }

    // The copy of a compiled car-texture material with the car's texture, or null when the material has another base texture.
    private static LitDiffuseMaterial? CreateCarTextureCopy(MaterialBase? material, string slotName, Guid carTextureAssetId, CarPaint paint)
    {
        if (material is not LitDiffuseMaterial source || source.BasColorAssetId != carTextureAssetId)
        {
            return null;
        }

        return CopyWithBaseTexture(source, IsPaintSlot(slotName, Reflects(source)) ? paint.Painted : paint.Unpainted);
    }

    /// <summary>
    /// A copy of a compiled <see cref="LitDiffuseMaterial"/> with every value of the cooked material, a new id and
    /// <paramref name="baseColor"/> for base texture. The new id keeps it out of the compiled-material cache, so the
    /// renderer draws this texture and resolves the render features on the mesh, as for a material built in code.
    /// </summary>
    internal static LitDiffuseMaterial CopyWithBaseTexture(LitDiffuseMaterial source, Texture2D baseColor)
    {
        return new LitDiffuseMaterial
        {
            Id = Guid.NewGuid(),
            Name = source.Name,
            ShaderAssetId = source.ShaderAssetId,
            IsTransparent = source.IsTransparent,
            Queue = source.Queue,
            AlphaCutoff = source.AlphaCutoff,
            CastShadows = source.CastShadows,
            ReceiveShadows = source.ReceiveShadows,
            BlendState = source.BlendState,
            DepthStencilState = source.DepthStencilState,
            RasterizerState = source.RasterizerState,
            SamplerState = source.SamplerState,
            BasColor = baseColor,
            NormalMap = source.NormalMap,
            NormalMapAssetId = source.NormalMapAssetId,
            ReflectionCube = source.ReflectionCube,
            ReflectionCubeAssetId = source.ReflectionCubeAssetId,
            UseSceneReflectionCube = source.UseSceneReflectionCube,
            DiffuseColor = source.DiffuseColor,
            AmbientColor = source.AmbientColor,
            EmissiveColor = source.EmissiveColor,
            SpecularColor = source.SpecularColor,
            SpecularPower = source.SpecularPower,
            TintColor = source.TintColor,
            TintStrength = source.TintStrength,
            TintMaskFromBaseAlpha = source.TintMaskFromBaseAlpha,
            ReflectionAddAmount = source.ReflectionAddAmount,
            ReflectionMultiplyBase = source.ReflectionMultiplyBase,
            ReflectionMultiplyFactor = source.ReflectionMultiplyFactor,
        };
    }

    /// <summary>
    /// The paint textures already built for the car <paramref name="carIndex"/>, or null when there are none yet. Read-only access for the
    /// capture validator, which checks their mipmap chain; it builds nothing and changes no state.
    /// </summary>
    internal static (Texture2D Painted, Texture2D Unpainted)? GetCarPaintTextures(int carIndex)
    {
        lock (CacheLock)
        {
            return CarPaints.TryGetValue(carIndex, out CarPaint? paint) && paint != null ? (paint.Painted, paint.Unpainted) : null;
        }
    }

    // The car's paint, built on first use and kept for the game's life; null when the texture cannot be read.
    private static CarPaint? GetCarPaint(int carIndex, AssetContentManager assetContentManager)
    {
        if (CarPaints.TryGetValue(carIndex, out CarPaint? paint))
        {
            return paint;
        }

        string texturePath = Path.Combine(GetProjectContentPath(), "Textures", CarDiffuseTextureNames[carIndex] + ".png");
        try
        {
            paint = File.Exists(texturePath) ? CarPaint.Load(texturePath, assetContentManager.GraphicsDevice) : null;
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            paint = null;
        }

        CarPaints[carIndex] = paint;
        return paint;
    }

    private static BoundingBox ComputeModelBounds(StaticModel model)
    {
        if (model.RootNode == null)
        {
            return GetDefaultCarBounds();
        }

        bool hasBounds = false;
        Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);
        ExpandNodeBounds(model, model.RootNode, Matrix.Identity, ref min, ref max, ref hasBounds);

        return hasBounds
            ? new BoundingBox(min, max)
            : GetDefaultCarBounds();
    }

    private static void ExpandNodeBounds(
        StaticModel model,
        StaticModelNode node,
        Matrix parentTransform,
        ref Vector3 min,
        ref Vector3 max,
        ref bool hasBounds)
    {
        Matrix absoluteTransform = node.LocalTransform * parentTransform;

        if (node.MeshIndex >= 0 && node.MeshIndex < model.Meshes.Count)
        {
            IReadOnlyList<VertexPositionNormalTexture> vertices = model.Meshes[node.MeshIndex].GetVertices();
            for (int index = 0; index < vertices.Count; index++)
            {
                Vector3 point = Vector3.Transform(vertices[index].Position, absoluteTransform);
                min = hasBounds ? Vector3.Min(min, point) : point;
                max = hasBounds ? Vector3.Max(max, point) : point;
                hasBounds = true;
            }
        }

        for (int index = 0; index < node.Children.Count; index++)
        {
            ExpandNodeBounds(model, node.Children[index], absoluteTransform, ref min, ref max, ref hasBounds);
        }
    }

    private static BoundingBox GetDefaultCarBounds()
    {
        return new BoundingBox(
            new Vector3(-LegacyCarCollisionWidth * 0.5f, -LegacyCarCollisionHeight * 0.5f, -LegacyCarCollisionLength * 0.5f),
            new Vector3(LegacyCarCollisionWidth * 0.5f, LegacyCarCollisionHeight * 0.5f, LegacyCarCollisionLength * 0.5f));
    }

    private static int NormalizeModulo(int value, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int normalized = value % count;
        return normalized < 0 ? normalized + count : normalized;
    }

    private static string GetProjectContentPath()
    {
        return !string.IsNullOrWhiteSpace(EngineEnvironment.ProjectPath)
            ? EngineEnvironment.ProjectPath
            : throw new InvalidOperationException("EngineEnvironment.ProjectPath must be configured before loading race content.");
    }

    /// <summary>
    /// A car texture decoded once, and two textures made from it: <see cref="Unpainted"/> (the texture as it is) and
    /// <see cref="Painted"/> (the texture painted with the selected colour where its alpha says, alpha kept), repainted
    /// in place when the colour changes. Magenta texels stay the transparent colour key, as when the texture was loaded
    /// directly.
    /// </summary>
    private sealed class CarPaint
    {
        // Rows painted per parallel chunk, and the paint buffer, shared by the cars: painting is done under CacheLock.
        private const int ChunkLength = 1 << 16;
        private static Color[] _painted = [];

        private readonly Color[] _source;
        private int _colorIndex = -1;

        private CarPaint(Color[] source, Texture2D unpainted, Texture2D painted)
        {
            _source = source;
            Unpainted = unpainted;
            Painted = painted;
        }

        public Texture2D Unpainted { get; }

        public Texture2D Painted { get; }

        public static CarPaint Load(string texturePath, GraphicsDevice graphicsDevice)
        {
            using FileStream fileStream = File.OpenRead(texturePath);
            ImageResult image = ImageResult.FromStream(fileStream, ColorComponents.RedGreenBlueAlpha);
            var source = new Color[image.Width * image.Height];

            for (int pixelIndex = 0, dataIndex = 0; pixelIndex < source.Length; pixelIndex++, dataIndex += 4)
            {
                byte red = image.Data[dataIndex];
                byte green = image.Data[dataIndex + 1];
                byte blue = image.Data[dataIndex + 2];
                byte alpha = image.Data[dataIndex + 3];
                bool isColorKey = red == LegacyTextureColorKey.R && green == LegacyTextureColorKey.G && blue == LegacyTextureColorKey.B;

                source[pixelIndex] = isColorKey ? LegacyTextureColorKey : new Color(red, green, blue, alpha);
            }

            // Both paint textures carry their full mipmap chain, as the original's content project builds it (S1m).
            var unpainted = new Texture2D(graphicsDevice, image.Width, image.Height, true, SurfaceFormat.Color);
            MipmapChain.UploadChain(unpainted, source);
            var painted = new Texture2D(graphicsDevice, image.Width, image.Height, true, SurfaceFormat.Color);
            MipmapChain.UploadChain(painted, source);
            return new CarPaint(source, unpainted, painted);
        }

        /// <summary>Paints <see cref="Painted"/> with the catalogue colour <paramref name="colorIndex"/>, unless it already is.</summary>
        public void Paint(int colorIndex)
        {
            if (colorIndex == _colorIndex || RaceFrontEndCatalog.CarColors.Count == 0)
            {
                return;
            }

            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            _colorIndex = colorIndex;
            Color color = RaceFrontEndCatalog.CarColors[colorIndex].Value;
            // Exact size: the chain upload checks it against the texture.
            if (_painted.Length != _source.Length)
            {
                _painted = new Color[_source.Length];
            }

            Color[] source = _source;
            Color[] painted = _painted;
            int chunkCount = (source.Length + ChunkLength - 1) / ChunkLength;
            System.Threading.Tasks.Parallel.For(0, chunkCount, chunk =>
            {
                int end = Math.Min(source.Length, (chunk + 1) * ChunkLength);
                for (int i = chunk * ChunkLength; i < end; i++)
                {
                    Color texel = source[i];
                    if (texel == LegacyTextureColorKey)
                    {
                        painted[i] = texel;
                        continue;
                    }

                    // lerp(rgb, colour, alpha), rounded.
                    int mask = texel.A;
                    painted[i] = new Color(
                        (byte)((texel.R * (255 - mask) + color.R * mask + 127) / 255),
                        (byte)((texel.G * (255 - mask) + color.G * mask + 127) / 255),
                        (byte)((texel.B * (255 - mask) + color.B * mask + 127) / 255),
                        texel.A);
                }
            });

            // The chain follows each paint: level 0 and every level below it are written again.
            MipmapChain.UploadChain(Painted, painted);
            Logs.WriteTrace($"Car paint: colour {colorIndex} painted in {System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:0.0} ms");
        }
    }
}