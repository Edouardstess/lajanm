using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Communication;

/// <summary>Sujet de discussion ouvert dans un sous-forum.</summary>
public class ForumTopic : AuditableEntity, ISoftDeletable
{
    public int ForumCategoryId { get; set; }

    public ForumCategory ForumCategory { get; set; } = null!;

    public int AuthorId { get; set; }

    public ApplicationUser Author { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public TopicStatus Status { get; set; } = TopicStatus.Ouvert;

    public bool IsPinned { get; set; }

    public int ViewCount { get; set; }

    public int ReplyCount { get; set; }

    public DateTime? LastPostAt { get; set; }

    public int? LastPostAuthorId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }

    public ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();

    public bool CanReply => Status == TopicStatus.Ouvert && !IsDeleted;
}
