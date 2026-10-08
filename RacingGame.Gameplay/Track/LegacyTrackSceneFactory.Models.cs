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
}

internal static partial class LegacyTrackSceneFactory
{
    private const string StaticModelAssetNamePrefix = "StaticModel.";

    private static readonly HashSet<string> MissingModelNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The catalogue id of the cooked model of a legacy model name, after the name aliases. Returns false, with one
    /// warning per name, when the catalogue has no such model.
    /// </summary>
    internal static bool TryGetStaticModelAssetId(string legacyModelName, out Guid assetId)
    {
        string modelName = RaceTrackLayout.NormalizeLegacyModelName(legacyModelName);
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

    /// <summary>
    /// The size of a cooked model for the scenery placement, null when the catalogue has no such model. Computed once per
    /// model, from the model the engine loads; the hold on it is given back at once, the components that place it hold it.
    /// </summary>
    private static float? GetLegacyModelSize(string legacyModelName, AssetContentManager assetContentManager)
    {
        string modelName = RaceTrackLayout.NormalizeLegacyModelName(legacyModelName);
        if (ModelSizeCache.TryGetValue(modelName, out float cachedSize))
        {
            return cachedSize;
        }

        if (!TryGetStaticModelAssetId(modelName, out Guid assetId))
        {
            return null;
        }

        using AssetHandle<StaticModel> handle = assetContentManager.Acquire<StaticModel>(assetId);
        float size = ComputeLegacyModelSize(handle.Asset);
        ModelSizeCache[modelName] = size;
        return size;
    }
}
