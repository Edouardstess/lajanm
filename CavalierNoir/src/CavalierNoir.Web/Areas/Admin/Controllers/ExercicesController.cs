using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.ValueObjects;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>
/// Gestion de la bibliothèque d'exercices et du calendrier de l'exercice
/// quotidien. Réservé aux entraîneurs et au responsable pédagogique.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Exercises.Manage)]
public sealed class ExercicesController(
    ExerciseService exercises,
    DailyExerciseDispatcher dispatcher,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Exercices";

        var result = await exercises.SearchAsync(
            new ExerciseFilter
            {
                Search = q,
                IncludeUnpublished = true,
                Page = page,
                PageSize = 20,
                Sort = "recent"
            },
            currentUser.UserId,
            ct);

        ViewData["Recherche"] = q;
        return View(result);
    }

    public async Task<IActionResult> Creer(CancellationToken ct) =>
        View("Formulaire", new ExerciseFormViewModel
        {
            Collections = await LoadCollectionsAsync(ct)
        });

    public async Task<IActionResult> Modifier(int id, CancellationToken ct)
    {
        var exercise = await exercises.GetByIdAsync(id, includeUnpublished: true, ct);
        if (exercise is null)
        {
            return NotFound();
        }

        return View("Formulaire", new ExerciseFormViewModel
        {
            Id = exercise.Id,
            Title = exercise.Title,
            Fen = exercise.Fen,
            Solution = exercise.Solution,
            Theme = exercise.Theme,
            Difficulty = exercise.Difficulty,
            EstimatedTimeSeconds = exercise.EstimatedTimeSeconds,
            Hint1 = exercise.Hint1,
            Hint2 = exercise.Hint2,
            Hint3 = exercise.Hint3,
            Explanation = exercise.Explanation,
            Source = exercise.Source,
            CollectionId = exercise.CollectionId,
            IsPublished = exercise.IsPublished,
            IsPublic = exercise.IsPublic,
            Collections = await LoadCollectionsAsync(ct)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(ExerciseFormViewModel model, CancellationToken ct)
    {
        if (!Fen.IsValid(model.Fen))
        {
            ModelState.AddModelError(nameof(model.Fen), "La position FEN est invalide.");
        }

        if (MoveSequence.Parse(model.Solution).IsEmpty)
        {
            ModelState.AddModelError(nameof(model.Solution), "La solution doit contenir au moins un coup.");
        }

        if (!ModelState.IsValid)
        {
            model.Collections = await LoadCollectionsAsync(ct);
            return View("Formulaire", model);
        }

        var exercise = model.Id > 0
            ? await context.Exercises.FirstOrDefaultAsync(e => e.Id == model.Id, ct)
            : new Exercise { CreatedAt = clock.UtcNow, CreatedById = currentUser.UserId };

        if (exercise is null)
        {
            return NotFound();
        }

        exercise.Title = model.Title.Trim();
        exercise.Fen = model.Fen.Trim();
        exercise.Solution = model.Solution.Trim();
        exercise.Theme = model.Theme;
        exercise.Difficulty = model.Difficulty;
        exercise.EstimatedTimeSeconds = model.EstimatedTimeSeconds;
        exercise.Hint1 = model.Hint1;
        exercise.Hint2 = model.Hint2;
        exercise.Hint3 = model.Hint3;
        exercise.Explanation = model.Explanation;
        exercise.Source = model.Source;
        exercise.CollectionId = model.CollectionId;
        exercise.IsPublic = model.IsPublic;
        exercise.IsPublished = model.IsPublished;
        exercise.UpdatedAt = clock.UtcNow;
        exercise.UpdatedById = currentUser.UserId;

        if (model.Id == 0)
        {
            context.Exercises.Add(exercise);
        }

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = model.Id > 0 ? "Exercice mis à jour." : "Exercice créé.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Archivage : l'exercice n'est jamais effacé, ses tentatives restent exploitables.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archiver(int id, CancellationToken ct)
    {
        var exercise = await context.Exercises.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null)
        {
            return NotFound();
        }

        exercise.IsDeleted = true;
        exercise.DeletedAt = clock.UtcNow;
        exercise.DeletedById = currentUser.UserId;
        exercise.IsPublished = false;

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Exercice archivé.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Calendrier éditorial de l'exercice du jour, sur trente jours.</summary>
    public async Task<IActionResult> Planning(CancellationToken ct)
    {
        ViewData["Title"] = "Exercice du jour";

        var today = clock.Today;
        var horizon = today.AddDays(30);

        var planned = await context.DailyExercises
            .AsNoTracking()
            .Include(d => d.Exercise)
            .Where(d => d.ScheduledOn >= today.AddDays(-7) && d.ScheduledOn <= horizon)
            .OrderBy(d => d.ScheduledOn)
            .ToListAsync(ct);

        ViewData["Aujourdhui"] = today;
        return View(planned);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Planifier(DateOnly date, int exerciceId, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await exercises.ScheduleDailyAsync(date, exerciceId, userId, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? $"Exercice planifié pour le {date:dd/MM/yyyy}."
            : result.Error;

        return RedirectToAction(nameof(Planning));
    }

    /// <summary>Déclenche manuellement l'envoi du jour (UC-146).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Envoyer(DateOnly? date, CancellationToken ct)
    {
        var report = await dispatcher.DispatchAsync(date, ct);

        TempData["Succes"] =
            $"Envoi du {report.Date:dd/MM/yyyy} : {report.Sent} message(s) expédié(s), "
            + $"{report.Failed} échec(s). {report.Note}";

        return RedirectToAction(nameof(Planning));
    }

    private async Task<IReadOnlyList<(int Id, string Title)>> LoadCollectionsAsync(CancellationToken ct)
    {
        var collections = await context.ExerciseCollections
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new { c.Id, c.Title })
            .ToListAsync(ct);

        return collections.Select(c => (c.Id, c.Title)).ToList();
    }
}
