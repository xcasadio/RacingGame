using CasaEngine.Framework.Assets.Textures;
using CasaEngine.Framework.Materials.Runtime;
using CasaEngine.Framework.Rendering.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Import;
using Texture = CasaEngine.Framework.Assets.Textures.Texture;

namespace RgceAssetCooker;

/// <summary>
/// Fidelity check, with no GPU. Rebuilds the expected assets from the glTF files and the recipes, then re-reads every cooked
/// file the way the engine does (StaticModel.Load, MaterialAsset.Load, Texture.Load from the JSON on disk) and compares:
/// - models: vertices, indices and nodes against the glTF read (the legacy root added where it applies), mesh and
///   material ids against the cooked materials;
/// - materials: every <c>lit-pbr</c> value against the recipe (family, base colour, detail texture, emissive), under the
///   effective-state rule (a state the game leaves null is the state the renderer applies), plus the render state the
///   engine resolves for the asset (MaterialRenderStateResolver);
/// - textures: ids, image reference and sampler;
/// - the catalogue: no duplicate id or name, every entry's file exists, every cooked asset catalogued.
/// </summary>
internal sealed class Verifier
{
    private readonly string _contentDirectory;
    private readonly List<string> _mismatches = [];
    private readonly List<string> _notes = [];

    public Verifier(string contentDirectory)
    {
        _contentDirectory = contentDirectory;
    }

    public IReadOnlyList<string> Mismatches => _mismatches;

    public IReadOnlyList<string> Notes => _notes;

    public int Models { get; private set; }
    public int Meshes { get; private set; }
    public int Vertices { get; private set; }
    public int MeshesWithTangents { get; private set; }
    public int Tangents { get; private set; }
    public int Indices { get; private set; }
    public int Nodes { get; private set; }
    public int Materials { get; private set; }
    public int MaterialValues { get; private set; }
    public int Textures { get; private set; }
    public int CatalogueEntries { get; private set; }

    public void Run()
    {
        CookPlan expected = CookPlanner.Build(_contentDirectory);
        CatalogFile catalog = CatalogFile.Load(Path.Combine(_contentDirectory, "AssetInfos.json"));

        foreach (CookedModel model in expected.Models)
        {
            VerifyModel(model);
        }

        VerifyCarPaintRule(expected);

        foreach (CookedMaterial material in expected.AllMaterials)
        {
            try
            {
                VerifyMaterial(material);
            }
            catch (InvalidDataException ex)
            {
                // A cooked file that lacks a value of its definition (cooked with another definition, for instance).
                Mismatch(ex.Message);
            }
        }

        foreach (CookedTexture texture in expected.Textures)
        {
            VerifyTexture(texture);
        }

        VerifyCatalogue(expected, catalog);
    }

    private void Mismatch(string text) => _mismatches.Add(text);

    private JObject? ReadJson(string relativeFile)
    {
        string path = Path.Combine(_contentDirectory, relativeFile);
        if (!File.Exists(path))
        {
            Mismatch($"{relativeFile}: missing");
            return null;
        }

        return JObject.Parse(File.ReadAllText(path));
    }

    // ---- models ----------------------------------------------------------------------------------------------

    private void VerifyModel(CookedModel cooked)
    {
        JObject? json = ReadJson(cooked.FileName);
        if (json == null)
        {
            return;
        }

        var actual = new StaticModel();
        actual.Load(json);
        StaticModel expected = cooked.Model;
        string where = cooked.FileName;
        Models++;

        if (actual.Id != expected.Id || actual.Name != expected.Name)
        {
            Mismatch($"{where}: id/name {actual.Id}/{actual.Name}, expected {expected.Id}/{expected.Name}");
        }

        CompareNode(where, "root", expected.RootNode, actual.RootNode);
        if (actual.Meshes.Count != expected.Meshes.Count)
        {
            Mismatch($"{where}: {actual.Meshes.Count} meshes, expected {expected.Meshes.Count}");
            return;
        }

        for (int index = 0; index < expected.Meshes.Count; index++)
        {
            CompareMesh($"{where} mesh {index}", expected.Meshes[index], actual.Meshes[index]);
        }
    }

