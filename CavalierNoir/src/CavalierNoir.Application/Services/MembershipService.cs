using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Finance;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Memberships;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Cycle de vie complet d'une adhésion : candidature, instruction par le
/// secrétariat, évaluation pédagogique, encaissement, activation, relance et
/// suspension automatique (§2.2 du DAL, règles BR-02 à BR-04).
/// </summary>
public sealed class MembershipService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    IUserAccountService accounts,
    INotificationService notifications,
    ILogger<MembershipService> logger)
{
    public async Task<IReadOnlyList<MembershipTypeDto>> GetActiveTypesAsync(CancellationToken ct = default) =>
        await context.MembershipTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new MembershipTypeDto(
                t.Id,
                t.Name,
                t.Code,
                t.Description,
                t.Price.Amount,
                t.Price.Currency,
                t.DurationDays,
                t.RequiresProof))
            .ToListAsync(ct);

    /// <summary>Adhésion en cours d'un membre, éventuellement expirée.</summary>
    public async Task<Membership?> GetCurrentMembershipAsync(int userId, CancellationToken ct = default) =>
        await context.Memberships
            .Include(m => m.MembershipType)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Period.End)
            .FirstOrDefaultAsync(ct);

    public async Task<MembershipSummary?> GetSummaryAsync(int userId, CancellationToken ct = default)
    {
        var membership = await GetCurrentMembershipAsync(userId, ct);
        if (membership is null)
        {
            return null;
        }

        var today = clock.Today;
        return new MembershipSummary(
            membership.Id,
            membership.MemberNumber,
            membership.MembershipType?.Name ?? "Adhésion",
            membership.Status,
            membership.Period.Start,
            membership.Period.End,
            membership.Amount.Amount,
            membership.Amount.Currency,
            membership.DaysUntilExpiry(today),
            membership.NeedsRenewalReminder(today) || membership.Status == MembershipStatus.DelaiDeGrace);
    }

    /// <summary>
    /// Enregistre et soumet une demande d'adhésion. Une seule demande peut être
    /// en cours d'instruction par candidat.
    /// </summary>
    public async Task<Result<int>> SubmitApplicationAsync(
        MembershipApplicationRequest request,
        CancellationToken ct = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Result<int>.Failure("Utilisateur introuvable.", "NOTFOUND_001");
        }

        var type = await context.MembershipTypes
            .FirstOrDefaultAsync(t => t.Id == request.MembershipTypeId && t.IsActive, ct);
        if (type is null)
        {
            return Result<int>.Failure("Formule d'adhésion introuvable ou désactivée.", "NOTFOUND_002");
        }

        var today = clock.Today;
        var age = user.Age(today);

        // BR-02 : âge minimum de 6 ans, sauf autorisation parentale explicite.
        if (age is < 6)
        {
            return Result<int>.Failure("L'adhésion est ouverte à partir de 6 ans.", "BR-02");
        }

        if (age is < 18 && !request.ParentalConsent)
        {
            return Result<int>.Failure(
                "Une autorisation parentale est requise pour les candidats mineurs.",
                "BR-02");
        }

        if (type.MinimumAge is { } min && age is not null && age < min)
        {
            return Result<int>.Failure($"Cette formule est réservée aux personnes de {min} ans et plus.", "BR-02");
        }

        if (type.MaximumAge is { } max && age is not null && age > max)
        {
            return Result<int>.Failure($"Cette formule est réservée aux personnes de moins de {max} ans.", "BR-02");
        }

        var alreadyPending = await context.MembershipApplications.AnyAsync(
            a => a.UserId == request.UserId
                 && (a.Status == ApplicationStatus.Soumise
                     || a.Status == ApplicationStatus.EnInstruction
                     || a.Status == ApplicationStatus.ComplementsDemandes),
            ct);

        if (alreadyPending)
        {
            return Result<int>.Failure("Une demande est déjà en cours d'instruction.", "CONFLICT_002");
        }

        // BR-07 : le classement déclaré est plafonné à 1600 sans justificatif.
        var declaredElo = Math.Clamp(request.DeclaredElo, EloRating.Minimum, 1600);

        var now = clock.UtcNow;
        var application = new MembershipApplication
        {
            UserId = request.UserId,
            MembershipTypeId = type.Id,
            DeclaredElo = declaredElo,
            Motivation = request.Motivation,
            SupportingDocuments = request.SupportingDocuments,
            ParentalConsent = request.ParentalConsent,
            ParentContact = request.ParentContact,
            CreatedAt = now,
            CreatedById = request.UserId
        };

        application.Submit(now);

        context.MembershipApplications.Add(application);
        await context.SaveChangesAsync(ct);

        await accounts.AddToRoleAsync(request.UserId, Roles.Candidat, ct);
        await notifications.NotifyRoleAsync(
            Roles.Secretaire,
            "Nouvelle demande d'adhésion",
            $"{user.FullName} a déposé une demande d'adhésion ({type.Name}).",
            $"/Admin/Adhesions/Details/{application.Id}",
            "person-plus",
            ct);

        logger.LogInformation(
            "Demande d'adhésion {ApplicationId} soumise par l'utilisateur {UserId}.",
            application.Id,
            request.UserId);

        return Result<int>.Success(application.Id);
    }

    /// <summary>Liste paginée des demandes, filtrée par statut, pour le back-office.</summary>
    public async Task<PagedList<MembershipApplicationSummary>> GetApplicationsAsync(
        ApplicationStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = context.MembershipApplications
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var projected = query
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .Select(a => new MembershipApplicationSummary(
                a.Id,
                a.UserId,
                a.User.FirstName + " " + a.User.LastName,
                a.User.Email,
                a.MembershipType.Name,
                a.Status,
                a.SubmittedAt,
                a.DeclaredElo,
                a.AssessedElo,
                a.ParentalConsent));

        return await PagedList<MembershipApplicationSummary>.CreateAsync(projected, page, pageSize, ct);
    }

    /// <summary>
    /// Approuve une demande : crée l'adhésion en attente de paiement, aligne le
    /// classement initial et notifie le candidat.
    /// </summary>
    public async Task<Result<int>> ApproveApplicationAsync(
        int applicationId,
        int reviewerId,
        int? assessedElo,
        CancellationToken ct = default)
    {
        var application = await context.MembershipApplications
            .Include(a => a.MembershipType)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application is null)
        {
            return Result<int>.Failure("Demande introuvable.", "NOTFOUND_003");
        }

        try
        {
            application.Approve(reviewerId, assessedElo, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Result<int>.Failure(ex.Message, ex.Code);
        }

        var today = clock.Today;
        var duration = application.MembershipType.DurationDays > 0
            ? application.MembershipType.DurationDays
            : 365;

        var membership = new Membership
        {
            UserId = application.UserId,
            MembershipTypeId = application.MembershipTypeId,
            Period = new DateRange(today, today.AddDays(duration - 1)),
            Amount = application.MembershipType.Price,
            Status = MembershipStatus.EnAttentePaiement,
            MemberNumber = await NextMemberNumberAsync(today.Year, ct),
            CreatedAt = clock.UtcNow,
            CreatedById = reviewerId
        };

        context.Memberships.Add(membership);
        await context.SaveChangesAsync(ct);

        application.ResultingMembershipId = membership.Id;
        application.User.Elo = application.AssessedElo ?? application.DeclaredElo;
        await context.SaveChangesAsync(ct);

        await notifications.NotifyAsync(
            application.UserId,
            "Votre demande d'adhésion est acceptée",
            $"Bienvenue ! Votre numéro d'adhérent est {membership.MemberNumber}. "
            + "Réglez votre cotisation pour activer votre compte.",
            "/Membre/Adhesion",
            "check-circle",
            1,
            NotificationChannel.InApp,
            ct);

        logger.LogInformation(
            "Demande {ApplicationId} approuvée par {ReviewerId} ; adhésion {MembershipId} créée.",
            applicationId,
            reviewerId,
            membership.Id);

        return Result<int>.Success(membership.Id);
    }

    public async Task<Result> RejectApplicationAsync(
        int applicationId,
        int reviewerId,
        string reason,
        CancellationToken ct = default)
    {
        var application = await context.MembershipApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application is null)
        {
            return Result.Failure("Demande introuvable.", "NOTFOUND_003");
        }

        try
        {
            application.Reject(reviewerId, reason, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, ex.Code);
        }

        await context.SaveChangesAsync(ct);

        await notifications.NotifyAsync(
            application.UserId,
            "Votre demande d'adhésion n'a pas été retenue",
            reason,
            "/Adherer",
            "x-circle",
            1,
            NotificationChannel.InApp,
            ct);

        return Result.Success();
    }

    public async Task<Result> RequestMoreInformationAsync(
        int applicationId,
        int reviewerId,
        string comment,
        CancellationToken ct = default)
    {
        var application = await context.MembershipApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (application is null)
        {
            return Result.Failure("Demande introuvable.", "NOTFOUND_003");
        }

        try
        {
            application.RequestMoreInformation(reviewerId, comment, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, ex.Code);
        }

        await context.SaveChangesAsync(ct);

        await notifications.NotifyAsync(
            application.UserId,
            "Complément demandé pour votre adhésion",
            comment,
            "/Membre/Adhesion",
            "exclamation-circle",
            2,
            NotificationChannel.InApp,
            ct);

        return Result.Success();
    }

    /// <summary>
    /// Enregistre le règlement d'une cotisation et active l'adhésion. Idempotent
    /// vis-à-vis d'un rejeu de webhook grâce à la clé d'idempotence.
    /// </summary>
    public async Task<Result<int>> RecordPaymentAsync(
        int membershipId,
        PaymentMethod method,
        decimal? amount,
        int? recordedById,
        string? transactionId,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        var membership = await context.Memberships
            .Include(m => m.MembershipType)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership is null)
        {
            return Result<int>.Failure("Adhésion introuvable.", "NOTFOUND_004");
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await context.Payments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);

            if (existing is not null)
            {
                return Result<int>.Success(existing.Id);
            }
        }

        // Séparation des tâches : un trésorier ne valide pas son propre paiement.
        if (recordedById is { } recorder && recorder == membership.UserId && method == PaymentMethod.Especes)
        {
            return Result<int>.Failure(
                "Un paiement en espèces doit être enregistré par un tiers habilité.",
                "SoD-03");
        }

        var now = clock.UtcNow;
        var money = amount.HasValue
            ? new Money(amount.Value, membership.Amount.Currency)
            : membership.Amount;

        var payment = new Payment
        {
            UserId = membership.UserId,
            MembershipId = membership.Id,
            Purpose = PaymentPurpose.Cotisation,
            Amount = money,
            Method = method,
            TransactionId = transactionId,
            IdempotencyKey = idempotencyKey,
            RecordedById = recordedById,
            ReceiptNumber = Payment.BuildReceiptNumber(
                clock.Today.Year,
                await NextReceiptSequenceAsync(clock.Today.Year, ct)),
            CreatedAt = now,
            CreatedById = recordedById
        };

        payment.MarkAsPaid(now, transactionId);
        context.Payments.Add(payment);

        membership.Activate(now);
        membership.Amount = money;

        await context.SaveChangesAsync(ct);

        await accounts.RemoveFromRoleAsync(membership.UserId, Roles.Candidat, ct);
        await accounts.AddToRoleAsync(membership.UserId, Roles.Membre, ct);

        await notifications.NotifyAsync(
            membership.UserId,
            "Cotisation enregistrée",
            $"Votre adhésion est active jusqu'au {membership.Period.End:dd/MM/yyyy}. "
            + $"Reçu n° {payment.ReceiptNumber}.",
            "/Membre/Adhesion",
            "receipt",
            1,
            NotificationChannel.InApp,
            ct);

        logger.LogInformation(
            "Paiement {PaymentId} enregistré pour l'adhésion {MembershipId} ({Method}).",
            payment.Id,
            membership.Id,
            method);

        return Result<int>.Success(payment.Id);
    }

    /// <summary>Renouvelle une adhésion pour une nouvelle période.</summary>
    public async Task<Result> RenewAsync(int membershipId, int? recordedById, CancellationToken ct = default)
    {
        var membership = await context.Memberships
            .Include(m => m.MembershipType)
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership is null)
        {
            return Result.Failure("Adhésion introuvable.", "NOTFOUND_004");
        }

        var duration = membership.MembershipType is { DurationDays: > 0 } type ? type.DurationDays : 365;
        var price = membership.MembershipType is { } priced ? priced.Price : membership.Amount;
        membership.Renew(duration, price, clock.UtcNow);
        membership.UpdatedById = recordedById;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Recalcule quotidiennement l'état des adhésions : passage en délai de grâce,
    /// expiration puis suspension (BR-04). Renvoie le nombre de lignes modifiées.
    /// </summary>
    public async Task<int> RefreshStatusesAsync(CancellationToken ct = default)
    {
        var today = clock.Today;
        var memberships = await context.Memberships
            .Where(m => m.Status == MembershipStatus.Active || m.Status == MembershipStatus.DelaiDeGrace)
            .ToListAsync(ct);

        var changed = 0;
        foreach (var membership in memberships)
        {
            var previous = membership.Status;
            var current = membership.Refresh(today);

            if (previous == current)
            {
                continue;
            }

            changed++;

            if (current == MembershipStatus.Expiree)
            {
                await accounts.RemoveFromRoleAsync(membership.UserId, Roles.Membre, ct);
                await notifications.NotifyAsync(
                    membership.UserId,
                    "Votre adhésion a expiré",
                    "Renouvelez votre cotisation pour retrouver l'accès à l'espace membre.",
                    "/Membre/Adhesion",
                    "exclamation-triangle",
                    1,
                    NotificationChannel.InApp,
                    ct);
            }
            else if (current == MembershipStatus.DelaiDeGrace)
            {
                await notifications.NotifyAsync(
                    membership.UserId,
                    "Votre adhésion arrive à échéance",
                    $"Vous disposez d'un délai de grâce jusqu'au {membership.GraceEndDate:dd/MM/yyyy}.",
                    "/Membre/Adhesion",
                    "clock-history",
                    1,
                    NotificationChannel.InApp,
                    ct);
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation("{Count} adhésion(s) ont changé de statut.", changed);
        }

        return changed;
    }

    /// <summary>Adhésions à relancer (échéance dans 30 jours), pour la tâche de fond.</summary>
    public async Task<IReadOnlyList<Membership>> GetMembershipsToRemindAsync(CancellationToken ct = default)
    {
        var today = clock.Today;
        var limit = today.AddDays(Membership.RenewalReminderDays);

        return await context.Memberships
            .Include(m => m.User)
            .Include(m => m.MembershipType)
            .Where(m => m.Status == MembershipStatus.Active
                        && m.RenewalReminderSentAt == null
                        && m.Period.End <= limit
                        && m.Period.End >= today)
            .ToListAsync(ct);
    }

    private async Task<string> NextMemberNumberAsync(int year, CancellationToken ct)
    {
        var prefix = $"CN-{year}-";
        var count = await context.Memberships.CountAsync(m => m.MemberNumber.StartsWith(prefix), ct);
        return Membership.BuildMemberNumber(year, count + 1);
    }

    private async Task<int> NextReceiptSequenceAsync(int year, CancellationToken ct)
    {
        var prefix = $"REC-{year}-";
        var count = await context.Payments.CountAsync(
            p => p.ReceiptNumber != null && p.ReceiptNumber.StartsWith(prefix),
            ct);
        return count + 1;
    }
}
