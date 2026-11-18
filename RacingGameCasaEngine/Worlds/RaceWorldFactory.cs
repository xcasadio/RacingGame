using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.World;
using RacingGame.Gameplay.Race;

namespace RacingGameCasaEngine.Worlds;

public static class RaceWorldFactory
{
    // The worlds are assets of the catalogue (written by scripts/RgceAssetCooker --worlds): a loaded world takes the name of
    // its entry, and is asked for by the file_name of that entry.
    internal const string FrontEndWorldName = "World.FrontEnd";
    internal const string FrontEndWorldFileName = "Worlds/FrontEnd.world";
    internal const string RaceWorldNamePrefix = "World.Race.";
    internal const string PlayerCarAssetName = "Entity.PlayerCar";
    internal const string KeyLightEntityName = RaceWorldNames.KeyLightEntityName;
    internal const string CameraEntityName = RaceWorldNames.CameraEntityName;
    internal const string PlayerStartEntityName = RaceWorldNames.PlayerStartEntityName;
    internal const string PlayerCarEntityName = RaceWorldNames.PlayerCarEntityName;
    internal const string CheckpointEntityNamePrefix = RaceWorldNames.CheckpointEntityNamePrefix;
    internal const string TrackRoadEntityNamePrefix = RaceWorldNames.TrackRoadEntityNamePrefix;
    internal const string TrackGroundEntityNamePrefix = RaceWorldNames.TrackGroundEntityNamePrefix;
    internal const string TrackSceneryEntityNamePrefix = RaceWorldNames.TrackSceneryEntityNamePrefix;
    internal const string TrackGuardRailEntityNamePrefix = RaceWorldNames.TrackGuardRailEntityNamePrefix;
    internal const string TrackGuardRailHolderEntityNamePrefix = RaceWorldNames.TrackGuardRailHolderEntityNamePrefix;
    internal const string TrackColumnsEntityNamePrefix = RaceWorldNames.TrackColumnsEntityNamePrefix;
    internal const string TrackColumnSegmentEntityNamePrefix = RaceWorldNames.TrackColumnSegmentEntityNamePrefix;

    internal static bool IsRaceWorld(World world)
    {
        return world.Name?.StartsWith(RaceWorldNamePrefix, StringComparison.Ordinal) == true;
    }

    internal static bool IsRaceRenderableEntity(Entity entity)
    {
        string? entityName = entity.Name;
        return entityName?.StartsWith(TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackGroundEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackSceneryEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackGuardRailEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackGuardRailHolderEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackColumnsEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(TrackColumnSegmentEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName?.StartsWith(CheckpointEntityNamePrefix, StringComparison.Ordinal) == true
            || entityName == PlayerStartEntityName
            || entityName == PlayerCarEntityName;
    }

    internal static bool IsVisibleInCircuitOnlyView(Entity entity)
    {
        return entity.Name?.StartsWith(TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// The <c>file_name</c> of the catalogue entry of the race world of <paramref name="trackName"/>, which is what
    /// <c>GameManager.SetWorldToLoad(string)</c> looks the world up by (<c>Worlds/Race/Track&lt;track&gt;.world</c>, the entry
    /// <c>World.Race.Track&lt;track&gt;</c>). Throws when the catalogue has no such entry.
    /// </summary>
    internal static string GetRaceWorldFileName(string trackName)
    {
        string worldName = $"{RaceWorldNamePrefix}Track{trackName}";
        return AssetCatalog.Get(worldName)?.FileName
            ?? throw new InvalidOperationException($"The race world '{worldName}' is not in the asset catalog.");
    }
}
