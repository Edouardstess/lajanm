using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Club;
using CavalierNoir.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Gestion documentaire : consultation filtrée par visibilité, versionnage et
/// comptage des téléchargements.
/// </summary>
public sealed class DocumentService(IApplicationDbContext context, IDateTimeProvider clock)
{
    /// <summary>
    /// Documents visibles pour le niveau d'accès demandé. Un visiteur ne voit que
    /// les documents publics, un membre y ajoute les documents internes, le bureau
    /// voit l'ensemble hors documents privés.
    /// </summary>
    public async Task<PagedList<Document>> SearchAsync(
        Visibility maxVisibility,
        DocumentCategoryCode? category = null,
        string? search = null,
        bool includeArchived = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = context.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted && d.Visibility <= maxVisibility);

        if (!includeArchived)
        {
            query = query.Where(d => !d.IsArchived);
        }

        if (category.HasValue)
        {
            query = query.Where(d => d.Category == category.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Title.Contains(term)
                                     || (d.Description != null && d.Description.Contains(term)));
        }

        var ordered = query
            .OrderBy(d => d.Category)
            .ThenByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Version);

        return await PagedList<Document>.CreateAsync(ordered, page, pageSize, ct);
    }

    public Task<Document?> GetAsync(int id, Visibility maxVisibility, CancellationToken ct = default) =>
        context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted && d.Visibility <= maxVisibility, ct);

    /// <summary>Historique des versions d'un document, de la plus récente à la plus ancienne.</summary>
    public async Task<IReadOnlyList<Document>> GetVersionsAsync(int documentId, CancellationToken ct = default)
    {
        var versions = new List<Document>();
        var current = await context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        var guard = 0;
        while (current is not null && guard++ < 50)
        {
            versions.Add(current);
            if (current.PreviousVersionId is not { } previousId)
            {
                break;
            }

            current = await context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == previousId, ct);
        }

        return versions;
    }

    public async Task RegisterDownloadAsync(int documentId, CancellationToken ct = default)
    {
        await context.Documents
            .Where(d => d.Id == documentId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.DownloadCount, d => d.DownloadCount + 1), ct);
    }

    /// <summary>Publie une nouvelle version et archive la précédente.</summary>
    public async Task<Result<int>> CreateNewVersionAsync(
        int documentId,
        string fileUrl,
        long sizeInBytes,
        int authorId,
        CancellationToken ct = default)
    {
        var current = await context.Documents.FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (current is null)
        {
            return Result<int>.Failure("Document introuvable.", "NOTFOUND_016");
        }

        var next = current.CreateNextVersion(fileUrl, sizeInBytes, authorId, clock.UtcNow);
        context.Documents.Add(next);
        await context.SaveChangesAsync(ct);

        return Result<int>.Success(next.Id);
    }
}
