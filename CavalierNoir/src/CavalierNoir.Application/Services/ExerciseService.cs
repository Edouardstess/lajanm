using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Bibliothèque d'exercices, résolution, correction et progression.
/// Porte également la sélection de l'exercice du jour (BR-08).
/// </summary>
public sealed class ExerciseService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    ILogger<ExerciseService> logger)
{
    /// <summary>Recherche paginée dans la bibliothèque.</summary>
    public async Task<PagedList<ExerciseCard>> SearchAsync(
        ExerciseFilter filter,
        int? currentUserId = null,
        CancellationToken ct = default)
    {
        var query = context.Exercises.AsNoTracking().Where(e => !e.IsDeleted);

        if (!filter.IncludeUnpublished)
        {
            query = query.Where(e => e.IsPublished);
        }

        if (filter.PublicOnly)
        {
            query = query.Where(e => e.IsPublic);
        }

        if (filter.Theme.HasValue)
        {
            query = query.Where(e => e.Theme == filter.Theme.Value);
        }

        if (filter.Difficulty.HasValue)
        {
            query = query.Where(e => e.Difficulty == filter.Difficulty.Value);
        }

        if (filter.CollectionId.HasValue)
        {
            query = query.Where(e => e.CollectionId == filter.CollectionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(e => e.Title.Contains(term) || (e.Source != null && e.Source.Contains(term)));
        }

        if (filter.UnsolvedByUserId is { } unsolvedFor)
        {
            query = query.Where(e => !context.ExerciseAttempts
                .Any(a => a.ExerciseId == e.Id && a.UserId == unsolvedFor && a.IsCorrect));
        }

        query = filter.Sort switch
        {
            "difficulte" => query.OrderBy(e => e.Difficulty).ThenByDescending(e => e.Id),
            "populaire" => query.OrderByDescending(e => e.AttemptCount).ThenByDescending(e => e.Id),
            "reussite" => query
                .OrderByDescending(e => e.AttemptCount == 0 ? 0 : e.SuccessCount * 100 / e.AttemptCount)
                .ThenByDescending(e => e.Id),
            _ => query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
        };

        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = Math.Clamp(filter.PageSize, 1, PagedList<ExerciseCard>.MaxPageSize);

        var total = await query.CountAsync(ct);

        var rows = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Fen,
                e.Theme,
                e.Difficulty,
                e.AttemptCount,
                e.SuccessCount,
                e.IsPublic,
                e.IsPublished,
                Solved = currentUserId != null && context.ExerciseAttempts
                    .Any(a => a.ExerciseId == e.Id && a.UserId == currentUserId && a.IsCorrect)
            })
            .ToListAsync(ct);

        var cards = rows
            .Select(r => new ExerciseCard(
                r.Id,
                r.Title,
                r.Fen,
                r.Theme,
                r.Difficulty,
                Fen.TryParse(r.Fen, out var parsed, out _) && parsed!.WhiteToMove,
                r.AttemptCount == 0 ? 0 : (int)Math.Round(r.SuccessCount * 100.0 / r.AttemptCount),
                r.AttemptCount,
                r.IsPublic,
                r.IsPublished,
                r.Solved))
            .ToList();

