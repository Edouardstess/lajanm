using System.Net;
using System.Net.Mail;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Infrastructure.Services;

/// <summary>
/// Expédition par SMTP, avec réessais à intervalles croissants (1 s, 2 s, 4 s).
/// Un échec définitif n'interrompt jamais le traitement appelant : il est
/// journalisé et remonté dans <see cref="EmailDeliveryResult"/>.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    IOptions<SiteOptions> siteOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = emailOptions.Value;
    private readonly SiteOptions _site = siteOptions.Value;

    public async Task<EmailDeliveryResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(1, _options.MaxRetries);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.UseSsl,
                    Timeout = _options.TimeoutSeconds * 1000
                };

                if (!string.IsNullOrWhiteSpace(_options.UserName))
                {
                    client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
                }

                using var mail = BuildMailMessage(message);
                await client.SendMailAsync(mail, cancellationToken);

                logger.LogInformation("Courriel « {Subject} » envoyé à {To}.", message.Subject, message.To);
                return EmailDeliveryResult.Ok();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is SmtpException or IOException or InvalidOperationException)
            {
                logger.LogWarning(
                    ex,
                    "Échec d'envoi ({Attempt}/{Total}) du courriel « {Subject} » à {To}.",
                    attempt,
                    attempts,
                    message.Subject,
                    message.To);

                if (attempt == attempts)
                {
                    return EmailDeliveryResult.Failed(ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
            }
        }

        return EmailDeliveryResult.Failed("Envoi impossible.");
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(_site.FromEmail, _site.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(string.IsNullOrWhiteSpace(message.ToName)
            ? new MailAddress(message.To)
            : new MailAddress(message.To, message.ToName));

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            mail.ReplyToList.Add(new MailAddress(message.ReplyTo));
        }

        if (!string.IsNullOrWhiteSpace(message.UnsubscribeUrl))
        {
            mail.Headers.Add("List-Unsubscribe", $"<{message.UnsubscribeUrl}>");
            mail.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");
        }

        return mail;
    }
}
