using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Memberships;

/// <summary>
/// Formule d'adhésion proposée par le club (plein tarif, réduit, famille, soutien,
/// bienfaiteur). Le tarif et la durée sont paramétrables par le trésorier.
/// </summary>
public class MembershipType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Money Price { get; set; } = Money.Zero();

    /// <summary>Durée de validité en jours (365 par défaut, BR-03).</summary>
    public int DurationDays { get; set; } = 365;

    /// <summary>Âge minimum requis pour souscrire, le cas échéant.</summary>
    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }

    /// <summary>Un justificatif (étudiant, scolaire…) est exigé.</summary>
    public bool RequiresProof { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();

    public ICollection<MembershipApplication> Applications { get; set; } = new List<MembershipApplication>();
}
