using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CavalierNoir.Application;

/// <summary>
/// Enregistrement des services applicatifs. Tous sont <c>Scoped</c> : ils
/// partagent le même <see cref="IApplicationDbContext"/> le temps d'une requête.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<MembershipService>();
        services.AddScoped<ExerciseService>();
        services.AddScoped<TournamentService>();
        services.AddScoped<BlogService>();
        services.AddScoped<ForumService>();
        services.AddScoped<NewsletterService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<EventService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<DailyExerciseDispatcher>();

        services.AddSingleton<EmailTemplateFactory>();
        services.AddSingleton<ExerciseTokenService>();

        return services;
    }
}
