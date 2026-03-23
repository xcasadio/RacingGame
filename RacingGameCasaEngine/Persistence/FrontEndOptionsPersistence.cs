using System;
using System.IO;
using Newtonsoft.Json.Linq;
using RacingGameCasaEngine.Bootstrap;

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
        state.ShowFps = rootElement["ShowFps"]?.Value<bool>() ?? state.ShowFps;
        state.SoundVolume = ClampPercentage(rootElement["SoundVolume"]?.Value<int>() ?? state.SoundVolume);
        state.MusicVolume = ClampPercentage(rootElement["MusicVolume"]?.Value<int>() ?? state.MusicVolume);
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
            ["ShowFps"] = state.ShowFps,
            ["SoundVolume"] = ClampPercentage(state.SoundVolume),
            ["MusicVolume"] = ClampPercentage(state.MusicVolume),
        };

        File.WriteAllText(fileName, rootElement.ToString());
    }

    private static int ClampPercentage(int value)
    {
        return Math.Clamp(value, 0, 100);
    }
}