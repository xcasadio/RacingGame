using CasaEngine.Framework.Rendering;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Bootstrap;

internal static class RaceSkySystem
{
    internal const string LegacySharedSkyCubeFileName = "SkyCubeMap.dds";
    internal static readonly Color LegacySkyCubeTintColor = new(232, 232, 232);

    internal static SkySettings Settings { get; } = new()
    {
        ZenithColor = new Color(39, 78, 124),
        HorizonColor = new Color(242, 208, 159),
        GroundColor = new Color(113, 92, 68),
        SunColor = new Color(255, 246, 223),
        SunDirection = Vector3.Normalize(new Vector3(-0.42f, -0.86f, -0.29f)),
        SunSize = 0.045f,
        ReflectionCubeSize = 64,
    };

    internal static bool ShouldUseSceneReflectionCube(string? reflectionTexturePath)
    {
        return string.IsNullOrWhiteSpace(reflectionTexturePath)
            || Path.GetFileName(reflectionTexturePath)
                .Equals(LegacySharedSkyCubeFileName, StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolveLegacySharedSkyCubePath(string contentRootDirectory)
    {
        string rootDirectory = string.IsNullOrWhiteSpace(contentRootDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Content")
            : contentRootDirectory;

        if (!Path.IsPathRooted(rootDirectory))
        {
            rootDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rootDirectory));
        }

        return Path.Combine(rootDirectory, "Textures", LegacySharedSkyCubeFileName);
    }
}