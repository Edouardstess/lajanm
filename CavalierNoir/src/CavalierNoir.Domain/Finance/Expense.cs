using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Finance;

/// <summary>
/// Dépense engagée par le club. La séparation des tâches impose que
/// <see cref="ApprovedById"/> diffère de <see cref="AuditableEntity.CreatedById"/>.
/// </summary>
public class Expense : AuditableEntity
{
    public ExpenseCategory Category { get; set; } = ExpenseCategory.Autre;

    public string Description { get; set; } = string.Empty;

    public Money Amount { get; set; } = Money.Zero();

    public DateOnly IncurredOn { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string? Supplier { get; set; }

    public string? ReceiptUrl { get; set; }

    public int? ApprovedById { get; set; }

    public DateTime? ApprovedAt { get; set; }

    /// <summary>Rattachement facultatif à un tournoi ou à un événement.</summary>
    public int? TournamentId { get; set; }

    public bool IsApproved => ApprovedById.HasValue;

    public void Approve(int approverId, DateTime when)
    {
        if (CreatedById.HasValue && CreatedById.Value == approverId)
        {
            throw new DomainException(
                "SoD-01",
                "Séparation des tâches : une dépense ne peut pas être approuvée par la personne qui l'a saisie.");
        }

        ApprovedById = approverId;
        ApprovedAt = when;
        UpdatedAt = when;
    }
}
