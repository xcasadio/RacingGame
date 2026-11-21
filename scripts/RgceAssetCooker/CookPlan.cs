using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Common;
using CasaEngine.Framework.Materials.Runtime;
using CasaEngine.Framework.Rendering.Models;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Import;

namespace RgceAssetCooker;

/// <summary>A texture asset (<c>Textures/&lt;Stem&gt;.texture</c>) wrapping one image of Content/Textures.</summary>
internal sealed record CookedTexture(string Stem, string Name, Guid Id, string FileName, string ImageFileName, Guid ImageId, string SamplerName);

/// <summary>A material asset and the recipe it was cooked from.</summary>
internal sealed class CookedMaterial
{
    public required string Name { get; init; }
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required RaceMaterialRecipe Recipe { get; init; }
    public required Guid BaseTextureId { get; init; }
    public required Guid NormalTextureId { get; init; }
    public required Guid DetailTextureId { get; init; }

    /// <summary>The emissive texture of the material, or empty when its recipe names none (the key is then not cooked).</summary>
    public required Guid EmissiveTextureId { get; init; }

    /// <summary>The occlusion texture of the material, or empty when its recipe names none (neither occlusion key is then cooked).</summary>
    public required Guid OcclusionTextureId { get; init; }

    // The states the renderer applies when the game leaves one null (RenderStateCache.cs:51-54, and the defaults of MaterialAsset).
    public string BlendState => Recipe.BlendState ?? MaterialAsset.DefaultBlendStateName;
    public string DepthStencilState => Recipe.DepthStencilState ?? MaterialAsset.DefaultDepthStencilStateName;
    public string RasterizerState => Recipe.RasterizerState ?? MaterialAsset.DefaultRasterizerStateName;
    public string SamplerState => Recipe.SamplerState ?? MaterialAsset.DefaultSamplerStateName;
}

/// <summary>A static model asset and the materials of its glTF, by glTF material index.</summary>
internal sealed class CookedModel
{
    public required string Name { get; init; }
    public required string GltfFileName { get; init; }
    public required string FileName { get; init; }
    public required bool HasLegacyRootCorrection { get; init; }
    public required StaticModel Model { get; init; }
    public required IReadOnlyList<CookedMaterial> Materials { get; init; }
}

internal sealed class CookPlan
{
    public List<CookedModel> Models { get; } = [];

    public List<CookedMaterial> TrackMaterials { get; } = [];

    public List<CookedTexture> Textures { get; } = [];

    public IEnumerable<CookedMaterial> AllMaterials => Models.SelectMany(model => model.Materials).Concat(TrackMaterials);
}

/// <summary>
/// Builds, in memory, every asset the cooker writes: reads the glTF models as the game does (LegacyGltfModelReader
/// with the RacingGame import profile), computes each material from the recipes of RaceMaterialRecipes, and wraps the
/// images they name (base, normal, detail, emissive and occlusion textures). Nothing is written here.
/// </summary>
internal static class CookPlanner
{
    public const string CarModelName = RaceMaterialRecipes.LegacyCarModelName;
    public const string PlateModelName = "CarSelectionPlate";

    // The painted car slots take this texture; the game replaces it with the painted copy at run time (T5.6).
    public const string CarTextureStem = "RacerCar";

    // LegacyTrackSceneFactory.LegacyXModelCorrection, applied to every model LoadLegacyModel loads.
    private static readonly Quaternion LegacyXModelCorrection = Quaternion.CreateFromAxisAngle(Vector3.Right, MathHelper.Pi / 2.0f);

    private static readonly string[] TrackRoles = ["Road", "Ground", "Terrain", "GuardRail", "RoadColumn", "RoadTunnel", "RoadBack"];

    public static CookPlan Build(string contentDirectory)
    {
        string modelsDirectory = Path.Combine(contentDirectory, "Models");
        string texturesDirectory = Path.GetFullPath(Path.Combine(contentDirectory, "Textures"));
        var textures = new TextureRegistry(texturesDirectory);
        var plan = new CookPlan();

        string[] gltfFiles = Directory.GetFiles(modelsDirectory, "*.gltf");
        Array.Sort(gltfFiles, StringComparer.OrdinalIgnoreCase);
        foreach (string gltfFile in gltfFiles)
        {
            plan.Models.Add(BuildModel(gltfFile, texturesDirectory, textures));
        }

        plan.TrackMaterials.AddRange(BuildTrackMaterials(texturesDirectory, textures));
        plan.Textures.AddRange(textures.Textures.OrderBy(texture => texture.Name, StringComparer.OrdinalIgnoreCase));
        return plan;
    }

