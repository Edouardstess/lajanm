using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CavalierNoir.Web.Controllers;

/// <summary>Calendrier du club : événements, stages, assemblées.</summary>
[Route("Evenements")]
public sealed class EvenementsController(
    EventService events,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? annee, int? mois, CancellationToken ct)
    {
        var today = clock.Today;
        var year = annee ?? today.Year;
        var month = mois is >= 1 and <= 12 ? mois.Value : today.Month;

        ViewData["Annee"] = year;
        ViewData["Mois"] = month;
        ViewData["Prochains"] = await events.GetUpcomingAsync(5, ct);

        return View(await events.GetForMonthAsync(year, month, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var clubEvent = await events.GetBySlugAsync(slug, ct);
        if (clubEvent is null || !clubEvent.IsPublished)
        {
            return NotFound();
        }

        ViewData["Title"] = clubEvent.Title;
        ViewData["MetaDescription"] = clubEvent.Description;
        ViewData["EstInscrit"] = currentUser.UserId is { } userId
                                 && clubEvent.Registrations.Any(r => r.UserId == userId);

        return View(clubEvent);
    }

    [HttpPost("{id:int}/Inscription")]
    [Authorize(Policy = "EspaceMembre")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscription(int id, string slug, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await events.RegisterAsync(id, userId, ct);
        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded ? result.Value : result.Error;

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("{id:int}/Annulation")]
    [Authorize(Policy = "EspaceMembre")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Annulation(int id, string slug, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await events.CancelRegistrationAsync(id, userId, ct);
        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Votre inscription a été annulée."
            : result.Error;

        return RedirectToAction(nameof(Details), new { slug });
    }
}
