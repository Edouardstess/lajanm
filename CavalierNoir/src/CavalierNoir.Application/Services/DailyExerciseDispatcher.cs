using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Envoi quotidien de l'exercice du jour. Déclenché par la tâche de fond à
/// l'heure locale configurée (6 h par défaut) ou manuellement depuis le
/// back-office. L'opération est idempotente : un exercice déjà expédié pour une
/// date donnée ne l'est pas une seconde fois.
/// </summary>
public sealed class DailyExerciseDispatcher(
    IApplicationDbContext context,
    ExerciseService exercises,
    ExerciseTokenService tokens,
    EmailTemplateFactory templates,
    IEmailSender emailSender,
    IDateTimeProvider clock,
    IOptions<SiteOptions> siteOptions,
    ILogger<DailyExerciseDispatcher> logger)
{
    private readonly SiteOptions _site = siteOptions.Value;

    /// <summary>Expédie l'exercice du jour et renvoie le nombre de destinataires servis.</summary>
    public async Task<DispatchReport> DispatchAsync(DateOnly? forDate = null, CancellationToken ct = default)
    {
        var date = forDate ?? clock.Today;

        var daily = await exercises.GetOrCreateDailyAsync(date, ct);
        if (daily?.Exercise is null)
        {
            logger.LogWarning("Aucun exercice disponible pour le {Date}.", date);
            return new DispatchReport(date, 0, 0, "Aucun exercice éligible.");
        }

        if (daily.IsDispatched)
        {
            logger.LogInformation("Exercice du {Date} déjà expédié à {Count} destinataires.", date, daily.RecipientCount);
            return new DispatchReport(date, daily.RecipientCount, 0, "Déjà expédié.");
        }

        if (!_site.DailyExerciseEnabled)
        {
            logger.LogInformation("Envoi de l'exercice quotidien désactivé par configuration.");
            return new DispatchReport(date, 0, 0, "Envoi désactivé.");
        }

        var recipients = await GetRecipientsAsync(ct);
        var sent = 0;
        var failed = 0;
        var now = clock.UtcNow;

        foreach (var recipient in recipients)
        {
            ct.ThrowIfCancellationRequested();

            var token = tokens.Create(recipient.UserId, date);
            var message = templates.BuildDailyExercise(
                recipient.Email,
                recipient.Name,
                daily.Exercise,
                daily,
                token);

            var log = new EmailLog
            {
                To = recipient.Email,
                Subject = message.Subject,
                Template = message.Template,
                UserId = recipient.UserId,
                DailyExerciseId = daily.Id,
                CreatedAt = now,
                AttemptCount = 1
            };

            var result = await emailSender.SendAsync(message, ct);

            if (result.Success)
            {
                sent++;
                log.Status = EmailStatus.Envoye;
                log.SentAt = clock.UtcNow;
                log.ProviderMessageId = result.ProviderMessageId;
            }
            else
            {
                failed++;
                log.Status = EmailStatus.Echoue;
                log.ErrorMessage = result.Error;
            }

            context.EmailLogs.Add(log);
        }

        daily.DispatchedAt = clock.UtcNow;
        daily.RecipientCount = sent;
        daily.DispatchError = failed > 0 ? $"{failed} envoi(s) en échec." : null;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Exercice du {Date} expédié : {Sent} envoi(s), {Failed} échec(s).",
            date,
            sent,
            failed);

        return new DispatchReport(date, sent, failed, null);
    }

    /// <summary>
    /// Destinataires : membres ayant activé l'option dans leurs préférences, plus
    /// les abonnés confirmés de la lettre d'information qui l'ont demandé.
    /// </summary>
    private async Task<List<Recipient>> GetRecipientsAsync(CancellationToken ct)
    {
        var members = await context.Users
            .Where(u => u.IsActive
                        && u.Email != null
                        && u.EmailConfirmed
                        && (u.Preference == null || u.Preference.ReceiveDailyExercise))
            .Select(u => new Recipient(
                u.Id,
                u.Email!,
                u.Pseudonym ?? (u.FirstName + " " + u.LastName)))
            .ToListAsync(ct);

        var subscribers = await context.NewsletterSubscribers
            .Where(s => s.IsActive && s.IsConfirmed && s.WantsDailyExercise && s.UserId == null)
            .Select(s => new Recipient(0, s.Email, s.Name))
            .ToListAsync(ct);

        return members
            .Concat(subscribers)
            .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private sealed record Recipient(int UserId, string Email, string? Name);
}

/// <summary>Compte rendu d'un envoi quotidien.</summary>
/// <param name="Date">Date de l'exercice.</param>
/// <param name="Sent">Nombre de courriels acceptés par le transporteur.</param>
/// <param name="Failed">Nombre d'échecs.</param>
/// <param name="Note">Message explicatif lorsque rien n'a été envoyé.</param>
public readonly record struct DispatchReport(DateOnly Date, int Sent, int Failed, string? Note);
