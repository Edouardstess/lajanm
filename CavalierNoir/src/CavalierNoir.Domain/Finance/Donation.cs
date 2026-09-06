using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Finance;

/// <summary>Don ponctuel ou récurrent, avec émission d'un reçu.</summary>
public class Donation : AuditableEntity
{
    /// <summary>Nul pour un don anonyme ou d'un tiers non inscrit.</summary>
    public int? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public string DonorName { get; set; } = string.Empty;

    public string? DonorEmail { get; set; }

    public Money Amount { get; set; } = Money.Zero();

    public DateOnly DonatedOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string? Message { get; set; }

    public bool IsAnonymous { get; set; }

    public bool ReceiptIssued { get; set; }

    public string? ReceiptNumber { get; set; }

    public int? PaymentId { get; set; }

    public Payment? Payment { get; set; }
}
