using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Club;

/// <summary>Message reçu via le formulaire de contact public.</summary>
public class ContactMessage : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.Nouveau;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    public int? HandledById { get; set; }

    public DateTime? HandledAt { get; set; }

    public string? InternalNote { get; set; }

    /// <summary>Consentement explicite au traitement des données (RGPD).</summary>
    public bool ConsentGiven { get; set; }
}
