using CasaEngine.Framework.Assets;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Track;

/// <summary>
/// A model a generator component places under itself, with its transform relative to the generator.
/// <paramref name="OwnsBuffers"/> is true for a procedural <paramref name="Model"/> the generator created, whose GPU buffers it
/// frees. It is false for a catalogued model (posts, column segments), named by <paramref name="StaticModelAssetId"/> with a
/// null <paramref name="Model"/>: the engine shares it, and the generator never frees it.
/// </summary>
internal sealed record RaceGeneratedModel(string Name, StaticModel? Model, Matrix Transform, bool CastShadows, bool OwnsBuffers, Guid StaticModelAssetId = default, bool ReceiveShadows = true);

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

    /// <summary>
    /// The layout a generator component works from: the post and column spacing of its settings. The neutral objects, the
    /// posts and the column segments go through the same add path as the race's, in the same order, so a generator's lists are
    /// the race's. None of them draws a random number, so there is no seed and no generated scenery.
    /// </summary>
    internal static RaceTrackLayout ComputeGeneratorLayout(RaceTrackSource source, RaceTrackLayoutSettings settings)
    {
        return RaceTrackLayout.Compute(
            source.TrackName,
            source.Track,
            source.Heights,
            LoadCombiModel,
            seed: 0,
            settings,
            includeScenery: false);
    }

    internal static RaceGeneratedModel CreateTerrainModel(RaceTrackSource source, Guid materialAssetId)
    {
        Vector3 origin = RaceTrackLayout.ComputeOrigin(source.Track.TrackPoints);
        return LegacyTerrainMeshBuilder.CreateModel(source.TrackName, origin, source.HeightmapPath, materialAssetId);
    }

    internal static RaceGeneratedModel CreateCityGroundGeneratorModel(RaceTrackSource source)
    {
        Vector3 origin = RaceTrackLayout.ComputeOrigin(source.Track.TrackPoints);
        return CreateCityGroundModel(source.TrackName, origin, RaceTrackMaterialAssets.Ground);
    }

    internal static IReadOnlyList<RaceGeneratedModel> CreateGuardRailModels(RaceTrackLayout layout, bool leftSide, Guid materialAssetId)
    {
        return LegacyTrackGuardRailBuilder.CreateRailModels(layout.TrackName, layout, leftSide, materialAssetId);
    }

    internal static IReadOnlyList<RaceGeneratedModel> CreateColumnGeneratorModels(RaceTrackLayout layout, Guid materialAssetId)
    {
        return LegacyTrackGuardRailBuilder.CreateColumnModels(layout.TrackName, layout, materialAssetId);
    }

    internal static RaceGeneratedModel CreateRoadGeneratorModel(RaceTrackLayout layout, Guid materialAssetId)
    {
        return CreateRoadModel(layout.TrackName, layout.RoadPoints, layout.Origin, materialAssetId);
    }

    internal static RaceGeneratedModel CreateRoadBackGeneratorModel(RaceTrackLayout layout, Guid materialAssetId)
    {
        return CreateRoadBackModel(layout.TrackName, layout.RoadPoints, layout.Origin, materialAssetId);
    }

    /// <summary>The tunnels of the road, or null when the track has none.</summary>
    internal static RaceGeneratedModel? CreateTunnelGeneratorModel(RaceTrackLayout layout, Guid materialAssetId)
    {
        return CreateTunnelModel(layout.TrackName, layout.RoadPoints, layout.TunnelSpans, layout.Origin, materialAssetId);
    }
}
