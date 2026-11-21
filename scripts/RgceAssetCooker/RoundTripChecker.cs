using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CasaEngine.Core.Logging;
using CasaEngine.EditorServices;
using CasaEngine.EditorServices.AssetPreview;
using CasaEngine.Engine.Environment;
using CasaEngine.Framework.Assets.Sprites;
using CasaEngine.Framework.Audio;
using CasaEngine.Framework.Materials.Runtime;
using CasaEngine.Framework.Rendering.Models;
using CasaEngine.Framework.Scene.Entities;
using CasaEngine.Framework.Scene.Entities.Components;
using CasaEngine.Framework.UI.MGUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RacingGame.Gameplay.Entities;
using Texture = CasaEngine.Framework.Assets.Textures.Texture;
using World = CasaEngine.Framework.Scene.World.World;

namespace RgceAssetCooker;

/// <summary>The names of the checks of <see cref="RoundTripChecker"/>, in the order that picks the one a failed asset is reported under.</summary>
internal static class RoundTripCheck
{
    public const string FileMissing = "file missing";
    public const string NotCovered = "type not covered";
    public const string LoadFailed = "load failed";
    public const string WriteFailed = "write failed";
    public const string ReferenceNotSupported = "asset reference not supported";
    public const string UnresolvedType = "unresolved type";
    public const string LoggedError = "logged error";
    public const string ComponentCount = "component count";
    public const string Unstable = "unstable";
    public const string KeyLost = "key lost";
    public const string ValueChanged = "value changed";
    public const string UnreadablePreview = "unreadable preview";

    public static readonly string[] Order =
    [
        FileMissing, NotCovered, LoadFailed, WriteFailed, ReferenceNotSupported, UnresolvedType, LoggedError, ComponentCount,
        Unstable, KeyLost, ValueChanged, UnreadablePreview,
    ];
}

/// <summary>What a negative control alters, to prove that a check sees it.</summary>
internal enum RoundTripTamper
{
    None,

    /// <summary>Removes one key from the first write before it is compared with the source.</summary>
    RemoveKey,

    /// <summary>Changes one value of the first write before it is compared with the source.</summary>
    ChangeValue,

    /// <summary>Alters one byte of the second write before it is compared with the first.</summary>
    AlterSecondWrite,
}

/// <summary>The verdict of one asset: the checks it failed, the keys and values that differ, the component counts, the preview header.</summary>
internal sealed class AssetRecord
{
    private const int MaxListedChanges = 30;
    private const int MaxListedMessages = 10;

    private static readonly Regex ArrayIndex = new(@"\[\d+\]", RegexOptions.Compiled);

    public AssetRecord(string fileName, string type, string group)
    {
        FileName = fileName;
        Type = type;
        Group = group;
    }

    public string FileName { get; }

    /// <summary>The extension without its dot, in lower case.</summary>
    public string Type { get; }

    /// <summary>"editable" or "preview".</summary>
    public string Group { get; }

    public List<string> FailedChecks { get; } = [];

    public List<string> Details { get; } = [];

    /// <summary>Lost keys by path with the array indices removed: the count and one full path as an example.</summary>
    public SortedDictionary<string, (int Count, string Example)> LostKeys { get; } = new(StringComparer.Ordinal);

    public SortedDictionary<string, (int Count, string Example)> AddedKeys { get; } = new(StringComparer.Ordinal);

    public List<(string Path, string Source, string Written)> ChangedValues { get; } = [];

    public int ChangedValuesTotal { get; set; }

    public int? ComponentsReal { get; set; }

    public int? ComponentsUnresolved { get; set; }

    public int? ComponentsSource { get; set; }

    public List<string> Errors { get; } = [];

    public int ErrorCount { get; set; }

    public List<string> Warnings { get; } = [];

    public int WarningCount { get; set; }

    public string? PreviewKind { get; set; }

    public string? PreviewProblem { get; set; }

    public bool Failed => FailedChecks.Count > 0;

    public string Verdict => Failed ? "fail" : "pass";

    /// <summary>The check the asset is reported under: the first of <see cref="RoundTripCheck.Order"/> it failed.</summary>
    public string? PrimaryCheck => RoundTripCheck.Order.FirstOrDefault(FailedChecks.Contains);

    public static string Normalize(string path) => ArrayIndex.Replace(path, "[]");

    public void Fail(string check, string detail)
    {
        if (!FailedChecks.Contains(check))
        {
            FailedChecks.Add(check);
        }

        Details.Add($"{check}: {detail}");
    }

    public void AddKey(SortedDictionary<string, (int Count, string Example)> keys, string path)
    {
        string normalized = Normalize(path);
        keys[normalized] = keys.TryGetValue(normalized, out var known) ? (known.Count + 1, known.Example) : (1, path);
    }

