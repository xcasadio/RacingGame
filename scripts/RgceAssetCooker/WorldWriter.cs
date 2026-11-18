using CasaEngine.EditorServices;
using CasaEngine.Engine.Environment;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Common;
using CasaEngine.Framework.Scene.Entities;
using CasaEngine.Framework.Scene.Entities.Components;
using RacingGame.Gameplay.Entities;
using RacingGame.Gameplay.Race;
using World = CasaEngine.Framework.Scene.World.World;

namespace RgceAssetCooker;

/// <summary>A world of the game, built in code and ready to be written: its catalogue name, its file and its entities.</summary>
internal sealed record WorldAsset(string EntryName, string FileName, World World, IReadOnlyList<Entity> Entities);

/// <summary>
/// What <c>--worlds</c> writes and <c>--worlds --verify</c> checks, built from the game's own constructors
/// (<c>RaceWorldTemplates</c>, <c>RaceCarEntityFactory</c>): the five worlds and the player car, each with the deterministic
/// ids of <see cref="WorldIds"/>. Nothing here is initialized or added to a world.
/// </summary>
internal sealed class WorldAssets
{
    public const string SkyCubeEntryName = "Texture.SkyCubeMap";
    public const string CarEntryName = "Entity.PlayerCar";
    public const string CarFileName = "Entities/PlayerCar.entity";

    private WorldAssets(IReadOnlyList<WorldAsset> worlds, Entity car, Guid skyCubeId)
    {
        Worlds = worlds;
        Car = car;
        SkyCubeId = skyCubeId;
    }

    public IReadOnlyList<WorldAsset> Worlds { get; }

    public Entity Car { get; }

    /// <summary>The catalogue id of the sky cube, the specular environment cube of the race and carousel worlds.</summary>
    public Guid SkyCubeId { get; }

    /// <summary>
    /// Loads the catalogue of <paramref name="contentDirectory"/> and builds the worlds and the car with
    /// <c>EngineEnvironment.ProjectPath</c> set to it (the track scene reads the catalogue, that path and the track files).
    /// </summary>
    public static WorldAssets Build(string contentDirectory)
    {
        AssetCatalog.Load(Path.Combine(contentDirectory, "AssetInfos.json"));
        Guid skyCubeId = AssetCatalog.Get(SkyCubeEntryName)?.Id
            ?? throw new InvalidOperationException($"The catalogue has no '{SkyCubeEntryName}' entry.");

        string previousProjectPath = EngineEnvironment.ProjectPath;
        EngineEnvironment.ProjectPath = contentDirectory;
        try
        {
            var worlds = new List<WorldAsset>
            {
                CreateWorld("World.FrontEnd", "Worlds/FrontEnd.world", RaceWorldTemplates.CreateFrontEndEntities(), _ => { }),
            };

            foreach (string track in new[] { "Beginner", "Advanced", "Expert" })
            {
                worlds.Add(CreateWorld(
                    $"World.Race.Track{track}",
                    $"Worlds/Race/Track{track}.world",
                    RaceWorldTemplates.CreateRaceEntities(track, castShadows: true),
                    environment =>
                    {
                        RaceWorldTemplates.ApplyRaceEnvironment(environment, shadowsEnabled: true, linearColorPipeline: true);
                        environment.SpecularEnvironmentCubemapAssetId = skyCubeId;
                    }));
            }

            var carousel = new List<Entity> { RaceWorldTemplates.CreateCarSelectionLightEntity() };
            carousel.AddRange(RaceWorldTemplates.CreateCarSelectionPlateEntities());
            worlds.Add(CreateWorld(
                "World.CarSelection",
                "Worlds/CarSelection.world",
                carousel,
                environment =>
                {
                    RaceWorldTemplates.ApplyCarSelectionEnvironment(environment);
                    environment.SpecularEnvironmentCubemapAssetId = skyCubeId;
                }));

            Entity car = RaceCarEntityFactory.Create();
            car.Name = CarEntryName;
            car.FileName = CarFileName;
            WorldIds.AssignEntityAsset(CarEntryName, car);

            return new WorldAssets(worlds, car, skyCubeId);
        }
        finally
        {
            EngineEnvironment.ProjectPath = previousProjectPath;
        }
    }

    private static WorldAsset CreateWorld(string entryName, string fileName, List<Entity> entities, Action<CasaEngine.Framework.Rendering.Environment.WorldEnvironmentSettings> applyEnvironment)
    {
        var world = new World { Name = entryName, FileName = fileName };
        applyEnvironment(world.EnvironmentSettings);
        WorldIds.AssignWorld(entryName, world, entities);
        return new WorldAsset(entryName, fileName, world, entities);
    }
}

