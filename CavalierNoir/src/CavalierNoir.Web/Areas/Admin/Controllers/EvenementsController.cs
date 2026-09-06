using CavalierNoir.Application.Common.Interfaces;
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

/// <summary>Calendrier du club : création et publication des événements.</summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Tournaments.Manage)]
public sealed class EvenementsController(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    IOptions<SiteOptions> siteOptions) : Controller
{
    private readonly SiteOptions _site = siteOptions.Value;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Événements";

        var list = await context.ClubEvents
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.StartDate)
            .Take(100)
            .ToListAsync(ct);

        return View(list);
    }

    public IActionResult Creer() => View("Formulaire", new EventFormViewModel());

    public async Task<IActionResult> Modifier(int id, CancellationToken ct)
    {
        var clubEvent = await context.ClubEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (clubEvent is null)
        {
            return NotFound();
        }

        return View("Formulaire", new EventFormViewModel
        {
            Id = clubEvent.Id,
            Title = clubEvent.Title,
            Description = clubEvent.Description,
            Type = clubEvent.Type,
            StartDate = clubEvent.StartDate,
            EndDate = clubEvent.EndDate,
            Location = clubEvent.Location,
            MaxParticipants = clubEvent.MaxParticipants,
            EntryFee = clubEvent.EntryFee.Amount,
            IsRegistrationOpen = clubEvent.IsRegistrationOpen,
            MembersOnly = clubEvent.MembersOnly,
            IsPublished = clubEvent.IsPublished
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(EventFormViewModel model, CancellationToken ct)
    {
        if (model.EndDate < model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "La date de fin précède la date de début.");
        }

        if (!ModelState.IsValid)
        {
            return View("Formulaire", model);
        }

        var clubEvent = model.Id > 0
            ? await context.ClubEvents.FirstOrDefaultAsync(e => e.Id == model.Id, ct)
            : new ClubEvent { CreatedAt = clock.UtcNow, CreatedById = currentUser.UserId };

        if (clubEvent is null)
        {
            return NotFound();
        }

        clubEvent.Title = model.Title.Trim();
        clubEvent.Description = model.Description;
        clubEvent.Type = model.Type;
        clubEvent.StartDate = model.StartDate;
        clubEvent.EndDate = model.EndDate;
        clubEvent.Location = model.Location;
        clubEvent.MaxParticipants = model.MaxParticipants;
        clubEvent.EntryFee = new Money(model.EntryFee, _site.DefaultCurrency);
        clubEvent.IsRegistrationOpen = model.IsRegistrationOpen;
        clubEvent.MembersOnly = model.MembersOnly;
        clubEvent.IsPublished = model.IsPublished;
        clubEvent.UpdatedAt = clock.UtcNow;
        clubEvent.UpdatedById = currentUser.UserId;

        if (model.Id == 0)
        {
            var baseSlug = Slug.From(model.Title, "evenement");
            var candidate = baseSlug;
            var suffix = 2;

            while (await context.ClubEvents.AnyAsync(e => e.Slug == candidate, ct))
            {
                candidate = Slug.WithSuffix(baseSlug, suffix++);
            }

            clubEvent.Slug = candidate;
            context.ClubEvents.Add(clubEvent);
        }

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = model.Id > 0 ? "Événement mis à jour." : "Événement créé.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Feuille de présence d'un événement.</summary>
    public async Task<IActionResult> Participants(int id, CancellationToken ct)
    {
        var clubEvent = await context.ClubEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (clubEvent is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Participants — {clubEvent.Title}";
        ViewData["Evenement"] = clubEvent;

        var registrations = await context.EventRegistrations
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ClubEventId == id)
            .OrderBy(r => r.RegisteredAt)
            .ToListAsync(ct);

        return View(registrations);
    }
}
