using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Materials.Runtime;
using RacingGameCasaEngine.Components;

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

        if (buildingMaterial.UsesReflection)
        {
            failures.Add("Building.gltf facade should stay non-reflective.");
        }

        ExpectFamily(failures, "Building.gltf facade", buildingMaterial, RacingGamePbrMaterialMapping.Masonry);
    }

    private static void VerifySign(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Sign.gltf");
        var neutralResult = LegacyGltfModelReader.ReadWithMetadata(filePath, null);
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial neutralMaterial = FindMaterialByDiffuseTexture(neutralResult.Materials, "Schild.tga");
        StaticModelImportedMaterial profileMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Schild.tga");

        if (neutralMaterial.BrightAmbientHint)
        {
            failures.Add("Sign.gltf should stay neutral without the RacingGame profile.");
        }

        if (profileMaterial.BrightAmbientHint || profileMaterial.UsesReflection || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.OpaqueLit)
        {
            failures.Add("Sign.gltf should keep its own ambient and not reflect the scene with the RacingGame profile.");
        }

        ExpectFamily(failures, "Sign.gltf", profileMaterial, RacingGamePbrMaterialMapping.Signage);
    }

    private static void VerifyStartLight(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "StartLight.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial signalPole = FindMaterialByDiffuseTexture(profileResult.Materials, "TLight.tga");
        StaticModelImportedMaterial signalLens = FindMaterialByDiffuseTexture(profileResult.Materials, "Light.tga");

        if (signalPole.UsesReflection || signalLens.UsesReflection)
        {
            failures.Add("StartLight.gltf should not reflect the scene.");
        }

        ExpectFamily(failures, "StartLight.gltf pole", signalPole, RacingGamePbrMaterialMapping.LampPost);
        ExpectFamily(failures, "StartLight.gltf light lenses", signalLens, RacingGamePbrMaterialMapping.LampPost);
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

        ExpectFamily(failures, "AlphaPalm.gltf leaves", profileMaterial, RacingGamePbrMaterialMapping.Vegetation);

        // The cut-out is what shapes the shadow of the palms (leaf-shaped shadows): the runtime material keeps it.
        LegacyImportedMaterialPresentation presentation = LegacyImportedMaterialPresentationResolver.Resolve(profileMaterial);
        if (presentation.Queue != RenderQueue.AlphaTest)
        {
            failures.Add("AlphaPalm.gltf leaves should be drawn in the alpha-test queue so that the shadow follows the leaves.");
        }
    }

    private static void VerifyBanner(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Banner.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial bannerMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "banner.tga");

        if (profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Banner.gltf should keep the ambient of its own materials with the RacingGame profile (the original never applies its 128 override).");
        }

        if (bannerMaterial.UsesReflection)
        {
            failures.Add("Banner.gltf should stay non-reflective.");
        }

        ExpectFamily(failures, "Banner.gltf", bannerMaterial, RacingGamePbrMaterialMapping.Signage);
    }

    private static void VerifyHotelGlass(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Hotel02.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial glassMaterial = FindMaterialByDisplayName(profileResult.Materials, "fenster");

        if (!glassMaterial.UsesReflection)
        {
            failures.Add("Hotel02.gltf glass should keep the reflection hint of its legacy glass effect.");
        }

        // The glass of the buildings is an opaque smooth metal, a mirror of the sky (D5).
        RacingGamePbrFamily glassFamily = ExpectFamily(failures, "Hotel02.gltf glass", glassMaterial, RacingGamePbrMaterialMapping.BuildingGlass);
        if (glassFamily.Metallic < 1f || glassFamily.Roughness > 0.1f)
        {
            failures.Add("Hotel02.gltf glass should be a smooth metal (metallic 1, roughness at most 0.1).");
        }

        if (LegacyImportedMaterialPresentationResolver.Resolve(glassMaterial).Queue != RenderQueue.Opaque)
        {
            failures.Add("Hotel02.gltf glass should stay opaque.");
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

        if (!glassMaterial.UsesReflection)
        {
            failures.Add("Car.gltf glass should keep the reflection hint of its legacy glass effect.");
        }

        if (tireMaterial.UsesReflection)
        {
            failures.Add("Car.gltf tires should stay non-reflective.");
        }

        // The car's families are chosen by material name (LegacyCarVisualFactory.ResolveFamily).
        if (LegacyCarVisualFactory.ResolveFamily(glassMaterial) != RacingGamePbrMaterialMapping.CarGlass)
        {
            failures.Add("Car.gltf glass should map to the car glass family (opaque smooth metal).");
        }

        RacingGamePbrFamily paintFamily = LegacyCarVisualFactory.ResolveFamily(paintMaterial);
        if (paintFamily != RacingGamePbrMaterialMapping.CarPaint || paintFamily.Metallic != 0f || paintFamily.ClearCoat < 1f)
        {
            failures.Add("Car.gltf paint should be a dielectric under a clear coat.");
        }

        if (LegacyCarVisualFactory.ResolveFamily(chromeMaterial) != RacingGamePbrMaterialMapping.CarChrome)
        {
            failures.Add("Car.gltf chrome should map to the car chrome family (smooth metal).");
        }

        if (LegacyCarVisualFactory.ResolveFamily(tireMaterial) != RacingGamePbrMaterialMapping.CarTyre)
        {
            failures.Add("Car.gltf tires should map to the car tyre family (rough dielectric).");
        }
    }

    private static void VerifyWindmill(string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Windmill.gltf");
        var profileResult = LegacyGltfModelReader.ReadWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        if (profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Windmill.gltf should keep the ambient of its own materials with the RacingGame profile (the original never applies its 128 override).");
        }

        if (profileResult.Materials.Any(static material => material.UsesReflection))
        {
            failures.Add("Windmill.gltf should not reflect the scene with the RacingGame profile.");
        }

        StaticModelImportedMaterial windmillMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Windmill.tga");
        ExpectFamily(failures, "Windmill.gltf", windmillMaterial, RacingGamePbrMaterialMapping.MatteIndustrial);
    }

    private static RacingGamePbrFamily ExpectFamily(
        List<string> failures,
        string description,
        StaticModelImportedMaterial importedMaterial,
        RacingGamePbrFamily expected)
    {
        RacingGamePbrFamily actual = RacingGamePbrMaterialMapping.ResolveImportedFamily(importedMaterial);
        if (actual != expected)
        {
            failures.Add($"{description} should map to the PBR family {expected} but maps to {actual}.");
        }

        return actual;
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