using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Learning;

/// <summary>Attribution d'un badge à un membre.</summary>
public class UserBadge : Entity
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int BadgeId { get; set; }

    public Badge Badge { get; set; } = null!;

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Renseigné lorsque le badge a été attribué manuellement.</summary>
    public int? AwardedById { get; set; }

    public string? Comment { get; set; }
}
