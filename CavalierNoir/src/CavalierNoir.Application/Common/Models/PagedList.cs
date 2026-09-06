using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Common.Models;

/// <summary>
/// Page de résultats. Toute liste exposée par l'application est paginée
/// (20 éléments par défaut, 100 au maximum) — exigence de performance RNF-01.
/// </summary>
public sealed class PagedList<T>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public PagedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    public int FirstItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);

    public static PagedList<T> Empty(int pageSize = DefaultPageSize) =>
        new(Array.Empty<T>(), 0, 1, pageSize);

    /// <summary>Exécute la requête en ne ramenant que la page demandée.</summary>
    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var total = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, total, page, size);
    }
}
