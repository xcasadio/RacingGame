using System.Diagnostics;

namespace RgceAssetCooker;

internal static class Program
{
    private const string Usage = """
        RgceAssetCooker: cooks the glTF models of RacingGameCasaEngine into catalogued assets.

        Usage: RgceAssetCooker [--content <Content folder>] [--check | --verify] [--worlds]

          (no option)  Writes Content/Models/<Name>.staticModel, Content/Materials/**/*.material and
                       Content/Textures/<Name>.texture, updates their entries in Content/AssetInfos.json,
                       then verifies the files it wrote. A second run changes no byte.
          --check      Lists the glTF models and the images of cooked materials that have no cooked asset
                       (or a cooked file outside the plan). Writes nothing. Exits 1 if there is any.
          --verify     Re-reads every cooked file with the engine loaders and compares it with the glTF read
                       and the material recipes. Writes nothing. Exits 1 on any mismatch.
          --worlds     Writes the five worlds (Content/Worlds/FrontEnd.world, Worlds/Race/Track<Beginner|Advanced|Expert>.world,
                       Worlds/CarSelection.world) and Content/Entities/PlayerCar.entity, built by the game's own constructors,
                       with deterministic ids. A second run changes no byte. The catalogue is not touched (run
                       scripts/generate_rgce_content_assets.py).
          --worlds --verify
                       Re-reads those six files with the engine loaders and compares them with the constructors and with the
                       catalogue. Writes nothing. Exits 1 on any mismatch. (--worlds --check is not an option.)
          --content    The project Content folder (default: RacingGameCasaEngine/Content of the repository).
        """;

