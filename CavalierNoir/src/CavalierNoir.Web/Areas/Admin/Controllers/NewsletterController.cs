using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Abonnés et campagnes d'e-mailing.</summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Content.SendNewsletter)]
public sealed class NewsletterController(
    NewsletterService newsletter,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Lettre d'information";

        ViewData["Abonnes"] = await context.NewsletterSubscribers.CountAsync(s => s.IsActive && s.IsConfirmed, ct);
        ViewData["EnAttente"] = await context.NewsletterSubscribers.CountAsync(s => !s.IsConfirmed, ct);
        ViewData["Desabonnes"] = await context.NewsletterSubscribers.CountAsync(s => !s.IsActive, ct);

        var campaigns = await context.NewsletterCampaigns
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return View(campaigns);
    }

    public IActionResult Creer() => View("Formulaire", new NewsletterCampaign());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(
        int id,
        string nom,
        string objet,
        string contenu,
        string audience,
        CancellationToken ct)
    {
        var campaign = id > 0
            ? await context.NewsletterCampaigns.FirstOrDefaultAsync(c => c.Id == id, ct)
            : new NewsletterCampaign { CreatedAt = clock.UtcNow, CreatedById = currentUser.UserId };

        if (campaign is null)
        {
            return NotFound();
        }

        if (campaign.Status == CampaignStatus.Envoyee)
        {
            TempData["Erreur"] = "Une campagne déjà expédiée ne peut plus être modifiée.";
            return RedirectToAction(nameof(Index));
        }

        campaign.Name = nom.Trim();
        campaign.Subject = objet.Trim();
        campaign.BodyHtml = contenu;
        campaign.Audience = audience;
        campaign.UpdatedAt = clock.UtcNow;
        campaign.UpdatedById = currentUser.UserId;

        if (id == 0)
        {
            context.NewsletterCampaigns.Add(campaign);
        }

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Campagne enregistrée.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Envoyer(int id, CancellationToken ct)
    {
        var result = await newsletter.SendCampaignAsync(id, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? $"Campagne expédiée à {result.Value} destinataire(s)."
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Abonnes(int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Abonnés";

        var subscribers = await context.NewsletterSubscribers
            .AsNoTracking()
            .OrderByDescending(s => s.SubscribedAt)
            .Skip((Math.Max(page, 1) - 1) * 50)
            .Take(50)
            .ToListAsync(ct);

        ViewData["Page"] = Math.Max(page, 1);
        return View(subscribers);
    }

    /// <summary>Export CSV de la liste d'abonnés confirmés.</summary>
    public async Task<IActionResult> Exporter(CancellationToken ct)
    {
        var rows = await context.NewsletterSubscribers
            .AsNoTracking()
            .Where(s => s.IsActive && s.IsConfirmed)
            .OrderBy(s => s.Email)
            .Select(s => new { s.Email, s.Name, s.SubscribedAt })
            .ToListAsync(ct);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Courriel;Nom;Inscrit le");

        foreach (var row in rows)
        {
            csv.AppendLine($"{row.Email};{row.Name};{row.SubscribedAt:yyyy-MM-dd}");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"abonnes-{clock.Today:yyyy-MM-dd}.csv");
    }
}
