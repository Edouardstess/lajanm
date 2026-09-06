using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Learning;

/// <summary>Présence d'un membre à une séance d'entraînement.</summary>
public class Attendance : Entity
{
    public int TrainingSessionId { get; set; }

    public TrainingSession TrainingSession { get; set; } = null!;

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public string? Comment { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public int? RecordedById { get; set; }
}
