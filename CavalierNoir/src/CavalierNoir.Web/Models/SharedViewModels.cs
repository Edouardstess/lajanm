using System.Text;
using CavalierNoir.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace CavalierNoir.Web.Models;

/// <summary>
/// Pagination réutilisable. Reconstruit les liens en conservant les filtres
/// présents dans la requête courante, sans dépendre d'une bibliothèque.
/// </summary>
public sealed class PaginationModel
{
    private readonly IReadOnlyDictionary<string, string?> _query;

    private PaginationModel(
        string path,
        IReadOnlyDictionary<string, string?> query,
        int pageNumber,
        int totalPages,
        int totalCount,
        int firstItemIndex,
        int lastItemIndex,
        string pageParameter)
    {
        Path = path;
        _query = query;
        PageNumber = pageNumber;
        TotalPages = totalPages;
        TotalCount = totalCount;
        FirstItemIndex = firstItemIndex;
        LastItemIndex = lastItemIndex;
        PageParameter = pageParameter;
    }

    public string Path { get; }

    public int PageNumber { get; }

    public int TotalPages { get; }

    public int TotalCount { get; }

    public int FirstItemIndex { get; }

    public int LastItemIndex { get; }

    public string PageParameter { get; }

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    /// <summary>Construit le modèle à partir d'une page de résultats et de la requête HTTP.</summary>
    public static PaginationModel From<T>(PagedList<T> page, HttpRequest request, string pageParameter = "page")
    {
        var query = request.Query
            .Where(kv => !string.Equals(kv.Key, pageParameter, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => (string?)kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        return new PaginationModel(
            request.Path.Value ?? "/",
            query,
            page.PageNumber,
            page.TotalPages,
            page.TotalCount,
            page.FirstItemIndex,
            page.LastItemIndex,
            pageParameter);
    }

    /// <summary>Fenêtre de numéros affichés autour de la page courante.</summary>
    public IEnumerable<int> PagesAffichees(int fenetre = 2)
    {
        var premier = Math.Max(1, PageNumber - fenetre);
        var dernier = Math.Min(TotalPages, PageNumber + fenetre);

        for (var i = premier; i <= dernier; i++)
        {
            yield return i;
        }
    }

    public string UrlPour(int page)
    {
        var builder = new StringBuilder(Path);
        var premier = true;

        foreach (var (key, value) in _query)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            builder.Append(premier ? '?' : '&');
            builder.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
            premier = false;
        }

        builder.Append(premier ? '?' : '&');
        builder.Append(PageParameter).Append('=').Append(page);

        return builder.ToString();
    }
}

/// <summary>Paramètres d'affichage d'un échiquier.</summary>
public sealed class EchiquierModel
{
    public required string Fen { get; init; }

    /// <summary>Identifiant HTML : nécessaire pour relier les boutons d'action.</summary>
    public string Id { get; init; } = "echiquier";

    /// <summary>L'utilisateur peut déplacer les pièces.</summary>
    public bool Interactif { get; init; }

    /// <summary>Oriente l'échiquier du côté des noirs.</summary>
    public bool Retourne { get; init; }

    /// <summary>Identifiant du champ caché recevant les coups saisis.</summary>
    public string? ChampCoups { get; init; }

    /// <summary>Identifiant de l'élément affichant les coups en clair.</summary>
    public string? AffichageCoups { get; init; }

    public string? Legende { get; init; }

    /// <summary>Construit le modèle en orientant automatiquement l'échiquier vers le camp au trait.</summary>
    public static EchiquierModel Pour(string fen, bool interactif = false, string id = "echiquier")
    {
        // Le nom « Fen » désigne ici la propriété de cette classe : on qualifie
        // complètement le type du domaine pour lever l'ambiguïté.
        var traitAuxBlancs =
            !CavalierNoir.Domain.ValueObjects.Fen.TryParse(fen, out var position, out _)
            || position!.WhiteToMove;

        return new EchiquierModel
        {
            Fen = fen,
            Id = id,
            Interactif = interactif,
            Retourne = !traitAuxBlancs,
            Legende = traitAuxBlancs ? "Les blancs jouent" : "Les noirs jouent"
        };
    }
}