    private static int Main(string[] args)
    {
        string? contentDirectory = null;
        bool check = false;
        bool verify = false;
        bool worlds = false;
        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--content" when index + 1 < args.Length:
                    contentDirectory = Path.GetFullPath(args[++index]);
                    break;
                case "--check":
                    check = true;
                    break;
                case "--verify":
                    verify = true;
                    break;
                case "--worlds":
                    worlds = true;
                    break;
                default:
                    Console.Error.WriteLine(Usage);
                    return args[index] is "--help" or "-h" ? 0 : 2;
            }
        }

        if (check && verify)
        {
            Console.Error.WriteLine("--check and --verify are exclusive.");
            return 2;
        }

        if (worlds && check)
        {
            Console.Error.WriteLine("--worlds and --check are exclusive.");
            return 2;
        }

        contentDirectory ??= FindContentDirectory();
        if (contentDirectory == null || !Directory.Exists(contentDirectory))
        {
            Console.Error.WriteLine("Content folder not found: pass --content <folder>.");
            return 2;
        }

        if (worlds)
        {
            return verify ? VerifyWorlds(contentDirectory) : WriteWorlds(contentDirectory);
        }

        var clock = Stopwatch.StartNew();
        CookPlan plan = CookPlanner.Build(contentDirectory);
        Console.WriteLine($"plan: {plan.Models.Count} models, {plan.AllMaterials.Count()} materials "
            + $"({plan.Models.Sum(model => model.Materials.Count)} of models, {plan.TrackMaterials.Count} of the track), {plan.Textures.Count} textures");

        if (check)
        {
            return Check(contentDirectory, plan);
        }

        if (verify)
        {
            return Verify(contentDirectory);
        }

        var catalog = CatalogFile.Load(Path.Combine(contentDirectory, "AssetInfos.json"));
        int before = catalog.Entries.Count;
        var cooker = new Cooker(contentDirectory, plan, catalog);
        cooker.Run();
        Console.WriteLine($"cooked: {cooker.FilesWritten} files written, {cooker.FilesUnchanged} unchanged; "
            + $"catalogue {before} -> {catalog.Entries.Count} entries, {cooker.CatalogueEntriesChanged} cooker entries added or changed");

        foreach (string stale in FindStaleFiles(contentDirectory, plan))
        {
            Console.WriteLine($"warning: {stale} is in a cooked folder but not in the plan");
        }

        int verified = Verify(contentDirectory);
        Console.WriteLine($"done in {clock.Elapsed.TotalSeconds:0.0}s");
        return verified;
    }

    private static int WriteWorlds(string contentDirectory)
    {
        var writer = new WorldWriter(contentDirectory);
        writer.Run();
        Console.WriteLine($"worlds: {writer.FilesWritten} files written, {writer.FilesUnchanged} unchanged");
        return 0;
    }

    private static int VerifyWorlds(string contentDirectory)
    {
        var verifier = new WorldVerifier(contentDirectory);
        verifier.Run();
        Console.WriteLine($"verify worlds: {verifier.Files} files, {verifier.Entities} entities, {verifier.Components} components");
        foreach (string mismatch in verifier.Mismatches)
        {
            Console.WriteLine($"MISMATCH {mismatch}");
        }

        Console.WriteLine($"verify worlds: {verifier.Mismatches.Count} mismatches");
        return verifier.Mismatches.Count == 0 ? 0 : 1;
    }

    private static int Verify(string contentDirectory)
    {
        var verifier = new Verifier(contentDirectory);
        verifier.Run();
        Console.WriteLine($"verify: {verifier.Models} models ({verifier.Meshes} meshes, {verifier.MeshesWithTangents} with tangents, {verifier.Vertices} vertices, {verifier.Tangents} tangents, {verifier.Indices} indices, {verifier.Nodes} nodes), "
            + $"{verifier.Materials} materials ({verifier.MaterialValues} values), {verifier.Textures} textures, {verifier.CatalogueEntries} catalogue entries");
        foreach (string note in verifier.Notes)
        {
            Console.WriteLine($"note: {note}");
        }

        foreach (string mismatch in verifier.Mismatches)
        {
            Console.WriteLine($"MISMATCH {mismatch}");
        }

        Console.WriteLine($"verify: {verifier.Mismatches.Count} mismatches");
        return verifier.Mismatches.Count == 0 ? 0 : 1;
    }

    private static int Check(string contentDirectory, CookPlan plan)
    {
        CatalogFile catalog = CatalogFile.Load(Path.Combine(contentDirectory, "AssetInfos.json"));
        int missing = 0;

        void Require(string description, string name, string relativeFile)
        {
            bool fileExists = File.Exists(Path.Combine(contentDirectory, relativeFile));
            bool catalogued = catalog.FindByName(name)?.FileName == relativeFile;
            if (!fileExists || !catalogued)
            {
                missing++;
                Console.WriteLine($"no cooked asset: {description} -> {relativeFile} ({(fileExists ? "not catalogued" : "file missing")})");
            }
        }

        foreach (CookedModel model in plan.Models)
        {
            Require($"model {model.GltfFileName}", model.Name, model.FileName);
        }

        foreach (CookedMaterial material in plan.AllMaterials)
        {
            Require($"material {material.Name}", material.Name, material.FileName);
        }

        foreach (CookedTexture texture in plan.Textures)
        {
            Require($"image {Path.GetFileName(texture.ImageFileName)}", texture.Name, texture.FileName);
        }

        foreach (string stale in FindStaleFiles(contentDirectory, plan))
        {
            missing++;
            Console.WriteLine($"cooked file outside the plan: {stale}");
        }

        Console.WriteLine($"{missing} glTF models, images or cooked files without a cooked asset");
        return missing == 0 ? 0 : 1;
    }

    private static IEnumerable<string> FindStaleFiles(string contentDirectory, CookPlan plan)
    {
        var planned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Cooker.OwnedEntries(plan))
        {
            planned.Add(entry.FileName);
        }

        var found = new List<string>();
        void Scan(string folder, string pattern, SearchOption option)
        {
            string path = Path.Combine(contentDirectory, folder);
            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(path, pattern, option))
            {
                string relative = Path.GetRelativePath(contentDirectory, file).Replace('\\', '/');
                if (!planned.Contains(relative))
                {
                    found.Add(relative);
                }
            }
        }

        Scan("Models", "*.staticModel", SearchOption.TopDirectoryOnly);
        Scan("Materials", "*.material", SearchOption.AllDirectories);
        Scan("Textures", "*.texture", SearchOption.TopDirectoryOnly);
        found.Sort(StringComparer.OrdinalIgnoreCase);
        return found;
    }

    private static string? FindContentDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RacingGame.slnx")))
            {
                return Path.Combine(directory.FullName, "RacingGameCasaEngine", "Content");
            }
        }

        return null;
    }
}
