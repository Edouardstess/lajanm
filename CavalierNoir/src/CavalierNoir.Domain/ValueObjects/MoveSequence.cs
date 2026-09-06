using System.Text;
using System.Text.RegularExpressions;

namespace CavalierNoir.Domain.ValueObjects;

/// <summary>
/// Suite de coups d'échecs, indépendante de la notation saisie (SAN avec ou sans
/// numérotation, ou notation par coordonnées UCI). Sert à comparer la réponse d'un
/// membre à la solution enregistrée d'un exercice.
/// </summary>
public sealed partial record MoveSequence
{
    private MoveSequence(IReadOnlyList<string> moves, string raw)
    {
        Moves = moves;
        Raw = raw;
    }

    /// <summary>Coups normalisés, un élément par demi-coup.</summary>
    public IReadOnlyList<string> Moves { get; }

    /// <summary>Chaîne d'origine telle que saisie ou stockée.</summary>
    public string Raw { get; }

    public int Count => Moves.Count;

    public bool IsEmpty => Moves.Count == 0;

    public static MoveSequence Parse(string? input)
    {
        var raw = input?.Trim() ?? string.Empty;
        if (raw.Length == 0)
        {
            return new MoveSequence(Array.Empty<string>(), string.Empty);
        }

        // Suppression des commentaires PGN, des variantes et des évaluations.
        var cleaned = CommentPattern().Replace(raw, " ");
        cleaned = VariationPattern().Replace(cleaned, " ");
        cleaned = ResultPattern().Replace(cleaned, " ");

        var tokens = cleaned.Split(
            new[] { ' ', '\t', '\r', '\n', ',', ';' },
            StringSplitOptions.RemoveEmptyEntries);

        var moves = new List<string>(tokens.Length);
        foreach (var token in tokens)
        {
            var normalized = NormalizeToken(token);
            if (normalized.Length > 0)
            {
                moves.Add(normalized);
            }
        }

        return new MoveSequence(moves, raw);
    }

    /// <summary>
    /// Compare deux suites de coups. La comparaison ignore la numérotation, les
    /// symboles d'échec/mat et les annotations ; elle reste sensible à la casse des
    /// lettres de pièces (indispensable en SAN : « B » fou, « b » colonne b).
    /// </summary>
    public bool Matches(MoveSequence expected)
    {
        if (expected.IsEmpty || IsEmpty)
        {
            return false;
        }

        if (Moves.Count != expected.Moves.Count)
        {
            return false;
        }

        for (var i = 0; i < Moves.Count; i++)
        {
            if (!string.Equals(Moves[i], expected.Moves[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Nombre de demi-coups initiaux communs aux deux suites : sert à indiquer au
    /// membre jusqu'où sa solution était correcte.
    /// </summary>
    public int CommonPrefixLength(MoveSequence expected)
    {
        var length = Math.Min(Moves.Count, expected.Moves.Count);
        var common = 0;
        while (common < length && string.Equals(Moves[common], expected.Moves[common], StringComparison.Ordinal))
        {
            common++;
        }

        return common;
    }

    /// <summary>Rend la suite sous forme numérotée « 1. e4 e5 2. Cf3 ».</summary>
    public string ToNumberedNotation()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < Moves.Count; i++)
        {
            if (i % 2 == 0)
            {
                builder.Append(i / 2 + 1).Append(". ");
            }

            builder.Append(Moves[i]);
            if (i < Moves.Count - 1)
            {
                builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    public override string ToString() => string.Join(' ', Moves);

    private static string NormalizeToken(string token)
    {
        // « 1. », « 12... » : numérotation de coup.
        if (MoveNumberPattern().IsMatch(token))
        {
            return string.Empty;
        }

        var value = MoveNumberPrefixPattern().Replace(token, string.Empty);

        // Annotations : +, #, !, ?, e.p.
        value = value.Replace("e.p.", string.Empty, StringComparison.OrdinalIgnoreCase);
        value = AnnotationPattern().Replace(value, string.Empty);
        value = value.Replace("x", string.Empty, StringComparison.Ordinal);
        value = value.Replace("-", string.Empty, StringComparison.Ordinal);

        // Le roque est ramené à une forme canonique.
        var upper = value.ToUpperInvariant();
        if (upper is "OO" or "00")
        {
            return "O-O";
        }

        if (upper is "OOO" or "000")
        {
            return "O-O-O";
        }

        return value;
    }

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex VariationPattern();

    [GeneratedRegex(@"(1-0|0-1|1/2-1/2|\*)")]
    private static partial Regex ResultPattern();

    [GeneratedRegex(@"^\d+\.*$")]
    private static partial Regex MoveNumberPattern();

    [GeneratedRegex(@"^\d+\.+")]
    private static partial Regex MoveNumberPrefixPattern();

    [GeneratedRegex(@"[+#!?]+")]
    private static partial Regex AnnotationPattern();
}
