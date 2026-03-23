namespace RacingGameCasaEngine.Bootstrap;

internal static class RaceBootstrapAssets
{
    public const string MenuBackground = "MenuBackground";
    public const string MenuButtons = "MenuButtons";
    public const string TrackBeginnerData = "TrackBeginnerData";
    public const string TrackAdvancedData = "TrackAdvancedData";
    public const string TrackExpertData = "TrackExpertData";

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
}