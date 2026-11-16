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

        string effectFileName = Path.GetFileName(context.ImportedMaterial.EffectFilePath ?? string.Empty);
        if (effectFileName.Equals("ReflectionSimpleGlass.fx", StringComparison.OrdinalIgnoreCase))
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
}
