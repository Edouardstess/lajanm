using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Membre.Controllers;

/// <summary>Historique d'entraînement et statistiques personnelles.</summary>
[Area("Membre")]
[Authorize(Policy = "EspaceMembre")]
public sealed class ExercicesController(
    ExerciseService exercises,
    IApplicationDbContext context,
    ICurrentUser currentUser) : Controller
{
    public async Task<IActionResult> Historique(int page = 1, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        ViewData["Title"] = "Mon entraînement";

        var attempts = context.ExerciseAttempts
            .AsNoTracking()
            .Include(a => a.Exercise)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AttemptedAt);

        var byTheme = await context.ExerciseAttempts
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Exercise.Theme)
            .Select(g => new
            {
                Theme = g.Key,
                Attempts = g.Count(),
                Correct = g.Count(a => a.IsCorrect)
            })
            .ToListAsync(ct);

        return View(new TrainingHistoryViewModel
        {
            Progress = await exercises.GetProgressAsync(userId, ct),
            Attempts = await PagedList<ExerciseAttempt>.CreateAsync(attempts, page, 20, ct),
            Badges = await context.UserBadges
                .AsNoTracking()
                .Include(b => b.Badge)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.EarnedAt)
                .ToListAsync(ct),
            ByTheme = byTheme
                .Select(t => new ThemeStat(t.Theme.ToString(), t.Attempts, t.Correct))
                .OrderByDescending(t => t.Attempts)
                .ToList()
        });
    }
}
