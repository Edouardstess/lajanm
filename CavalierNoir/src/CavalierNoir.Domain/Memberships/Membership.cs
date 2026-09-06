using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Finance;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Memberships;

/// <summary>
/// Adhésion effective d'un membre sur une période donnée. Racine d'agrégat :
/// les paiements rattachés ne vivent que par elle.
/// </summary>
public class Membership : AuditableEntity, IAggregateRoot
{
    /// <summary>BR-04 : délai de grâce avant suspension automatique.</summary>
    public const int GracePeriodDays = 15;

    /// <summary>Fenêtre de relance avant échéance.</summary>
    public const int RenewalReminderDays = 30;

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int MembershipTypeId { get; set; }

    public MembershipType MembershipType { get; set; } = null!;

    /// <summary>Période de validité (BR-03).</summary>
    public DateRange Period { get; set; } = DateRange.OneYearFrom(DateOnly.FromDateTime(DateTime.UtcNow));

    public Money Amount { get; set; } = Money.Zero();

    public MembershipStatus Status { get; set; } = MembershipStatus.EnAttentePaiement;

    public DateTime? ActivatedAt { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTime? RenewalReminderSentAt { get; set; }

    /// <summary>Numéro d'adhérent lisible, par exemple « CN-2026-0042 ».</summary>
    public string MemberNumber { get; set; } = string.Empty;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // --- Comportement métier ---

    public bool IsCurrentlyActive(DateOnly today) =>
        Status == MembershipStatus.Active && Period.Contains(today);

    public DateOnly GraceEndDate => Period.End.AddDays(GracePeriodDays);

    public bool IsInGracePeriod(DateOnly today) =>
        today > Period.End && today <= GraceEndDate;

    public int DaysUntilExpiry(DateOnly today) => Period.End.DayNumber - today.DayNumber;

    public bool NeedsRenewalReminder(DateOnly today) =>
        Status == MembershipStatus.Active
        && RenewalReminderSentAt is null
        && DaysUntilExpiry(today) is <= RenewalReminderDays and >= 0;

    /// <summary>Active l'adhésion après encaissement de la cotisation.</summary>
    public void Activate(DateTime when, DateOnly? startingOn = null)
    {
        if (Status is MembershipStatus.Resiliee)
        {
            throw new DomainException("BR-03", "Une adhésion résiliée ne peut pas être réactivée ; créez-en une nouvelle.");
        }

        var start = startingOn ?? DateOnly.FromDateTime(when);
        var duration = MembershipType is { DurationDays: > 0 } type ? type.DurationDays : 365;
        Period = new DateRange(start, start.AddDays(duration - 1));
        Status = MembershipStatus.Active;
        ActivatedAt = when;
        SuspendedAt = null;
        SuspensionReason = null;
        UpdatedAt = when;
    }

    /// <summary>
    /// Recalcule l'état en fonction de la date du jour : expiration, délai de grâce,
    /// suspension automatique (BR-04). Appelée par la tâche de fond quotidienne.
    /// </summary>
    public MembershipStatus Refresh(DateOnly today)
    {
        if (Status is MembershipStatus.Suspendue or MembershipStatus.Resiliee or MembershipStatus.EnAttentePaiement)
        {
            return Status;
        }

        if (Period.Contains(today))
        {
            Status = MembershipStatus.Active;
        }
        else if (IsInGracePeriod(today))
        {
            Status = MembershipStatus.DelaiDeGrace;
        }
        else if (today > GraceEndDate)
        {
            Status = MembershipStatus.Expiree;
        }

        return Status;
    }

    public void Suspend(string reason, DateTime when)
    {
        Status = MembershipStatus.Suspendue;
        SuspendedAt = when;
        SuspensionReason = reason;
        UpdatedAt = when;
    }

    public void Reactivate(DateTime when)
    {
        if (Status != MembershipStatus.Suspendue)
        {
            throw new DomainException("Seule une adhésion suspendue peut être réactivée.");
        }

        Status = MembershipStatus.Active;
        SuspendedAt = null;
        SuspensionReason = null;
        UpdatedAt = when;
    }

    /// <summary>
    /// Prolonge l'adhésion d'une nouvelle période. Le renouvellement anticipé
    /// démarre à la fin de la période courante, sans perte de jours.
    /// </summary>
    public void Renew(int durationDays, Money amount, DateTime when)
    {
        var today = DateOnly.FromDateTime(when);
        var start = Period.End >= today ? Period.End.AddDays(1) : today;
        Period = new DateRange(start, start.AddDays(durationDays - 1));
        Amount = amount;
        Status = MembershipStatus.Active;
        RenewalReminderSentAt = null;
        UpdatedAt = when;
    }

    public static string BuildMemberNumber(int year, int sequence) => $"CN-{year}-{sequence:D4}";
}
