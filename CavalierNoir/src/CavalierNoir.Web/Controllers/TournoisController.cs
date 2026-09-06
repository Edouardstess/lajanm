using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>Tournois : calendrier public, fiche, appariements, classement et inscription.</summary>
[Route("Tournois")]
public sealed class TournoisController(
    TournamentService tournaments,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(TournamentStatus? statut, int page = 1, CancellationToken ct = default)
    {
        ViewData["Statut"] = statut;
        return View(await tournaments.SearchAsync(statut, page, 12, ct));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, int? ronde, CancellationToken ct)
    {
        var tournament = await tournaments.GetBySlugAsync(slug, ct);
        if (tournament is null)
        {
            return NotFound();
        }

        var userId = currentUser.UserId;
        var roundCount = tournament.Rounds.Count;
        var selected = Math.Clamp(ronde ?? roundCount, roundCount > 0 ? 1 : 0, Math.Max(roundCount, 0));

        ViewData["Title"] = tournament.Title;
        ViewData["MetaDescription"] = tournament.Description;

        return View(new TournamentDetailsViewModel
        {
            Tournament = tournament,
            Standings = await tournaments.GetStandingsAsync(tournament.Id, ct),
            Pairings = selected > 0
                ? await tournaments.GetPairingsAsync(tournament.Id, selected, ct)
                : [],
            SelectedRound = selected,
            RoundCount = roundCount,
            IsRegistered = userId.HasValue && await context.TournamentRegistrations
                .AnyAsync(
                    r => r.TournamentId == tournament.Id
                         && r.UserId == userId
                         && r.Status != RegistrationStatus.Annulee,
                    ct),
            CanRegister = userId.HasValue && tournament.IsRegistrationOpen(clock.UtcNow)
        });
    }

    [HttpPost("{id:int}/Inscription")]
    [Authorize(Policy = "EspaceMembre")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscription(int id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await tournaments.RegisterAsync(id, userId, ct);
        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded ? result.Value : result.Error;

        return await RedirectToTournamentAsync(id, ct);
    }

    [HttpPost("{id:int}/Desinscription")]
    [Authorize(Policy = "EspaceMembre")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desinscription(int id, string? motif, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await tournaments.CancelRegistrationAsync(id, userId, motif, ct);
        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Votre inscription a été annulée."
            : result.Error;

        return await RedirectToTournamentAsync(id, ct);
    }

    private async Task<IActionResult> RedirectToTournamentAsync(int id, CancellationToken ct)
    {
        var slug = await context.Tournaments
            .Where(t => t.Id == id)
            .Select(t => t.Slug)
            .FirstOrDefaultAsync(ct);

        return slug is null
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { slug });
    }
}
