using System.ComponentModel;
using System.Reflection;
using CasaEngine.Core.Serialization;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Scene.Entities;
using CasaEngine.Framework.Scene.Entities.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using World = CasaEngine.Framework.Scene.World.World;

namespace RgceAssetCooker;

/// <summary>
/// <c>--worlds --verify</c>. Re-reads the five worlds and the player car with the engine's public loaders only
/// (<c>World.Load</c> on the file, <c>Entity.Load</c> on each inline entity node and on the car file, never a private field)
/// and compares them with the game's constructors run again:
/// - the world: id, name, the keys of the file, the environment key by key against what <c>WorldEnvironmentSettings.Save</c>
///   writes for the expected environment, the id of the sky cube, the number of entities, the id of each file against its
///   catalogue entry;
/// - each entity, in order: name, id, policies, components (type, name, id, tree of children), local transforms to 1e-6, the
///   model id and shadow flags of the static models, the light values, the camera values, the player start, and every
///   <c>[SerializedProperty]</c> (checkpoint half extents, track and height map ids, generator sides, car anchors);
/// - the car: the same, against <c>RaceCarEntityFactory.Create()</c> renamed <c>Entity.PlayerCar</c>.
/// </summary>
internal sealed class WorldVerifier
{
    private const float Tolerance = 1e-6f;
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    private static readonly string[] WorldKeys =
    [
        "id", "name", "entity_references", "script_class_name", "space_policy", "player_startup_settings_asset_id",
        "gameplay_mode_asset_id", "environment",
    ];

    // Entities of each world: the front end 2 (camera, player start); a race 5 generators + the scenery counted in M1 (649, 904, 1745)
    // + camera, RaceRoot, Light.Key, 3 checkpoints, player start; the carousel 4 (light, three plates).
    private static readonly Dictionary<string, int> ExpectedEntityCounts = new()
    {
        ["World.FrontEnd"] = 2,
        ["World.Race.TrackBeginner"] = 661,
        ["World.Race.TrackAdvanced"] = 916,
        ["World.Race.TrackExpert"] = 1757,
        ["World.CarSelection"] = 4,
    };

    private readonly string _contentDirectory;
    private readonly List<string> _mismatches = [];
    private string _scope = string.Empty;

    public WorldVerifier(string contentDirectory)
    {
        _contentDirectory = contentDirectory;
    }

    public IReadOnlyList<string> Mismatches => _mismatches;

    public int Files { get; private set; }

    public int Entities { get; private set; }

    public int Components { get; private set; }

    public void Run()
    {
        WorldAssets expected = WorldAssets.Build(_contentDirectory);
        CatalogFile catalog = CatalogFile.Load(Path.Combine(_contentDirectory, "AssetInfos.json"));

        foreach (WorldAsset world in expected.Worlds)
        {
            VerifyWorld(world, expected.SkyCubeId, catalog);
        }

        VerifyCar(expected.Car, catalog);
    }

    private void Mismatch(string text) => _mismatches.Add($"{_scope}: {text}");

    private JObject? ReadJson(string relativeFile)
    {
        _scope = relativeFile;
        string path = Path.Combine(_contentDirectory, relativeFile);
        if (!File.Exists(path))
        {
            Mismatch("missing");
            return null;
        }

        Files++;
        return JObject.Parse(File.ReadAllText(path));
    }

    // ---- worlds ----------------------------------------------------------------------------------------------

    private void VerifyWorld(WorldAsset expected, Guid skyCubeId, CatalogFile catalog)
    {
        JObject? json = ReadJson(expected.FileName);
        if (json == null)
        {
            return;
        }

        VerifyCatalogueEntry(expected.EntryName, expected.FileName, expected.World.Id, "world", catalog);

        if (!json.Properties().Select(property => property.Name).SequenceEqual(WorldKeys))
        {
            Mismatch($"the keys are [{string.Join(", ", json.Properties().Select(property => property.Name))}], expected [{string.Join(", ", WorldKeys)}]");
        }

        var actual = new World();
        actual.Load(json);
        if (actual.Id != expected.World.Id)
        {
            Mismatch($"id {actual.Id}, expected {expected.World.Id}");
        }

        if (actual.Name != expected.World.Name)
        {
            Mismatch($"name '{actual.Name}', expected '{expected.World.Name}'");
        }

        if (!string.IsNullOrEmpty(actual.GameplayProxyClassName) || !string.IsNullOrEmpty(actual.SpacePolicyName)
            || actual.PlayerStartupSettingsAssetId != Guid.Empty || actual.GameplayModeAssetId != Guid.Empty
            || actual.StartupCutsceneAssetId != Guid.Empty || actual.VirtualResolution != null)
        {
            Mismatch("a world-level field other than the environment is not the default");
        }

        VerifyEnvironment(expected, actual, json, skyCubeId);
        VerifyWorldEntities(expected, json);
    }

