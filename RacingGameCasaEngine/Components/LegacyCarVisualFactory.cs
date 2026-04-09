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
using StbImageSharp;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Components;

internal static class LegacyCarVisualFactory
{
    private const string LegacyCarModelName = "Car";
    private const float LegacyCarCollisionLength = 5.6f;
    private const float LegacyCarCollisionWidth = 2.6f;
    private const float LegacyCarCollisionHeight = 1.8f;
    private const float LegacyCarReflectionMultiplyBase = 0.85f;
    private const float LegacyCarReflectionMultiplyFactor = 0.75f;
    private const string CarNormalTextureFileName = "RacerCarNormal.tga";
    private static readonly Color LegacyTextureColorKey = new(255, 0, 255, 0);
    private static readonly string[] CarDiffuseTextureFileNames = ["RacerCar.tga", "RacerCar2.tga", "RacerCar3.tga"];

    private static readonly StaticModelImporter StaticModelImporter = new();
    private static readonly Texture2DLoader TextureLoader = new();
    private static readonly Dictionary<string, Texture2D?> TextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, TextureCube?> TextureCubeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<CarVariantKey, StaticModel?> ConfiguredCarModelCache = new();
    private static readonly object CacheLock = new();

    private static BoundingBox _cachedCarBounds;
    private static bool _hasCachedCarBounds;
    private static bool _hasLoggedMissingCarModel;
    private static bool _hasLoggedUnsupportedCarModel;

