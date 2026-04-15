using System;
using System.IO;
using Newtonsoft.Json.Linq;
using RacingGameCasaEngine.Bootstrap;
using RacingGameCasaEngine.Components;

namespace RacingGameCasaEngine.Persistence;

internal static class FrontEndOptionsPersistence
{
    internal static void Load(string fileName, RaceFrontEndState state)
    {
        if (!File.Exists(fileName))
        {
            return;
        }

        JObject rootElement = JObject.Parse(File.ReadAllText(fileName));
        state.PlayerName = rootElement["PlayerName"]?.Value<string>() ?? state.PlayerName;
        state.EnablePostEffects = rootElement["EnablePostEffects"]?.Value<bool>() ?? state.EnablePostEffects;
        state.EnableShadows = rootElement["EnableShadows"]?.Value<bool>() ?? state.EnableShadows;
        state.EnableHighDetail = rootElement["EnableHighDetail"]?.Value<bool>() ?? state.EnableHighDetail;
        state.ShowFps = rootElement["ShowFps"]?.Value<bool>() ?? state.ShowFps;
        state.EnableVibration = rootElement["EnableVibration"]?.Value<bool>() ?? state.EnableVibration;
        state.SoundVolume = ClampPercentage(rootElement["SoundVolume"]?.Value<int>() ?? state.SoundVolume);
        state.MusicVolume = ClampPercentage(rootElement["MusicVolume"]?.Value<int>() ?? state.MusicVolume);
        state.ControllerSensitivity = ClampPercentage(rootElement["ControllerSensitivity"]?.Value<int>() ?? state.ControllerSensitivity);

        string? selectedDrivingMode = rootElement["SelectedDrivingMode"]?.Value<string>();
        if (Enum.TryParse(selectedDrivingMode, ignoreCase: true, out VehicleDrivingMode drivingMode)
            && Enum.IsDefined(drivingMode))
        {
            state.SelectedDrivingMode = drivingMode;
        }
    }

    internal static void Save(string fileName, RaceFrontEndState state)
    {
        string? directoryName = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        var rootElement = new JObject
        {
            ["PlayerName"] = state.PlayerName,
            ["EnablePostEffects"] = state.EnablePostEffects,
            ["EnableShadows"] = state.EnableShadows,
            ["EnableHighDetail"] = state.EnableHighDetail,
            ["ShowFps"] = state.ShowFps,
            ["EnableVibration"] = state.EnableVibration,
            ["SoundVolume"] = ClampPercentage(state.SoundVolume),
            ["MusicVolume"] = ClampPercentage(state.MusicVolume),
            ["ControllerSensitivity"] = ClampPercentage(state.ControllerSensitivity),
            ["SelectedDrivingMode"] = state.SelectedDrivingMode.ToString(),
        };

        File.WriteAllText(fileName, rootElement.ToString());
    }

    private static int ClampPercentage(int value)
    {
        return Math.Clamp(value, 0, 100);
    }
}