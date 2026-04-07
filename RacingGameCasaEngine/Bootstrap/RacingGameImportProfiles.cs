using CasaEngine.Framework.Assets.Loaders;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// Central bootstrap point for game-side import profiles.
/// Runtime legacy model loading resolves profiles from here so the project can
/// replace the implementation without touching CasaEngine itself.
/// </summary>
internal static class RacingGameImportProfiles
{
    public static ILegacyMaterialImportProfile LegacyMaterialProfile { get; } = new RacingGameLegacyMaterialImportProfile();
}