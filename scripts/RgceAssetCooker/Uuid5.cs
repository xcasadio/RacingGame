using System.Security.Cryptography;
using System.Text;

namespace RgceAssetCooker;

/// <summary>
/// RFC 4122 name-based UUID (version 5), as Python's <c>uuid.uuid5</c>, so the cooker and the Python generators
/// (scripts/generate_rgce_content_assets.py, generate_rgce_ui_assets.py) derive the same id from an asset name.
/// </summary>
internal static class Uuid5
{
    private static readonly Guid NamespaceUrl = new("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

    /// <summary>The namespace of the RacingGameCasaEngine catalogue ids: uuid5(NAMESPACE_URL, "racinggame-casaengine/ui-assets").</summary>
    public static readonly Guid CatalogueNamespace = Create(NamespaceUrl, "racinggame-casaengine/ui-assets");

    /// <summary>The catalogue id of the asset <paramref name="assetName"/>.</summary>
    public static Guid ForAsset(string assetName) => Create(CatalogueNamespace, assetName);

    public static Guid Create(Guid namespaceId, string name)
    {
        byte[] namespaceBytes = ToRfcBytes(namespaceId);
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(input, 0);
        nameBytes.CopyTo(input, namespaceBytes.Length);

        byte[] hash = SHA1.HashData(input);
        byte[] bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return FromRfcBytes(bytes);
    }

    // Guid.ToByteArray() stores the first three groups little-endian; the RFC (and Python) hash them big-endian.
    private static byte[] ToRfcBytes(Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        Array.Reverse(bytes, 0, 4);
        Array.Reverse(bytes, 4, 2);
        Array.Reverse(bytes, 6, 2);
        return bytes;
    }

    private static Guid FromRfcBytes(byte[] bytes)
    {
        byte[] copy = (byte[])bytes.Clone();
        Array.Reverse(copy, 0, 4);
        Array.Reverse(copy, 4, 2);
        Array.Reverse(copy, 6, 2);
        return new Guid(copy);
    }
}
