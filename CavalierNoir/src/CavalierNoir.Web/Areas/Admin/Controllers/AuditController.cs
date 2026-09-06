using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>
/// Consultation du journal d'audit et de l'historique des connexions.
/// Réservé au super-administrateur : ces données sont sensibles et immuables.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Administration.ViewAudit)]
public sealed class AuditController(IApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(
        string? entite,
        string? action,
        int? utilisateurId,
        int page = 1,
        CancellationToken ct = default)
    {
        ViewData["Title"] = "Journal d'audit";
        ViewData["Entite"] = entite;
        ViewData["Action"] = action;
        ViewData["UtilisateurId"] = utilisateurId;

        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entite))
        {
            query = query.Where(a => a.EntityName == entite);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (utilisateurId.HasValue)
        {
            query = query.Where(a => a.UserId == utilisateurId.Value);
        }

        var ordered = query.OrderByDescending(a => a.Timestamp);
        return View(await PagedList<Domain.Identity.AuditLog>.CreateAsync(ordered, page, 50, ct));
    }

    public async Task<IActionResult> Connexions(bool? echecs, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Historique des connexions";
        ViewData["Echecs"] = echecs;

        var query = context.LoginHistory.AsNoTracking().AsQueryable();

        if (echecs == true)
        {
            query = query.Where(h => !h.IsSuccessful);
        }

        var ordered = query.OrderByDescending(h => h.OccurredAt);
        return View(await PagedList<Domain.Identity.LoginHistory>.CreateAsync(ordered, page, 50, ct));
    }
}