    private void CompareNode(string where, string path, StaticModelNode? expected, StaticModelNode? actual)
    {
        if (expected == null || actual == null)
        {
            if (expected != actual)
            {
                Mismatch($"{where} node {path}: one side is null");
            }

            return;
        }

        Nodes++;
        if (actual.Name != expected.Name
            || actual.MeshIndex != expected.MeshIndex
            || actual.Position != expected.Position
            || actual.Rotation != expected.Rotation
            || actual.Scale != expected.Scale)
        {
            Mismatch($"{where} node {path}: '{actual.Name}' mesh {actual.MeshIndex} pos {actual.Position} rot {actual.Rotation} scale {actual.Scale}, "
                + $"expected '{expected.Name}' mesh {expected.MeshIndex} pos {expected.Position} rot {expected.Rotation} scale {expected.Scale}");
        }

        if (actual.Children.Count != expected.Children.Count)
        {
            Mismatch($"{where} node {path}: {actual.Children.Count} children, expected {expected.Children.Count}");
            return;
        }

        for (int index = 0; index < expected.Children.Count; index++)
        {
            CompareNode(where, $"{path}/{index}", expected.Children[index], actual.Children[index]);
        }
    }

    private void CompareMesh(string where, StaticModelMesh expected, StaticModelMesh actual)
    {
        Meshes++;
        if (actual.Name != expected.Name
            || actual.SlotName != expected.SlotName
            || actual.MaterialSlotIndex != expected.MaterialSlotIndex
            || actual.PrimitiveType != expected.PrimitiveType
            || actual.MaterialIndex != expected.MaterialIndex
            || actual.TextureAssetId != expected.TextureAssetId
            || actual.MaterialAssetId != expected.MaterialAssetId
            || actual.SubMeshes.Count != expected.SubMeshes.Count
            || actual.HasTangents != expected.HasTangents)
        {
            Mismatch($"{where}: mesh fields differ (name '{actual.Name}'/'{expected.Name}', slot {actual.MaterialSlotIndex}/{expected.MaterialSlotIndex}, "
                + $"material index {actual.MaterialIndex}/{expected.MaterialIndex}, material asset {actual.MaterialAssetId}/{expected.MaterialAssetId})");
        }

        IReadOnlyList<VertexPositionNormalTexture> expectedVertices = expected.GetVertices();
        IReadOnlyList<VertexPositionNormalTexture> actualVertices = actual.GetVertices();
        if (actualVertices.Count != expectedVertices.Count)
        {
            Mismatch($"{where}: {actualVertices.Count} vertices, expected {expectedVertices.Count}");
        }
        else
        {
            int different = 0;
            for (int index = 0; index < expectedVertices.Count; index++)
            {
                if (actualVertices[index].Position != expectedVertices[index].Position
                    || actualVertices[index].Normal != expectedVertices[index].Normal
                    || actualVertices[index].TextureCoordinate != expectedVertices[index].TextureCoordinate)
                {
                    different++;
                }
            }

            Vertices += expectedVertices.Count;
            if (different > 0)
            {
                Mismatch($"{where}: {different} of {expectedVertices.Count} vertices differ");
            }
        }

        // The tangents: same count as the vertices (the engine loader drops a mismatching array) and the same values.
        IReadOnlyList<VertexPositionNormalTextureTangent> expectedTangents = expected.GetTangentVertices();
        IReadOnlyList<VertexPositionNormalTextureTangent> actualTangents = actual.GetTangentVertices();
        if (actualTangents.Count != expectedTangents.Count)
        {
            Mismatch($"{where}: {actualTangents.Count} tangent vertices, expected {expectedTangents.Count}");
        }
        else
        {
            if (actual.HasTangents && actualTangents.Count != actualVertices.Count)
            {
                Mismatch($"{where}: {actualTangents.Count} tangents for {actualVertices.Count} vertices");
            }

            int differentTangents = 0;
            for (int index = 0; index < expectedTangents.Count; index++)
            {
                if (actualTangents[index].Tangent != expectedTangents[index].Tangent)
                {
                    differentTangents++;
                }
            }

            MeshesWithTangents += expected.HasTangents ? 1 : 0;
            Tangents += expectedTangents.Count;
            if (differentTangents > 0)
            {
                Mismatch($"{where}: {differentTangents} of {expectedTangents.Count} tangent vertices differ");
            }
        }

        IReadOnlyList<uint> expectedIndices = expected.GetIndices();
        IReadOnlyList<uint> actualIndices = actual.GetIndices();
        Indices += expectedIndices.Count;
        if (!actualIndices.SequenceEqual(expectedIndices))
        {
            Mismatch($"{where}: indices differ ({actualIndices.Count} read, {expectedIndices.Count} expected)");
        }
    }