    public void AddChange(string path, string source, string written)
    {
        ChangedValuesTotal++;
        if (ChangedValues.Count < MaxListedChanges)
        {
            ChangedValues.Add((path, Truncate(source), Truncate(written)));
        }
    }

    public void SetMessages(IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        ErrorCount = errors.Count;
        Errors.AddRange(errors.Take(MaxListedMessages).Select(Truncate));
        WarningCount = warnings.Count;
        Warnings.AddRange(warnings.Distinct().Take(MaxListedMessages).Select(Truncate));
    }

    public JObject ToJson()
    {
        var json = new JObject
        {
            ["file_name"] = FileName,
            ["type"] = Type,
            ["group"] = Group,
            ["verdict"] = Verdict,
            ["check"] = PrimaryCheck,
            ["failed_checks"] = new JArray(FailedChecks),
            ["details"] = new JArray(Details.Select(Truncate)),
        };

        if (Group == "editable")
        {
            json["lost_keys"] = KeyArray(LostKeys);
            json["changed_values_total"] = ChangedValuesTotal;
            json["changed_values"] = new JArray(ChangedValues.Select(change => new JObject
            {
                ["path"] = change.Path,
                ["source"] = change.Source,
                ["written"] = change.Written,
            }));
            json["added_keys"] = KeyArray(AddedKeys);
            if (ComponentsSource != null)
            {
                json["components"] = new JObject
                {
                    ["loaded_as_real_type"] = ComponentsReal,
                    ["unresolved"] = ComponentsUnresolved,
                    ["source_nodes"] = ComponentsSource,
                };
            }

            json["logged_errors_total"] = ErrorCount;
            json["logged_errors"] = new JArray(Errors);
            json["logged_warnings_total"] = WarningCount;
            json["logged_warnings"] = new JArray(Warnings);
        }
        else
        {
            json["preview_kind"] = PreviewKind;
            json["preview_problem"] = PreviewProblem;
        }

        return json;
    }

    private static JArray KeyArray(SortedDictionary<string, (int Count, string Example)> keys) =>
        new(keys.Select(key => new JObject { ["path"] = key.Key, ["count"] = key.Value.Count, ["example"] = key.Value.Example }));

    private static string Truncate(string text) => text.Length <= 240 ? text : text[..240] + "...";
}

/// <summary>Compares a source JSON with the JSON written from it: lost keys, changed values and added keys.</summary>
internal static class JsonDiff
{
    public const double Tolerance = 1e-6;

    public static void Compare(JToken source, JToken written, string path, AssetRecord record)
    {
        switch (source)
        {
            case JObject sourceObject when written is JObject writtenObject:
                foreach (JProperty property in sourceObject.Properties())
                {
                    string child = path.Length == 0 ? property.Name : $"{path}.{property.Name}";
                    if (writtenObject.TryGetValue(property.Name, out JToken? writtenValue))
                    {
                        Compare(property.Value, writtenValue, child, record);
                    }
                    else
                    {
                        record.AddKey(record.LostKeys, child);
                    }
                }

                foreach (JProperty property in writtenObject.Properties())
                {
                    if (!sourceObject.ContainsKey(property.Name))
                    {
                        record.AddKey(record.AddedKeys, path.Length == 0 ? property.Name : $"{path}.{property.Name}");
                    }
                }

                break;

            case JArray sourceArray when written is JArray writtenArray:
                int common = Math.Min(sourceArray.Count, writtenArray.Count);
                for (int index = 0; index < common; index++)
                {
                    Compare(sourceArray[index], writtenArray[index], $"{path}[{index}]", record);
                }

                if (sourceArray.Count != writtenArray.Count)
                {
                    record.AddChange(path, $"array of {sourceArray.Count} elements", $"array of {writtenArray.Count} elements");
                }

                break;

            case JValue sourceValue when written is JValue writtenValue:
                if (!ValuesEqual(sourceValue, writtenValue))
                {
                    record.AddChange(path, sourceValue.ToString(Formatting.None), writtenValue.ToString(Formatting.None));
                }

                break;

            default:
                record.AddChange(path, Describe(source), Describe(written));
                break;
        }
    }

    private static string Describe(JToken token) => token switch
    {
        JObject => "an object",
        JArray => "an array",
        _ => token.ToString(Formatting.None),
    };

    private static bool IsNumber(JTokenType type) => type is JTokenType.Integer or JTokenType.Float;

    private static bool ValuesEqual(JValue source, JValue written)
    {
        if (IsNumber(source.Type) && IsNumber(written.Type))
        {
            if (source.Type == JTokenType.Integer && written.Type == JTokenType.Integer)
            {
                return JToken.DeepEquals(source, written);
            }

            double a = source.ToObject<double>();
            double b = written.ToObject<double>();
            return a.Equals(b) || Math.Abs(a - b) <= Tolerance;
        }

        return source.Type == written.Type && JToken.DeepEquals(source, written);
    }
}

