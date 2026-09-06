using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Club;

/// <summary>Partenaire ou sponsor du club.</summary>
public class Partner : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string? Website { get; set; }

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public PartnerLevel Level { get; set; } = PartnerLevel.Soutien;

    public Money AnnualContribution { get; set; } = Money.Zero();

    public DateOnly? ContractStart { get; set; }

    public DateOnly? ContractEnd { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }
}
