using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Club;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Gestion documentaire : téléversement, versionnage et archivage.</summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Documents.Manage)]
public sealed class DocumentsController(
    DocumentService documents,
    IApplicationDbContext context,
    IFileStorage storage,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(DocumentCategoryCode? categorie, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Documents";
        ViewData["Categorie"] = categorie;

        return View(await documents.SearchAsync(
            Visibility.Prive,
            categorie,
            null,
            includeArchived: true,
            page,
            25,
            ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Televerser(
        IFormFile fichier,
        string titre,
        string? description,
        DocumentCategoryCode categorie,
        Visibility visibilite,
        DataClassification classification,
        int? versionDe,
        CancellationToken ct)
    {
        if (fichier is not { Length: > 0 })
        {
            TempData["Erreur"] = "Aucun fichier reçu.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = fichier.OpenReadStream();
        var stored = await storage.SaveAsync(stream, fichier.FileName, "documents", ct);

        if (versionDe is { } previousId)
        {
            var result = await documents.CreateNewVersionAsync(
                previousId,
                stored.RelativeUrl,
                stored.SizeInBytes,
                currentUser.UserId ?? 0,
                ct);

            TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
                ? "Nouvelle version publiée ; la précédente est archivée."
                : result.Error;

            return RedirectToAction(nameof(Index));
        }

        context.Documents.Add(new Document
        {
            Title = titre.Trim(),
            Description = description,
            FileUrl = stored.RelativeUrl,
            ContentType = stored.ContentType,
            SizeInBytes = stored.SizeInBytes,
            Category = categorie,
            Visibility = visibilite,
            Classification = classification,
            EffectiveDate = clock.Today,
            CreatedAt = clock.UtcNow,
            CreatedById = currentUser.UserId
        });

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Document publié.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Versions(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Historique du document";
        return View(await documents.GetVersionsAsync(id, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archiver(int id, string? motif, CancellationToken ct)
    {
        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null)
        {
            return NotFound();
        }

        document.IsArchived = true;
        document.ArchivedAt = clock.UtcNow;
        document.ArchiveReason = motif;

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Document archivé.";
        return RedirectToAction(nameof(Index));
    }
}
