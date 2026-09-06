using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Tournaments;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Services;

/// <summary>Calendrier du club : événements, inscriptions et pointage des présences.</summary>
public sealed class EventService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    INotificationService notifications)
{
    public Task<List<ClubEvent>> GetUpcomingAsync(int take = 6, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        return context.ClubEvents
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.IsPublished && e.EndDate >= now)
            .OrderBy(e => e.StartDate)
            .Take(take)
            .ToListAsync(ct);
    }

    /// <summary>Événements d'un mois donné, pour l'affichage du calendrier.</summary>
    public Task<List<ClubEvent>> GetForMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        return context.ClubEvents
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.IsPublished && e.StartDate < end && e.EndDate >= start)
            .OrderBy(e => e.StartDate)
            .ToListAsync(ct);
    }

    public Task<ClubEvent?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        context.ClubEvents
            .Include(e => e.Registrations)
            .Include(e => e.Tournament)
            .FirstOrDefaultAsync(e => e.Slug == slug && !e.IsDeleted, ct);

    /// <summary>Inscrit un membre à un événement, ou le place en liste d'attente.</summary>
    public async Task<Result<string>> RegisterAsync(int eventId, int userId, CancellationToken ct = default)
    {
        var clubEvent = await context.ClubEvents
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId && !e.IsDeleted, ct);

        if (clubEvent is null)
        {
            return Result<string>.Failure("Événement introuvable.", "NOTFOUND_014");
        }

        if (!clubEvent.IsRegistrationOpen)
        {
            return Result<string>.Failure("Les inscriptions à cet événement sont fermées.");
        }

        if (clubEvent.Registrations.Any(r => r.UserId == userId && r.Status != RegistrationStatus.Annulee))
        {
            return Result<string>.Failure("Vous êtes déjà inscrit à cet événement.", "CONFLICT_006");
        }

        var now = clock.UtcNow;
        var registration = new EventRegistration
        {
            ClubEventId = eventId,
            UserId = userId,
            RegisteredAt = now,
            CheckInToken = Guid.NewGuid().ToString("N")[..12],
            Status = clubEvent.IsFull ? RegistrationStatus.ListeAttente : RegistrationStatus.Confirmee,
            CreatedAt = now,
            CreatedById = userId
        };

        context.EventRegistrations.Add(registration);
        await context.SaveChangesAsync(ct);

        return Result<string>.Success(
            registration.Status == RegistrationStatus.ListeAttente
                ? "L'événement est complet : vous êtes en liste d'attente."
                : "Votre inscription est confirmée.");
    }

    public async Task<Result> CancelRegistrationAsync(int eventId, int userId, CancellationToken ct = default)
    {
        var registration = await context.EventRegistrations
            .FirstOrDefaultAsync(r => r.ClubEventId == eventId && r.UserId == userId, ct);

        if (registration is null)
        {
            return Result.Failure("Inscription introuvable.", "NOTFOUND_006");
        }

        var wasConfirmed = registration.Status == RegistrationStatus.Confirmee;
        registration.Status = RegistrationStatus.Annulee;
        registration.UpdatedAt = clock.UtcNow;

        if (wasConfirmed)
        {
            var next = await context.EventRegistrations
                .Where(r => r.ClubEventId == eventId && r.Status == RegistrationStatus.ListeAttente)
                .OrderBy(r => r.RegisteredAt)
                .FirstOrDefaultAsync(ct);

            if (next is { UserId: { } promotedId })
            {
                next.Status = RegistrationStatus.Confirmee;
                await notifications.NotifyAsync(
                    promotedId,
                    "Une place s'est libérée",
                    "Votre inscription à l'événement est confirmée.",
                    $"/Evenements/{eventId}",
                    "calendar-check",
                    1,
                    NotificationChannel.InApp,
                    ct);
            }
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Pointage à l'entrée par jeton (QR code).</summary>
    public async Task<Result<string>> CheckInAsync(string token, CancellationToken ct = default)
    {
        var registration = await context.EventRegistrations
            .Include(r => r.ClubEvent)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.CheckInToken == token, ct);

        if (registration is null)
        {
            return Result<string>.Failure("Jeton de pointage inconnu.", "NOTFOUND_015");
        }

        if (registration.CheckedInAt.HasValue)
        {
            return Result<string>.Failure("Ce participant est déjà pointé.");
        }

        registration.CheckedInAt = clock.UtcNow;
        registration.Status = RegistrationStatus.Presente;
        await context.SaveChangesAsync(ct);

        var name = registration.User is { } user
            ? $"{user.FirstName} {user.LastName}".Trim()
            : registration.GuestName ?? "Participant";

        return Result<string>.Success($"{name} — présence enregistrée.");
    }
}
