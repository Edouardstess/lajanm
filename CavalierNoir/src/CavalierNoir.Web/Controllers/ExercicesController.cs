using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>
/// Bibliothèque d'exercices et écran de résolution. Les visiteurs non
/// authentifiés ne voient que les exercices marqués publics et ne peuvent pas
/// soumettre de réponse.
/// </summary>
[Route("Exercices")]
public sealed class ExercicesController(
    ExerciseService exercises,
    ExerciseTokenService tokens,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? q,
        ExerciseTheme? theme,
        DifficultyLevel? difficulte,
        int? recueil,
        string tri = "recent",
        bool nonResolus = false,
        int page = 1,
        CancellationToken ct = default)
    {
        var isMember = User.IsInRole(Roles.Membre) || User.IsInRole(Roles.SuperAdmin) || IsStaff();
        var userId = currentUser.UserId;

        var filter = new ExerciseFilter
        {
            Search = q,
            Theme = theme,
            Difficulty = difficulte,
            CollectionId = recueil,
            PublicOnly = !isMember,
            UnsolvedByUserId = nonResolus && userId.HasValue ? userId : null,
            Sort = tri,
            Page = page,
            PageSize = 12
        };

        return View(new ExerciseIndexViewModel
        {
            Exercises = await exercises.SearchAsync(filter, userId, ct),
            Filter = filter,
            Collections = await context.ExerciseCollections
                .AsNoTracking()
                .Where(c => c.IsPublished)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(ct),
            IsMember = isMember
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var exercise = await exercises.GetByIdAsync(id, ct: ct);
        if (exercise is null)
        {
            return NotFound();
        }

        var isMember = User.IsInRole(Roles.Membre) || IsStaff();
        if (!exercise.IsPublic && !isMember)
        {
            TempData["Erreur"] = "Cet exercice est réservé aux membres du club.";
            return RedirectToAction("Index", "Adherer");
        }

        var userId = currentUser.UserId;

        return View("Resoudre", new ExerciseSolveViewModel
        {
            Exercise = exercise,
            CanSubmit = userId.HasValue,
            AlreadySolved = userId.HasValue && await context.ExerciseAttempts
                .AnyAsync(a => a.UserId == userId && a.ExerciseId == id && a.IsCorrect, ct),
            PreviousAttempts = userId.HasValue
                ? await exercises.GetAttemptsAsync(id, userId.Value, ct)
                : []
        });
    }

    /// <summary>
    /// Ouverture de l'exercice du jour depuis le lien reçu par courriel. Le jeton
    /// identifie le membre : il n'a pas besoin d'être connecté pour voir la
    /// position, mais sa tentative n'est enregistrée que s'il l'est.
    /// </summary>
    [HttpGet("Quotidien/{token}")]
    public async Task<IActionResult> Quotidien(string token, CancellationToken ct)
    {
        if (!tokens.TryValidate(token, out var userId, out var date))
        {
            TempData["Erreur"] = "Ce lien n'est pas valide.";
            return RedirectToAction(nameof(Index));
        }

        if (tokens.IsExpired(date, clock.Today))
        {
            TempData["Erreur"] = "Ce lien a expiré. Voici la bibliothèque complète des exercices.";
            return RedirectToAction(nameof(Index));
        }

        var daily = await exercises.GetDailyAsync(date, ct);
        if (daily?.Exercise is null)
        {
            return NotFound();
        }

        // Comptabilise l'ouverture du courriel pour les statistiques d'envoi.
        await context.DailyExercises
            .Where(d => d.Id == daily.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.OpenedCount, d => d.OpenedCount + 1), ct);

        var effectiveUserId = currentUser.UserId ?? (userId > 0 ? userId : null);

        return View("Resoudre", new ExerciseSolveViewModel
        {
            Exercise = daily.Exercise,
            Token = token,
            CanSubmit = effectiveUserId.HasValue,
            AlreadySolved = effectiveUserId.HasValue && await context.ExerciseAttempts
                .AnyAsync(a => a.UserId == effectiveUserId && a.ExerciseId == daily.ExerciseId && a.IsCorrect, ct),
            PreviousAttempts = effectiveUserId.HasValue
                ? await exercises.GetAttemptsAsync(daily.ExerciseId, effectiveUserId.Value, ct)
                : []
        });
    }

    [HttpPost("Soumettre")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Soumettre(ExerciseSubmitViewModel form, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        // Un lien signé permet d'attribuer la tentative même hors session.
        if (userId is null
            && !string.IsNullOrWhiteSpace(form.Token)
            && tokens.TryValidate(form.Token, out var tokenUserId, out _)
            && tokenUserId > 0)
        {
            userId = tokenUserId;
        }

        if (userId is null)
        {
            TempData["Erreur"] = "Connectez-vous pour enregistrer votre résultat.";
            return RedirectToAction(nameof(Details), new { id = form.ExerciseId });
        }

        var exercise = await exercises.GetByIdAsync(form.ExerciseId, ct: ct);
        if (exercise is null)
        {
            return NotFound();
        }

        var result = await exercises.SubmitAsync(
            new ExerciseSubmission
            {
                ExerciseId = form.ExerciseId,
                UserId = userId.Value,
                Moves = form.Moves,
                TimeSpentSeconds = form.TimeSpentSeconds,
                HintsUsed = form.HintsUsed,
                FromDailyEmail = !string.IsNullOrWhiteSpace(form.Token)
            },
            ct);

        if (result.Failed)
        {
            TempData["Erreur"] = result.Error;
            return RedirectToAction(nameof(Details), new { id = form.ExerciseId });
        }

        return View("Resoudre", new ExerciseSolveViewModel
        {
            Exercise = exercise,
            Token = form.Token,
            CanSubmit = true,
            AlreadySolved = result.Value!.IsCorrect,
            PreviousAttempts = await exercises.GetAttemptsAsync(form.ExerciseId, userId.Value, ct),
            Correction = result.Value,
            HintsRevealed = form.HintsUsed
        });
    }

    /// <summary>Renvoie un indice, sans dévoiler la solution (appel AJAX).</summary>
    [HttpGet("Indice/{id:int}/{niveau:int}")]
    [Authorize(Policy = "EspaceMembre")]
    public async Task<IActionResult> Indice(int id, int niveau, CancellationToken ct)
    {
        var exercise = await exercises.GetByIdAsync(id, ct: ct);
        if (exercise is null)
        {
            return NotFound();
        }

        var hint = exercise.HintAt(niveau);
        return string.IsNullOrWhiteSpace(hint)
            ? Json(new { disponible = false, texte = "Aucun indice supplémentaire pour cet exercice." })
            : Json(new { disponible = true, texte = hint });
    }

    private bool IsStaff() => Roles.StaffRoles.Split(',').Any(User.IsInRole);
}
