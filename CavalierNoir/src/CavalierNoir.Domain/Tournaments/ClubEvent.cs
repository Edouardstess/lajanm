using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>
/// Événement du calendrier du club : stage, simultanée, assemblée générale…
/// Un tournoi peut être relié à son événement de calendrier.
/// </summary>
public class ClubEvent : AuditableEntity, IAggregateRoot, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public EventType Type { get; set; } = EventType.Entrainement;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public string? Address { get; set; }

    public int? MaxParticipants { get; set; }

    public Money EntryFee { get; set; } = Money.Zero();

    public bool IsRegistrationOpen { get; set; }

    public bool MembersOnly { get; set; }

    public bool IsPublished { get; set; }

    public string? ImageUrl { get; set; }

    public int? TournamentId { get; set; }

    public Tournament? Tournament { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }

    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();

    public int ConfirmedParticipants => Registrations.Count(r => r.Status == RegistrationStatus.Confirmee);

    public bool IsFull => MaxParticipants.HasValue && ConfirmedParticipants >= MaxParticipants.Value;

    public bool IsUpcoming(DateTime now) => StartDate > now;
}