/// <summary>Keeps the errors and warnings the engine logs while an asset is processed.</summary>
internal sealed class CollectingLogger : ILogger
{
    public List<string> Errors { get; } = [];

    public List<string> Warnings { get; } = [];

    public void Reset()
    {
        Errors.Clear();
        Warnings.Clear();
    }

    public void Close()
    {
    }

    public void WriteTrace(string msg)
    {
    }

    public void WriteDebug(string msg)
    {
    }

    public void WriteInfo(string msg)
    {
    }

    public void WriteWarning(string msg) => Warnings.Add(msg);

    public void WriteError(string msg) => Errors.Add(msg);
}

/// <summary>
/// <c>--roundtrip</c>. For each asset of the catalogue of a COPY of the project (never the live Content folder): an editable
/// asset (world, entity, static model, material, texture, sound, screen, sprite) is loaded with the engine loader of its type
/// (no graphics device, no game), written with the editor's public writer, the first write is loaded again and written again;
/// a preview asset (image, cube map, wav, bitmap font, glTF, track, combined model, data) has its header read by
/// <see cref="AssetPreviewReader.Read"/>. Checks of an editable asset: the second write has the bytes of the first
/// ("unstable"); no key of the source is missing from the first write ("key lost"); no value of the source differs, numbers to
/// 1e-6 and arrays in order ("value changed"); for a world or an entity, every component node of the source is loaded under
/// its real type, none as an <see cref="UnresolvedComponent"/>, and the engine logs no error. Keys the writer adds are listed,
/// not failed. A preview asset fails when <see cref="AssetPreviewInfo.Problem"/> is not null. Nothing is repaired.
/// </summary>
internal sealed class RoundTripChecker
{
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    private static readonly string[] EditableExtensions =
        [".world", ".entity", ".staticmodel", ".material", ".texture", ".sound", ".uiscreen", ".sprite"];

    private static readonly string[] PreviewExtensions =
        [".png", ".dds", ".wav", ".fnt", ".gltf", ".track", ".combimodel", ".data"];

    private readonly string _contentDirectory;
    private readonly string _outputDirectory;
    private readonly CollectingLogger _logger = new();