    // ---- car paint rule -------------------------------------------------------------------------------------------

    // The game paints a car-textured slot when its cooked material has a clear coat (LegacyCarVisualFactory.IsPaintSlot, on
    // the clear_coat value read from the cooked file). For every mesh of the cooked car, it must give the role
    // RaceMaterialRecipes.ForCar gave the glTF material: car-textured or not, painted or not.
    private void VerifyCarPaintRule(CookPlan expected)
    {
        CookedModel? car = expected.Models.FirstOrDefault(model => model.Name == $"StaticModel.{CookPlanner.CarModelName}");
        if (car == null)
        {
            Mismatch("car paint rule: no Car model in the plan");
            return;
        }

        Guid carTextureId = Uuid5.ForAsset($"Texture.{CookPlanner.CarTextureStem}");
        var checkedMaterials = new HashSet<int>();
        foreach (StaticModelMesh mesh in car.Model.Meshes)
        {
            if (mesh.MaterialIndex < 0 || mesh.MaterialIndex >= car.Materials.Count)
            {
                continue;
            }

            CookedMaterial material = car.Materials[mesh.MaterialIndex];
            RaceBaseTextureRole oldRole = material.Recipe.BaseTextureRole;
            bool carTextured = material.BaseTextureId == carTextureId;
            bool painted = carTextured && LegacyCarVisualFactory.IsPaintSlot(new LitPbrMaterial { ClearCoat = ReadClearCoat(material) });
            RaceBaseTextureRole newRole = !carTextured
                ? RaceBaseTextureRole.File
                : painted ? RaceBaseTextureRole.CarPainted : RaceBaseTextureRole.CarUnpainted;
            if (newRole != oldRole)
            {
                Mismatch($"car paint rule: slot '{mesh.SlotName}' ({material.Name}) is {newRole}, RaceMaterialRecipes.ForCar says {oldRole}");
            }

            if (checkedMaterials.Add(mesh.MaterialIndex))
            {
                _notes.Add($"car paint rule: {material.Name} -> {newRole} (ForCar: {oldRole}), slot '{mesh.SlotName}'");
            }
        }

        _notes.Add($"car paint rule: {checkedMaterials.Count} of {car.Materials.Count} glTF materials compared, over {car.Model.Meshes.Count} meshes");
    }

    // The clear_coat value of the cooked file of a material, 0 when the file or the value is missing (VerifyMaterial reports it).
    private float ReadClearCoat(CookedMaterial cooked)
    {
        string path = Path.Combine(_contentDirectory, cooked.FileName);
        if (!File.Exists(path))
        {
            return 0f;
        }

        var actual = new MaterialAsset();
        actual.Load(JObject.Parse(File.ReadAllText(path)));
        return actual.TryGetPropertyValue("clear_coat", out MaterialValue? value) && value.TryGetFloat(out float clearCoat) ? clearCoat : 0f;
    }

    // ---- materials -------------------------------------------------------------------------------------------

