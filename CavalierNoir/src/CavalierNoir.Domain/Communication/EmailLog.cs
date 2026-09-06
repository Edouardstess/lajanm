using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Communication;

/// <summary>
/// Journal d'envoi. Preuve d'expédition (non-répudiation) et base des statistiques
/// d'ouverture et de clic.
/// </summary>
public class EmailLog : Entity
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    /// <summary>Modèle utilisé : « daily-exercise », « membership-renewal »…</summary>
    public string? Template { get; set; }

    public int? UserId { get; set; }

    public int? CampaignId { get; set; }

    public NewsletterCampaign? Campaign { get; set; }

    public int? DailyExerciseId { get; set; }

    public EmailStatus Status { get; set; } = EmailStatus.EnAttente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ClickedAt { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? ErrorMessage { get; set; }

    public int AttemptCount { get; set; }
}
