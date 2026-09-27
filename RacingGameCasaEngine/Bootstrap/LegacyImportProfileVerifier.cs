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
            var failures = new List<string>();

            VerifyBuilding(projectContentPath, failures);
            VerifySign(projectContentPath, failures);
            VerifyStartLight(projectContentPath, failures);
            VerifyAlphaPalm(projectContentPath, failures);
            VerifyBanner(projectContentPath, failures);
            VerifyHotelGlass(projectContentPath, failures);
            VerifyCar(projectContentPath, failures);
            VerifyWindmill(projectContentPath, failures);

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

    private static void VerifyBuilding(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Building.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial buildingMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Building.tga");
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Building", buildingMaterial);

        if (buildingMaterial.UsesReflection || tuning.EnableReflection)
        {
            failures.Add("Building.gltf facade should stay non-reflective after material retuning.");
        }

        if (tuning.ApplySpecularColor(buildingMaterial.SpecularColor).X > 0.15f)
        {
            failures.Add("Building.gltf facade should have a muted specular response after material retuning.");
        }
    }

    private static void VerifySign(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Sign.gltf");
        var neutralResult = LegacyGltfModelReader.ReadWithMetadata(filePath, null);
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial neutralMaterial = FindMaterialByDiffuseTexture(neutralResult.Materials, "Schild.tga");
        StaticModelImportedMaterial profileMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Schild.tga");
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Sign", profileMaterial);

        if (neutralMaterial.BrightAmbientHint)
        {
            failures.Add("Sign.gltf should stay neutral without the RacingGame profile.");
        }

        if (!profileMaterial.BrightAmbientHint || profileMaterial.UsesReflection || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.OpaqueLit)
        {
            failures.Add("Sign.gltf should become bright-ambient without reflecting the scene with the RacingGame profile.");
        }

        if (tuning.EnableReflection || tuning.ApplySpecularColor(profileMaterial.SpecularColor).X > 0.2f)
        {
            failures.Add("Sign.gltf should keep a toned-down matte response after material retuning.");
        }
    }

    private static void VerifyStartLight(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "StartLight.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial signalPole = FindMaterialByDiffuseTexture(profileResult.Materials, "TLight.tga");
        StaticModelImportedMaterial signalLens = FindMaterialByDiffuseTexture(profileResult.Materials, "Light.tga");
        RacingGameLegacyMaterialRuntimeTuning poleTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("StartLight", signalPole);
        RacingGameLegacyMaterialRuntimeTuning lensTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("StartLight", signalLens);

        if (signalPole.UsesReflection || signalLens.UsesReflection || poleTuning.EnableReflection || lensTuning.EnableReflection)
        {
            failures.Add("StartLight.gltf should not reflect the scene after material retuning.");
        }

        if (lensTuning.ApplySpecularColor(signalLens.SpecularColor).X > 0.2f)
        {
            failures.Add("StartLight.gltf light lenses should have a reduced specular response after material retuning.");
        }
    }

    private static void VerifyAlphaPalm(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "AlphaPalm.gltf");
        var neutralResult = LegacyGltfModelReader.ReadWithMetadata(filePath, null);
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial neutralMaterial = FindMaterialByDiffuseTexture(neutralResult.Materials, "PalmLeave.tga");
        StaticModelImportedMaterial profileMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "PalmLeave.tga");

        if (neutralMaterial.AlphaCutoutHint)
        {
            failures.Add("AlphaPalm.gltf should stay opaque without the RacingGame profile.");
        }

        if (!profileMaterial.AlphaCutoutHint
            || profileMaterial.UsesReflection
            || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.AlphaCutoutLit)
        {
            failures.Add("AlphaPalm.gltf should enable alpha-cutout without reintroducing scene reflection with the RacingGame profile.");
        }
    }

    private static void VerifyBanner(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Banner.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial bannerMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "banner.tga");

        if (!profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Banner.gltf should produce at least one bright-ambient material with the RacingGame profile.");
        }

        if (bannerMaterial.UsesReflection)
        {
            failures.Add("Banner.gltf should stay non-reflective after material retuning.");
        }
    }

    private static void VerifyHotelGlass(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Hotel02.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial glassMaterial = FindMaterialByDisplayName(profileResult.Materials, "fenster");
        RacingGameLegacyMaterialRuntimeTuning tuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Hotel02", glassMaterial);

        if (!glassMaterial.UsesReflection || !tuning.EnableReflection)
        {
            failures.Add("Hotel02.gltf glass should remain reflective after material retuning.");
        }
    }

    private static void VerifyCar(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Car.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial glassMaterial = FindMaterialByEffectFile(profileResult.Materials, "ReflectionSimpleGlass.fx");
        StaticModelImportedMaterial chromeMaterial = FindMaterialByDisplayName(profileResult.Materials, "chrome");
        StaticModelImportedMaterial paintMaterial = FindMaterialByDisplayName(profileResult.Materials, "lack");
        StaticModelImportedMaterial tireMaterial = FindMaterialByDisplayName(profileResult.Materials, "gummi");
        RacingGameLegacyMaterialRuntimeTuning glassTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Car", glassMaterial);
        RacingGameLegacyMaterialRuntimeTuning paintTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Car", paintMaterial);
        RacingGameLegacyMaterialRuntimeTuning chromeTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Car", chromeMaterial);
        RacingGameLegacyMaterialRuntimeTuning tireTuning = RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning("Car", tireMaterial);

        if (!glassMaterial.UsesReflection || !glassTuning.EnableReflection)
        {
            failures.Add("Car.gltf glass should remain reflective after material retuning.");
        }

        if (paintTuning.EnableReflection || chromeTuning.EnableReflection)
        {
            failures.Add("Car.gltf paint and chrome should stay non-reflective at runtime after material retuning.");
        }

        if (tireMaterial.UsesReflection || tireTuning.EnableReflection)
        {
            failures.Add("Car.gltf tires should stay non-reflective after material retuning.");
        }
    }

    private static void VerifyWindmill(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Windmill.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        if (!profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Windmill.gltf should produce at least one bright-ambient material with the RacingGame profile.");
        }

        if (profileResult.Materials.Any(static material => material.UsesReflection))
        {
            failures.Add("Windmill.gltf should keep the bright-ambient exception without reflecting the scene.");
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

    private static StaticModelImportedMaterial FindMaterialByEffectFile(
        IReadOnlyList<StaticModelImportedMaterial> materials,
        string effectFileName)
    {
        StaticModelImportedMaterial? material = materials.FirstOrDefault(
            candidate => string.Equals(Path.GetFileName(candidate.EffectFilePath), effectFileName, StringComparison.OrdinalIgnoreCase));

        return material
            ?? throw new InvalidOperationException($"Unable to find material using effect '{effectFileName}'.");
    }
}