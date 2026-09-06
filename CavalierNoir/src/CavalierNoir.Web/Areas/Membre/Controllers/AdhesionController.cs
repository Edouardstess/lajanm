using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Membre.Controllers;

/// <summary>Suivi de l'adhésion du membre : statut, paiements, renouvellement.</summary>
[Area("Membre")]
[Authorize]
public sealed class AdhesionController(
    MembershipService memberships,
    IApplicationDbContext context,
    ICurrentUser currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        ViewData["Title"] = "Mon adhésion";

        return View(new MemberMembershipViewModel
        {
            Current = await memberships.GetCurrentMembershipAsync(userId, ct),
            Payments = await context.Payments
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync(ct),
            PendingApplication = await context.MembershipApplications
                .AsNoTracking()
                .Include(a => a.MembershipType)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct),
            Types = await memberships.GetActiveTypesAsync(ct)
        });
    }

    /// <summary>
    /// Demande de renouvellement. Le paiement est ensuite constaté par le
    /// trésorier : l'adhésion n'est prolongée qu'après encaissement.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renouveler(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var membership = await memberships.GetCurrentMembershipAsync(userId, ct);
        if (membership is null)
        {
            TempData["Erreur"] = "Aucune adhésion à renouveler. Déposez d'abord une demande d'adhésion.";
            return RedirectToAction("Index", "Adherer", new { area = "" });
        }

        if (membership.Status == MembershipStatus.EnAttentePaiement)
        {
            TempData["Erreur"] = "Votre cotisation est déjà en attente de règlement.";
            return RedirectToAction(nameof(Index));
        }

        membership.Status = MembershipStatus.EnAttentePaiement;
        await context.SaveChangesAsync(ct);

        TempData["Succes"] =
            "Demande de renouvellement enregistrée. Réglez votre cotisation auprès du trésorier "
            + "ou en ligne pour la valider.";

        return RedirectToAction(nameof(Index));
    }
}
