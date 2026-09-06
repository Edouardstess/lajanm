namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>
/// Horloge injectable. Aucun service n'appelle <c>DateTime.UtcNow</c> directement :
/// les règles temporelles (expiration d'adhésion, exercice du jour) restent testables.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Instant courant en temps universel.</summary>
    DateTime UtcNow { get; }

    /// <summary>Instant courant dans le fuseau du club (Amérique/Port-au-Prince).</summary>
    DateTime LocalNow { get; }

    /// <summary>Date du jour dans le fuseau du club.</summary>
    DateOnly Today { get; }

    /// <summary>Fuseau horaire de référence du club.</summary>
    TimeZoneInfo ClubTimeZone { get; }
}
