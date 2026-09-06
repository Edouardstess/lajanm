using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Abstractions;
using CavalierNoir.Infrastructure.BackgroundJobs;
using CavalierNoir.Infrastructure.Identity;
using CavalierNoir.Infrastructure.Persistence;
using CavalierNoir.Infrastructure.Persistence.Interceptors;
using CavalierNoir.Infrastructure.Persistence.Repositories;
using CavalierNoir.Infrastructure.Persistence.SeedData;
using CavalierNoir.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Infrastructure;

/// <summary>
/// Enregistrement de la couche d'infrastructure : persistance, envoi de
/// courriels, stockage de fichiers et tâches planifiées.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.Configure<TokenOptions>(configuration.GetSection(TokenOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();

        AddPersistence(services, configuration);
        AddEmail(services, configuration);
        AddStorage(services, environment);

        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<DataSeeder>();
        services.AddHostedService<MaintenanceWorker>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default")
                               ?? "Data Source=App_Data/cavaliernoir.db";

        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString, sql =>
                    {
                        sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                    break;

                default:
                    options.UseSqlite(connectionString, sqlite =>
                        sqlite.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
                    break;
            }

            options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>($"{EmailOptions.SectionName}:Provider") ?? "fichier";

        switch (provider.ToLowerInvariant())
        {
            case "smtp":
                services.AddScoped<IEmailSender, SmtpEmailSender>();
                break;

            case "aucun":
            case "none":
                services.AddScoped<IEmailSender, NullEmailSender>();
                break;

            default:
                services.AddScoped<IEmailSender, FileEmailSender>();
                break;
        }
    }

    private static void AddStorage(IServiceCollection services, IHostEnvironment environment)
    {
        services.AddSingleton<IFileStorage>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var root = Path.IsPathRooted(options.RootPath)
                ? options.RootPath
                : Path.Combine(environment.ContentRootPath, options.RootPath);

            Directory.CreateDirectory(root);

            return new LocalFileStorage(
                root,
                options.PublicPrefix,
                sp.GetRequiredService<ILogger<LocalFileStorage>>());
        });
    }
}
