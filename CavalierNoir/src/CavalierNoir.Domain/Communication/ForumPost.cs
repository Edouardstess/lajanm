using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Communication;

/// <summary>Message publié dans un sujet de forum.</summary>
public class ForumPost : Entity, ISoftDeletable
{
    public int ForumTopicId { get; set; }

    public ForumTopic ForumTopic { get; set; } = null!;

    public int AuthorId { get; set; }

    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    /// <summary>Position FEN facultative : permet de joindre un diagramme au message.</summary>
    public string? Fen { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EditedAt { get; set; }

    public int? EditedById { get; set; }

    public int ReportCount { get; set; }

    public CommentStatus Status { get; set; } = CommentStatus.Approuve;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }
}