/// <summary>
/// The deterministic ids of a written world, set by <see cref="CookPlanner.SetId"/> (the engine types expose no setter):
/// the world and the car are <c>uuid5</c> of their catalogue name, an entity of a world of
/// <c>"&lt;entry&gt;/&lt;entity name&gt;"</c>, a component of <c>"&lt;entry&gt;/&lt;entity name&gt;/&lt;component path&gt;"</c>.
/// The component path is the names of the components from the root of the entity down (the name alone for a component
/// outside the root's tree); among siblings, a name that occurs more than once becomes <c>Name#rank</c>, the rank counted
/// from 0 among the siblings of that name. For the car the prefix <c>"&lt;entry&gt;/&lt;entity name&gt;"</c> is
/// <c>Entity.PlayerCar</c>.
/// </summary>
internal static class WorldIds
{
    public static void AssignWorld(string entryName, World world, IReadOnlyList<Entity> entities)
    {
        var ids = new HashSet<Guid>();
        Guid worldId = Uuid5.ForAsset(entryName);
        CookPlanner.SetId(world, worldId);
        ids.Add(worldId);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Entity entity in entities)
        {
            if (!names.Add(entity.Name))
            {
                throw new InvalidOperationException($"{entryName}: two entities are named '{entity.Name}'.");
            }

            RejectEntityChildren(entryName, entity);
            string prefix = $"{entryName}/{entity.Name}";
            Guid entityId = Uuid5.ForAsset(prefix);
            CookPlanner.SetId(entity, entityId);
            AddUnique(ids, entityId, entryName, entity.Name, "entity");
            AssignComponents(entryName, prefix, entity, ids);
        }
    }

    /// <summary>The ids of an entity that is an asset of its own (the car): the entity takes the id of its entry.</summary>
    public static void AssignEntityAsset(string entryName, Entity entity)
    {
        RejectEntityChildren(entryName, entity);
        var ids = new HashSet<Guid>();
        Guid entityId = Uuid5.ForAsset(entryName);
        CookPlanner.SetId(entity, entityId);
        ids.Add(entityId);
        AssignComponents(entryName, entryName, entity, ids);
    }

    /// <summary>Every component of an entity with its path: the root's tree first, then the components outside it.</summary>
    public static List<(string Path, EntityComponent Component)> EnumerateComponents(Entity entity)
    {
        var found = new List<(string, EntityComponent)>();
        if (entity.RootComponent != null)
        {
            Walk(entity.RootComponent.Name, entity.RootComponent, found);
        }

        var others = entity.Components.ToList();
        foreach ((EntityComponent component, string label) in Label(others, other => other.Name))
        {
            Walk(label, component, found);
        }

        return found;
    }

    private static void Walk(string path, EntityComponent component, List<(string, EntityComponent)> found)
    {
        found.Add((path, component));
        if (component is not SceneComponent scene)
        {
            return;
        }

        foreach ((SceneComponent child, string label) in Label(scene.Children, child => child.Name))
        {
            Walk($"{path}/{label}", child, found);
        }
    }

    // A name that occurs more than once among siblings gets its rank among them.
    private static IEnumerable<(T Item, string Label)> Label<T>(IReadOnlyList<T> siblings, Func<T, string> name)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (T sibling in siblings)
        {
            counts[name(sibling)] = counts.GetValueOrDefault(name(sibling)) + 1;
        }

        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (T sibling in siblings)
        {
            string text = name(sibling);
            int rank = ranks.GetValueOrDefault(text);
            ranks[text] = rank + 1;
            yield return (sibling, counts[text] > 1 ? $"{text}#{rank}" : text);
        }
    }

    private static void AssignComponents(string entryName, string prefix, Entity entity, HashSet<Guid> ids)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string path, EntityComponent component) in EnumerateComponents(entity))
        {
            if (!paths.Add(path))
            {
                throw new InvalidOperationException($"{entryName}: two components of '{entity.Name}' have the path '{path}'.");
            }

            Guid id = Uuid5.ForAsset($"{prefix}/{path}");
            CookPlanner.SetId(component, id);
            AddUnique(ids, id, entryName, entity.Name, $"component '{path}'");
        }
    }

    private static void AddUnique(HashSet<Guid> ids, Guid id, string entryName, string entityName, string what)
    {
        if (!ids.Add(id))
        {
            throw new InvalidOperationException($"{entryName}: the id of the {what} of '{entityName}' is already used.");
        }
    }

    // The editor writer saves an entity's components and not its child entities: a child would be lost.
    private static void RejectEntityChildren(string entryName, Entity entity)
    {
        if (entity.Children.Any())
        {
            throw new InvalidOperationException($"{entryName}: the entity '{entity.Name}' has child entities, which the entity writer does not save.");
        }
    }
}

/// <summary>
/// <c>--worlds</c>: writes the five worlds and the player car with the engine's editor writers, into a scratch folder, then
/// copies only the files whose bytes changed, so a second run touches nothing.
/// </summary>
internal sealed class WorldWriter
{
    private readonly string _contentDirectory;

    public WorldWriter(string contentDirectory)
    {
        _contentDirectory = contentDirectory;
    }

    public int FilesWritten { get; private set; }

    public int FilesUnchanged { get; private set; }

    public void Run()
    {
        WorldAssets assets = WorldAssets.Build(_contentDirectory);

        string staging = Path.Combine(Path.GetTempPath(), "RgceAssetCooker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        string previousProjectPath = EngineEnvironment.ProjectPath;
        try
        {
            // The writers save under EngineEnvironment.ProjectPath, in folders that must exist.
            EngineEnvironment.ProjectPath = staging;
            foreach (WorldAsset world in assets.Worlds)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(staging, world.FileName))!);
                EditorWorldWriter.SaveWorld(world.World, world.Entities);
                Console.WriteLine($"{world.FileName}: {world.Entities.Count} entities");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(staging, WorldAssets.CarFileName))!);
            EditorEntityWriter.SaveEntity(assets.Car);

            foreach (string relativeFile in assets.Worlds.Select(world => world.FileName).Append(WorldAssets.CarFileName))
            {
                Copy(staging, relativeFile);
            }
        }
        finally
        {
            EngineEnvironment.ProjectPath = previousProjectPath;
            Directory.Delete(staging, recursive: true);
        }
    }

    private void Copy(string staging, string relativeFile)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(staging, relativeFile));
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
}
