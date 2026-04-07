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

            VerifySign(importer, projectContentPath, failures);
            VerifyAlphaPalm(importer, projectContentPath, failures);
            VerifyBanner(importer, projectContentPath, failures);
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

    private static void VerifySign(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Sign.X");
        var neutralResult = importer.ImportWithMetadata(filePath);
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        StaticModelImportedMaterial neutralMaterial = FindMaterialByDiffuseTexture(neutralResult.Materials, "Schild.tga");
        StaticModelImportedMaterial profileMaterial = FindMaterialByDiffuseTexture(profileResult.Materials, "Schild.tga");

        if (neutralMaterial.BrightAmbientHint)
        {
            failures.Add("Sign.X should stay neutral without the RacingGame profile.");
        }

        if (!profileMaterial.BrightAmbientHint || !profileMaterial.UsesReflection || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.ReflectiveLit)
        {
            failures.Add("Sign.X should become bright-ambient reflective with the RacingGame profile.");
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

        if (!profileMaterial.AlphaCutoutHint || profileMaterial.SurfaceIntent != LegacyMaterialSurfaceIntent.AlphaCutoutLit)
        {
            failures.Add("AlphaPalm.X should become alpha-cutout with the RacingGame profile.");
        }
    }

    private static void VerifyBanner(StaticModelImporter importer, string projectContentPath, List<string> failures)
    {
        string filePath = Path.Combine(projectContentPath, "Models", "Banner.X");
        var profileResult = importer.ImportWithMetadata(filePath, RacingGameImportProfiles.LegacyMaterialProfile);

        if (!profileResult.Materials.Any(static material => material.BrightAmbientHint))
        {
            failures.Add("Banner.X should produce at least one bright-ambient material with the RacingGame profile.");
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
}