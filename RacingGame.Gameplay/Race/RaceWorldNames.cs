namespace RacingGame.Gameplay.Race;

/// <summary>The names and prefixes of the entities of a race world that the game's components look up. The world
/// assembly (<c>RaceWorldFactory</c>) names the entities with them.</summary>
internal static class RaceWorldNames
{
    internal const string CameraEntityName = "BootstrapCamera";
    internal const string PlayerStartEntityName = "PlayerStart";
    internal const string PlayerCarEntityName = "PlayerCar";
    internal const string CheckpointEntityNamePrefix = "Checkpoint.";
    internal const string TrackRoadEntityNamePrefix = "Track.Road.";
    internal const string TrackGroundEntityNamePrefix = "Track.Ground.";
    internal const string TrackSceneryEntityNamePrefix = "Track.Scenery.";
    internal const string TrackGuardRailEntityNamePrefix = "Track.GuardRail.";
    internal const string TrackGuardRailHolderEntityNamePrefix = "Track.GuardRailHolder.";
    internal const string TrackColumnsEntityNamePrefix = "Track.Columns.";
    internal const string TrackColumnSegmentEntityNamePrefix = "Track.ColumnSegment.";
}
