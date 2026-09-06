using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Communication;

/// <summary>Sous-forum thématique.</summary>
public class ForumCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Icon { get; set; } = "chat-square-text";

    public Visibility Visibility { get; set; } = Visibility.Membres;

    public int DisplayOrder { get; set; }

    public bool IsLocked { get; set; }

    public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();

    public int TopicCount { get; set; }

    public int PostCount { get; set; }

    public DateTime? LastActivityAt { get; set; }
}