    private void VerifyMaterial(CookedMaterial cooked)
    {
        JObject? json = ReadJson(cooked.FileName);
        if (json == null)
        {
            return;
        }

        var actual = new MaterialAsset();
        actual.Load(json);
        Materials++;
        string where = cooked.FileName;
        RaceMaterialRecipe recipe = cooked.Recipe;

        void Check<T>(string field, T actualValue, T expectedValue)
        {
            MaterialValues++;
            if (!EqualityComparer<T>.Default.Equals(actualValue, expectedValue))
            {
                Mismatch($"{where}: {field} is {actualValue}, recipe says {expectedValue}");
            }
        }

        Check("id", actual.Id, cooked.Id);
        Check("name", actual.Name, cooked.Name);
        Check("definition", actual.DefinitionId, "lit-pbr");
        Check("parent", actual.ParentMaterialAssetId, Guid.Empty);
        Check("shader", actual.ShaderAssetId, Guid.Empty);
        Check("is_transparent", actual.IsTransparent, false);
        Check("queue", actual.Queue, recipe.Queue);
        Check("cast_shadows", actual.CastShadows, true);
        Check("receive_shadows", actual.ReceiveShadows, true);
        // The effective-state rule: a state the game leaves null is the state the renderer applies.
        Check("blend_state", actual.BlendStateName, recipe.BlendState ?? "Opaque");
        Check("depth_stencil_state", actual.DepthStencilStateName, recipe.DepthStencilState ?? "Default");
        Check("rasterizer_state", actual.RasterizerStateName, recipe.RasterizerState ?? "CullCounterClockwise");
        Check("sampler_state", actual.SamplerStateName, recipe.SamplerState ?? "AnisotropicClamp");

        Check("base_color_texture", TextureId("base_color_texture"), cooked.BaseTextureId);
        Check("normal_texture", TextureId("normal_texture"), cooked.NormalTextureId);
        Check("detail_texture", TextureId("detail_texture"), cooked.DetailTextureId);
        Check("detail_tiling", Float("detail_tiling"), recipe.DetailTiling);
        Check("base_color", Value("base_color", (MaterialValue v) => v.TryGetColor(out Color c) ? c : default), recipe.BaseColor);
        Check("metallic", Float("metallic"), recipe.Metallic);
        Check("roughness", Float("roughness"), recipe.Roughness);
        Check("emissive_color", Vector("emissive_color"), recipe.EmissiveColor);
        // The key is only cooked for a recipe that names an emissive texture: an absent key reads as the null id.
        Check("emissive_texture", actual.TryGetPropertyValue("emissive_texture", out MaterialValue? emissiveTexture) && emissiveTexture.TryGetTextureId(out Guid emissiveTextureId) ? emissiveTextureId : Guid.Empty, cooked.EmissiveTextureId);
        // The key is only cooked for a recipe that asks for a non-default value: an absent key reads as the engine default, 0.
        Check("emissive_shadow_factor", actual.TryGetPropertyValue("emissive_shadow_factor", out MaterialValue? emissiveShadowFactor) && emissiveShadowFactor.TryGetFloat(out float emissiveShadowFactorValue) ? emissiveShadowFactorValue : 0f, recipe.EmissiveShadowFactor);
        Check("clear_coat", Float("clear_coat"), recipe.ClearCoat);
        Check("clear_coat_roughness", Float("clear_coat_roughness"), recipe.ClearCoatRoughness);
        Check("alpha_cutoff", Float("alpha_cutoff"), recipe.AlphaCutoff);

        // What the engine resolves for the asset (the compiler's ApplyCommonSettings): not transparent, the recipe's queue.
        var values = actual.PropertyValues.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        ResolvedMaterialRenderState resolved = MaterialRenderStateResolver.Resolve(actual, actual.GetRequiredDefinition(), values);
        Check("resolved is_transparent", resolved.IsTransparent, false);
        Check("resolved queue", resolved.Queue, recipe.Queue);
        Check("resolved blend", resolved.BlendStateName, recipe.BlendState ?? "Opaque");
        Check("resolved depth", resolved.DepthStencilStateName, recipe.DepthStencilState ?? "Default");

        T Value<T>(string key, Func<MaterialValue, T> read)
            => actual.TryGetPropertyValue(key, out MaterialValue? value) ? read(value) : throw new InvalidDataException($"{where}: {key} is missing");
        float Float(string key) => Value(key, v => v.TryGetFloat(out float f) ? f : float.NaN);
        Vector3 Vector(string key) => Value(key, v => v.TryGetVector3(out Vector3 vector) ? vector : new Vector3(float.NaN));
        Guid TextureId(string key) => Value(key, v => v.TryGetTextureId(out Guid id) ? id : new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));
    }

    // ---- textures --------------------------------------------------------------------------------------------

    private void VerifyTexture(CookedTexture cooked)
    {
        JObject? json = ReadJson(cooked.FileName);
        if (json == null)
        {
            return;
        }

        var actual = new Texture();
        actual.Load(json);
        Textures++;
        string where = cooked.FileName;
        if (actual.Id != cooked.Id || actual.Name != cooked.Name)
        {
            Mismatch($"{where}: id/name {actual.Id}/{actual.Name}, expected {cooked.Id}/{cooked.Name}");
        }

        string? imageId = json["texture_asset_id"]?.Value<string>();
        if (imageId != cooked.ImageId.ToString())
        {
            Mismatch($"{where}: texture_asset_id {imageId}, expected {cooked.ImageId}");
        }

        SamplerState expectedSampler = Cooker.ToSamplerState(cooked.SamplerName);
        SamplerState sampler = actual.PreferredSamplerState;
        if (sampler.Filter != expectedSampler.Filter
            || sampler.AddressU != expectedSampler.AddressU
            || sampler.AddressV != expectedSampler.AddressV
            || sampler.AddressW != expectedSampler.AddressW
            || sampler.MaxAnisotropy != expectedSampler.MaxAnisotropy)
        {
            Mismatch($"{where}: sampler {sampler.Filter}/{sampler.AddressU}/{sampler.MaxAnisotropy}, expected {cooked.SamplerName}");
        }

        if (json["generate_mipmaps"]?.Value<bool>() != true || !actual.GenerateMipmaps)
        {
            Mismatch($"{where}: generate_mipmaps {json["generate_mipmaps"]}, expected true");
        }

        if (!File.Exists(Path.Combine(_contentDirectory, cooked.ImageFileName)))
        {
            Mismatch($"{where}: image {cooked.ImageFileName} is missing");
        }
    }

    // ---- catalogue -------------------------------------------------------------------------------------------

    private void VerifyCatalogue(CookPlan expected, CatalogFile catalog)
    {
        CatalogueEntries = catalog.Entries.Count;
        foreach (var group in catalog.Entries.GroupBy(entry => entry.Id).Where(group => group.Count() > 1))
        {
            Mismatch($"catalogue: duplicate id {group.Key} ({string.Join(", ", group.Select(entry => entry.Name))})");
        }

        foreach (var group in catalog.Entries.GroupBy(entry => entry.Name).Where(group => group.Count() > 1))
        {
            Mismatch($"catalogue: duplicate name {group.Key}");
        }

        foreach (CatalogEntry entry in catalog.Entries)
        {
            if (!File.Exists(Path.Combine(_contentDirectory, CatalogFile.Normalize(entry.FileName))))
            {
                Mismatch($"catalogue: {entry.Name} names the missing file {entry.FileName}");
            }
        }

        foreach (CatalogEntry owned in Cooker.OwnedEntries(expected))
        {
            CatalogEntry? entry = catalog.FindByName(owned.Name);
            if (entry != owned)
            {
                Mismatch($"catalogue: entry {owned.Name} is {(entry == null ? "missing" : $"{entry.Id} {entry.FileName} {entry.AssetType}")}, expected {owned.Id} {owned.FileName} {owned.AssetType}");
            }
        }

        // The png entries are renamed Image.<stem> by generate_rgce_content_assets.py: informational only.
        int unrenamed = 0;
        foreach (CookedTexture texture in expected.Textures)
        {
            CatalogEntry? image = catalog.FindByName($"Image.{texture.Stem}");
            if (image == null || image.Id != texture.ImageId.ToString() || !string.Equals(CatalogFile.Normalize(image.FileName), texture.ImageFileName, StringComparison.OrdinalIgnoreCase))
            {
                unrenamed++;
            }
        }

        if (unrenamed > 0)
        {
            _notes.Add($"{unrenamed} of {expected.Textures.Count} textures have no Image.<stem> png entry yet: run scripts/generate_rgce_content_assets.py");
        }
    }
}
