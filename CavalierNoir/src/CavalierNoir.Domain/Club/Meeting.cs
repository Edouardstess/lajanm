using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Club;

/// <summary>Réunion statutaire et son procès-verbal.</summary>
public class Meeting : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public MeetingType Type { get; set; } = MeetingType.ReunionBureau;

    public DateTime ScheduledAt { get; set; }

    public string? Location { get; set; }

    public string? Agenda { get; set; }

    public string? Minutes { get; set; }

    public MeetingStatus Status { get; set; } = MeetingStatus.Planifiee;

    public int? SecretaryId { get; set; }

    public int? ApprovedById { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? AttendeeCount { get; set; }

    public int? DocumentId { get; set; }

    public Document? Document { get; set; }

    /// <summary>Le président valide le procès-verbal rédigé par le secrétaire.</summary>
    public void ApproveMinutes(int presidentId, DateTime when)
    {
        if (Status != MeetingStatus.PvRedige)
        {
            throw new DomainException("Le procès-verbal doit être rédigé avant validation.");
        }

        if (SecretaryId.HasValue && SecretaryId.Value == presidentId)
        {
            throw new DomainException(
                "SoD-02",
                "Séparation des tâches : le rédacteur du procès-verbal ne peut pas le valider.");
        }

        Status = MeetingStatus.PvValide;
        ApprovedById = presidentId;
        ApprovedAt = when;
        UpdatedAt = when;
    }
}
