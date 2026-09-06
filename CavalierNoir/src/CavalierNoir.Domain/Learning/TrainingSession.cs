using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Learning;

/// <summary>Séance d'entraînement physique, avec feuille de présence.</summary>
public class TrainingSession : AuditableEntity
{
    public int? CourseId { get; set; }

    public Course? Course { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Location { get; set; }

    public int? CoachId { get; set; }

    public string? Topic { get; set; }

    public string? Notes { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public int PresentCount => Attendances.Count(a => a.Status == Enums.AttendanceStatus.Present);
}