    private void VerifyEnvironment(WorldAsset expected, World actual, JObject json, Guid skyCubeId)
    {
        // Both sides go through the writer's text and the JSON parser, so a float is compared as the file holds it.
        var expectedNode = new JObject();
        expected.World.EnvironmentSettings.Save(expectedNode);
        expectedNode = JObject.Parse(expectedNode.ToString(Formatting.None));

        if (json["environment"] is not JObject actualNode)
        {
            Mismatch("environment: missing");
            return;
        }

        VerifyJsonKeys("environment", expectedNode, actualNode);

        Guid expectedCube = expected.EntryName == "World.FrontEnd" ? Guid.Empty : skyCubeId;
        if (actual.EnvironmentSettings.SpecularEnvironmentCubemapAssetId != expectedCube)
        {
            Mismatch($"specular_cubemap_asset_id {actual.EnvironmentSettings.SpecularEnvironmentCubemapAssetId}, expected {expectedCube}");
        }
    }

    // Key by key, to any depth: every difference is listed with the path of its key.
    private void VerifyJsonKeys(string path, JObject expected, JObject actual)
    {
        foreach (JProperty property in expected.Properties())
        {
            if (!actual.TryGetValue(property.Name, out JToken? actualValue))
            {
                Mismatch($"{path}.{property.Name}: missing");
            }
            else if (property.Value is JObject expectedChild && actualValue is JObject actualChild)
            {
                VerifyJsonKeys($"{path}.{property.Name}", expectedChild, actualChild);
            }
            else if (!JToken.DeepEquals(property.Value, actualValue))
            {
                Mismatch($"{path}.{property.Name}: {actualValue.ToString(Formatting.None)}, expected {property.Value.ToString(Formatting.None)}");
            }
        }

        foreach (JProperty property in actual.Properties())
        {
            if (!expected.ContainsKey(property.Name))
            {
                Mismatch($"{path}.{property.Name}: unexpected key");
            }
        }
    }

    private void VerifyWorldEntities(WorldAsset expected, JObject json)
    {
        var references = json["entity_references"] as JArray;
        if (references == null)
        {
            Mismatch("entity_references: missing");
            return;
        }

        if (references.Count != expected.Entities.Count || (ExpectedEntityCounts.TryGetValue(expected.EntryName, out int count) && count != references.Count))
        {
            Mismatch($"{references.Count} entities, the constructors give {expected.Entities.Count}, the plan expects {ExpectedEntityCounts.GetValueOrDefault(expected.EntryName)}");
            return;
        }

        for (int index = 0; index < references.Count; index++)
        {
            var reference = (JObject)references[index];
            string asset = reference["asset_id"]?.ToString() ?? string.Empty;
            if (asset != EmptyGuid || reference["entity"] is not JObject entityNode)
            {
                Mismatch($"entity_references[{index}]: not an inline entity (asset_id '{asset}')");
                continue;
            }

            var actual = new Entity();
            actual.Load(entityNode);
            string saved = _scope;
            _scope = $"{saved} entity {index} '{expected.Entities[index].Name}'";
            VerifyEntity(expected.Entities[index], actual);
            _scope = saved;
            Entities++;
        }
    }

    // ---- the car ---------------------------------------------------------------------------------------------

    private void VerifyCar(Entity expected, CatalogFile catalog)
    {
        JObject? json = ReadJson(WorldAssets.CarFileName);
        if (json == null)
        {
            return;
        }

        VerifyCatalogueEntry(WorldAssets.CarEntryName, WorldAssets.CarFileName, expected.Id, "entity", catalog);
        var actual = new Entity();
        actual.Load(json);
        VerifyEntity(expected, actual);
        Entities++;
    }

