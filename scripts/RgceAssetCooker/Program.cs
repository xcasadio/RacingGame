using System.Diagnostics;

namespace RgceAssetCooker;

internal static class Program
{
    private const string Usage = """
        RgceAssetCooker: cooks the glTF models of RacingGameCasaEngine into catalogued assets.

        Usage: RgceAssetCooker [--content <Content folder>] [--check | --verify]

          (no option)  Writes Content/Models/<Name>.staticModel, Content/Materials/**/*.material and
                       Content/Textures/<Name>.texture, updates their entries in Content/AssetInfos.json,
                       then verifies the files it wrote. A second run changes no byte.
          --check      Lists the glTF models and the images of cooked materials that have no cooked asset
                       (or a cooked file outside the plan). Writes nothing. Exits 1 if there is any.
          --verify     Re-reads every cooked file with the engine loaders and compares it with the glTF read
                       and the material recipes. Writes nothing. Exits 1 on any mismatch.
          --content    The project Content folder (default: RacingGameCasaEngine/Content of the repository).
        """;

    private static int Main(string[] args)
    {
        string? contentDirectory = null;
        bool check = false;
        bool verify = false;
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

        contentDirectory ??= FindContentDirectory();
        if (contentDirectory == null || !Directory.Exists(contentDirectory))
        {
            Console.Error.WriteLine("Content folder not found: pass --content <folder>.");
            return 2;
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

    private static int Verify(string contentDirectory)
    {
        var verifier = new Verifier(contentDirectory);
        verifier.Run();
        Console.WriteLine($"verify: {verifier.Models} models ({verifier.Meshes} meshes, {verifier.Vertices} vertices, {verifier.Indices} indices, {verifier.Nodes} nodes), "
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
