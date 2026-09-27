using System.Globalization;
using System.Text.RegularExpressions;

namespace LegacyModelGltfConverter;

/// <summary>
/// Effect instance declared by a material of a legacy DirectX .x file.
/// </summary>
internal sealed class LegacyEffectInstance
{
    public string MaterialName { get; init; } = string.Empty;

    public string EffectFilePath { get; set; } = string.Empty;

    public Dictionary<string, int> Dwords { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, float[]> Floats { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Strings { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Text parser for the EffectInstance blocks of legacy .x files.
/// Ported verbatim from the CasaEngine StaticModelImporter removed in 726b2f5a1
/// (CasaEngine 295db0c6, CasaEngine/Framework/Assets/Loaders/StaticModelImporter.cs:206-345, 462-477, 786-840)
/// so the converted models carry exactly the metadata the old runtime importer read,
/// including its matching rules (material name, last effect instance with a .fx file).
/// </summary>
internal static class LegacyXFileParser
{
    public static IReadOnlyDictionary<string, LegacyEffectInstance> ParseLegacyEffectInstances(string filePath)
    {
        var result = new Dictionary<string, LegacyEffectInstance>(StringComparer.Ordinal);
        if (!Path.GetExtension(filePath).Equals(".x", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(filePath))
        {
            return result;
        }

        string text = File.ReadAllText(filePath);
        int searchIndex = 0;

        while (true)
        {
            int materialIndex = IndexOfToken(text, "Material", searchIndex);
            if (materialIndex < 0)
            {
                break;
            }

            int nameIndex = materialIndex + "Material".Length;
            SkipWhitespace(text, ref nameIndex);

            string materialName = ReadIdentifier(text, ref nameIndex);
            if (string.IsNullOrWhiteSpace(materialName))
            {
                searchIndex = materialIndex + "Material".Length;
                continue;
            }

            int braceOpenIndex = text.IndexOf('{', nameIndex);
            if (braceOpenIndex < 0)
            {
                break;
            }

            string materialBody = ExtractBraceBlock(text, braceOpenIndex, out int braceCloseIndex);
            LegacyEffectInstance? effectInstance = ParseLegacyEffectInstance(materialName, materialBody);
            if (effectInstance != null)
            {
                result[materialName] = effectInstance;
            }

            searchIndex = braceCloseIndex + 1;
        }

        return result;
    }

    private static LegacyEffectInstance? ParseLegacyEffectInstance(string materialName, string materialBody)
    {
        int searchIndex = 0;
        LegacyEffectInstance? lastInstance = null;

        while (true)
        {
            int effectIndex = IndexOfToken(materialBody, "EffectInstance", searchIndex);
            if (effectIndex < 0)
            {
                break;
            }

            int braceOpenIndex = materialBody.IndexOf('{', effectIndex);
            if (braceOpenIndex < 0)
            {
                break;
            }

            string effectBody = ExtractBraceBlock(materialBody, braceOpenIndex, out int braceCloseIndex);
            LegacyEffectInstance effectInstance = ParseSingleLegacyEffectInstance(materialName, effectBody);
            if (!string.IsNullOrWhiteSpace(effectInstance.EffectFilePath))
            {
                lastInstance = effectInstance;
            }

            searchIndex = braceCloseIndex + 1;
        }

        return lastInstance;
    }

    private static LegacyEffectInstance ParseSingleLegacyEffectInstance(string materialName, string effectBody)
    {
        var effectInstance = new LegacyEffectInstance
        {
            MaterialName = materialName,
        };

        Match fileMatch = Regex.Match(
            effectBody,
            "EffectFilename\\s*\\{\\s*\"(?<path>[^\"]+\\.fx)\"\\s*;\\s*\\}",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!fileMatch.Success)
        {
            fileMatch = Regex.Match(
                effectBody,
                "\"(?<path>[^\"]+\\.fx)\"\\s*;",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        if (fileMatch.Success)
        {
            effectInstance.EffectFilePath = fileMatch.Groups["path"].Value;
        }

        foreach (Match match in Regex.Matches(
                     effectBody,
                     "EffectParamDWord\\s*\\{\\s*\"(?<name>[^\"]+)\"\\s*;\\s*(?<value>\\d+)\\s*;\\s*\\}",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            if (int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                effectInstance.Dwords[match.Groups["name"].Value] = value;
            }
        }

        foreach (Match match in Regex.Matches(
                     effectBody,
                     "EffectParamString\\s*\\{\\s*\"(?<name>[^\"]+)\"\\s*;\\s*\"(?<value>[^\"]*)\"\\s*;\\s*\\}",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            effectInstance.Strings[match.Groups["name"].Value] = match.Groups["value"].Value;
        }

        foreach (Match match in Regex.Matches(
                     effectBody,
                     "EffectParamFloats\\s*\\{\\s*\"(?<name>[^\"]+)\"\\s*;\\s*(?<count>\\d+)\\s*;\\s*(?<values>[^;]+?)\\s*;\\s*\\}",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            float[] values = ParseFloatList(match.Groups["values"].Value);
            if (values.Length > 0)
            {
                effectInstance.Floats[match.Groups["name"].Value] = values;
            }
        }

        return effectInstance;
    }

    private static float[] ParseFloatList(string valueList)
    {
        string normalized = valueList.Replace(";", string.Empty);
        string[] parts = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new List<float>(parts.Length);

        for (int i = 0; i < parts.Length; i++)
        {
            if (float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                values.Add(value);
            }
        }

        return values.ToArray();
    }

    private static int IndexOfToken(string text, string token, int startIndex)
        => text.IndexOf(token, startIndex, StringComparison.OrdinalIgnoreCase);

    private static void SkipWhitespace(string text, ref int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }
    }

    private static string ReadIdentifier(string text, ref int index)
    {
        SkipWhitespace(text, ref index);

        int start = index;
        while (index < text.Length)
        {
            char character = text[index];
            if (char.IsLetterOrDigit(character) || character == '_' || character == '-')
            {
                index++;
                continue;
            }

            break;
        }

        return text.Substring(start, index - start).Trim();
    }

    private static string ExtractBraceBlock(string text, int braceOpenIndex, out int braceCloseIndex)
    {
        int depth = 0;

        for (int i = braceOpenIndex; i < text.Length; i++)
        {
            char character = text[i];
            if (character == '{')
            {
                depth++;
            }
            else if (character == '}')
            {
                depth--;
                if (depth == 0)
                {
                    braceCloseIndex = i;
                    return text.Substring(braceOpenIndex + 1, i - braceOpenIndex - 1);
                }
            }
        }

        throw new InvalidOperationException("Unmatched braces while parsing .x file.");
    }
}
