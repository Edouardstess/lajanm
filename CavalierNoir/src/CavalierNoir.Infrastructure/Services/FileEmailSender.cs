using System.Text;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Infrastructure.Services;

/// <summary>
/// Transporteur de développement : chaque courriel est écrit dans un fichier
/// <c>.html</c> horodaté au lieu d'être expédié. Permet de vérifier le rendu
/// des gabarits sans configurer de serveur SMTP ni risquer un envoi réel.
/// </summary>
public sealed class FileEmailSender(
    IOptions<EmailOptions> options,
    ILogger<FileEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailDeliveryResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_options.DropFolder);

            var safeTo = message.To.Replace('@', '_').Replace('/', '_').Replace('\\', '_');
            var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{safeTo}.html";
            var path = Path.Combine(_options.DropFolder, fileName);

            var content = new StringBuilder();
            content.AppendLine("<!-- ============================================");
            content.AppendLine($"     À        : {message.To}");
            content.AppendLine($"     Objet    : {message.Subject}");
            content.AppendLine($"     Gabarit  : {message.Template ?? "(aucun)"}");
            content.AppendLine($"     Généré   : {DateTime.UtcNow:O}");
            content.AppendLine("     ============================================ -->");
            content.AppendLine(message.HtmlBody);

            await File.WriteAllTextAsync(path, content.ToString(), Encoding.UTF8, cancellationToken);

            logger.LogInformation(
                "Courriel « {Subject} » écrit dans {Path} (mode développement).",
                message.Subject,
                path);

            return EmailDeliveryResult.Ok(fileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Impossible d'écrire le courriel sur disque.");
            return EmailDeliveryResult.Failed(ex.Message);
        }
    }
}