    public RoundTripChecker(string contentDirectory, string outputDirectory)
    {
        _contentDirectory = contentDirectory;
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(outputDirectory);

        // The editor writer saves under EngineEnvironment.ProjectPath unless it is given an absolute path.
        EngineEnvironment.ProjectPath = contentDirectory;
        Logs.AddLogger(_logger);

        // The engine finds a component type by name in the assemblies that are already loaded: load the game's now, or a game
        // component would be read as an UnresolvedComponent that writes its node back unchanged.
        _ = typeof(RaceCarEntityFactory).Assembly;
        if (!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "RacingGame.Gameplay"))
        {
            throw new InvalidOperationException("The RacingGame.Gameplay assembly is not loaded.");
        }
    }

    public List<AssetRecord> Records { get; } = [];

    private sealed record CatalogueItem(string FileName, string AssetType);

    private sealed class UnsupportedReferenceException : Exception
    {
        public UnsupportedReferenceException(string message) : base(message)
        {
        }
    }

    private sealed class LoadedAsset
    {
        public object? Asset { get; init; }

        public World? World { get; init; }

        public List<Entity> Entities { get; } = [];
    }

    // ---- the run ---------------------------------------------------------------------------------------------

    /// <summary>Processes every catalogued asset, or those of one type (an extension without its dot) or one file.</summary>
    public int Run(string? typeFilter, string? fileFilter)
    {
        foreach (CatalogueItem item in ReadCatalogue())
        {
            string extension = Path.GetExtension(item.FileName).ToLowerInvariant();
            string type = extension.TrimStart('.');
            if ((typeFilter != null && !string.Equals(type, typeFilter.TrimStart('.'), StringComparison.OrdinalIgnoreCase))
                || (fileFilter != null && !string.Equals(item.FileName, fileFilter, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            string sourcePath = Path.Combine(_contentDirectory, item.FileName);
            if (EditableExtensions.Contains(extension))
            {
                Records.Add(ProcessEditable(item.FileName, sourcePath, _outputDirectory, RoundTripTamper.None));
            }
            else if (PreviewExtensions.Contains(extension))
            {
                Records.Add(ProcessPreview(item.FileName, sourcePath));
            }
            else
            {
                var record = new AssetRecord(item.FileName, type, "editable");
                record.Fail(RoundTripCheck.NotCovered, $"the extension '{extension}' (catalogue type '{item.AssetType}') is neither editable nor previewed");
                Records.Add(record);
            }
        }

        if (Records.Count == 0)
        {
            Console.Error.WriteLine("No catalogued asset matches --type / --file.");
            return 2;
        }

        WriteReports();
        return Records.Any(record => record.Failed) ? 1 : 0;
    }

    private List<CatalogueItem> ReadCatalogue()
    {
        string path = Path.Combine(_contentDirectory, "AssetInfos.json");
        JObject root = ParseJson(File.ReadAllText(path));
        var items = new List<CatalogueItem>();
        foreach (JToken entry in (JArray)root["asset_infos"]!)
        {
            items.Add(new CatalogueItem(entry.Value<string>("file_name")!, entry.Value<string>("asset_type") ?? string.Empty));
        }

        items.Sort((left, right) => string.Compare(left.FileName, right.FileName, StringComparison.OrdinalIgnoreCase));
        return items;
    }

    private static JObject ParseJson(string text)
    {
        // Strings that look like dates stay strings, so that a value is compared as the file holds it.
        using var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None };
        return JObject.Load(reader);
    }

    // ---- an editable asset -----------------------------------------------------------------------------------

    internal AssetRecord ProcessEditable(string fileName, string sourcePath, string stageDirectory, RoundTripTamper tamper)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        var record = new AssetRecord(fileName, extension.TrimStart('.'), "editable");
        _logger.Reset();

        try
        {
            ProcessEditableCore(record, extension, fileName, sourcePath, stageDirectory, tamper);
        }
        catch (UnsupportedReferenceException exception)
        {
            record.Fail(RoundTripCheck.ReferenceNotSupported, exception.Message);
        }

        record.SetMessages(_logger.Errors, _logger.Warnings);
        if (record.ErrorCount > 0)
        {
            record.Fail(RoundTripCheck.LoggedError, $"{record.ErrorCount} error(s) logged by the engine, the first: {record.Errors[0]}");
        }

        return record;
    }

    private void ProcessEditableCore(AssetRecord record, string extension, string fileName, string sourcePath, string stageDirectory, RoundTripTamper tamper)
    {
        if (!File.Exists(sourcePath))
        {
            record.Fail(RoundTripCheck.FileMissing, sourcePath);
            return;
        }

        string firstPath = Path.GetFullPath(Path.Combine(stageDirectory, "first", fileName));
        string secondPath = Path.GetFullPath(Path.Combine(stageDirectory, "second", fileName));
        Directory.CreateDirectory(Path.GetDirectoryName(firstPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(secondPath)!);

        JObject source = ParseJson(File.ReadAllText(sourcePath));

        LoadedAsset? loaded = Attempt(record, RoundTripCheck.LoadFailed, "load the source", () => Load(extension, source));
        if (loaded == null)
        {
            return;
        }

        CountComponents(record, extension, source, loaded);

        if (!AttemptAction(record, RoundTripCheck.WriteFailed, "first write", () => Save(extension, loaded, firstPath)))
        {
            return;
        }

        byte[] firstBytes = File.ReadAllBytes(firstPath);
        JObject first = ParseJson(File.ReadAllText(firstPath));

        LoadedAsset? reloaded = Attempt(record, RoundTripCheck.LoadFailed, "reload the first write", () => Load(extension, first));
        if (reloaded == null || !AttemptAction(record, RoundTripCheck.WriteFailed, "second write", () => Save(extension, reloaded, secondPath)))
        {
            return;
        }

        byte[] secondBytes = File.ReadAllBytes(secondPath);
        if (tamper == RoundTripTamper.AlterSecondWrite)
        {
            secondBytes[secondBytes.Length / 2] ^= 0x01;
            File.WriteAllBytes(secondPath, secondBytes);
            record.Details.Add("control: one byte of the second write was altered");
        }

        JObject compared = first;
        if (tamper is RoundTripTamper.RemoveKey or RoundTripTamper.ChangeValue)
        {
            compared = (JObject)first.DeepClone();
            ApplyTamper(tamper, source, compared, record);
            File.WriteAllText(firstPath + ".tampered.json", compared.ToString(Formatting.Indented));
        }

        CheckStable(record, firstBytes, secondBytes, first);
        JsonDiff.Compare(source, compared, string.Empty, record);

        if (record.LostKeys.Count > 0)
        {
            record.Fail(RoundTripCheck.KeyLost, $"{record.LostKeys.Values.Sum(key => key.Count)} key(s) of the source are not in the written file, the first: {record.LostKeys.First().Value.Example}");
        }

        if (record.ChangedValuesTotal > 0)
        {
            (string path, string was, string now) = record.ChangedValues[0];
            record.Fail(RoundTripCheck.ValueChanged, $"{record.ChangedValuesTotal} value(s) differ, the first: {path} was {was}, is {now}");
        }
    }

    private static JObject? TryParse(byte[] bytes)
    {
        try
        {
            return ParseJson(Encoding.UTF8.GetString(bytes));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CheckStable(AssetRecord record, byte[] firstBytes, byte[] secondBytes, JObject first)
    {
        if (firstBytes.AsSpan().SequenceEqual(secondBytes))
        {
            return;
        }

        JObject? second = TryParse(secondBytes);
        int offset = 0;
        int shortest = Math.Min(firstBytes.Length, secondBytes.Length);
        while (offset < shortest && firstBytes[offset] == secondBytes[offset])
        {
            offset++;
        }

        string detail = $"the second write has {secondBytes.Length} bytes, the first {firstBytes.Length}; the first difference is at byte {offset}";
        if (second != null)
        {
            var difference = new AssetRecord(record.FileName, record.Type, record.Group);
            JsonDiff.Compare(first, second, string.Empty, difference);
            if (difference.ChangedValuesTotal > 0)
            {
                detail += $"; {difference.ChangedValuesTotal} value(s) differ, the first: {difference.ChangedValues[0].Path}";
            }
            else if (difference.LostKeys.Count > 0 || difference.AddedKeys.Count > 0)
            {
                detail += "; the keys differ";
            }
            else
            {
                detail += "; the JSON values are the same (formatting only)";
            }
        }

        record.Fail(RoundTripCheck.Unstable, detail);
    }

    // Runs one phase; an exception is the failure of the asset under the name of the phase's check.
    private static T? Attempt<T>(AssetRecord record, string check, string phase, Func<T> action) where T : class
    {
        try
        {
            return action();
        }
        catch (UnsupportedReferenceException)
        {
            throw;
        }
        catch (Exception exception)
        {
            record.Fail(check, $"{phase}: {Describe(exception)}");
            return null;
        }
    }

    private static bool AttemptAction(AssetRecord record, string check, string phase, Action action) =>
        Attempt<object>(record, check, phase, () =>
        {
            action();
            return new object();
        }) != null;

    private static string Describe(Exception exception)
    {
        string[] frames = (exception.StackTrace ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string where = string.Join(" <- ", frames.Take(3).Select(frame => frame.Trim()));
        return $"{exception.GetType().FullName}: {exception.Message} [{where}]";
    }

    // ---- load and write --------------------------------------------------------------------------------------

    private static LoadedAsset Load(string extension, JObject source)
    {
        JObject json = (JObject)source.DeepClone();
        switch (extension)
        {
            case ".world":
                return LoadWorld(json);
            case ".entity":
            {
                var entity = new Entity();
                entity.Load(json);
                return new LoadedAsset { Asset = entity };
            }

            case ".staticmodel":
            {
                var model = new StaticModel();
                model.Load(json);
                return new LoadedAsset { Asset = model };
            }

            case ".material":
            {
                var material = new MaterialAsset();
                material.Load(json);
                return new LoadedAsset { Asset = material };
            }

            case ".texture":
            {
                var texture = new Texture();
                texture.Load(json);
                return new LoadedAsset { Asset = texture };
            }

            case ".sound":
            {
                var sound = new SoundAsset();
                sound.Load(json);
                return new LoadedAsset { Asset = sound };
            }

            case ".uiscreen":
            {
                var screen = new UIScreenAsset();
                screen.Load(json);
                return new LoadedAsset { Asset = screen };
            }

            case ".sprite":
            {
                var sprite = new SpriteData();
                sprite.Load(json);
                return new LoadedAsset { Asset = sprite };
            }

            default:
                throw new InvalidOperationException($"No loader for '{extension}'.");
        }
    }

    // A world creates its entities only in a running game: load the world without its entity references, then each inline
    // entity node by itself, as WorldVerifier does.
    private static LoadedAsset LoadWorld(JObject json)
    {
        if (json["entity_references"] is not JArray references)
        {
            throw new InvalidDataException("The world has no entity_references array.");
        }

        var entityNodes = new List<JObject>();
        for (int index = 0; index < references.Count; index++)
        {
            var reference = references[index] as JObject;
            string assetId = reference?["asset_id"]?.ToString() ?? EmptyGuid;
            if (reference == null || assetId != EmptyGuid || reference["entity"] is not JObject entityNode)
            {
                throw new UnsupportedReferenceException($"entity_references[{index}] is not an inline entity (asset_id '{assetId}')");
            }

            entityNodes.Add(entityNode);
        }

        json["entity_references"] = new JArray();
        var world = new World();
        world.Load(json);

        var loaded = new LoadedAsset { World = world };
        foreach (JObject entityNode in entityNodes)
        {
            var entity = new Entity();
            entity.Load(entityNode);
            loaded.Entities.Add(entity);
        }

        return loaded;
    }

    private static void Save(string extension, LoadedAsset loaded, string path)
    {
        if (extension == ".world")
        {
            loaded.World!.FileName = path;
            EditorWorldWriter.SaveWorld(loaded.World, loaded.Entities);
        }
        else
        {
            EditorAssetWriterService.SaveAsset(path, loaded.Asset!);
        }
    }

    // ---- components ------------------------------------------------------------------------------------------

    private static void CountComponents(AssetRecord record, string extension, JObject source, LoadedAsset loaded)
    {
        if (extension is not (".world" or ".entity"))
        {
            return;
        }

        int real = 0;
        int unresolved = 0;
        int sourceNodes = 0;
        if (extension == ".world")
        {
            foreach (JToken reference in (JArray)source["entity_references"]!)
            {
                sourceNodes += CountEntityNodes((JObject)reference["entity"]!);
            }

            foreach (Entity entity in loaded.Entities)
            {
                CountEntity(entity, ref real, ref unresolved);
            }
        }
        else
        {
            sourceNodes = CountEntityNodes(source);
            CountEntity((Entity)loaded.Asset!, ref real, ref unresolved);
        }

        record.ComponentsReal = real;
        record.ComponentsUnresolved = unresolved;
        record.ComponentsSource = sourceNodes;

        if (unresolved > 0)
        {
            record.Fail(RoundTripCheck.UnresolvedType, $"{unresolved} component(s) loaded as UnresolvedComponent");
        }

        if (real != sourceNodes)
        {
            record.Fail(RoundTripCheck.ComponentCount, $"{real} component(s) loaded under their real type, {sourceNodes} component node(s) in the source file");
        }
    }

    // The root component, the "components" list, and the "children_component" tree under each of them.
    private static int CountEntityNodes(JObject entityNode)
    {
        int count = 0;
        if (entityNode["root_component"] is JObject root)
        {
            count += CountNodes(root);
        }

        if (entityNode["components"] is JArray components)
        {
            foreach (JToken component in components)
            {
                count += CountNodes((JObject)component);
            }
        }

        return count;
    }

    private static int CountNodes(JObject node)
    {
        int count = 1;
        if (node["children_component"] is JArray children)
        {
            foreach (JToken child in children)
            {
                count += CountNodes((JObject)child);
            }
        }

        return count;
    }

    private static void CountEntity(Entity entity, ref int real, ref int unresolved)
    {
        if (entity.RootComponent != null)
        {
            CountComponent(entity.RootComponent, ref real, ref unresolved);
        }

        foreach (EntityComponent component in entity.Components)
        {
            CountComponent(component, ref real, ref unresolved);
        }
    }

    private static void CountComponent(EntityComponent component, ref int real, ref int unresolved)
    {
        if (component is UnresolvedComponent)
        {
            unresolved++;
            return;
        }

        real++;
        if (component is SceneComponent scene)
        {
            foreach (SceneComponent child in scene.Children)
            {
                CountComponent(child, ref real, ref unresolved);
            }
        }
    }

    // ---- tampering, for the negative controls ----------------------------------------------------------------

    private static void ApplyTamper(RoundTripTamper tamper, JObject source, JObject written, AssetRecord record)
    {
        if (tamper == RoundTripTamper.RemoveKey)
        {
            JProperty key = written.Properties().First(property => property.Name != "id" && source.ContainsKey(property.Name));
            key.Remove();
            record.Details.Add($"control: the key '{key.Name}' was removed from the first write");
        }
        else
        {
            JProperty value = written.Properties().First(property =>
                property.Name != "id" && property.Value.Type == JTokenType.String && source.ContainsKey(property.Name));
            string was = value.Value.ToString();
            value.Value = was + "_changed";
            record.Details.Add($"control: the value of '{value.Name}' was changed from '{was}' in the first write");
        }
    }

    // ---- a preview asset -------------------------------------------------------------------------------------

    internal AssetRecord ProcessPreview(string fileName, string fullPath)
    {
        var record = new AssetRecord(fileName, Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.'), "preview");
        if (!File.Exists(fullPath))
        {
            record.Fail(RoundTripCheck.FileMissing, fullPath);
            return record;
        }

        try
        {
            AssetPreviewInfo info = AssetPreviewReader.Read(fullPath);
            record.PreviewKind = info.Kind.ToString();
            record.PreviewProblem = info.Problem;
            if (info.Problem != null)
            {
                record.Fail(RoundTripCheck.UnreadablePreview, info.Problem);
            }
        }
        catch (Exception exception)
        {
            record.Fail(RoundTripCheck.UnreadablePreview, $"the reader threw: {Describe(exception)}");
        }

        return record;
    }

    // ---- reports ---------------------------------------------------------------------------------------------

    private void WriteReports()
    {
        var summary = new StringBuilder();
        var summaryJson = new JObject();
        foreach (IGrouping<string, AssetRecord> type in Records.GroupBy(record => record.Type).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            int passed = type.Count(record => !record.Failed);
            int failed = type.Count() - passed;
            summary.AppendLine($"{type.Key,-12} {type.Count(),4} assets  {passed,4} passed  {failed,4} failed");

            var checks = type.SelectMany(record => record.FailedChecks).GroupBy(check => check).OrderBy(group => group.Key, StringComparer.Ordinal).ToList();
            foreach (var check in checks)
            {
                summary.AppendLine($"    {check.Key}: {check.Count()} asset(s)");
            }

            var added = new SortedDictionary<string, (int Assets, int Occurrences)>(StringComparer.Ordinal);
            foreach (AssetRecord record in type)
            {
                foreach (var key in record.AddedKeys)
                {
                    added[key.Key] = added.TryGetValue(key.Key, out var known)
                        ? (known.Assets + 1, known.Occurrences + key.Value.Count)
                        : (1, key.Value.Count);
                }
            }

            foreach (var key in added)
            {
                summary.AppendLine($"    added key {key.Key}: {key.Value.Assets} asset(s), {key.Value.Occurrences} time(s)");
            }

            summaryJson[type.Key] = new JObject
            {
                ["assets"] = type.Count(),
                ["passed"] = passed,
                ["failed"] = failed,
                ["failed_checks"] = new JObject(checks.Select(check => new JProperty(check.Key, check.Count()))),
                ["added_keys"] = new JArray(added.Select(key => new JObject
                {
                    ["path"] = key.Key,
                    ["assets"] = key.Value.Assets,
                    ["occurrences"] = key.Value.Occurrences,
                })),
            };
        }

        summary.AppendLine($"total {Records.Count} assets, {Records.Count(record => !record.Failed)} passed, {Records.Count(record => record.Failed)} failed");

        summary.AppendLine("components of worlds and entities (loaded under their real type / unresolved / component nodes of the source):");
        foreach (AssetRecord record in Records.Where(record => record.ComponentsSource != null))
        {
            summary.AppendLine($"    {record.FileName}: {record.ComponentsReal} / {record.ComponentsUnresolved} / {record.ComponentsSource}  {record.Verdict}");
        }

        summary.AppendLine("failures:");
        foreach (AssetRecord record in Records.Where(record => record.Failed))
        {
            summary.AppendLine($"    {record.FileName}: {string.Join(", ", record.FailedChecks)}");
            foreach (string detail in record.Details)
            {
                summary.AppendLine($"        {detail}");
            }
        }

        Console.Write(summary.ToString());
        File.WriteAllText(Path.Combine(_outputDirectory, "summary.txt"), summary.ToString());

        var report = new JObject
        {
            ["content"] = _contentDirectory,
            ["summary"] = summaryJson,
            ["assets"] = new JArray(Records.Select(record => record.ToJson())),
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "roundtrip.json"), report.ToString(Formatting.Indented));
        Console.WriteLine($"reports: {Path.Combine(_outputDirectory, "roundtrip.json")}");
    }

    // ---- the negative controls -------------------------------------------------------------------------------

    /// <summary>
    /// <c>--roundtrip-controls</c>: runs each check on a copy that is made to fail, in a scratch folder under the output folder,
    /// and requires that the check of that name, and no other one first, reports it. Returns 1 if a control is not caught.
    /// </summary>
    public int RunControls()
    {
        string scratch = Path.Combine(_outputDirectory, "controls");
        Directory.CreateDirectory(scratch);
        List<CatalogueItem> catalogue = ReadCatalogue();
        var failures = new List<string>();
        var results = new JArray();

        CatalogueItem Pick(string extension, string? preferred = null) =>
            catalogue.FirstOrDefault(item => preferred != null && string.Equals(item.FileName, preferred, StringComparison.OrdinalIgnoreCase))
            ?? catalogue.First(item => item.FileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

        void Report(string control, string expected, AssetRecord record)
        {
            bool caught = record.PrimaryCheck == expected;
            string verdict = caught ? "CAUGHT" : "NOT CAUGHT";
            Console.WriteLine($"control {control}: expected '{expected}', reported under '{record.PrimaryCheck ?? "pass"}' (all: [{string.Join(", ", record.FailedChecks)}]) {verdict}");
            foreach (string detail in record.Details)
            {
                Console.WriteLine($"    {detail}");
            }

            if (!caught)
            {
                failures.Add(control);
            }

            results.Add(new JObject { ["control"] = control, ["expected"] = expected, ["reported"] = record.PrimaryCheck, ["caught"] = caught, ["record"] = record.ToJson() });
        }

        void Baseline(string control, AssetRecord record)
        {
            Console.WriteLine($"baseline of control {control}: {record.FileName} {record.Verdict}");
            if (record.Failed)
            {
                foreach (string detail in record.Details)
                {
                    Console.WriteLine($"    {detail}");
                }

                failures.Add($"baseline of {control}");
            }
        }

        // (a), (b), (c): a copy of a material.
        CatalogueItem material = Pick(".material");
        string materialCopy = CopyToScratch(scratch, "material", Path.Combine(_contentDirectory, material.FileName), material.FileName);
        Baseline("a-c", ProcessEditable(material.FileName, materialCopy, Path.Combine(scratch, "material-baseline"), RoundTripTamper.None));
        Report("(a) key lost", RoundTripCheck.KeyLost, ProcessEditable(material.FileName, materialCopy, Path.Combine(scratch, "a-key-lost"), RoundTripTamper.RemoveKey));
        Report("(b) value changed", RoundTripCheck.ValueChanged, ProcessEditable(material.FileName, materialCopy, Path.Combine(scratch, "b-value-changed"), RoundTripTamper.ChangeValue));
        Report("(c) unstable", RoundTripCheck.Unstable, ProcessEditable(material.FileName, materialCopy, Path.Combine(scratch, "c-unstable"), RoundTripTamper.AlterSecondWrite));

        // (d): a copy of a world in which one component type is replaced by a name no assembly declares.
        CatalogueItem world = Pick(".world", "Worlds/FrontEnd.world");
        string worldBaseline = CopyToScratch(scratch, "world-baseline", Path.Combine(_contentDirectory, world.FileName), world.FileName);
        Baseline("d", ProcessEditable(world.FileName, worldBaseline, Path.Combine(scratch, "world-baseline"), RoundTripTamper.None));
        JObject worldJson = ParseJson(File.ReadAllText(worldBaseline));
        string replaced = ReplaceOneComponentType(worldJson, "RgceControlNoSuchComponentType");
        string worldCopy = Path.Combine(scratch, "d-unresolved-type", "source", world.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(worldCopy)!);
        File.WriteAllText(worldCopy, worldJson.ToString(Formatting.Indented));
        Console.WriteLine($"control (d): the component type '{replaced}' of {world.FileName} was replaced by 'RgceControlNoSuchComponentType'");
        Report("(d) unresolved type", RoundTripCheck.UnresolvedType, ProcessEditable(world.FileName, worldCopy, Path.Combine(scratch, "d-unresolved-type"), RoundTripTamper.None));

        // (e): a copy of a wav overwritten with bytes that are not a RIFF.
        CatalogueItem wav = Pick(".wav");
        string wavBaseline = CopyToScratch(scratch, "wav-baseline", Path.Combine(_contentDirectory, wav.FileName), wav.FileName);
        Baseline("e", ProcessPreview(wav.FileName, wavBaseline));
        string wavCopy = Path.Combine(scratch, "e-unreadable-preview", "source", wav.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(wavCopy)!);
        File.WriteAllBytes(wavCopy, Encoding.ASCII.GetBytes("This is not a RIFF file, only text of the negative control. 0123456789012345678901234567890123456789"));
        Report("(e) unreadable preview", RoundTripCheck.UnreadablePreview, ProcessPreview(wav.FileName, wavCopy));

        File.WriteAllText(Path.Combine(scratch, "controls.json"), results.ToString(Formatting.Indented));
        Console.WriteLine(failures.Count == 0 ? "controls: all 5 caught under their own name" : $"controls: FAILED: {string.Join(", ", failures)}");
        return failures.Count == 0 ? 0 : 1;
    }

    private static string CopyToScratch(string scratch, string folder, string sourcePath, string fileName)
    {
        string target = Path.Combine(scratch, folder, "source", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(sourcePath, target, overwrite: true);
        return target;
    }

    // Renames the type of the first leaf component (no children) of the first entity that has one; returns the old type.
    private static string ReplaceOneComponentType(JObject world, string newType)
    {
        foreach (JToken reference in (JArray)world["entity_references"]!)
        {
            if (reference["entity"] is not JObject entity)
            {
                continue;
            }

            var nodes = new List<JObject>();
            if (entity["root_component"] is JObject root)
            {
                nodes.Add(root);
            }

            if (entity["components"] is JArray components)
            {
                nodes.AddRange(components.OfType<JObject>());
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                JObject node = nodes[index];
                if (node["children_component"] is JArray children)
                {
                    nodes.AddRange(children.OfType<JObject>());
                }
            }

            foreach (JObject node in nodes)
            {
                bool leaf = node["children_component"] is not JArray { Count: > 0 };
                if (leaf && node["type"]?.Type == JTokenType.String)
                {
                    string old = node["type"]!.Value<string>()!;
                    node["type"] = newType;
                    return old;
                }
            }
        }

        throw new InvalidOperationException("The world has no component to rename.");
    }
}
