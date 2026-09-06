using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Communication;

/// <summary>Campagne d'e-mailing préparée par le responsable communication.</summary>
public class NewsletterCampaign : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public string? BodyText { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Brouillon;

    /// <summary>Ciblage : « tous », « membres », « abonnes ».</summary>
    public string Audience { get; set; } = "abonnes";

    public DateTime? ScheduledFor { get; set; }

    public DateTime? SentAt { get; set; }

    public int RecipientCount { get; set; }

    public int SentCount { get; set; }

    public int FailedCount { get; set; }

    public int OpenedCount { get; set; }

    public int ClickedCount { get; set; }

    public decimal OpenRate => SentCount == 0 ? 0m : decimal.Round(OpenedCount * 100m / SentCount, 1);

    public decimal ClickRate => SentCount == 0 ? 0m : decimal.Round(ClickedCount * 100m / SentCount, 1);
}
