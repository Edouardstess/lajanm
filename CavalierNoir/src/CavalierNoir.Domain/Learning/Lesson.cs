using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Learning;

/// <summary>Chapitre d'un cours.</summary>
public class Lesson : AuditableEntity
{
    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public string? AttachmentUrl { get; set; }

    public int Order { get; set; }

    public bool IsPublished { get; set; }
}
