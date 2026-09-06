using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Finance;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SiteOptions = CavalierNoir.Application.Common.Models.SiteOptions;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>
/// Suivi financier : encaissements, dons, dépenses et synthèse budgétaire.
/// La séparation des tâches est appliquée à l'approbation des dépenses.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Finance.View)]
public sealed class FinancesController(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IOptions<SiteOptions> siteOptions) : Controller
{
    private readonly SiteOptions _site = siteOptions.Value;

    public async Task<IActionResult> Index(int? annee, CancellationToken ct)
    {
        var year = annee ?? clock.Today.Year;
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        ViewData["Title"] = $"Finances {year}";
        ViewData["Annee"] = year;
        ViewData["Devise"] = _site.DefaultCurrency;

        ViewData["Cotisations"] = await context.Payments
            .Where(p => p.Status == PaymentStatus.Paye
                        && p.Purpose == PaymentPurpose.Cotisation
                        && p.PaidAt >= start && p.PaidAt < end)
            .SumAsync(p => (decimal?)p.Amount.Amount, ct) ?? 0m;

        ViewData["Dons"] = await context.Donations
            .Where(d => d.DonatedOn >= DateOnly.FromDateTime(start) && d.DonatedOn < DateOnly.FromDateTime(end))
            .SumAsync(d => (decimal?)d.Amount.Amount, ct) ?? 0m;

        ViewData["Depenses"] = await context.Expenses
            .Where(e => e.IncurredOn >= DateOnly.FromDateTime(start) && e.IncurredOn < DateOnly.FromDateTime(end))
            .SumAsync(e => (decimal?)e.Amount.Amount, ct) ?? 0m;

        var payments = await context.Payments
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.CreatedAt >= start && p.CreatedAt < end)
            .OrderByDescending(p => p.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        return View(payments);
    }

    public async Task<IActionResult> Depenses(CancellationToken ct)
    {
        ViewData["Title"] = "Dépenses";

        var expenses = await context.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.IncurredOn)
            .Take(200)
            .ToListAsync(ct);

        return View(expenses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Finance.Manage)]
    public async Task<IActionResult> AjouterDepense(
        ExpenseCategory categorie,
        string description,
        decimal montant,
        DateOnly date,
        string? fournisseur,
        CancellationToken ct)
    {
        context.Expenses.Add(new Expense
        {
            Category = categorie,
            Description = description.Trim(),
            Amount = new Money(montant, _site.DefaultCurrency),
            IncurredOn = date,
            Supplier = fournisseur,
            CreatedAt = clock.UtcNow,
            CreatedById = currentUser.UserId
        });

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Dépense enregistrée. Elle doit être approuvée par un tiers.";
        return RedirectToAction(nameof(Depenses));
    }

    /// <summary>Approbation d'une dépense — interdite à son auteur (SoD-01).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Finance.Manage)]
    public async Task<IActionResult> ApprouverDepense(int id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } approverId)
        {
            return Challenge();
        }

        var expense = await context.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null)
        {
            return NotFound();
        }

        expense.Approve(approverId, clock.UtcNow);
        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Dépense approuvée.";
        return RedirectToAction(nameof(Depenses));
    }

    public async Task<IActionResult> Dons(CancellationToken ct)
    {
        ViewData["Title"] = "Dons";

        var donations = await context.Donations
            .AsNoTracking()
            .OrderByDescending(d => d.DonatedOn)
            .Take(200)
            .ToListAsync(ct);

        return View(donations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Finance.Manage)]
    public async Task<IActionResult> AjouterDon(
        string donateur,
        string? courriel,
        decimal montant,
        DateOnly date,
        bool anonyme,
        string? message,
        CancellationToken ct)
    {
        context.Donations.Add(new Donation
        {
            DonorName = donateur.Trim(),
            DonorEmail = courriel,
            Amount = new Money(montant, _site.DefaultCurrency),
            DonatedOn = date,
            IsAnonymous = anonyme,
            Message = message,
            CreatedAt = clock.UtcNow,
            CreatedById = currentUser.UserId
        });

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Don enregistré.";
        return RedirectToAction(nameof(Dons));
    }

    /// <summary>Remboursement d'un encaissement, avec motif obligatoire.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Finance.Refund)]
    public async Task<IActionResult> Rembourser(int id, string motif, CancellationToken ct)
    {
        var payment = await context.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            TempData["Erreur"] = "Un motif de remboursement est obligatoire.";
            return RedirectToAction(nameof(Index));
        }

        payment.Refund(motif, clock.UtcNow);
        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Remboursement enregistré.";
        return RedirectToAction(nameof(Index));
    }
}
