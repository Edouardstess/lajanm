using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Club;

/// <summary>Affectation d'un membre à un poste du bureau.</summary>
public class CommitteeMember : Entity
{
    public int CommitteeId { get; set; }

    public Committee Committee { get; set; } = null!;

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public CommitteePosition Position { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Biography { get; set; }

    public string? PhotoUrl { get; set; }

    public int DisplayOrder { get; set; }
}
