using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Tournaments;
using CavalierNoir.Domain.ValueObjects;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SiteOptions = CavalierNoir.Application.Common.Models.SiteOptions;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>
/// Pilotage des compétitions : création, inscriptions, appariements, saisie des
/// résultats et clôture.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Tournaments.Manage)]
public sealed class TournoisController(
    TournamentService tournaments,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IOptions<SiteOptions> siteOptions) : Controller
{
    private readonly SiteOptions _site = siteOptions.Value;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Tournois";

        var list = await context.Tournaments
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.StartDate)
            .Take(100)
            .ToListAsync(ct);

        return View(list);
    }

    public IActionResult Creer() => View("Formulaire", new TournamentFormViewModel());

    public async Task<IActionResult> Modifier(int id, CancellationToken ct)
    {
        var tournament = await tournaments.GetByIdAsync(id, ct);
        if (tournament is null)
        {
            return NotFound();
        }

        return View("Formulaire", new TournamentFormViewModel
        {
            Id = tournament.Id,
            Title = tournament.Title,
            Description = tournament.Description,
            Regulations = tournament.Regulations,
            Type = tournament.Type,
            TimeControl = tournament.TimeControl,
            TimeControlLabel = tournament.TimeControlLabel,
            StartDate = tournament.StartDate,
            EndDate = tournament.EndDate,
            RegistrationClosesAt = tournament.RegistrationClosesAt,
            Location = tournament.Location,
            MaxPlayers = tournament.MaxPlayers,
            PlannedRounds = tournament.PlannedRounds,
            EntryFee = tournament.EntryFee.Amount,
            IsRated = tournament.IsRated,
            MembersOnly = tournament.MembersOnly,
            MinimumElo = tournament.MinimumElo,
            MaximumElo = tournament.MaximumElo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(TournamentFormViewModel model, CancellationToken ct)
    {
        if (model.EndDate < model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "La date de fin précède la date de début.");
        }

        if (!ModelState.IsValid)
        {
            return View("Formulaire", model);
        }

        var tournament = model.Id > 0
            ? await context.Tournaments.FirstOrDefaultAsync(t => t.Id == model.Id, ct)
            : new Tournament { CreatedAt = clock.UtcNow, CreatedById = currentUser.UserId };

        if (tournament is null)
        {
            return NotFound();
        }

        if (model.Id > 0)
        {
            tournament.EnsureEditable();
        }

        tournament.Title = model.Title.Trim();
        tournament.Description = model.Description;
        tournament.Regulations = model.Regulations;
        tournament.Type = model.Type;
        tournament.TimeControl = model.TimeControl;
        tournament.TimeControlLabel = model.TimeControlLabel;
        tournament.StartDate = model.StartDate;
        tournament.EndDate = model.EndDate;
        tournament.RegistrationClosesAt = model.RegistrationClosesAt;
        tournament.Location = model.Location;
        tournament.MaxPlayers = model.MaxPlayers;
        tournament.PlannedRounds = model.PlannedRounds;
        tournament.EntryFee = new Money(model.EntryFee, _site.DefaultCurrency);
        tournament.IsRated = model.IsRated;
        tournament.MembersOnly = model.MembersOnly;
        tournament.MinimumElo = model.MinimumElo;
        tournament.MaximumElo = model.MaximumElo;
        tournament.OrganizerId ??= currentUser.UserId;
        tournament.UpdatedAt = clock.UtcNow;
        tournament.UpdatedById = currentUser.UserId;

        if (model.Id == 0)
        {
            tournament.Slug = await GenerateSlugAsync(model.Title, ct);
            context.Tournaments.Add(tournament);
        }

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = model.Id > 0 ? "Tournoi mis à jour." : "Tournoi créé en brouillon.";
        return RedirectToAction(nameof(Details), new { id = tournament.Id });
    }

    public async Task<IActionResult> Details(int id, int? ronde, CancellationToken ct)
    {
        var tournament = await tournaments.GetByIdAsync(id, ct);
        if (tournament is null)
        {
            return NotFound();
        }

        var roundCount = tournament.Rounds.Count;
        var selected = ronde ?? roundCount;

        ViewData["Title"] = tournament.Title;
        ViewData["Inscriptions"] = await context.TournamentRegistrations
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.TournamentId == id)
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.EloAtRegistration)
            .ToListAsync(ct);

        return View(new TournamentDetailsViewModel
        {
            Tournament = tournament,
            Standings = await tournaments.GetStandingsAsync(id, ct),
            Pairings = selected > 0 ? await tournaments.GetPairingsAsync(id, selected, ct) : [],
            SelectedRound = selected,
            RoundCount = roundCount,
            IsRegistered = false,
            CanRegister = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangerStatut(int id, string action, CancellationToken ct)
    {
        var tournament = await tournaments.GetByIdAsync(id, ct);
        if (tournament is null)
        {
            return NotFound();
        }

        var now = clock.UtcNow;

        switch (action)
        {
            case "ouvrir":
                tournament.OpenRegistration(now);
                break;
            case "cloturer":
                tournament.CloseRegistration(now);
                break;
            case "demarrer":
                tournament.Start(now);
                break;
            case "annuler":
                tournament.Cancel(now);
                break;
            case "archiver":
                tournament.Archive(now);
                break;
            default:
                TempData["Erreur"] = "Action inconnue.";
                return RedirectToAction(nameof(Details), new { id });
        }

        await context.SaveChangesAsync(ct);
        TempData["Succes"] = "Statut du tournoi mis à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Génère les appariements de la ronde suivante (BR-06).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Tournaments.Pair)]
    public async Task<IActionResult> GenererRonde(int id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } arbiterId)
        {
            return Challenge();
        }

        var result = await tournaments.GenerateNextRoundAsync(id, arbiterId, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Appariements générés et publiés."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Tournaments.EnterResults)]
    public async Task<IActionResult> SaisirResultat(
        int id,
        int partieId,
        GameResult resultat,
        string? justification,
        int? ronde,
        CancellationToken ct)
    {
        if (currentUser.UserId is not { } arbiterId)
        {
            return Challenge();
        }

        var result = await tournaments.EnterResultAsync(partieId, resultat, arbiterId, justification, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Résultat enregistré, classement et ELO mis à jour."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id, ronde });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cloturer(int id, CancellationToken ct)
    {
        if (currentUser.UserId is not { } actorId)
        {
            return Challenge();
        }

        var result = await tournaments.FinishAsync(id, actorId, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Tournoi clôturé, podium publié."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Confirme ou annule une inscription depuis le back-office.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GererInscription(
        int id,
        int inscriptionId,
        string action,
        CancellationToken ct)
    {
        var registration = await context.TournamentRegistrations
            .FirstOrDefaultAsync(r => r.Id == inscriptionId && r.TournamentId == id, ct);

        if (registration is null)
        {
            return NotFound();
        }

        var now = clock.UtcNow;

        switch (action)
        {
            case "confirmer":
                registration.Confirm(now);
                break;
            case "annuler":
                registration.Cancel("Annulée par l'organisation", now);
                break;
            case "pointer":
                registration.RecordAttendance(now);
                break;
            case "absent":
                registration.RecordAbsence(now);
                break;
        }

        await context.SaveChangesAsync(ct);
        TempData["Succes"] = "Inscription mise à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<string> GenerateSlugAsync(string title, CancellationToken ct)
    {
        var baseSlug = Slug.From(title, "tournoi");
        var candidate = baseSlug;
        var suffix = 2;

        while (await context.Tournaments.AnyAsync(t => t.Slug == candidate, ct))
        {
            candidate = Slug.WithSuffix(baseSlug, suffix++);
        }

        return candidate;
    }
}
