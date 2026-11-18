namespace RacingGame.Gameplay.Race;

internal static class RaceBootstrapAssets
{
    public const string TrackBeginnerData = "TrackBeginnerData";
    public const string TrackAdvancedData = "TrackAdvancedData";
    public const string TrackExpertData = "TrackExpertData";
    public const string LandscapeHeightsData = "Data.LandscapeHeights";

    public static string GetTrackDataAssetName(string trackName)
    {
        return trackName switch
        {
            "Beginner" => TrackBeginnerData,
            "Advanced" => TrackAdvancedData,
            "Expert" => TrackExpertData,
            _ => TrackBeginnerData,
        };
    }

    /// <summary>
    /// The reverse of <see cref="GetTrackDataAssetName"/>: the track a catalogued .Track asset belongs to. The scenery's seed
    /// is one constant (<see cref="Track.RaceTrackLayout.ScenerySeed"/>, ADR-0041); the track name only selects the track's
    /// data and rules, so a generator component gets it from its track asset.
    /// </summary>
    public static bool TryGetTrackName(string trackDataAssetName, out string trackName)
    {
        switch (trackDataAssetName)
        {
            case TrackBeginnerData:
                trackName = "Beginner";
                return true;
            case TrackAdvancedData:
                trackName = "Advanced";
                return true;
            case TrackExpertData:
                trackName = "Expert";
                return true;
            default:
                trackName = string.Empty;
                return false;
        }
    }
}