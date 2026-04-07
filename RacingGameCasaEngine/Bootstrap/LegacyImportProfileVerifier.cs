using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets.Loaders;

namespace RacingGameCasaEngine.Bootstrap;

internal static class LegacyImportProfileVerifier
{
    public static int Run(string projectContentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectContentPath);

        try
        {
            var importer = new StaticModelImporter();
            var failures = new List<string>();

            VerifyBuilding(importer, projectContentPath, failures);
            VerifySign(importer, projectContentPath, failures);
            VerifyStartLight(importer, projectContentPath, failures);
            VerifyAlphaPalm(importer, projectContentPath, failures);
            VerifyBanner(importer, projectContentPath, failures);
            VerifyHotelGlass(importer, projectContentPath, failures);
            VerifyCar(importer, projectContentPath, failures);
            VerifyWindmill(importer, projectContentPath, failures);

            if (failures.Count == 0)
            {
                Logs.WriteInfo("[LegacyImportProfileVerifier] Verification passed.");
                Console.WriteLine("Legacy import profile verification passed.");
                return 0;
            }

            foreach (string failure in failures)
            {
                Logs.WriteError($"[LegacyImportProfileVerifier] {failure}");
                Console.Error.WriteLine(failure);
            }

            return 1;
        }
        catch (Exception ex)
        {
            Logs.WriteException(ex);
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void VerifyBuilding(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Building.X");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial buildingMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Building.tga");
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Building", buildingMaterial);

        if (buildingMaterial.UsesReflection || tuning.EnableReflection)
        {
            failures.Add("Building.X facade should stay non-reflective after material retuning.");
        }

        if (tuning.ApplySpecularColor(buildingMaterial.SpecularColor).X > 0.15f)
        {
            failures.Add("Building.X facade should have a muted specular response after material retuning.");
        }
    }

    private static void VerifySign(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Sign.X");
        var neutralResult = importer.ImportWithMetadata(filePath);
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial neutralMaterial = FindMaterialByDiffuseTexture(neutralResult.Materials, "Schild.tga");
        StaticModelImportedMaterial profileMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Schild.tga");
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Sign", profileMaterial);

        if (neutralMaterial.BrightAmbientHint)
        {
            failures.Add("Sign.X should stay neutral without the RacingGame profile.");
        }

        if (!profileMaterial.BrightAmbientHint || profileMaterial.UsesReflection || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.OpaqueLit)
        {
            failures.Add("Sign.X should become bright-ambient without reflecting the scene with the RacingGame profile.");
        }

        if (tuning.EnableReflection || tuning.ApplySpecularColor(profileMaterial.SpecularColor).X > 0.2f)
        {
            failures.Add("Sign.X should keep a toned-down matte response after material retuning.");
        }
    }

    private static void VerifyStartLight(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "StartLight.X");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial signalPole = FindMaterialByDiffuseTexture(profileResult.Materials, "TLight.tga");
        StaticModelImportedMaterial signalLens = FindMaterialByDiffuseTexture(profileResult.Materials, "Light.tga");
        RacingGameLegacyMaterialRuntimeTuning poleTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("StartLight", signalPole);
        RacingGameLegacyMaterialRuntimeTuning lensTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("StartLight", signalLens);

        if (signalPole.UsesReflection || signalLens.UsesReflection || poleTuning.EnableReflection || lensTuning.EnableReflection)
        {
            failures.Add("StartLight.X should not reflect the scene after material retuning.");
        }

        if (lensTuning.ApplySpecularColor(signalLens.SpecularColor).X > 0.2f)
        {
            failures.Add("StartLight.X light lenses should have a reduced specular response after material retuning.");
        }
    }

    private static void VerifyAlphaPalm(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "AlphaPalm.X");
        var neutralResult = importer.ImportWithMetadata(filePath);
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial neutralMaterial = FindMaterialByDiffuseTexture(neutralResult.Materials, "PalmLeave.tga");
        StaticModelImportedMaterial profileMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "PalmLeave.tga");

        if (neutralMaterial.AlphaCutoutHint)
        {
            failures.Add("AlphaPalm.X should stay opaque without the RacingGame profile.");
        }

        if (!profileMaterial.AlphaCutoutHint
            || profileMaterial.UsesReflection
            || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.AlphaCutoutLit)
        {
            failures.Add("AlphaPalm.X should enable alpha-cutout without reintroducing scene reflection with the RacingGame profile.");
        }
    }

    private static void VerifyBanner(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Banner.X");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial bannerMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "banner.tga");

        if (!profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Banner.X should produce at least one bright-ambient material with the RacingGame profile.");
        }

        if (bannerMaterial.UsesReflection)
        {
            failures.Add("Banner.X should stay non-reflective after material retuning.");
        }
    }

    private static void VerifyHotelGlass(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Hotel02.X");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial glassMaterial = FindMaterialByDisplayName(profileResult.Materials, "fenster");
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Hotel02", glassMaterial);

        if (!glassMaterial.UsesReflection || !tuning.EnableReflection)
        {
            failures.Add("Hotel02.X glass should remain reflective after material retuning.");
        }
    }

    private static void VerifyCar(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Car.x");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial chromeMaterial = FindMaterialByDisplayName(profileResult.Materials, "chrome");
        StaticModelImportedMaterial paintMaterial = FindMaterialByDisplayName(profileResult.Materials, "lack");
        StaticModelImportedMaterial tireMaterial = FindMaterialByDisplayName(profileResult.Materials, "gummi");
        RacingGameLegacyMaterialRuntimeTuning chromeTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Car", chromeMaterial);
        RacingGameLegacyMaterialRuntimeTuning tireTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Car", tireMaterial);

        if (!chromeMaterial.UsesReflection || !paintMaterial.UsesReflection || !chromeTuning.EnableReflection)
        {
            failures.Add("Car.x chrome and paint should remain reflective after material retuning.");
        }

        if (tireMaterial.UsesReflection || tireTuning.EnableReflection)
        {
            failures.Add("Car.x tires should stay non-reflective after material retuning.");
        }
    }

    private static void VerifyWindmill(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Windmill.X");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        if (!profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Windmill.X should produce at least one bright-ambient material with the RacingGame profile.");
        }

        if (profileResult.Materials.Any(static material => material.UsesReflection))
        {
            failures.Add("Windmill.X should keep the bright-ambient exception without reflecting the scene.");
        }
    }

    private static StaticModelImportedMaterial FindMaterialByDiffuseTexture(
        IReadOnlyList<StaticModelImportedMaterial> materials,
        string textureFileName)
    {
        StaticModelImportedMaterial? material = materials.FirstOrDefault(
            candidate => string.Equals(Path.GetFileName(candidate.DiffuseTextureFilePath), textureFileName, StringComparison.OrdinalIgnoreCase));

        return material
            ?? throw new InvalidOperationException($"Unable to find material using diffuse texture '{textureFileName}'.");
    }

    private static StaticModelImportedMaterial FindMaterialByDisplayName(
        IReadOnlyList<StaticModelImportedMaterial> materials,
        string displayName)
    {
        StaticModelImportedMaterial? material = materials.FirstOrDefault(
            candidate => string.Equals(candidate.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

        return material
            ?? throw new InvalidOperationException($"Unable to find material named '{displayName}'.");
    }
}