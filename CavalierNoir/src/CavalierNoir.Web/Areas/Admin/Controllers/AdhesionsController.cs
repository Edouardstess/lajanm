using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>
/// Instruction des demandes d'adhésion et suivi des adhésions actives.
/// Réservé au secrétariat, au bureau et à l'administration.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Permissions.MembershipsPermissions.Review)]
public sealed class AdhesionsController(
    MembershipService memberships,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(ApplicationStatus? statut, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Demandes d'adhésion";
        ViewData["Statut"] = statut;

        return View(await memberships.GetApplicationsAsync(statut, page, 20, ct));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var application = await context.MembershipApplications
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.MembershipType)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Demande n° {application.Id}";
        ViewData["Age"] = application.User.Age(clock.Today);

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MembershipsPermissions.Approve)]
    public async Task<IActionResult> Approuver(int id, int? eloEvalue, CancellationToken ct)
    {
        if (currentUser.UserId is not { } reviewerId)
        {
            return Challenge();
        }

        var result = await memberships.ApproveApplicationAsync(id, reviewerId, eloEvalue, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Demande approuvée. L'adhésion est créée en attente de paiement."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.MembershipsPermissions.Approve)]
    public async Task<IActionResult> Rejeter(int id, string motif, CancellationToken ct)
    {
        if (currentUser.UserId is not { } reviewerId)
        {
            return Challenge();
        }

        var result = await memberships.RejectApplicationAsync(id, reviewerId, motif, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Demande rejetée ; le candidat en a été informé."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DemanderComplement(int id, string commentaire, CancellationToken ct)
    {
        if (currentUser.UserId is not { } reviewerId)
        {
            return Challenge();
        }

        var result = await memberships.RequestMoreInformationAsync(id, reviewerId, commentaire, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Demande de complément transmise au candidat."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Liste des adhésions, filtrable par statut.</summary>
    public async Task<IActionResult> Membres(MembershipStatus? statut, CancellationToken ct)
    {
        ViewData["Title"] = "Adhésions";
        ViewData["Statut"] = statut;

        var query = context.Memberships
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.MembershipType)
            .AsQueryable();

        if (statut.HasValue)
        {
            query = query.Where(m => m.Status == statut.Value);
        }

        var list = await query
            .OrderByDescending(m => m.Period.End)
            .Take(300)
            .ToListAsync(ct);

        return View(list);
    }

    /// <summary>Enregistrement d'un règlement de cotisation par le trésorier.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Finance.Manage)]
    public async Task<IActionResult> EnregistrerPaiement(
        int adhesionId,
        PaymentMethod methode,
        decimal? montant,
        string? reference,
        CancellationToken ct)
    {
        var result = await memberships.RecordPaymentAsync(
            adhesionId,
            methode,
            montant,
            currentUser.UserId,
            reference,
            idempotencyKey: $"manuel-{adhesionId}-{clock.UtcNow:yyyyMMddHHmmss}",
            ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Paiement enregistré et adhésion activée."
            : result.Error;

        return RedirectToAction(nameof(Membres));
    }

    /// <summary>Export des adhérents au format CSV, pour le trésorier.</summary>
    [Authorize(Policy = Permissions.Finance.View)]
    public async Task<IActionResult> Exporter(CancellationToken ct)
    {
        var rows = await context.Memberships
            .AsNoTracking()
            .Include(m => m.User)
            .Include(m => m.MembershipType)
            .OrderBy(m => m.MemberNumber)
            .ToListAsync(ct);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Numéro;Nom;Prénom;Courriel;Formule;Statut;Début;Fin;Montant;Devise");

        foreach (var m in rows)
        {
            csv.AppendLine(string.Join(';',
                Csv(m.MemberNumber),
                Csv(m.User.LastName),
                Csv(m.User.FirstName),
                Csv(m.User.Email),
                Csv(m.MembershipType?.Name),
                Csv(m.Status.ToString()),
                m.Period.Start.ToString("yyyy-MM-dd"),
                m.Period.End.ToString("yyyy-MM-dd"),
                m.Amount.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Csv(m.Amount.Currency)));
        }

        // Le BOM permet à un tableur de reconnaître l'encodage UTF-8.
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"adherents-{clock.Today:yyyy-MM-dd}.csv");
    }

    private static string Csv(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Replace(';', ',').Replace('\n', ' ');
}
