using CasaEngine.Framework.Assets;

namespace RacingGame.Gameplay.Track;

/// <summary>
/// The catalogue ids of the cooked track materials (scripts/RgceAssetCooker, <c>Material.Track.&lt;Role&gt;</c>): the
/// default <c>material_asset_id</c> of the generator components. They are uuid5(uuid5(NAMESPACE_URL,
/// "racinggame-casaengine/ui-assets"), asset name), like every catalogued id.
/// </summary>
internal static class RaceTrackMaterialAssets
{
    /// <summary>Material.Track.Road</summary>
    public static readonly Guid Road = new("9115353e-e656-50e3-a5c1-586824eec3fa");

    /// <summary>Material.Track.Terrain</summary>
    public static readonly Guid Terrain = new("fc904080-b053-51e1-82e2-33858bfe1140");

    /// <summary>Material.Track.GuardRail</summary>
    public static readonly Guid GuardRail = new("e3ace877-45bf-5a01-8b79-4ef50791173d");

    /// <summary>Material.Track.RoadColumn</summary>
    public static readonly Guid RoadColumn = new("32637518-c040-5490-914f-78db79eab3df");

    /// <summary>Material.Track.RoadTunnel</summary>
    public static readonly Guid RoadTunnel = new("c89f5ae0-0731-536b-b1d5-d442853b589f");
}

internal static partial class LegacyTrackSceneFactory
{
    private const string StaticModelAssetNamePrefix = "StaticModel.";

    private static readonly HashSet<string> MissingModelNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The catalogue id of the cooked model of a legacy model name (already resolved to the name the original's add path
    /// keeps, see <see cref="LegacyLandscapePlacer"/>). Returns false, with one warning per name, when the catalogue has no
    /// such model.
    /// </summary>
    internal static bool TryGetStaticModelAssetId(string modelName, out Guid assetId)
    {
        AssetInfo? assetInfo = AssetCatalog.Get(StaticModelAssetNamePrefix + modelName);
        if (assetInfo != null)
        {
            assetId = assetInfo.Id;
            return true;
        }

        if (MissingModelNames.Add(modelName))
        {
            Logs.WriteWarning($"Legacy track model '{modelName}' has no '{StaticModelAssetNamePrefix}{modelName}' asset in the catalog.");
        }

        assetId = Guid.Empty;
        return false;
    }
}
