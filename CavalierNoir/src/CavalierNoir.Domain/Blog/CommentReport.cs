using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Blog;

/// <summary>Signalement d'un commentaire par un membre.</summary>
public class CommentReport : Entity
{
    public int BlogCommentId { get; set; }

    public BlogComment BlogComment { get; set; } = null!;

    public int ReporterId { get; set; }

    public ApplicationUser Reporter { get; set; } = null!;

    public ReportReason Reason { get; set; } = ReportReason.Autre;

    public string? Details { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public bool IsHandled { get; set; }

    public int? HandledById { get; set; }

    public DateTime? HandledAt { get; set; }
}