    private static CookedModel BuildModel(string gltfFile, string texturesDirectory, TextureRegistry textures)
    {
        string modelName = Path.GetFileNameWithoutExtension(gltfFile);
        if (!LegacyGltfModelReader.IsFileSupported(gltfFile))
        {
            throw new InvalidOperationException($"{gltfFile}: not supported by the legacy glTF reader.");
        }

        StaticModelImportResult import = LegacyGltfModelReader.ReadWithMetadata(gltfFile, RacingGameImportProfiles.LegacyMaterialProfile);
        StaticModel model = import.Model;
        if (model.Meshes.Count == 0)
        {
            throw new InvalidOperationException($"{gltfFile}: the reader returned no mesh.");
        }

        // The occlusion texture of the scenery materials that lose the light of the sky: the black image of the road, or null.
        string occlusionPath = Path.Combine(texturesDirectory, RaceMaterialRecipes.RoadOcclusionTextureName);
        string? occlusionTexturePath = File.Exists(occlusionPath) ? occlusionPath : null;

        var usedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var materials = new List<CookedMaterial>(import.Materials.Count);
        foreach (StaticModelImportedMaterial imported in import.Materials)
        {
            string slot = UniqueSlot(imported, usedSlots);
            RaceMaterialRecipe recipe = modelName switch
            {
                CarModelName => RaceMaterialRecipes.ForCar(imported, 0, texturesDirectory),
                PlateModelName => RaceMaterialRecipes.ForPlate(modelName, imported),
                _ => RaceMaterialRecipes.ForScenery(modelName, imported, occlusionTexturePath),
            };

            string name = $"Material.{modelName}.{slot}";
            materials.Add(BuildMaterial(name, $"Materials/Models/{modelName}/{slot}.material", recipe, textures, texturesDirectory));
        }

        bool hasRootCorrection = modelName != CarModelName && modelName != PlateModelName && model.RootNode != null;
        if (hasRootCorrection)
        {
            var correctedRoot = new StaticModelNode
            {
                Name = $"{model.Name}.LegacyRootCorrection",
                Rotation = LegacyXModelCorrection,
            };
            correctedRoot.Children.Add(model.RootNode);
            model.RootNode = correctedRoot;
        }

        foreach (StaticModelMesh mesh in model.Meshes)
        {
            if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < materials.Count)
            {
                mesh.MaterialAssetId = materials[mesh.MaterialIndex].Id;
            }
        }

        string assetName = $"StaticModel.{modelName}";
        SetIdentity(model, assetName);