    private void VerifyCatalogueEntry(string name, string fileName, Guid id, string assetType, CatalogFile catalog)
    {
        CatalogEntry? entry = catalog.FindByName(name);
        if (entry == null)
        {
            Mismatch($"no catalogue entry '{name}'");
            return;
        }

        if (entry.Id != id.ToString())
        {
            Mismatch($"the catalogue entry '{name}' has id {entry.Id}, the file {id}");
        }

        if (entry.FileName != fileName)
        {
            Mismatch($"the catalogue entry '{name}' names the file '{entry.FileName}'");
        }

        if (entry.AssetType != assetType || entry.AssetType != AssetInfo.InferAssetType(fileName))
        {
            Mismatch($"the catalogue entry '{name}' has the type '{entry.AssetType}', expected '{assetType}'");
        }
    }

    // ---- entities and components -----------------------------------------------------------------------------

    private void VerifyEntity(Entity expected, Entity actual)
    {
        if (actual.Name != expected.Name)
        {
            Mismatch($"name '{actual.Name}', expected '{expected.Name}'");
        }

        if (actual.Id != expected.Id)
        {
            Mismatch($"id {actual.Id}, expected {expected.Id}");
        }

        if (actual.Policies.PolicySourceMode != expected.Policies.PolicySourceMode || actual.Policies.Mobility != expected.Policies.Mobility
            || actual.Policies.TickPolicy != expected.Policies.TickPolicy || actual.Policies.SpatialPolicy != expected.Policies.SpatialPolicy
            || actual.Policies.RenderDynamicPolicy != expected.Policies.RenderDynamicPolicy)
        {
            Mismatch($"policies {Describe(actual.Policies)}, expected {Describe(expected.Policies)}");
        }

        if (!string.IsNullOrEmpty(actual.GameplayProxyClassName) || !string.IsNullOrEmpty(expected.GameplayProxyClassName))
        {
            Mismatch($"script class '{actual.GameplayProxyClassName}', expected '{expected.GameplayProxyClassName}'");
        }

        if ((expected.RootComponent == null) != (actual.RootComponent == null))
        {
            Mismatch("root component present on one side only");
        }
        else if (expected.RootComponent != null)
        {
            VerifyComponent(expected.RootComponent.Name, expected.RootComponent, actual.RootComponent!);
        }

        List<EntityComponent> expectedOthers = expected.Components.Where(IsSaved).ToList();
        List<EntityComponent> actualOthers = actual.Components.ToList();
        if (expectedOthers.Count != actualOthers.Count)
        {
            Mismatch($"{actualOthers.Count} components outside the root tree, expected {expectedOthers.Count}");
            return;
        }

        for (int index = 0; index < expectedOthers.Count; index++)
        {
            VerifyComponent(expectedOthers[index].Name, expectedOthers[index], actualOthers[index]);
        }
    }

    private static string Describe(EntityPolicyState policies)
        => $"{policies.PolicySourceMode}/{policies.Mobility}/{policies.TickPolicy}/{policies.SpatialPolicy}/{policies.RenderDynamicPolicy}";

