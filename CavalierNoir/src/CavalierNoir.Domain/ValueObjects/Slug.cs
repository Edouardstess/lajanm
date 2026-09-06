using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CavalierNoir.Domain.ValueObjects;

/// <summary>
/// Fabrique de segments d'URL lisibles et stables (SEO) : « Tournoi d'été 2026 »
/// devient « tournoi-d-ete-2026 ».
/// </summary>
public static partial class Slug
{
    public const int MaxLength = 120;

    public static string From(string? input, string fallback = "element")
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return fallback;
        }

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c);
        }

        var ascii = builder.ToString().Normalize(NormalizationForm.FormC);
        ascii = InvalidCharPattern().Replace(ascii, "-");
        ascii = MultipleDashPattern().Replace(ascii, "-").Trim('-');

        if (ascii.Length == 0)
        {
            return fallback;
        }

        return ascii.Length <= MaxLength ? ascii : ascii[..MaxLength].TrimEnd('-');
    }

    /// <summary>Ajoute un suffixe numérique pour lever une collision d'unicité.</summary>
    public static string WithSuffix(string slug, int suffix)
    {
        var candidate = $"{slug}-{suffix}";
        return candidate.Length <= MaxLength ? candidate : $"{slug[..(MaxLength - 1 - suffix.ToString().Length)]}-{suffix}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidCharPattern();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashPattern();
}
