namespace CavalierNoir.Application.Common.Models;

/// <summary>Courriel prêt à être expédié.</summary>
public sealed class EmailMessage
{
    public required string To { get; init; }

    public string? ToName { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }

    public string? TextBody { get; init; }

    /// <summary>Nom du modèle utilisé, journalisé dans <c>EmailLogs</c>.</summary>
    public string? Template { get; init; }

    public int? UserId { get; init; }

    public int? CampaignId { get; init; }

    public int? DailyExerciseId { get; init; }

    public string? ReplyTo { get; init; }

    /// <summary>Lien de désabonnement, requis pour tout envoi de masse.</summary>
    public string? UnsubscribeUrl { get; init; }
}

/// <summary>Résultat d'une expédition.</summary>
/// <param name="Success">L'envoi a été accepté par le transporteur.</param>
/// <param name="ProviderMessageId">Identifiant renvoyé par le transporteur.</param>
/// <param name="Error">Message d'erreur en cas d'échec.</param>
public readonly record struct EmailDeliveryResult(bool Success, string? ProviderMessageId, string? Error)
{
    public static EmailDeliveryResult Ok(string? messageId = null) => new(true, messageId, null);

    public static EmailDeliveryResult Failed(string error) => new(false, null, error);
}
