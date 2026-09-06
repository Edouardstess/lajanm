using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Infrastructure.BackgroundJobs;

/// <summary>
/// Ordonnanceur interne du site. Il remplace un ordonnanceur externe
/// (Hangfire, Quartz) pour un déploiement mono-instance : il réveille
/// l'application toutes les cinq minutes et déclenche les traitements dont
/// l'échéance est atteinte.
/// </summary>
/// <remarks>
/// Sur plusieurs instances, deux exécutions concurrentes sont possibles ; les
/// traitements sont donc écrits pour être idempotents (l'envoi de l'exercice du
/// jour est protégé par une contrainte d'unicité sur la date). Le passage à
/// Hangfire avec verrou distribué se fait en réimplémentant cette classe, sans
/// toucher aux services applicatifs qu'elle appelle.
/// </remarks>
public sealed class MaintenanceWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SiteOptions> siteOptions,
    ILogger<MaintenanceWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly SiteOptions _site = siteOptions.Value;

    private DateOnly _lastDispatchDate = DateOnly.MinValue;
    private DateOnly _lastMaintenanceDate = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Ordonnanceur démarré : réveil toutes les {Minutes} minutes, exercice du jour à {Time}.",
            Interval.TotalMinutes,
            _site.DailyExerciseTime);

        // Laisse à l'application le temps de finir son démarrage (migrations, amorçage).
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await RunDueJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Une tâche en échec ne doit jamais arrêter l'ordonnanceur.
                logger.LogError(ex, "Erreur pendant l'exécution des tâches planifiées.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));

        logger.LogInformation("Ordonnanceur arrêté.");
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RunDueJobsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var clock = provider.GetRequiredService<IDateTimeProvider>();

        var today = clock.Today;
        var localTime = TimeOnly.FromDateTime(clock.LocalNow);

        // 1. Exercice du jour, une fois par jour, à partir de l'heure configurée.
        if (_lastDispatchDate < today && localTime >= _site.DailyExerciseTime)
        {
            var dispatcher = provider.GetRequiredService<DailyExerciseDispatcher>();
            var report = await dispatcher.DispatchAsync(today, ct);
            _lastDispatchDate = today;

            logger.LogInformation(
                "Exercice du jour ({Date}) : {Sent} envoi(s), {Failed} échec(s). {Note}",
                report.Date,
                report.Sent,
                report.Failed,
                report.Note ?? string.Empty);
        }

        // 2. Maintenance quotidienne des adhésions et des relances.
        if (_lastMaintenanceDate < today)
        {
            var memberships = provider.GetRequiredService<MembershipService>();
            var changed = await memberships.RefreshStatusesAsync(ct);

            await SendRenewalRemindersAsync(provider, ct);
            _lastMaintenanceDate = today;

            logger.LogInformation("Maintenance quotidienne : {Changed} adhésion(s) mises à jour.", changed);
        }

        // 3. Publication des articles programmés, à chaque réveil.
        var blog = provider.GetRequiredService<BlogService>();
        var published = await blog.PublishScheduledAsync(ct);
        if (published > 0)
        {
            logger.LogInformation("{Count} article(s) programmé(s) publié(s).", published);
        }
    }

    /// <summary>Envoie la relance d'échéance aux adhésions arrivant à terme (BR-03).</summary>
    private async Task SendRenewalRemindersAsync(IServiceProvider provider, CancellationToken ct)
    {
        var memberships = provider.GetRequiredService<MembershipService>();
        var templates = provider.GetRequiredService<EmailTemplateFactory>();
        var sender = provider.GetRequiredService<IEmailSender>();
        var context = provider.GetRequiredService<IApplicationDbContext>();
        var clock = provider.GetRequiredService<IDateTimeProvider>();

        var due = await memberships.GetMembershipsToRemindAsync(ct);
        if (due.Count == 0)
        {
            return;
        }

        foreach (var membership in due)
        {
            var email = membership.User?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var message = templates.RenewalReminder(
                email,
                membership.User!.DisplayName,
                membership);

            var result = await sender.SendAsync(message, ct);
            if (result.Success)
            {
                membership.RenewalReminderSentAt = clock.UtcNow;
            }
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("{Count} relance(s) d'échéance envoyée(s).", due.Count);
    }
}
