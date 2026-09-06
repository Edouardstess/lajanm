using System.Text.RegularExpressions;
using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.ValueObjects;

/// <summary>
/// Position d'échiquier au format Forsyth-Edwards. Validée structurellement
/// (6 champs, 8 rangées, comptes de cases cohérents) sans moteur d'échecs.
/// </summary>
public sealed partial record Fen
{
    public const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private Fen(string value) => Value = value;

    public string Value { get; }

    public static Fen Parse(string? value)
    {
        if (!TryParse(value, out var fen, out var error))
        {
            throw new DomainException($"Position FEN invalide : {error}");
        }

        return fen!;
    }

    public static bool TryParse(string? value, out Fen? fen, out string? error)
    {
        fen = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "la chaîne est vide.";
            return false;
        }

        var normalized = value.Trim();
        var fields = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length is < 4 or > 6)
        {
            error = "une position FEN comporte de 4 à 6 champs séparés par des espaces.";
            return false;
        }

        var ranks = fields[0].Split('/');
        if (ranks.Length != 8)
        {
            error = "le placement des pièces doit décrire 8 rangées séparées par « / ».";
            return false;
        }

        foreach (var rank in ranks)
        {
            var squares = 0;
            foreach (var c in rank)
            {
                if (char.IsDigit(c))
                {
                    var empty = c - '0';
                    if (empty is < 1 or > 8)
                    {
                        error = $"nombre de cases vides invalide dans la rangée « {rank} ».";
                        return false;
                    }

                    squares += empty;
                }
                else if (PieceLetters.Contains(c))
                {
                    squares++;
                }
                else
                {
                    error = $"caractère « {c} » inattendu dans la rangée « {rank} ».";
                    return false;
                }
            }

            if (squares != 8)
            {
                error = $"la rangée « {rank} » décrit {squares} cases au lieu de 8.";
                return false;
            }
        }

        if (fields[1] is not ("w" or "b"))
        {
            error = "le trait doit valoir « w » ou « b ».";
            return false;
        }

        if (!CastlingPattern().IsMatch(fields[2]))
        {
            error = "les droits de roque sont mal formés.";
            return false;
        }

        if (!EnPassantPattern().IsMatch(fields[3]))
        {
            error = "la case de prise en passant est mal formée.";
            return false;
        }

        // Champs 5 et 6 (demi-coups et numéro de coup) sont facultatifs ; on les complète.
        var halfMove = fields.Length > 4 ? fields[4] : "0";
        var fullMove = fields.Length > 5 ? fields[5] : "1";

        if (!int.TryParse(halfMove, out var halfMoveValue) || halfMoveValue < 0)
        {
            error = "le compteur de demi-coups doit être un entier positif.";
            return false;
        }

        if (!int.TryParse(fullMove, out var fullMoveValue) || fullMoveValue < 1)
        {
            error = "le numéro de coup doit être un entier supérieur ou égal à 1.";
            return false;
        }

        if (!fields[0].Contains('K') || !fields[0].Contains('k'))
        {
            error = "chaque camp doit disposer d'un roi.";
            return false;
        }

        fen = new Fen($"{fields[0]} {fields[1]} {fields[2]} {fields[3]} {halfMoveValue} {fullMoveValue}");
        return true;
    }

    public static bool IsValid(string? value) => TryParse(value, out _, out _);

    /// <summary>Camp au trait : <c>true</c> si les blancs jouent.</summary>
    public bool WhiteToMove => Value.Split(' ')[1] == "w";

    public string PiecePlacement => Value.Split(' ')[0];

    public override string ToString() => Value;

    private const string PieceLetters = "pnbrqkPNBRQK";

    [GeneratedRegex("^(-|K?Q?k?q?)$")]
    private static partial Regex CastlingPattern();

    [GeneratedRegex("^(-|[a-h][36])$")]
    private static partial Regex EnPassantPattern();
}
