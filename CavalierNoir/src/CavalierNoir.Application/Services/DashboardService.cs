using System.Globalization;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Agrège les indicateurs du back-office et de l'espace membre. Toutes les
/// requêtes sont projetées : aucune entité n'est chargée inutilement.
/// </summary>
public sealed class DashboardService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    ExerciseService exercises,
    MembershipService memberships,
    TournamentService tournaments,
    INotificationService notifications,
    IOptions<SiteOptions> siteOptions)
{
    private readonly SiteOptions _site = siteOptions.Value;

    public async Task<AdminDashboardStats> GetAdminStatsAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var today = clock.Today;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horizon = today.AddDays(Domain.Memberships.Membership.RenewalReminderDays);

        var totalUsers = await context.Users.CountAsync(ct);

        var activeMembers = await context.Memberships
            .CountAsync(m => m.Status == MembershipStatus.Active, ct);

        var pendingApplications = await context.MembershipApplications
            .CountAsync(
                a => a.Status == ApplicationStatus.Soumise || a.Status == ApplicationStatus.EnInstruction,
                ct);

        var expiring = await context.Memberships
            .CountAsync(
                m => m.Status == MembershipStatus.Active && m.Period.End <= horizon && m.Period.End >= today,
                ct);

        var publishedExercises = await context.Exercises
            .CountAsync(e => e.IsPublished && !e.IsDeleted, ct);

        var attemptsThisMonth = await context.ExerciseAttempts
            .CountAsync(a => a.AttemptedAt >= startOfMonth, ct);

        var upcomingEvents = await context.ClubEvents
            .CountAsync(e => !e.IsDeleted && e.IsPublished && e.StartDate >= now, ct);

        var runningTournaments = await context.Tournaments
            .CountAsync(t => !t.IsDeleted && t.Status == TournamentStatus.EnCours, ct);

        var pendingComments = await context.BlogComments
            .CountAsync(c => c.Status == CommentStatus.EnAttente, ct);

        var newMessages = await context.ContactMessages
            .CountAsync(m => m.Status == ContactMessageStatus.Nouveau, ct);

        var revenue = await context.Payments
            .Where(p => p.Status == PaymentStatus.Paye && p.PaidAt >= startOfYear)
            .SumAsync(p => (decimal?)p.Amount.Amount, ct) ?? 0m;

        var since = now.AddDays(-13).Date;
        var attemptsRaw = await context.ExerciseAttempts
            .Where(a => a.AttemptedAt >= since)
            .GroupBy(a => a.AttemptedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var attemptsPerDay = new List<ActivityPoint>(14);
        for (var i = 13; i >= 0; i--)
        {
            var day = now.Date.AddDays(-i);
            var match = attemptsRaw.FirstOrDefault(a => a.Day == day);
            attemptsPerDay.Add(new ActivityPoint(day.ToString("dd/MM", CultureInfo.GetCultureInfo("fr-FR")), match?.Count ?? 0));
        }

        var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
        var membersRaw = await context.Memberships
            .Where(m => m.CreatedAt >= twelveMonthsAgo)
            .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var newMembersPerMonth = new List<ActivityPoint>(12);
        for (var i = 11; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var match = membersRaw.FirstOrDefault(m => m.Year == month.Year && m.Month == month.Month);
            newMembersPerMonth.Add(new ActivityPoint(month.ToString("MMM yy", culture), match?.Count ?? 0));
        }

        return new AdminDashboardStats(
            totalUsers,
            activeMembers,
            pendingApplications,
            expiring,
            publishedExercises,
            attemptsThisMonth,
            upcomingEvents,
            runningTournaments,
            pendingComments,
            newMessages,
            revenue,
            _site.DefaultCurrency,
            attemptsPerDay,
            newMembersPerMonth);
    }

    public async Task<MemberDashboard> GetMemberDashboardAsync(int userId, CancellationToken ct = default)
    {
        var today = clock.Today;

        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Pseudonym,
                u.Elo
            })
            .FirstOrDefaultAsync(ct);

        var displayName = user is null
            ? "Membre"
            : !string.IsNullOrWhiteSpace(user.Pseudonym)
                ? user.Pseudonym!
                : $"{user.FirstName} {user.LastName}".Trim();

        var elo = user?.Elo ?? EloRating.DefaultRating;
        var level = elo switch
        {
            <= 1200 => "Débutant",
            <= 1600 => "Intermédiaire",
            <= 2000 => "Avancé",
            _ => "Expert"
        };

        var progress = await exercises.GetProgressAsync(userId, ct);
        var membership = await memberships.GetSummaryAsync(userId, ct);
        var daily = await exercises.GetDailyAsync(today, ct);

        ExerciseCard? dailyCard = null;
        var dailySolved = false;

        if (daily?.Exercise is { } exercise)
        {
            dailySolved = await context.ExerciseAttempts
                .AnyAsync(a => a.UserId == userId && a.ExerciseId == exercise.Id && a.IsCorrect, ct);

            dailyCard = new ExerciseCard(
                exercise.Id,
                exercise.Title,
                exercise.Fen,
                exercise.Theme,
                exercise.Difficulty,
                exercise.WhiteToMove,
                exercise.SuccessRate,
                exercise.AttemptCount,
                exercise.IsPublic,
                exercise.IsPublished,
                dailySolved);
        }

        var upcoming = await tournaments.GetUpcomingAsync(3, ct);

        var badges = await context.UserBadges
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.EarnedAt)
            .Take(6)
            .Select(b => b.Badge.Name)
            .ToListAsync(ct);

        var unread = await notifications.CountUnreadAsync(userId, ct);

        return new MemberDashboard(
            displayName,
            elo,
            level,
            progress,
            membership,
            dailyCard,
            dailySolved,
            upcoming,
            badges,
            unread);
    }

    /// <summary>Classement ELO public du club.</summary>
    public async Task<PagedList<StandingLine>> GetClubRankingAsync(
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Where(u => u.IsActive
                        && u.AnonymizedAt == null
                        && (u.Preference == null || u.Preference.ShowInPublicRanking))
            .OrderByDescending(u => u.Elo)
            .ThenBy(u => u.LastName)
            .Select(u => new StandingLine(
                0,
                u.Id,
                u.Pseudonym ?? (u.FirstName + " " + u.LastName),
                u.Elo,
                0m,
                0m,
                0m,
                0,
                0,
                0,
                0,
                null));

        var result = await PagedList<StandingLine>.CreateAsync(query, page, pageSize, ct);

        var ranked = result.Items
            .Select((line, index) => line with { Rank = result.FirstItemIndex + index })
            .ToList();

        return new PagedList<StandingLine>(ranked, result.TotalCount, result.PageNumber, result.PageSize);
    }
}
