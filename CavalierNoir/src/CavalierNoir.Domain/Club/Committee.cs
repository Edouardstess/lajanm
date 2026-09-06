using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Club;

/// <summary>Bureau exécutif élu pour un mandat donné.</summary>
public class Committee : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public DateRange Mandate { get; set; } = DateRange.OneYearFrom(DateOnly.FromDateTime(DateTime.UtcNow));

    public string? Description { get; set; }

    public bool IsCurrent { get; set; }

    public ICollection<CommitteeMember> Members { get; set; } = new List<CommitteeMember>();
}
