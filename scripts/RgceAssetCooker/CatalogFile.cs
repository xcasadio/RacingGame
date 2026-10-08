using System.Text;
using Newtonsoft.Json.Linq;

namespace RgceAssetCooker;

internal sealed record CatalogEntry(string Id, string Name, string FileName, string AssetType);

/// <summary>
/// Content/AssetInfos.json, read and written byte for byte as the Python generators do (json.dumps indent=2,
/// ensure_ascii=False, CRLF, no final newline). Every entry is a flat object of the four string keys id, name,
/// file_name and asset_type; <see cref="Load"/> refuses anything else and refuses a file it could not write back unchanged.
/// </summary>
internal sealed class CatalogFile
{
    private static readonly string[] Keys = ["id", "name", "file_name", "asset_type"];

    private CatalogFile(string path, List<CatalogEntry> entries)
    {
        Path = path;
        Entries = entries;
    }

    public string Path { get; }

    public List<CatalogEntry> Entries { get; }

    public static CatalogFile Load(string path)
    {
        byte[] original = File.ReadAllBytes(path);
        JObject root = JObject.Parse(Encoding.UTF8.GetString(original));
        if (root.Properties().Count() != 1 || root["asset_infos"] is not JArray array)
        {
            throw new InvalidDataException($"{path}: the only top-level key must be asset_infos.");
        }

        var entries = new List<CatalogEntry>(array.Count);
        foreach (JToken token in array)
        {
            if (token is not JObject entry || !entry.Properties().Select(p => p.Name).SequenceEqual(Keys))
            {
                throw new InvalidDataException($"{path}: an entry does not have exactly the keys {string.Join(", ", Keys)}.");
            }

            entries.Add(new CatalogEntry(
                entry.Value<string>("id")!,
                entry.Value<string>("name")!,
                entry.Value<string>("file_name")!,
                entry.Value<string>("asset_type")!));
        }

        var catalog = new CatalogFile(path, entries);
        if (!catalog.ToBytes().AsSpan().SequenceEqual(original))
        {
            throw new InvalidDataException($"{path} is not in the format the catalogue writers produce: it would not be written back unchanged.");
        }

        return catalog;
    }

    /// <summary>Replaces the entry of the same name in place, or appends. Returns true if the catalogue changed.</summary>
    public bool Upsert(CatalogEntry entry)
    {
        int index = Entries.FindIndex(current => current.Name == entry.Name);
        if (index < 0)
        {
            Entries.Add(entry);
            return true;
        }

        if (Entries[index] == entry)
        {
            return false;
        }

        Entries[index] = entry;
        return true;
    }

    public CatalogEntry? FindByName(string name) => Entries.FirstOrDefault(entry => entry.Name == name);

    public CatalogEntry? FindByFile(string fileName)
        => Entries.FirstOrDefault(entry => string.Equals(Normalize(entry.FileName), Normalize(fileName), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string fileName) => fileName.Replace('\\', '/');

    public byte[] ToBytes()
    {
        var text = new StringBuilder();
        text.Append("{\r\n  \"asset_infos\": [");
        for (int index = 0; index < Entries.Count; index++)
        {
            CatalogEntry entry = Entries[index];
            text.Append(index == 0 ? "\r\n" : ",\r\n");
            text.Append("    {\r\n");
            text.Append("      \"id\": ").Append(Quote(entry.Id)).Append(",\r\n");
            text.Append("      \"name\": ").Append(Quote(entry.Name)).Append(",\r\n");
            text.Append("      \"file_name\": ").Append(Quote(entry.FileName)).Append(",\r\n");
            text.Append("      \"asset_type\": ").Append(Quote(entry.AssetType)).Append("\r\n");
            text.Append("    }");
        }

        text.Append(Entries.Count == 0 ? "]\r\n}" : "\r\n  ]\r\n}");
        return new UTF8Encoding(false).GetBytes(text.ToString());
    }

    // json.dumps with ensure_ascii=False.
    private static string Quote(string value)
    {
        var text = new StringBuilder("\"");
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                case '\b': text.Append("\\b"); break;
                case '\f': text.Append("\\f"); break;
                case < ' ': text.Append("\\u").Append(((int)c).ToString("x4")); break;
                default: text.Append(c); break;
            }
        }

        return text.Append('"').ToString();
    }
}
