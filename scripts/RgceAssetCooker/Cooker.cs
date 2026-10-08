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
        var asset = new MaterialAsset("lit-diffuse");
        CookPlanner.SetIdentity(asset, material.Name);
        asset.Queue = recipe.Queue;
        asset.BlendStateName = material.BlendState;
        asset.DepthStencilStateName = material.DepthStencilState;
        asset.RasterizerStateName = material.RasterizerState;
        asset.SamplerStateName = material.SamplerState;

        asset.SetPropertyValue("base_color_texture", MaterialValue.FromTextureId(material.BaseTextureId));
        asset.SetPropertyValue("normal_texture", MaterialValue.FromTextureId(material.NormalTextureId));
        asset.SetPropertyValue("reflection_texture", MaterialValue.FromTextureId(material.ReflectionTextureId));
        asset.SetPropertyValue("diffuse_color", MaterialValue.FromColor(recipe.DiffuseColor));
        asset.SetPropertyValue("alpha_cutoff", MaterialValue.FromFloat(recipe.AlphaCutoff));
        asset.SetPropertyValue("ambient_color", MaterialValue.FromVector3(recipe.AmbientColor));
        asset.SetPropertyValue("emissive_color", MaterialValue.FromVector3(recipe.EmissiveColor));
        asset.SetPropertyValue("specular_color", MaterialValue.FromVector3(recipe.SpecularColor));
        asset.SetPropertyValue("specular_power", MaterialValue.FromFloat(recipe.SpecularPower));
        asset.SetPropertyValue("use_scene_reflection_cube", MaterialValue.FromBoolean(material.UseSceneReflectionCube));
        asset.SetPropertyValue("reflection_add_amount", MaterialValue.FromFloat(recipe.ReflectionAddAmount));
        asset.SetPropertyValue("reflection_multiply_base", MaterialValue.FromFloat(recipe.ReflectionMultiplyBase));
        asset.SetPropertyValue("reflection_multiply_factor", MaterialValue.FromFloat(recipe.ReflectionMultiplyFactor));
        return asset;
    }

    private static Texture CreateTexture(CookedTexture cooked)
    {
        var texture = new Texture(Uuid5.ForAsset($"Image.{cooked.Stem}"), null!)
        {
            PreferredSamplerState = ToSamplerState(cooked.SamplerName),
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
