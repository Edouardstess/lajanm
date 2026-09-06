using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Lettre d'information : abonnement en double opt-in, désabonnement en un clic
/// et expédition des campagnes.
/// </summary>
public sealed class NewsletterService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    IEmailSender emailSender,
    EmailTemplateFactory templates,
    IOptions<SiteOptions> siteOptions,
    ILogger<NewsletterService> logger)
{
    private readonly SiteOptions _site = siteOptions.Value;

    /// <summary>
    /// Inscrit une adresse et envoie le courriel de confirmation. Une adresse déjà
    /// confirmée n'est pas réinitialisée : la réponse reste neutre pour ne pas
    /// révéler si l'adresse est connue du système.
    /// </summary>
    public async Task<Result> SubscribeAsync(
        string email,
        string? name,
        bool wantsDailyExercise,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        if (!normalized.Contains('@') || normalized.Length < 5)
        {
            return Result.Failure("Adresse électronique invalide.", "VALIDATION_005");
        }

        var existing = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == normalized, ct);

        var now = clock.UtcNow;

        if (existing is { IsConfirmed: true, IsActive: true })
        {
            return Result.Success();
        }

        if (existing is null)
        {
            existing = new NewsletterSubscriber
            {
                Email = normalized,
                Name = name,
                WantsDailyExercise = wantsDailyExercise,
                SubscribedAt = now,
                SubscriptionIp = ipAddress,
                IsActive = true,
                IsConfirmed = false
            };

            context.NewsletterSubscribers.Add(existing);
        }
        else
        {
            existing.Name = name ?? existing.Name;
            existing.WantsDailyExercise = wantsDailyExercise;
            existing.IsActive = true;
            existing.UnsubscribedAt = null;
        }

        existing.ConfirmationToken = Guid.NewGuid().ToString("N");
        await context.SaveChangesAsync(ct);

        var url = $"{_site.BaseUrl.TrimEnd('/')}/Newsletter/Confirmer/{existing.ConfirmationToken}";
        var message = templates.NewsletterConfirmation(normalized, url);
        var result = await emailSender.SendAsync(message, ct);

        context.EmailLogs.Add(new EmailLog
        {
            To = normalized,
            Subject = message.Subject,
            Template = message.Template,
            Status = result.Success ? EmailStatus.Envoye : EmailStatus.Echoue,
            SentAt = result.Success ? clock.UtcNow : null,
            ErrorMessage = result.Error,
            ProviderMessageId = result.ProviderMessageId,
            CreatedAt = now,
            AttemptCount = 1
        });

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ConfirmAsync(string token, CancellationToken ct = default)
    {
        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.ConfirmationToken == token, ct);

        if (subscriber is null)
        {
            return Result.Failure("Lien de confirmation invalide ou déjà utilisé.", "NOTFOUND_012");
        }

        subscriber.Confirm(clock.UtcNow);
        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnsubscribeAsync(string token, CancellationToken ct = default)
    {
        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.UnsubscribeToken == token, ct);

        if (subscriber is null)
        {
            return Result.Failure("Lien de désabonnement invalide.", "NOTFOUND_012");
        }

        subscriber.Unsubscribe(clock.UtcNow);
        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Expédie une campagne. L'audience est déterminée par le champ
    /// <c>Audience</c> : « abonnes », « membres » ou « tous ».
    /// </summary>
    public async Task<Result<int>> SendCampaignAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await context.NewsletterCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null)
        {
            return Result<int>.Failure("Campagne introuvable.", "NOTFOUND_013");
        }

        if (campaign.Status is CampaignStatus.Envoyee or CampaignStatus.EnCoursEnvoi)
        {
            return Result<int>.Failure("Cette campagne a déjà été expédiée.", "CONFLICT_005");
        }

        var recipients = await ResolveAudienceAsync(campaign.Audience, ct);
        if (recipients.Count == 0)
        {
            return Result<int>.Failure("Aucun destinataire pour cette audience.");
        }

        campaign.Status = CampaignStatus.EnCoursEnvoi;
        campaign.RecipientCount = recipients.Count;
        await context.SaveChangesAsync(ct);

        var sent = 0;
        var failed = 0;
        var now = clock.UtcNow;

        foreach (var recipient in recipients)
        {
            ct.ThrowIfCancellationRequested();

            var unsubscribeUrl = recipient.UnsubscribeToken is null
                ? null
                : $"{_site.BaseUrl.TrimEnd('/')}/Newsletter/Desabonnement/{recipient.UnsubscribeToken}";

            var message = new EmailMessage
            {
                To = recipient.Email,
                ToName = recipient.Name,
                Subject = campaign.Subject,
                HtmlBody = templates.Layout(campaign.Subject, campaign.BodyHtml, unsubscribeUrl),
                TextBody = campaign.BodyText,
                Template = "newsletter",
                CampaignId = campaign.Id,
                UnsubscribeUrl = unsubscribeUrl
            };

            var result = await emailSender.SendAsync(message, ct);

            if (result.Success)
            {
                sent++;
            }
            else
            {
                failed++;
            }

            context.EmailLogs.Add(new EmailLog
            {
                To = recipient.Email,
                Subject = campaign.Subject,
                Template = "newsletter",
                CampaignId = campaign.Id,
                Status = result.Success ? EmailStatus.Envoye : EmailStatus.Echoue,
                SentAt = result.Success ? clock.UtcNow : null,
                ErrorMessage = result.Error,
                ProviderMessageId = result.ProviderMessageId,
                CreatedAt = now,
                AttemptCount = 1
            });
        }

        campaign.Status = CampaignStatus.Envoyee;
        campaign.SentAt = clock.UtcNow;
        campaign.SentCount = sent;
        campaign.FailedCount = failed;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Campagne {CampaignId} expédiée : {Sent} envoi(s), {Failed} échec(s).",
            campaignId,
            sent,
            failed);

        return Result<int>.Success(sent);
    }

    private async Task<List<Recipient>> ResolveAudienceAsync(string audience, CancellationToken ct)
    {
        var subscribers = new List<Recipient>();

        if (audience is "abonnes" or "tous")
        {
            subscribers.AddRange(await context.NewsletterSubscribers
                .Where(s => s.IsActive && s.IsConfirmed)
                .Select(s => new Recipient(s.Email, s.Name, s.UnsubscribeToken))
                .ToListAsync(ct));
        }

        if (audience is "membres" or "tous")
        {
            subscribers.AddRange(await context.Users
                .Where(u => u.IsActive
                            && u.Email != null
                            && u.EmailConfirmed
                            && (u.Preference == null || u.Preference.ReceiveNewsletter))
                .Select(u => new Recipient(
                    u.Email!,
                    u.Pseudonym ?? (u.FirstName + " " + u.LastName),
                    null))
                .ToListAsync(ct));
        }

        return subscribers
            .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private sealed record Recipient(string Email, string? Name, string? UnsubscribeToken);
}