    private void VerifyComponent(string path, EntityComponent expected, EntityComponent actual)
    {
        Components++;
        if (actual.GetType() != expected.GetType())
        {
            Mismatch($"component '{path}': type {actual.GetType().Name}, expected {expected.GetType().Name}");
            return;
        }

        if (actual.Name != expected.Name)
        {
            Mismatch($"component '{path}': name '{actual.Name}', expected '{expected.Name}'");
        }

        if (actual.Id != expected.Id)
        {
            Mismatch($"component '{path}': id {actual.Id}, expected {expected.Id}");
        }

        foreach (SerializedPropertyMember member in SerializedProperties.GetMembers(expected.GetType()))
        {
            object? expectedValue = member.Property.GetValue(expected);
            object? actualValue = member.Property.GetValue(actual);
            if (!ValueEquals(expectedValue, actualValue))
            {
                Mismatch($"component '{path}': {member.JsonName} {actualValue}, expected {expectedValue}");
            }
        }

        switch (expected)
        {
            case StaticModelComponent model:
                var actualModel = (StaticModelComponent)actual;
                Compare(path, "static model id", model.StaticModelAssetId, actualModel.StaticModelAssetId);
                Compare(path, "cast_shadows", model.CastShadows, actualModel.CastShadows);
                Compare(path, "receive_shadows", model.ReceiveShadows, actualModel.ReceiveShadows);
                Compare(path, "material overrides", model.MaterialOverrides.Count, actualModel.MaterialOverrides.Count);
                break;
            case LightComponent light:
                var actualLight = (LightComponent)actual;
                Compare(path, "light type", light.Type, actualLight.Type);
                Compare(path, "color", light.Color, actualLight.Color);
                Compare(path, "specular color", light.SpecularColor, actualLight.SpecularColor);
                Compare(path, "intensity", light.Intensity, actualLight.Intensity);
                Compare(path, "range", light.Range, actualLight.Range);
                Compare(path, "inner cone", light.InnerConeAngleDegrees, actualLight.InnerConeAngleDegrees);
                Compare(path, "outer cone", light.OuterConeAngleDegrees, actualLight.OuterConeAngleDegrees);
                Compare(path, "cast_shadows", light.CastShadows, actualLight.CastShadows);
                break;
            case CameraComponent camera:
                var actualCamera = (CameraComponent)actual;
                Compare(path, "view_distance", camera.ViewDistance, actualCamera.ViewDistance);
                Compare(path, "viewport", ViewportText(camera.Viewport), ViewportText(actualCamera.Viewport));
                break;
            case PlayerStartComponent start:
                Compare(path, "player_index", start.PlayerIndex, ((PlayerStartComponent)actual).PlayerIndex);
                break;
        }

        if (expected is not SceneComponent expectedScene)
        {
            return;
        }

        var actualScene = (SceneComponent)actual;
        if (!ValueEquals(expectedScene.LocalPosition, actualScene.LocalPosition)
            || !ValueEquals(expectedScene.LocalOrientation, actualScene.LocalOrientation)
            || !ValueEquals(expectedScene.LocalScale, actualScene.LocalScale))
        {
            Mismatch($"component '{path}': local transform {actualScene.LocalPosition} {actualScene.LocalOrientation} {actualScene.LocalScale}, "
                + $"expected {expectedScene.LocalPosition} {expectedScene.LocalOrientation} {expectedScene.LocalScale}");
        }

        List<SceneComponent> expectedChildren = expectedScene.Children.Where(IsSaved).ToList();
        if (expectedChildren.Count != actualScene.Children.Count)
        {
            Mismatch($"component '{path}': {actualScene.Children.Count} children, expected {expectedChildren.Count}");
            return;
        }

        for (int index = 0; index < expectedChildren.Count; index++)
        {
            VerifyComponent($"{path}/{expectedChildren[index].Name}", expectedChildren[index], actualScene.Children[index]);
        }
    }

    // The editor writer skips a component marked [Browsable(false)].
    private static bool IsSaved(EntityComponent component)
        => component.GetType().GetCustomAttribute<BrowsableAttribute>()?.Browsable != false;

    private static string ViewportText(Viewport viewport)
        => $"{viewport.X},{viewport.Y},{viewport.Width},{viewport.Height},{viewport.MinDepth:R},{viewport.MaxDepth:R}";

    private void Compare<T>(string path, string what, T expected, T actual)
    {
        if (!ValueEquals(expected, actual))
        {
            Mismatch($"component '{path}': {what} {actual}, expected {expected}");
        }
    }

    private static bool ValueEquals(object? expected, object? actual)
    {
        return (expected, actual) switch
        {
            (null, null) => true,
            (float a, float b) => Math.Abs(a - b) <= Tolerance,
            (double a, double b) => Math.Abs(a - b) <= Tolerance,
            (Vector2 a, Vector2 b) => Close(a.X, b.X) && Close(a.Y, b.Y),
            (Vector3 a, Vector3 b) => Close(a.X, b.X) && Close(a.Y, b.Y) && Close(a.Z, b.Z),
            (Vector4 a, Vector4 b) => Close(a.X, b.X) && Close(a.Y, b.Y) && Close(a.Z, b.Z) && Close(a.W, b.W),
            (Quaternion a, Quaternion b) => Close(a.X, b.X) && Close(a.Y, b.Y) && Close(a.Z, b.Z) && Close(a.W, b.W),
            (string a, string b) => a == b,
            _ => expected != null && expected.Equals(actual),
        };
    }

    private static bool Close(float a, float b) => Math.Abs(a - b) <= Tolerance;
}
