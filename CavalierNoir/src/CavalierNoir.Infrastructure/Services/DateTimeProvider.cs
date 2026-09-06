using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Infrastructure.Services;

/// <summary>
/// Horloge système. Le fuseau du club (Amérique/Port-au-Prince) est résolu une
/// fois pour toutes ; en cas d'identifiant inconnu (image de conteneur sans base
/// de fuseaux), on retombe sur UTC plutôt que d'échouer au démarrage.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeProvider(IOptions<SiteOptions> options)
    {
        ClubTimeZone = ResolveTimeZone(options.Value.TimeZone);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ClubTimeZone);

    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public TimeZoneInfo ClubTimeZone { get; }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        foreach (var candidate in new[] { id, "America/Port-au-Prince", "Eastern Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
                // On essaie l'identifiant suivant.
            }
            catch (InvalidTimeZoneException)
            {
                // Idem.
            }
        }

        return TimeZoneInfo.Utc;
    }
}
