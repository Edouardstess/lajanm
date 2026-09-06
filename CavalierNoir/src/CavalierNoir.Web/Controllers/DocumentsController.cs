using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CavalierNoir.Web.Controllers;

/// <summary>
/// Espace documentaire. La visibilité d'un document dépend du profil du
/// visiteur : public, membre ou bureau.
/// </summary>
[Route("Documents")]
public sealed class DocumentsController(DocumentService documents, IFileStorage storage) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        DocumentCategoryCode? categorie,
        string? q,
        int page = 1,
        CancellationToken ct = default)
    {
        ViewData["Categorie"] = categorie;
        ViewData["Recherche"] = q;
        ViewData["Visibilite"] = CurrentVisibility();

        return View(await documents.SearchAsync(CurrentVisibility(), categorie, q, false, page, 20, ct));
    }

    [HttpGet("Telecharger/{id:int}")]
    public async Task<IActionResult> Telecharger(int id, CancellationToken ct)
    {
        var document = await documents.GetAsync(id, CurrentVisibility(), ct);
        if (document is null)
        {
            return NotFound();
        }

        var stream = await storage.OpenReadAsync(document.FileUrl, ct);
        if (stream is null)
        {
            TempData["Erreur"] = "Le fichier est introuvable sur le serveur. Signalez-le au secrétariat.";
            return RedirectToAction(nameof(Index));
        }

        await documents.RegisterDownloadAsync(id, ct);

        var fileName = $"{document.Title} (v{document.Version}){Path.GetExtension(document.FileUrl)}";
        return File(stream, document.ContentType ?? "application/octet-stream", fileName);
    }

    /// <summary>Niveau de visibilité maximal accessible au visiteur courant.</summary>
    private Visibility CurrentVisibility()
    {
        if (Roles.StaffRoles.Split(',').Any(User.IsInRole))
        {
            return Visibility.Bureau;
        }

        return Roles.MemberRoles.Split(',').Any(User.IsInRole)
            ? Visibility.Membres
            : Visibility.Public;
    }
}
