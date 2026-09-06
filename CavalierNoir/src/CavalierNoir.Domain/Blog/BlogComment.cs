using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Blog;

/// <summary>
/// Commentaire d'article, éventuellement imbriqué sous un commentaire parent.
/// </summary>
public class BlogComment : Entity
{
    /// <summary>BR-09 : masquage automatique au-delà de trois signalements.</summary>
    public const int ReportThreshold = 3;

    public int BlogPostId { get; set; }

    public BlogPost BlogPost { get; set; } = null!;

    public int AuthorId { get; set; }

    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    public BlogComment? Parent { get; set; }

    public ICollection<BlogComment> Replies { get; set; } = new List<BlogComment>();

    public CommentStatus Status { get; set; } = CommentStatus.EnAttente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int ReportCount { get; set; }

    public int LikeCount { get; set; }

    public int? ModeratedById { get; set; }

    public DateTime? ModeratedAt { get; set; }

    public string? IpAddress { get; set; }

    public ICollection<CommentReport> Reports { get; set; } = new List<CommentReport>();

    public void Approve(int moderatorId, DateTime when)
    {
        Status = CommentStatus.Approuve;
        ModeratedById = moderatorId;
        ModeratedAt = when;
        UpdatedAt = when;
    }

    public void Hide(int moderatorId, DateTime when)
    {
        Status = CommentStatus.Masque;
        ModeratedById = moderatorId;
        ModeratedAt = when;
        UpdatedAt = when;
    }

    /// <summary>Enregistre un signalement et applique BR-09.</summary>
    public bool Report(DateTime when)
    {
        ReportCount++;
        UpdatedAt = when;

        if (ReportCount >= ReportThreshold && Status != CommentStatus.Masque)
        {
            Status = CommentStatus.Masque;
            return true;
        }

        return false;
    }
}
