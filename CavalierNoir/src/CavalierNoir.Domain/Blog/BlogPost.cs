using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Blog;

/// <summary>
/// Article du blog. Racine de l'agrégat Blog : commentaires et étiquettes
/// n'existent que rattachés à un article.
/// </summary>
public class BlogPost : AuditableEntity, IAggregateRoot, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? FeaturedImageUrl { get; set; }

    public string? FeaturedImageAlt { get; set; }

    public int AuthorId { get; set; }

    public ApplicationUser Author { get; set; } = null!;

    public int? CategoryId { get; set; }

    public BlogCategory? Category { get; set; }

    public BlogStatus Status { get; set; } = BlogStatus.Brouillon;

    public DateTime? PublishedAt { get; set; }

    /// <summary>Date de publication programmée (workflow éditorial).</summary>
    public DateTime? ScheduledFor { get; set; }

    public int? ReviewedById { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewComment { get; set; }

    public int ViewCount { get; set; }

    public bool AllowComments { get; set; } = true;

    public bool IsFeatured { get; set; }

    // Référencement naturel
    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }

    public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();

    public ICollection<BlogPostTag> PostTags { get; set; } = new List<BlogPostTag>();

    // --- Comportement métier ---

    public bool IsVisible(DateTime now) =>
        !IsDeleted && Status == BlogStatus.Publie && PublishedAt.HasValue && PublishedAt.Value <= now;

    /// <summary>Temps de lecture estimé, à 200 mots par minute.</summary>
    public int ReadingTimeMinutes
    {
        get
        {
            var words = Content.Split(
                new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, (int)Math.Ceiling(words / 200.0));
        }
    }

    public int ApprovedCommentCount => Comments.Count(c => c.Status == CommentStatus.Approuve);

    public void SubmitForReview(DateTime when)
    {
        if (Status != BlogStatus.Brouillon)
        {
            throw new DomainException("Seul un brouillon peut être soumis à relecture.");
        }

        Status = BlogStatus.EnRelecture;
        UpdatedAt = when;
    }

    public void Approve(int reviewerId, DateTime when, DateTime? scheduledFor = null)
    {
        if (Status is not (BlogStatus.EnRelecture or BlogStatus.Brouillon))
        {
            throw new DomainException("Seul un article en relecture peut être validé.");
        }

        ReviewedById = reviewerId;
        ReviewedAt = when;

        if (scheduledFor.HasValue && scheduledFor.Value > when)
        {
            Status = BlogStatus.Programme;
            ScheduledFor = scheduledFor;
        }
        else
        {
            Publish(when);
        }

        UpdatedAt = when;
    }

    public void Publish(DateTime when)
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Content))
        {
            throw new DomainException("Un article publié doit avoir un titre et un contenu.");
        }

        Status = BlogStatus.Publie;
        PublishedAt ??= when;
        ScheduledFor = null;
        UpdatedAt = when;
    }

    public void Unpublish(DateTime when)
    {
        Status = BlogStatus.Archive;
        UpdatedAt = when;
    }

    public void RejectReview(int reviewerId, string comment, DateTime when)
    {
        Status = BlogStatus.Brouillon;
        ReviewedById = reviewerId;
        ReviewedAt = when;
        ReviewComment = comment;
        UpdatedAt = when;
    }
}
