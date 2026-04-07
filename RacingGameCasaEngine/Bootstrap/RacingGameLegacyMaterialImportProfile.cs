using CasaEngine.Framework.Assets.Loaders;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RacingGameLegacyMaterialImportProfile : ILegacyMaterialImportProfile
{
    public LegacyMaterialImportInterpretation Interpret(in LegacyMaterialImportContext context)
    {
        var interpretation = NeutralLegacyMaterialImportProfile.Instance.Interpret(context);
        LegacyMaterialImportHint hints = interpretation.Hints;

        if (UsesLegacyAlphaCutout(context.SourceAssetName, context.ImportedMaterial.DiffuseTextureFilePath))
        {
            hints |= LegacyMaterialImportHint.AlphaCutout;
        }

        if (UsesLegacyBrightAmbient(context.SourceAssetName))
        {
            hints |= LegacyMaterialImportHint.BrightAmbient;
        }

        if (RacingGameLegacyMaterialTuning.ShouldEnableReflection(context.SourceAssetName, context.ImportedMaterial))
        {
            hints |= LegacyMaterialImportHint.Reflection;
        }

        LegacyMaterialSurfaceIntent surfaceIntent = (hints & LegacyMaterialImportHint.Reflection) != 0
            ? LegacyMaterialSurfaceIntent.ReflectiveLit
            : (hints & LegacyMaterialImportHint.AlphaCutout) != 0
                ? LegacyMaterialSurfaceIntent.AlphaCutoutLit
                : LegacyMaterialSurfaceIntent.OpaqueLit;

        return new LegacyMaterialImportInterpretation(surfaceIntent, hints);
    }

    private static bool UsesLegacyAlphaCutout(string sourceAssetName, string? diffuseTextureFilePath)
    {
        if (sourceAssetName.StartsWith("Alpha", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(diffuseTextureFilePath))
        {
            return false;
        }

        string textureName = Path.GetFileNameWithoutExtension(diffuseTextureFilePath);
        return textureName.Contains("Palm", StringComparison.OrdinalIgnoreCase)
            || textureName.Contains("Leave", StringComparison.OrdinalIgnoreCase)
            || textureName.Contains("Ast", StringComparison.OrdinalIgnoreCase)
            || textureName.Contains("plants", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesLegacyBrightAmbient(string sourceAssetName)
        => sourceAssetName.StartsWith("Sign", StringComparison.OrdinalIgnoreCase)
            || sourceAssetName.StartsWith("Banner", StringComparison.OrdinalIgnoreCase)
            || sourceAssetName.StartsWith("Windmill", StringComparison.OrdinalIgnoreCase);
}