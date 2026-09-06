using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Memberships;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Finance;

/// <summary>
/// Encaissement. Aucune donnée de carte n'est stockée : seule la référence de
/// transaction du prestataire est conservée (conformité PCI DSS – SAQ A).
/// </summary>
public class Payment : AuditableEntity, IAggregateRoot
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int? MembershipId { get; set; }

    public Membership? Membership { get; set; }

    public PaymentPurpose Purpose { get; set; } = PaymentPurpose.Cotisation;

    public Money Amount { get; set; } = Money.Zero();

    public PaymentMethod Method { get; set; } = PaymentMethod.Especes;

    public PaymentStatus Status { get; set; } = PaymentStatus.EnAttente;

    /// <summary>Référence renvoyée par le prestataire (Stripe, MonCash…).</summary>
    public string? TransactionId { get; set; }

    /// <summary>Clé d'idempotence : rejoue sans double encaissement.</summary>
    public string? IdempotencyKey { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? ReceiptNumber { get; set; }

    public string? ReceiptUrl { get; set; }

    /// <summary>Agent ayant saisi un paiement en espèces (séparation des tâches).</summary>
    public int? RecordedById { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? RefundReason { get; set; }

    public void MarkAsPaid(DateTime when, string? transactionId = null)
    {
        if (Status == PaymentStatus.Paye)
        {
            return; // idempotent : un webhook peut être rejoué.
        }

        if (Status is PaymentStatus.Rembourse or PaymentStatus.Annule)
        {
            throw new DomainException("Un paiement remboursé ou annulé ne peut pas être encaissé.");
        }

        Status = PaymentStatus.Paye;
        PaidAt = when;
        TransactionId ??= transactionId;
        UpdatedAt = when;
    }

    public void Fail(string reason, DateTime when)
    {
        Status = PaymentStatus.Echoue;
        Notes = reason;
        UpdatedAt = when;
    }

    public void Refund(string reason, DateTime when)
    {
        if (Status != PaymentStatus.Paye)
        {
            throw new DomainException("Seul un paiement encaissé peut être remboursé.");
        }

        Status = PaymentStatus.Rembourse;
        RefundedAt = when;
        RefundReason = reason;
        UpdatedAt = when;
    }

    public static string BuildReceiptNumber(int year, int sequence) => $"REC-{year}-{sequence:D5}";
}