    internal static readonly Quaternion LegacyCarFacingCorrection = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.Pi);

    internal static StaticModel? LoadConfiguredCarModel(AssetContentManager assetContentManager, int selectedCarIndex, int selectedCarColorIndex)
    {
        ArgumentNullException.ThrowIfNull(assetContentManager);

        int normalizedCarIndex = NormalizeModulo(selectedCarIndex, CarDiffuseTextureFileNames.Length);
        int normalizedColorIndex = NormalizeModulo(selectedCarColorIndex, Math.Max(1, RaceFrontEndCatalog.CarColors.Count));
        var key = new CarVariantKey(normalizedCarIndex, normalizedColorIndex);

        lock (CacheLock)
        {
            if (ConfiguredCarModelCache.TryGetValue(key, out StaticModel? cachedModel))
            {
                return cachedModel;
            }

            string filePath = Path.Combine(GetProjectContentPath(), "Models", "Car.x");
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

            if (!StaticModelImporter.IsFileSupported(filePath))
            {
                if (!_hasLoggedUnsupportedCarModel)
                {
                    Logs.WriteWarning($"Legacy car model '{filePath}' uses an unsupported format for runtime import.");
                    _hasLoggedUnsupportedCarModel = true;
                }

                ConfiguredCarModelCache[key] = null;
                return null;
            }

            StaticModelImportResult importResult = StaticModelImporter.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);
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

    private static void ApplyImportedMaterials(
        StaticModel model,
        IReadOnlyList<StaticModelImportedMaterial> importedMaterials,
        CarVariantKey key,
        AssetContentManager assetContentManager)
    {
        foreach (StaticModelMesh mesh in model.Meshes)
        {
            LitDiffuseMaterial? material = null;

            if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < importedMaterials.Count)
            {
                StaticModelImportedMaterial importedMaterial = importedMaterials[mesh.MaterialIndex];
                material = CreateImportedRuntimeMaterial(LegacyCarModelName, importedMaterial, key, assetContentManager);
                mesh.Material = material;
            }

            for (int index = 0; index < mesh.SubMeshes.Count; index++)
            {
                mesh.SubMeshes[index].Material = material;
            }
        }
    }

    private static LitDiffuseMaterial CreateImportedRuntimeMaterial(
        string modelName,
        StaticModelImportedMaterial importedMaterial,
        CarVariantKey key,
        AssetContentManager assetContentManager)
    {
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning(modelName, importedMaterial);
        string? diffuseTexturePath = ResolveDiffuseTexturePath(importedMaterial, key.CarIndex);
        string? normalTexturePath = ResolveNormalTexturePath(importedMaterial);
        Texture2D? diffuseTexture = LoadTexture(diffuseTexturePath, assetContentManager);
        Texture2D? normalTexture = LoadTexture(normalTexturePath, assetContentManager);
        TextureCube? reflectionCube = tuning.EnableReflection
            ? LoadTextureCube(importedMaterial.ReflectionTextureFilePath, assetContentManager)
            : null;
        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(importedMaterial);
        Vector3 specularColor = tuning.ApplySpecularColor(importedMaterial.SpecularColor);
        float specularPower = Math.Clamp(tuning.ApplySpecularPower(importedMaterial.SpecularPower), 2f, 48f);
        (Vector3 tintColor, float tintStrength, float tintMaskFromBaseAlpha) = ResolveTintParameters(importedMaterial, key.ColorIndex);
        bool useLegacyCarReflectionBlend = UsesLegacyCarReflectionBlend(importedMaterial);

        return new LitDiffuseMaterial
        {
            Name = $"{modelName}.{importedMaterial.DisplayName}.Car{key.CarIndex}.Color{key.ColorIndex}",
            BasColor = diffuseTexture,
            NormalMap = normalTexture,
            ReflectionCube = reflectionCube,
            DiffuseColor = importedMaterial.DiffuseColor,
            AmbientColor = presentation.AmbientColor,
            EmissiveColor = presentation.EmissiveColor,
            SpecularColor = specularColor,
            SpecularPower = specularPower,
            TintColor = tintColor,
            TintStrength = tintStrength,
            TintMaskFromBaseAlpha = tintMaskFromBaseAlpha,
            ReflectionAddAmount = useLegacyCarReflectionBlend ? 0f : 1f,
            ReflectionMultiplyBase = useLegacyCarReflectionBlend ? LegacyCarReflectionMultiplyBase : 1f,
            ReflectionMultiplyFactor = useLegacyCarReflectionBlend ? LegacyCarReflectionMultiplyFactor : 0f,
            SamplerState = SamplerState.AnisotropicWrap,
            Queue = presentation.Queue,
            AlphaCutoff = presentation.AlphaCutoff,
            RasterizerState = presentation.DisableBackfaceCulling ? RasterizerState.CullNone : null,
        };
    }

    private static (Vector3 TintColor, float TintStrength, float TintMaskFromBaseAlpha) ResolveTintParameters(
        StaticModelImportedMaterial importedMaterial,
        int colorIndex)
    {
        if (!IsTintablePaintMaterial(importedMaterial) || colorIndex == 0 || RaceFrontEndCatalog.CarColors.Count == 0)
        {
            return (Vector3.One, 0f, 0f);
        }

        Color targetColor = RaceFrontEndCatalog.CarColors[NormalizeModulo(colorIndex, RaceFrontEndCatalog.CarColors.Count)].Value;
        return (targetColor.ToVector3(), 1f, 1f);
    }

    private static bool IsTintablePaintMaterial(StaticModelImportedMaterial importedMaterial)
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

    private static bool UsesLegacyCarReflectionBlend(StaticModelImportedMaterial importedMaterial)
    {
        if (!importedMaterial.UsesReflection)
        {
            return false;
        }

        string effectFileName = Path.GetFileName(importedMaterial.EffectFilePath ?? string.Empty);
        if (!effectFileName.Equals("NormalMapping.fx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string displayName = importedMaterial.DisplayName;
        return !displayName.Contains("glass", StringComparison.OrdinalIgnoreCase)
            && !displayName.Contains("fenster", StringComparison.OrdinalIgnoreCase)
            && !displayName.Contains("gummi", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveDiffuseTexturePath(StaticModelImportedMaterial importedMaterial, int carIndex)
    {
        if (!UsesCarDiffuseTexture(importedMaterial.DiffuseTextureFilePath))
        {
            return importedMaterial.DiffuseTextureFilePath;
        }

        return Path.Combine(GetProjectContentPath(), "Textures", CarDiffuseTextureFileNames[carIndex]);
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
            if (fileName.Equals(CarDiffuseTextureFileNames[index], StringComparison.OrdinalIgnoreCase))
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

            mesh.Material = new LitDiffuseMaterial
            {
                Name = $"{LegacyCarModelName}.{mesh.Name}.Fallback",
                DiffuseColor = new Color(196, 196, 196),
                AmbientColor = new Vector3(0.16f),
                EmissiveColor = new Vector3(0.02f),
                SpecularColor = new Vector3(0.32f),
                SpecularPower = 12f,
            };
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
            Texture2D texture = UsesCarDiffuseTexture(normalizedPath)
                ? LoadCarDiffuseTexture(normalizedPath, assetContentManager.GraphicsDevice)
                : (Texture2D)TextureLoader.LoadAsset(normalizedPath, assetContentManager);
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

    private static Texture2D LoadCarDiffuseTexture(string texturePath, GraphicsDevice graphicsDevice)
    {
        using FileStream fileStream = File.OpenRead(texturePath);
        ImageResult image = ImageResult.FromStream(fileStream, ColorComponents.RedGreenBlueAlpha);
        var pixelData = new Color[image.Width * image.Height];

        for (int pixelIndex = 0, dataIndex = 0; pixelIndex < pixelData.Length; pixelIndex++, dataIndex += 4)
        {
            byte red = image.Data[dataIndex];
            byte green = image.Data[dataIndex + 1];
            byte blue = image.Data[dataIndex + 2];
            byte alpha = image.Data[dataIndex + 3];

            pixelData[pixelIndex] = red == LegacyTextureColorKey.R
                && green == LegacyTextureColorKey.G
                && blue == LegacyTextureColorKey.B
                ? LegacyTextureColorKey
                : new Color(red, green, blue, alpha);
        }

        var texture = new Texture2D(graphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
        texture.SetData(pixelData);
        return texture;
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
            TextureCube texture = TextureCubeLoader.LoadTextureCube(normalizedPath, assetContentManager.GraphicsDevice);
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

    private readonly record struct CarVariantKey(int CarIndex, int ColorIndex);
}