using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Learning;

/// <summary>Programme de cours semestriel destiné à un niveau donné.</summary>
public class Course : AuditableEntity, IAggregateRoot
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PlayerLevel Level { get; set; } = PlayerLevel.Debutant;

    public DateRange Period { get; set; } = DateRange.OneYearFrom(DateOnly.FromDateTime(DateTime.UtcNow));

    public int? CoachId { get; set; }

    public int? MaxStudents { get; set; }

    public bool IsPublished { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public ICollection<TrainingSession> Sessions { get; set; } = new List<TrainingSession>();
}
