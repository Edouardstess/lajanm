using CavalierNoir.Application.Common.Models;

namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>
/// Expédition d'un courriel. L'implémentation est interchangeable
/// (SMTP, fichier en développement, API SendGrid/Brevo en production) : la
/// couche Application ne connaît que ce contrat.
/// </summary>
public interface IEmailSender
{
    Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
