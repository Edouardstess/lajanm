using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>Inscription d'un membre (ou d'un visiteur) à un événement du calendrier.</summary>
public class EventRegistration : AuditableEntity
{
    public int ClubEventId { get; set; }

    public ClubEvent ClubEvent { get; set; } = null!;

    public int? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>Renseignés pour un participant externe non inscrit sur le site.</summary>
    public string? GuestName { get; set; }

    public string? GuestEmail { get; set; }

    public RegistrationStatus Status { get; set; } = RegistrationStatus.EnAttente;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime? CheckedInAt { get; set; }

    /// <summary>Jeton du QR code de pointage à l'entrée.</summary>
    public string? CheckInToken { get; set; }

    public string? Comment { get; set; }

    public int? PaymentId { get; set; }
}