        return new CookedModel
        {
            Name = assetName,
            GltfFileName = Path.GetFileName(gltfFile),
            FileName = $"Models/{modelName}.staticModel",
            HasLegacyRootCorrection = hasRootCorrection,
            Model = model,
            Materials = materials,
        };
    }

    private static IEnumerable<CookedMaterial> BuildTrackMaterials(string texturesDirectory, TextureRegistry textures)
    {
        string? FirstExisting(IEnumerable<string> fileNames)
            => fileNames.Select(fileName => Path.Combine(texturesDirectory, fileName)).FirstOrDefault(File.Exists);

        string? ground = FirstExisting(RaceMaterialRecipes.GroundTextureCandidates);
        var recipes = new Dictionary<string, RaceMaterialRecipe>
        {
            ["Road"] = RaceMaterialRecipes.ForRoad(
                "Track",
                FirstExisting(RaceMaterialRecipes.RoadTextureCandidates),
                FirstExisting([RaceMaterialRecipes.RoadNormalTextureName]),
                FirstExisting([RaceMaterialRecipes.RoadOcclusionTextureName])),
            ["Ground"] = RaceMaterialRecipes.ForGround(
                "Track",
                FirstExisting([RaceMaterialRecipes.CityGroundTextureName]),
                FirstExisting([RaceMaterialRecipes.CityGroundNormalTextureName]),
                FirstExisting([RaceMaterialRecipes.RoadOcclusionTextureName])),
            ["Terrain"] = RaceMaterialRecipes.ForTerrain(
                "Track", ground,
                FirstExisting(RaceMaterialRecipes.TerrainNormalTextureCandidates),
                FirstExisting([RaceMaterialRecipes.TerrainDetailTextureName]),
                FirstExisting([RaceMaterialRecipes.RoadOcclusionTextureName])),
            ["GuardRail"] = RaceMaterialRecipes.ForGuardRail(
                "Track", "Left",
                FirstExisting([RaceMaterialRecipes.GuardRailTextureName]),
                FirstExisting([RaceMaterialRecipes.GuardRailNormalTextureName])),
            ["RoadColumn"] = RaceMaterialRecipes.ForRoadColumn(
                "Track",
                FirstExisting([RaceMaterialRecipes.RoadColumnTextureName]),
                FirstExisting([RaceMaterialRecipes.RoadColumnNormalTextureName])),
            ["RoadTunnel"] = RaceMaterialRecipes.ForRoadTunnel(
                "Track",
                FirstExisting([RaceMaterialRecipes.RoadTunnelTextureName]),
                FirstExisting([RaceMaterialRecipes.RoadTunnelNormalTextureName])),
            ["RoadBack"] = RaceMaterialRecipes.ForRoadBack(
                "Track",
                FirstExisting([RaceMaterialRecipes.RoadBackTextureName]),
                FirstExisting([RaceMaterialRecipes.RoadBackNormalTextureName])),
        };

        foreach (string role in TrackRoles)
        {
            yield return BuildMaterial($"Material.Track.{role}", $"Materials/Track/{role}.material", recipes[role], textures, texturesDirectory);
        }
    }

    private static CookedMaterial BuildMaterial(string name, string fileName, RaceMaterialRecipe recipe, TextureRegistry textures, string texturesDirectory)
    {
        string samplerName = recipe.SamplerState ?? MaterialAsset.DefaultSamplerStateName;
        string? baseTexturePath = recipe.BaseTextureRole == RaceBaseTextureRole.File
            ? recipe.BaseTextureFilePath
            : Path.Combine(texturesDirectory, CarTextureStem + ".png");

        return new CookedMaterial
        {
            Name = name,
            Id = Uuid5.ForAsset(name),
            FileName = fileName,
            Recipe = recipe,
            BaseTextureId = textures.Resolve(baseTexturePath, samplerName),
            NormalTextureId = textures.Resolve(recipe.NormalTextureFilePath, samplerName),
            DetailTextureId = textures.Resolve(recipe.DetailTextureFilePath, samplerName),
            EmissiveTextureId = textures.Resolve(recipe.EmissiveTextureFilePath, samplerName),
            OcclusionTextureId = textures.Resolve(recipe.OcclusionTextureFilePath, samplerName),
        };
    }

    // The material of a model is named after its display name; two materials of one model never share a file.
    private static string UniqueSlot(StaticModelImportedMaterial imported, HashSet<string> usedSlots)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string baseSlot = new string(imported.DisplayName.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray()).Trim();
        if (baseSlot.Length == 0)
        {
            baseSlot = $"Material {imported.MaterialIndex + 1}";
        }

        string slot = baseSlot;
        for (int suffix = 2; !usedSlots.Add(slot); suffix++)
        {
            slot = $"{baseSlot} {suffix}";
        }

        return slot;
    }

    /// <summary>Gives an asset the deterministic id and name of its catalogue entry (the engine types expose no setter for the id).</summary>
    public static void SetIdentity(ObjectBase asset, string name)
    {
        SetId(asset, Uuid5.ForAsset(name));
        asset.Name = name;
    }

    /// <summary>Gives an object a deterministic id and leaves its name alone (the worlds' entities and components keep the names the game gives them).</summary>
    public static void SetId(ObjectBase target, Guid id)
    {
        typeof(ObjectBase).GetProperty(nameof(ObjectBase.Id))!.GetSetMethod(nonPublic: true)!.Invoke(target, [id]);
    }

    /// <summary>The images of Content/Textures that cooked materials name, and the texture asset of each.</summary>
    private sealed class TextureRegistry
    {
        private readonly string _texturesDirectory;
        private readonly Dictionary<string, string> _actualFileNames;
        private readonly Dictionary<string, CookedTexture> _byStem = new(StringComparer.OrdinalIgnoreCase);

        public TextureRegistry(string texturesDirectory)
        {
            _texturesDirectory = texturesDirectory;
            _actualFileNames = Directory.GetFiles(texturesDirectory)
                .Select(path => Path.GetFileName(path))
                .ToDictionary(fileName => fileName, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<CookedTexture> Textures => _byStem.Values;

        /// <summary>The texture asset id of an image of Content/Textures, or empty for no image.</summary>
        public Guid Resolve(string? imagePath, string samplerName)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return Guid.Empty;
            }

            string actualFileName = ActualFileName(imagePath);
            if (!actualFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{imagePath}: only png images are cooked as .texture.");
            }

            string stem = Path.GetFileNameWithoutExtension(actualFileName);
            if (_byStem.TryGetValue(stem, out CookedTexture? existing))
            {
                if (existing.SamplerName != samplerName)
                {
                    throw new InvalidOperationException($"{stem} is used with the samplers {existing.SamplerName} and {samplerName}.");
                }

                return existing.Id;
            }

            string name = $"Texture.{stem}";
            var texture = new CookedTexture(
                stem, name, Uuid5.ForAsset(name), $"Textures/{stem}.texture", $"Textures/{actualFileName}", Uuid5.ForAsset($"Image.{stem}"), samplerName);
            _byStem[stem] = texture;
            return texture.Id;
        }

        private string ActualFileName(string imagePath)
        {
            string directory = Path.GetFullPath(Path.GetDirectoryName(imagePath)!);
            if (!directory.Equals(_texturesDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{imagePath}: an image outside Content/Textures cannot be catalogued by the cooker.");
            }

            return _actualFileNames.TryGetValue(Path.GetFileName(imagePath), out string? actual)
                ? actual
                : throw new FileNotFoundException(imagePath);
        }
    }
}
