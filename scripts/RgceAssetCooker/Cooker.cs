using CasaEngine.Engine.Environment;
using CasaEngine.EditorServices;
using CasaEngine.Framework.Assets.Textures;
using CasaEngine.Framework.Materials.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Texture = CasaEngine.Framework.Assets.Textures.Texture;

namespace RgceAssetCooker;

/// <summary>Writes the planned assets with the editor serializers and keeps the cooker's catalogue entries.</summary>
internal sealed class Cooker
{
    private readonly string _contentDirectory;
    private readonly CookPlan _plan;
    private readonly CatalogFile _catalog;

    public Cooker(string contentDirectory, CookPlan plan, CatalogFile catalog)
    {
        _contentDirectory = contentDirectory;
        _plan = plan;
        _catalog = catalog;
    }

    public int FilesWritten { get; private set; }

    public int FilesUnchanged { get; private set; }

    public int CatalogueEntriesChanged { get; private set; }

    /// <summary>Every catalogue entry the cooker owns, in the stable order it appends them: models, materials, textures.</summary>
    public static IEnumerable<CatalogEntry> OwnedEntries(CookPlan plan)
    {
        foreach (CookedModel model in plan.Models)
        {
            yield return new CatalogEntry(Uuid5.ForAsset(model.Name).ToString(), model.Name, model.FileName, "staticmodel");
        }

        foreach (CookedMaterial material in plan.AllMaterials)
        {
            yield return new CatalogEntry(material.Id.ToString(), material.Name, material.FileName, "material");
        }

        foreach (CookedTexture texture in plan.Textures)
        {
            yield return new CatalogEntry(texture.Id.ToString(), texture.Name, texture.FileName, "texture");
        }
    }

