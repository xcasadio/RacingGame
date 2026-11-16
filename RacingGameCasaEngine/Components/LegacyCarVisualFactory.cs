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
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.Worlds;
using StbImageSharp;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Components;

/// <summary>
/// Builds RacingGame's car for the race and the car selection: one model per car type, from Car.gltf with the car's
/// texture (RacerCar, RacerCar2, RacerCar3). The selected colour is painted into the texture as RacingGame's shader
/// painted it (<c>NormalMapping.fx</c>, SpecularWithReflectionForCar20: <c>lerp(rgb, carHueColor, texture alpha)</c>),
/// on the CPU since CasaEngine's shaders have no such tint (ADR-0009). The paint material samples the painted texture,
/// the car's other materials the texture as it is. The texture's alpha, the paint mask, stays in the texture; the PBR
/// materials (<see cref="RacingGamePbrMaterialMapping"/>) are opaque and ignore it. The paint is a dielectric under a
/// clear coat, the chromes and the windows are smooth metals, the tyres a rough dielectric (plan rgce-pbr-race-world,
/// "Correspondance"); their reflections come from the sky cube of the world.
/// </summary>
internal static class LegacyCarVisualFactory
{
    private const string LegacyCarModelName = "Car";
    private const float LegacyCarCollisionLength = RacingCarPawn.CollisionLength;
    private const float LegacyCarCollisionWidth = RacingCarPawn.CollisionWidth;
    private const float LegacyCarCollisionHeight = RacingCarPawn.CollisionHeight;
    private const string CarNormalTextureFileName = "RacerCarNormal.png";
    private static readonly Color LegacyCarDefaultDiffuseColor = new(210, 210, 210);
    private static readonly Color LegacyTextureColorKey = new(255, 0, 255, 0);
    private static readonly string[] CarDiffuseTextureFileNames = ["RacerCar.tga", "RacerCar2.tga", "RacerCar3.tga"];

    // Race-world textures carry their full mip chain, as the original builds them (GenerateMipmaps=True in its content project).
    private static readonly Texture2DLoader TextureLoader = new() { GenerateMipmaps = true };
    private static readonly Dictionary<string, Texture2D?> TextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, StaticModel?> ConfiguredCarModelCache = new();
    private static readonly Dictionary<int, CarPaint?> CarPaints = new();
    private static readonly object CacheLock = new();

    private static BoundingBox _cachedCarBounds;
    private static bool _hasCachedCarBounds;
    private static bool _hasLoggedMissingCarModel;
    private static bool _hasLoggedUnsupportedCarModel;
    private static bool _hasLoggedTextureLevels;
    private static bool _hasLoggedPaintLevels;