        return new PagedList<ExerciseCard>(cards, total, page, size);
    }

    public Task<Exercise?> GetByIdAsync(int id, bool includeUnpublished = false, CancellationToken ct = default)
    {
        var query = context.Exercises
            .Include(e => e.Collection)
            .Where(e => e.Id == id && !e.IsDeleted);

        if (!includeUnpublished)
        {
            query = query.Where(e => e.IsPublished);
        }

        return query.FirstOrDefaultAsync(ct);
    }

    /// <summary>Tentatives d'un membre sur un exercice donné, la plus récente d'abord.</summary>
    public Task<List<ExerciseAttempt>> GetAttemptsAsync(int exerciseId, int userId, CancellationToken ct = default) =>
        context.ExerciseAttempts
            .AsNoTracking()
            .Where(a => a.ExerciseId == exerciseId && a.UserId == userId)
            .OrderByDescending(a => a.AttemptedAt)
            .Take(10)
            .ToListAsync(ct);

    /// <summary>
    /// Enregistre une tentative, met à jour les compteurs de l'exercice et la
    /// progression du membre, puis attribue les badges éventuellement débloqués.
    /// </summary>
    public async Task<Result<ExerciseCorrection>> SubmitAsync(
        ExerciseSubmission submission,
        CancellationToken ct = default)
    {
        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == submission.ExerciseId && !e.IsDeleted, ct);

        if (exercise is null)
        {
            return Result<ExerciseCorrection>.Failure("Exercice introuvable.", "NOTFOUND_001");
        }

        var expected = MoveSequence.Parse(exercise.Solution);
        var answer = MoveSequence.Parse(submission.Moves);
        var isCorrect = answer.Matches(expected);
        var prefix = answer.CommonPrefixLength(expected);

        var now = clock.UtcNow;
        var today = clock.Today;

        var attempt = new ExerciseAttempt
        {
            ExerciseId = exercise.Id,
            UserId = submission.UserId,
            SubmittedMoves = submission.Moves,
            IsCorrect = isCorrect,
            TimeSpentSeconds = Math.Clamp(submission.TimeSpentSeconds, 0, 3600),
            HintsUsed = Math.Clamp(submission.HintsUsed, 0, 3),
            CorrectPrefixLength = prefix,
            AttemptedAt = now,
            FromDailyEmail = submission.FromDailyEmail
        };

        context.ExerciseAttempts.Add(attempt);
        exercise.RegisterAttempt(isCorrect, attempt.TimeSpentSeconds);

        var progress = await context.UserProgress
            .FirstOrDefaultAsync(p => p.UserId == submission.UserId, ct);

        if (progress is null)
        {
            progress = new UserProgress { UserId = submission.UserId };
            context.UserProgress.Add(progress);
        }

        progress.Register(isCorrect, attempt.TimeSpentSeconds, attempt.HintsUsed, today, now);

        if (isCorrect && submission.FromDailyEmail)
        {
            var daily = await context.DailyExercises
                .FirstOrDefaultAsync(d => d.ScheduledOn == today && d.ExerciseId == exercise.Id, ct);

            if (daily is not null)
            {
                daily.SolvedCount++;
            }
        }

        await context.SaveChangesAsync(ct);

        var badges = isCorrect
            ? await AwardBadgesAsync(submission.UserId, progress, ct)
            : Array.Empty<string>();

        logger.LogDebug(
            "Tentative sur l'exercice {ExerciseId} par {UserId} : {Result}.",
            exercise.Id,
            submission.UserId,
            isCorrect ? "réussie" : "échouée");

        return Result<ExerciseCorrection>.Success(new ExerciseCorrection(
            isCorrect,
            prefix,
            expected.ToNumberedNotation(),
            exercise.Explanation,
            progress.CurrentStreak,
            badges));
    }

    public async Task<ProgressSummary> GetProgressAsync(int userId, CancellationToken ct = default)
    {
        var progress = await context.UserProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (progress is null)
        {
            return new ProgressSummary(0, 0, 0, 0, 0, 0, null);
        }

        return new ProgressSummary(
            progress.TotalAttempts,
            progress.CorrectAttempts,
            progress.SuccessRate,
            progress.CurrentStreak,
            progress.LongestStreak,
            progress.AverageTimeSeconds,
            progress.LastSolvedOn);
    }

    /// <summary>
    /// Exercice du jour pour une date donnée. Le crée s'il n'existe pas encore,
    /// en respectant le délai de non-répétition de 30 jours (BR-08).
    /// </summary>
    public async Task<DailyExercise?> GetOrCreateDailyAsync(DateOnly date, CancellationToken ct = default)
    {
        var existing = await context.DailyExercises
            .Include(d => d.Exercise)
            .FirstOrDefaultAsync(d => d.ScheduledOn == date, ct);

        if (existing is not null)
        {
            return existing;
        }

        var cutoff = date.AddDays(-30);

        var candidate = await context.Exercises
            .Where(e => e.IsPublished
                        && !e.IsDeleted
                        && (e.LastUsedAsDailyOn == null || e.LastUsedAsDailyOn < cutoff))
            .OrderBy(e => e.LastUsedAsDailyOn == null ? 0 : 1)
            .ThenBy(e => e.AttemptCount)
            .ThenBy(e => e.Id)
            .FirstOrDefaultAsync(ct);

        if (candidate is null)
        {
            logger.LogWarning(
                "Aucun exercice éligible pour l'exercice du jour du {Date:yyyy-MM-dd}.",
                date.ToDateTime(TimeOnly.MinValue));
            return null;
        }

        var daily = new DailyExercise
        {
            ScheduledOn = date,
            ExerciseId = candidate.Id,
            CreatedAt = clock.UtcNow
        };

        candidate.LastUsedAsDailyOn = date;
        context.DailyExercises.Add(daily);
        await context.SaveChangesAsync(ct);

        daily.Exercise = candidate;
        return daily;
    }

    public Task<DailyExercise?> GetDailyAsync(DateOnly date, CancellationToken ct = default) =>
        context.DailyExercises
            .Include(d => d.Exercise)
            .FirstOrDefaultAsync(d => d.ScheduledOn == date, ct);

    /// <summary>Planifie manuellement un exercice pour une date donnée.</summary>
    public async Task<Result> ScheduleDailyAsync(
        DateOnly date,
        int exerciseId,
        int scheduledById,
        CancellationToken ct = default)
    {
        var exercise = await context.Exercises
            .FirstOrDefaultAsync(e => e.Id == exerciseId && e.IsPublished && !e.IsDeleted, ct);

        if (exercise is null)
        {
            return Result.Failure("Exercice introuvable ou non publié.", "NOTFOUND_001");
        }

        var existing = await context.DailyExercises.FirstOrDefaultAsync(d => d.ScheduledOn == date, ct);

        if (existing is not null && existing.IsDispatched)
        {
            return Result.Failure("L'exercice de cette date a déjà été envoyé.", "CONFLICT_003");
        }

        if (existing is null)
        {
            context.DailyExercises.Add(new DailyExercise
            {
                ScheduledOn = date,
                ExerciseId = exerciseId,
                IsManualSelection = true,
                CreatedAt = clock.UtcNow,
                CreatedById = scheduledById
            });
        }
        else
        {
            existing.ExerciseId = exerciseId;
            existing.IsManualSelection = true;
            existing.UpdatedAt = clock.UtcNow;
            existing.UpdatedById = scheduledById;
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Attribue les badges dont le seuil vient d'être franchi.</summary>
    public async Task<IReadOnlyList<string>> AwardBadgesAsync(
        int userId,
        UserProgress progress,
        CancellationToken ct = default)
    {
        var badges = await context.Badges
            .Where(b => b.IsActive
                        && (b.Criterion == BadgeCriterion.ExercicesResolus
                            || b.Criterion == BadgeCriterion.SerieQuotidienne))
            .ToListAsync(ct);

        if (badges.Count == 0)
        {
            return Array.Empty<string>();
        }

        var owned = await context.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BadgeId)
            .ToListAsync(ct);

        var earned = new List<string>();
        var now = clock.UtcNow;

        foreach (var badge in badges)
        {
            if (owned.Contains(badge.Id))
            {
                continue;
            }

            var value = badge.Criterion switch
            {
                BadgeCriterion.ExercicesResolus => progress.CorrectAttempts,
                BadgeCriterion.SerieQuotidienne => progress.CurrentStreak,
                _ => 0
            };

            if (value < badge.Threshold)
            {
                continue;
            }

            context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeId = badge.Id,
                EarnedAt = now
            });

            earned.Add(badge.Name);
        }

        if (earned.Count > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        return earned;
    }
}
