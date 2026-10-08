using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Track;

/// <summary>
/// A model a generator component places under itself, with its transform relative to the generator.
/// <paramref name="OwnsBuffers"/> is true for a procedural model the generator created, whose GPU buffers it frees;
/// it is false for a model shared through the factory's static caches (posts, column segments), which it never frees.
/// </summary>
internal sealed record RaceGeneratedModel(string Name, StaticModel Model, Matrix Transform, bool CastShadows, bool OwnsBuffers);

/// <summary>The catalogued inputs of a track: its .Track layout and its landscape height map.</summary>
internal sealed record RaceTrackSource(string TrackName, LegacyTrackLayout Track, string HeightmapPath, LegacyTerrainHeightSampler Heights);

internal static partial class LegacyTrackSceneFactory
{
    /// <summary>
    /// Resolves the track and height map assets of a generator component. Returns false after writing one warning
    /// that names <paramref name="owner"/> when an id is unknown, is not a race track, or has no file.
    /// </summary>
    internal static bool TryResolveTrackSource(string owner, Guid trackAssetId, Guid heightmapAssetId, out RaceTrackSource? source)
    {
        source = null;

        AssetInfo? trackInfo = trackAssetId == Guid.Empty ? null : AssetCatalog.Get(trackAssetId);
        if (trackInfo == null)
        {
            Logs.WriteWarning($"{owner}: track_asset_id '{trackAssetId}' is not an asset of the catalog; nothing is generated.");
            return false;
        }

        if (!RaceBootstrapAssets.TryGetTrackName(trackInfo.Name, out _))
        {
            Logs.WriteWarning($"{owner}: track_asset_id '{trackAssetId}' is the asset '{trackInfo.Name}', which is not a race track; nothing is generated.");
            return false;
        }

        AssetInfo? heightmapInfo = heightmapAssetId == Guid.Empty ? null : AssetCatalog.Get(heightmapAssetId);
        if (heightmapInfo == null)
        {
            Logs.WriteWarning($"{owner}: heightmap_asset_id '{heightmapAssetId}' is not an asset of the catalog; nothing is generated.");
            return false;
        }

        string trackPath = ResolveAssetFilePath(trackInfo);
        if (!File.Exists(trackPath))
        {
            Logs.WriteWarning($"{owner}: the file '{trackPath}' of the track asset '{trackInfo.Name}' does not exist; nothing is generated.");
            return false;
        }

        string heightmapPath = ResolveAssetFilePath(heightmapInfo);
        if (!File.Exists(heightmapPath))
        {
            Logs.WriteWarning($"{owner}: the file '{heightmapPath}' of the height map asset '{heightmapInfo.Name}' does not exist; nothing is generated.");
            return false;
        }

        source = LoadTrackSource(trackInfo, trackPath, heightmapPath);
        return true;
    }

    /// <summary>The same resolution for the race world's own assembly, where a missing asset is a developer error.</summary>
    private static RaceTrackSource ResolveTrackSource(Guid trackAssetId, Guid heightmapAssetId)
    {
        AssetInfo trackInfo = AssetCatalog.Get(trackAssetId)
            ?? throw new InvalidOperationException($"Asset '{trackAssetId}' is not registered in AssetInfos.json.");
        AssetInfo heightmapInfo = AssetCatalog.Get(heightmapAssetId)
            ?? throw new InvalidOperationException($"Asset '{heightmapAssetId}' is not registered in AssetInfos.json.");

        return LoadTrackSource(trackInfo, ResolveAssetFilePath(trackInfo), ResolveAssetFilePath(heightmapInfo));
    }

    private static RaceTrackSource LoadTrackSource(AssetInfo trackInfo, string trackPath, string heightmapPath)
    {
        RaceBootstrapAssets.TryGetTrackName(trackInfo.Name, out string trackName);
        LegacyTrackLayout track = LoadTrackLayout(trackInfo.Name, trackPath);
        LegacyTerrainHeightSampler heights = LoadTerrainHeightSampler(heightmapPath);
        return new RaceTrackSource(trackName, track, heightmapPath, heights);
    }

    /// <summary>The layout a generator component works from: no scenery, the post and column spacing of its settings.</summary>
    internal static RaceTrackLayout ComputeGeneratorLayout(RaceTrackSource source, RaceTrackLayoutSettings settings)
    {
        return RaceTrackLayout.Compute(
            source.TrackName,
            source.Track,
            source.Heights,
            LoadCombiModel,
            static _ => null,
            settings,
            includeScenery: false);
    }

    internal static RaceGeneratedModel CreateTerrainModel(RaceTrackSource source, AssetContentManager assetContentManager)
    {
        Vector3 origin = RaceTrackLayout.ComputeOrigin(source.Track.TrackPoints);
        return LegacyTerrainMeshBuilder.CreateModel(source.TrackName, origin, source.HeightmapPath, assetContentManager);
    }

    internal static IReadOnlyList<RaceGeneratedModel> CreateGuardRailModels(RaceTrackLayout layout, bool leftSide, AssetContentManager assetContentManager)
    {
        return LegacyTrackGuardRailBuilder.CreateRailModels(layout.TrackName, layout, leftSide, assetContentManager);
    }

    internal static IReadOnlyList<RaceGeneratedModel> CreateColumnGeneratorModels(RaceTrackLayout layout, AssetContentManager assetContentManager)
    {
        return LegacyTrackGuardRailBuilder.CreateColumnModels(layout.TrackName, layout, assetContentManager);
    }

    internal static RaceGeneratedModel CreateRoadGeneratorModel(RaceTrackLayout layout, AssetContentManager assetContentManager)
    {
        return CreateRoadModel(layout.TrackName, layout.RoadPoints, layout.Origin, assetContentManager);
    }
}