    public void Run()
    {
        string staging = Path.Combine(Path.GetTempPath(), "RgceAssetCooker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        string previousProjectPath = EngineEnvironment.ProjectPath;
        try
        {
            // The editor writer saves under EngineEnvironment.ProjectPath: write to a scratch folder, then copy only the
            // files whose bytes changed, so a second run touches nothing.
            EngineEnvironment.ProjectPath = staging;
            foreach (CookedModel model in _plan.Models)
            {
                Save(staging, model.FileName, model.Model);
            }

            foreach (CookedMaterial material in _plan.AllMaterials)
            {
                Save(staging, material.FileName, CreateMaterialAsset(material));
            }

            foreach (CookedTexture texture in _plan.Textures)
            {
                Save(staging, texture.FileName, CreateTexture(texture));
            }
        }
        finally
        {
            EngineEnvironment.ProjectPath = previousProjectPath;
            Directory.Delete(staging, recursive: true);
        }

        foreach (CatalogEntry entry in OwnedEntries(_plan))
        {
            if (_catalog.Upsert(entry))
            {
                CatalogueEntriesChanged++;
            }
        }

        CatalogFileGuard.ThrowOnDuplicates(_catalog);
        byte[] catalogBytes = _catalog.ToBytes();
        if (!File.Exists(_catalog.Path) || !File.ReadAllBytes(_catalog.Path).AsSpan().SequenceEqual(catalogBytes))
        {
            File.WriteAllBytes(_catalog.Path, catalogBytes);
        }
    }

    private void Save(string staging, string relativeFile, object asset)
    {
        string stagedFile = Path.Combine(staging, relativeFile);
        Directory.CreateDirectory(Path.GetDirectoryName(stagedFile)!);
        EditorAssetWriterService.SaveAsset(relativeFile, asset);

        byte[] bytes = File.ReadAllBytes(stagedFile);
        string target = Path.Combine(_contentDirectory, relativeFile);
        if (File.Exists(target) && File.ReadAllBytes(target).AsSpan().SequenceEqual(bytes))
        {
            FilesUnchanged++;
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllBytes(target, bytes);
        FilesWritten++;
    }

    private static MaterialAsset CreateMaterialAsset(CookedMaterial material)
    {
        var recipe = material.Recipe;
        var asset = new MaterialAsset("lit-pbr");
        CookPlanner.SetIdentity(asset, material.Name);
        asset.Queue = recipe.Queue;
        asset.BlendStateName = material.BlendState;
        asset.DepthStencilStateName = material.DepthStencilState;
        asset.RasterizerStateName = material.RasterizerState;
        asset.SamplerStateName = material.SamplerState;

        asset.SetPropertyValue("base_color_texture", MaterialValue.FromTextureId(material.BaseTextureId));
        asset.SetPropertyValue("normal_texture", MaterialValue.FromTextureId(material.NormalTextureId));
        asset.SetPropertyValue("detail_texture", MaterialValue.FromTextureId(material.DetailTextureId));
        asset.SetPropertyValue("detail_tiling", MaterialValue.FromFloat(recipe.DetailTiling));
        asset.SetPropertyValue("base_color", MaterialValue.FromColor(recipe.BaseColor));
        asset.SetPropertyValue("metallic", MaterialValue.FromFloat(recipe.Metallic));
        asset.SetPropertyValue("roughness", MaterialValue.FromFloat(recipe.Roughness));
        asset.SetPropertyValue("emissive_color", MaterialValue.FromVector3(recipe.EmissiveColor));
        if (material.EmissiveTextureId != Guid.Empty)
        {
            // Only the road tunnel and the signs have an emissive texture: the other materials keep their files as they are, without the key.
            asset.SetPropertyValue("emissive_texture", MaterialValue.FromTextureId(material.EmissiveTextureId));
        }

        if (recipe.EmissiveShadowFactor != 0f)
        {
            // Only the road tunnel asks for a shadowed emissive: the key is absent otherwise, and the engine default (0) applies.
            asset.SetPropertyValue("emissive_shadow_factor", MaterialValue.FromFloat(recipe.EmissiveShadowFactor));
        }

        if (material.OcclusionTextureId != Guid.Empty)
        {
            // Only the road has an occlusion texture (it scales the image-based light only): the other materials keep their files as they are, without either key.
            asset.SetPropertyValue("occlusion_texture", MaterialValue.FromTextureId(material.OcclusionTextureId));
            asset.SetPropertyValue("occlusion_strength", MaterialValue.FromFloat(recipe.OcclusionStrength));
        }

        asset.SetPropertyValue("clear_coat", MaterialValue.FromFloat(recipe.ClearCoat));
        asset.SetPropertyValue("clear_coat_roughness", MaterialValue.FromFloat(recipe.ClearCoatRoughness));
        asset.SetPropertyValue("alpha_cutoff", MaterialValue.FromFloat(recipe.AlphaCutoff));
        return asset;
    }

    private static Texture CreateTexture(CookedTexture cooked)
    {
        var texture = new Texture(Uuid5.ForAsset($"Image.{cooked.Stem}"), null!)
        {
            PreferredSamplerState = ToSamplerState(cooked.SamplerName),
            // The textures of the race world are drawn with their mipmap chain, as the original's content project builds them.
            GenerateMipmaps = true,
        };
        CookPlanner.SetIdentity(texture, cooked.Name);
        return texture;
    }

    public static SamplerState ToSamplerState(string name) => name switch
    {
        "AnisotropicWrap" => SamplerState.AnisotropicWrap,
        "AnisotropicClamp" => SamplerState.AnisotropicClamp,
        "LinearWrap" => SamplerState.LinearWrap,
        "LinearClamp" => SamplerState.LinearClamp,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown sampler state."),
    };
}

internal static class CatalogFileGuard
{
    public static void ThrowOnDuplicates(CatalogFile catalog)
    {
        string[] duplicateIds = catalog.Entries.GroupBy(entry => entry.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        string[] duplicateNames = catalog.Entries.GroupBy(entry => entry.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateIds.Length > 0 || duplicateNames.Length > 0)
        {
            throw new InvalidOperationException(
                $"duplicate catalogue id(s) [{string.Join(", ", duplicateIds)}] or name(s) [{string.Join(", ", duplicateNames)}]");
        }
    }
}
