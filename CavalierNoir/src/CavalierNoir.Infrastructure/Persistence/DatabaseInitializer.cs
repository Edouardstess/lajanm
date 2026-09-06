using CavalierNoir.Application.Common.Models;
using CavalierNoir.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Infrastructure.Persistence;

/// <summary>
/// Prépare la base au démarrage : application des migrations puis amorçage des
/// données de référence. Appelée explicitement depuis <c>Program.cs</c> plutôt
/// qu'en tâche de fond, afin qu'un échec empêche le démarrage au lieu de laisser
/// tourner une application sur un schéma incohérent.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Initialisation");
        var seedOptions = provider.GetRequiredService<IOptions<SeedOptions>>().Value;

        if (!seedOptions.Enabled)
        {
            logger.LogInformation("Initialisation de la base désactivée par configuration.");
            return;
        }

        var context = provider.GetRequiredService<ApplicationDbContext>();

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Application de {Count} migration(s) : {Migrations}.",
                pending.Count,
                string.Join(", ", pending));

            await context.Database.MigrateAsync(cancellationToken);
        }
        else if ((await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Any())
        {
            logger.LogInformation("Base à jour, aucune migration à appliquer.");
        }
        else
        {
            // Aucun historique de migration : cas d'un fournisseur sans migrations
            // générées (développement local, tests d'intégration).
            logger.LogWarning(
                "Aucune migration trouvée : création du schéma directement depuis le modèle. "
                + "Générez des migrations avant tout déploiement en production.");
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        var site = provider.GetRequiredService<IOptions<SiteOptions>>().Value;
        var seeder = provider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(seedOptions, site, cancellationToken);
    }
}