    internal static readonly Quaternion LegacyCarFacingCorrection = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi);

    internal static StaticModel? LoadConfiguredCarModel(AssetContentManager assetContentManager, int selectedCarIndex, int selectedCarColorIndex)
    {
        ArgumentNullException.ThrowIfNull(assetContentManager);

        int normalizedCarIndex = NormalizeModulo(selectedCarIndex, CarDiffuseTextureFileNames.Length);
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

            string filePath = Path.Combine(GetProjectContentPath(), "Models", "Car.gltf");
            if (!File.Exists(filePath))
            {
                if (!_hasLoggedMissingCarModel)
                {
                    Logs.WriteWarning($"Legacy car model not found at '{filePath}'.");
                    _hasLoggedMissingCarModel = true;
                }

                ConfiguredCarModelCache[key] = null;
                return null;
            }

            if (!LegacyGltfModelReader.IsFileSupported(filePath))
            {
                if (!_hasLoggedUnsupportedCarModel)
                {
                    Logs.WriteWarning($"Legacy car model '{filePath}' uses an unsupported format for runtime import.");
                    _hasLoggedUnsupportedCarModel = true;
                }

                ConfiguredCarModelCache[key] = null;
                return null;
            }

            StaticModelImportResult importResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);
            StaticModel model = importResult.Model;
            ApplyImportedMaterials(model, importResult.Materials, key, assetContentManager);
            ApplyFallbackMaterials(model);

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

    private static void ApplyImportedMaterials(
        StaticModel model,
        IReadOnlyList<StaticModelImportedMaterial> importedMaterials,
        int carIndex,
        AssetContentManager assetContentManager)
    {
        foreach (StaticModelMesh mesh in model.Meshes)
        {
            LitPbrMaterial? material = null;

            if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < importedMaterials.Count)
            {
                StaticModelImportedMaterial importedMaterial = importedMaterials[mesh.MaterialIndex];
                material = CreateImportedRuntimeMaterial(LegacyCarModelName, importedMaterial, carIndex, assetContentManager);
                mesh.Material = material;
            }

            for (int index = 0; index < mesh.SubMeshes.Count; index++)
            {
                mesh.SubMeshes[index].Material = material;
            }
        }
    }

    private static LitPbrMaterial CreateImportedRuntimeMaterial(
        string modelName,
        StaticModelImportedMaterial importedMaterial,
        int carIndex,
        AssetContentManager assetContentManager)
    {
        string? normalTexturePath = ResolveNormalTexturePath(importedMaterial);
        Texture2D? diffuseTexture;
        if (UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath))
        {
            CarPaint? paint = GetCarPaint(carIndex, assetContentManager);
            diffuseTexture = paint == null ? null : IsPaintMaterial(importedMaterial) ? paint.Painted : paint.Unpainted;
        }
        else
        {
            diffuseTexture = LoadTexture(importedMaterial.DiffuseTextureFilePath, assetContentManager);
        }
        Texture2D? normalTexture = LoadTexture(normalTexturePath, assetContentManager);

        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        // The original draws every car part but the glass with the default diffuse (210), whatever its .X material says
        // (RacingGameOriginal Model.RenderCar: Material.DefaultDiffuseColor); it becomes the base colour (P9).
        bool usesLegacyCarDefaultColor = !IsGlassMaterial(importedMaterial);

        return RacingGamePbrMaterialMapping.Create(
            $"{modelName}.{importedMaterial.DisplayName}.Car{carIndex}",
            ResolveFamily(importedMaterial),
            diffuseTexture,
            normalTexture,
            usesLegacyCarDefaultColor ? LegacyCarDefaultDiffuseColor : importedMaterial.DiffuseColor,
            presentation.DisableBackfaceCulling ? RasterizerState.CullNone : null,
            SamplerState.AnisotropicWrap,
            presentation.EmissiveColor,
            queue: presentation.Queue,
            alphaCutoff: presentation.AlphaCutoff);
    }

    // The car's families by material name (plan rgce-pbr-race-world, "Correspondance"): paint (lack, paint) under a clear
    // coat, chrome, windows (glass, fenster, or the glass effect), tyres (gummi); anything else is the default.
    private static RacingGamePbrFamily ResolveFamily(StaticModelImportedMaterial importedMaterial)
    {
        string displayName = importedMaterial.DisplayName;
        if (IsGlassMaterial(importedMaterial))
        {
            return RacingGamePbrMaterialMapping.CarGlass;
        }

        if (displayName.Contains("chrome", StringComparison.OrdinalIgnoreCase))
        {
            return RacingGamePbrMaterialMapping.CarChrome;
        }

        if (displayName.Contains("gummi", StringComparison.OrdinalIgnoreCase))
        {
            return RacingGamePbrMaterialMapping.CarTyre;
        }

        return IsPaintMaterial(importedMaterial)
            ? RacingGamePbrMaterialMapping.CarPaint
            : RacingGamePbrMaterialMapping.Default;
    }

    private static bool IsGlassMaterial(StaticModelImportedMaterial importedMaterial)
    {
        string displayName = importedMaterial.DisplayName;
        return displayName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("fenster", StringComparison.OrdinalIgnoreCase)
            || importedMaterial.EffectFilePath?.Contains("ReflectionSimpleGlass", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsPaintMaterial(StaticModelImportedMaterial importedMaterial)
    {
        string displayName = importedMaterial.DisplayName;
        if (displayName.Contains("chrome", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("gummi", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("fenster", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (displayName.Contains("lack", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("paint", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return importedMaterial.UsesReflection && UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath);
    }

    private static string? ResolveNormalTexturePath(StaticModelImportedMaterial importedMaterial)
    {
        if (UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath)
            || UsesCarNormalTexture(importedMaterial.NormalTextureFilePath))
        {
            return Path.Combine(GetProjectContentPath(), "Textures", CarNormalTextureFileName);
        }

        return importedMaterial.NormalTextureFilePath;
    }

    private static bool UsesCarDiffuseTexture(string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return false;
        }

        string fileName = Path.GetFileName(texturePath);
        for (int index = 0; index < CarDiffuseTextureFileNames.Length; index++)
        {
            if (RacingGamePbrMaterialMapping.IsSameTexture(fileName, CarDiffuseTextureFileNames[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesCarNormalTexture(string? texturePath)
    {
        return !string.IsNullOrWhiteSpace(texturePath)
            && Path.GetFileName(texturePath).Equals(CarNormalTextureFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyFallbackMaterials(StaticModel model)
    {
        foreach (StaticModelMesh mesh in model.Meshes)
        {
            if (mesh.Material != null)
            {
                continue;
            }

            mesh.Material = RacingGamePbrMaterialMapping.Create(
                $"{LegacyCarModelName}.{mesh.Name}.Fallback",
                RacingGamePbrMaterialMapping.Default,
                null,
                null,
                new Color(196, 196, 196),
                null,
                SamplerState.AnisotropicWrap,
                new Vector3(0.02f));
        }
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
            Texture2D texture = (Texture2D)TextureLoader.LoadAsset(normalizedPath, assetContentManager);
            if (!_hasLoggedTextureLevels)
            {
                _hasLoggedTextureLevels = true;
                Logs.WriteInfo($"Car textures: '{Path.GetFileName(normalizedPath)}' {texture.Width}x{texture.Height} loaded with {texture.LevelCount} mipmap level(s).");
            }

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

    // The car's paint, built on first use and kept for the game's life; null when the texture cannot be read.
    private static CarPaint? GetCarPaint(int carIndex, AssetContentManager assetContentManager)
    {
        if (CarPaints.TryGetValue(carIndex, out CarPaint? paint))
        {
            return paint;
        }

        string texturePath = Path.Combine(GetProjectContentPath(), "Textures", CarDiffuseTextureFileNames[carIndex]);
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
        // Rows painted per parallel chunk, and the paint buffer (the cars share it when their textures have the same size): painting is done under CacheLock.
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

            var unpainted = new Texture2D(graphicsDevice, image.Width, image.Height, true, SurfaceFormat.Color);
            MipmapChain.UploadChain(unpainted, source);
            var painted = new Texture2D(graphicsDevice, image.Width, image.Height, true, SurfaceFormat.Color);
            MipmapChain.UploadChain(painted, source);
            if (!_hasLoggedPaintLevels)
            {
                _hasLoggedPaintLevels = true;
                Logs.WriteInfo($"Car paint textures: '{Path.GetFileName(texturePath)}' {image.Width}x{image.Height} created with {painted.LevelCount} mipmap level(s).");
            }

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
            // Exact size: the chain upload checks it against the texture. Rebuilt with the chain at each colour change.
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

            MipmapChain.UploadChain(Painted, painted);
            Logs.WriteTrace($"Car paint: colour {colorIndex} painted in {System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:0.0} ms");
        }
    }
}