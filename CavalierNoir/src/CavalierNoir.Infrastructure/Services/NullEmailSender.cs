using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Infrastructure.Services;

/// <summary>
/// Transporteur inerte : rien n'est expédié, l'envoi est seulement journalisé.
/// Utilisé pour les tests et les environnements où aucun courriel ne doit
/// sortir du système.
/// </summary>
public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task<EmailDeliveryResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Envoi neutralisé : « {Subject} » vers {To} (gabarit {Template}).",
            message.Subject,
            message.To,
            message.Template ?? "aucun");

        return Task.FromResult(EmailDeliveryResult.Ok("null"));
    }
}
