using CasaEngine.Framework.Assets.Loaders;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RacingGameLegacyMaterialImportProfile : ILegacyMaterialImportProfile
{
    public LegacyMaterialImportInterpretation Interpret(in LegacyMaterialImportContext context)
    {
        var interpretation = NeutralLegacyMaterialImportProfile.Instance.Interpret(context);
        LegacyMaterialImportHint hints = interpretation.Hints;

        if (UsesLegacyBrightAmbient(context.SourceAssetName))
        {
            hints |= LegacyMaterialImportHint.BrightAmbient;
        }

        if (UsesReflectionTechnique(context.ImportedMaterial.EffectFilePath, context.ImportedMaterial.LegacyTechniqueIndex))
        {
            hints |= LegacyMaterialImportHint.Reflection;
        }

        LegacyMaterialSurfaceIntent surfaceIntent = (hints & LegacyMaterialImportHint.Reflection) != 0
            ? LegacyMaterialSurfaceIntent.ReflectiveLit
            : interpretation.SurfaceIntent;

        return new LegacyMaterialImportInterpretation(surfaceIntent, hints);
    }

    private static bool UsesLegacyBrightAmbient(string sourceAssetName)
        => sourceAssetName.StartsWith("Sign", StringComparison.OrdinalIgnoreCase)
            || sourceAssetName.StartsWith("Banner", StringComparison.OrdinalIgnoreCase)
            || sourceAssetName.StartsWith("Windmill", StringComparison.OrdinalIgnoreCase);

    private static bool UsesReflectionTechnique(string? effectFilePath, int techniqueIndex)
    {
        string effectFileName = Path.GetFileName(effectFilePath ?? string.Empty);
        if (effectFileName.Equals("ReflectionSimpleGlass.fx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!effectFileName.Equals("NormalMapping.fx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return techniqueIndex is 7 or 8 or 9 or 10 or 11;
    }
}