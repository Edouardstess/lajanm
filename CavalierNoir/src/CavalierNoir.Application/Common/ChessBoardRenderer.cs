using System.Text;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Application.Common;

/// <summary>
/// Rend une position FEN en HTML autonome (table + styles en ligne). Utilisé
/// dans les courriels, où ni feuille de style externe ni JavaScript ne sont
/// interprétés par la plupart des clients de messagerie.
/// </summary>
public static class ChessBoardRenderer
{
    private const string LightSquare = "#f0d9b5";
    private const string DarkSquare = "#b58863";

    private static readonly Dictionary<char, string> Glyphs = new()
    {
        ['K'] = "♔", ['Q'] = "♕", ['R'] = "♖",
        ['B'] = "♗", ['N'] = "♘", ['P'] = "♙",
        ['k'] = "♚", ['q'] = "♛", ['r'] = "♜",
        ['b'] = "♝", ['n'] = "♞", ['p'] = "♟"
    };

    /// <summary>Développe le placement FEN en une grille 8×8, rangée 8 en premier.</summary>
    public static char[,] ToGrid(string fen)
    {
        var grid = new char[8, 8];
        var placement = fen.Split(' ')[0];
        var ranks = placement.Split('/');

        for (var rank = 0; rank < 8 && rank < ranks.Length; rank++)
        {
            var file = 0;
            foreach (var c in ranks[rank])
            {
                if (file >= 8)
                {
                    break;
                }

                if (char.IsDigit(c))
                {
                    var empty = c - '0';
                    for (var i = 0; i < empty && file < 8; i++)
                    {
                        grid[rank, file++] = ' ';
                    }
                }
                else
                {
                    grid[rank, file++] = c;
                }
            }

            while (file < 8)
            {
                grid[rank, file++] = ' ';
            }
        }

        return grid;
    }

    /// <summary>Table HTML de 8×8 cases, avec coordonnées, prête à être insérée dans un courriel.</summary>
    public static string ToHtml(string fen, int squareSizePx = 44, bool flipped = false)
    {
        if (!Fen.IsValid(fen))
        {
            return "<p>Position indisponible.</p>";
        }

        var grid = ToGrid(fen);
        var builder = new StringBuilder();

        builder.Append(
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" " +
            "style=\"border-collapse:collapse;border:2px solid #1a1a1a;\">");

        for (var row = 0; row < 8; row++)
        {
            var rank = flipped ? 7 - row : row;
            builder.Append("<tr>");

            for (var col = 0; col < 8; col++)
            {
                var file = flipped ? 7 - col : col;
                var piece = grid[rank, file];
                var isLight = (rank + file) % 2 == 0;
                var background = isLight ? LightSquare : DarkSquare;
                var glyph = piece != ' ' && Glyphs.TryGetValue(piece, out var g) ? g : "&nbsp;";

                builder.Append(
                    $"<td style=\"width:{squareSizePx}px;height:{squareSizePx}px;" +
                    $"background:{background};text-align:center;vertical-align:middle;" +
                    $"font-size:{squareSizePx - 10}px;line-height:{squareSizePx}px;color:#111;\">{glyph}</td>");
            }

            builder.Append("</tr>");
        }

        builder.Append("</table>");
        return builder.ToString();
    }

    /// <summary>Rendu texte brut, pour la version non HTML des courriels.</summary>
    public static string ToText(string fen)
    {
        if (!Fen.IsValid(fen))
        {
            return "Position indisponible.";
        }

        var grid = ToGrid(fen);
        var builder = new StringBuilder();

        for (var rank = 0; rank < 8; rank++)
        {
            builder.Append(8 - rank).Append(" | ");
            for (var file = 0; file < 8; file++)
            {
                var piece = grid[rank, file];
                builder.Append(piece == ' ' ? '.' : piece).Append(' ');
            }

            builder.AppendLine();
        }

        builder.AppendLine("  +----------------");
        builder.AppendLine("    a b c d e f g h");
        return builder.ToString();
    }
}
