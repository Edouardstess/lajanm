namespace CavalierNoir.Application.Common.Models;

/// <summary>
/// Réglages généraux du site, liés à la section « Site » de la configuration.
/// Les valeurs modifiables en production le sont via la table <c>SystemSettings</c>.
/// </summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; set; } = "Cavalier Noir";

    public string Tagline { get; set; } = "Club d'échecs — Port-au-Prince, Haïti";

    /// <summary>URL publique canonique, utilisée dans les courriels et le sitemap.</summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";

    public string ContactEmail { get; set; } = "contact@cavaliernoir.ht";

    public string FromEmail { get; set; } = "no-reply@cavaliernoir.ht";

    public string FromName { get; set; } = "Cavalier Noir";

    public string Address { get; set; } = "Port-au-Prince, Haïti";

    public string? Phone { get; set; }

    public string? Facebook { get; set; }

    public string? Instagram { get; set; }

    public string? YouTube { get; set; }

    public string DefaultCurrency { get; set; } = "HTG";

    /// <summary>Fuseau horaire IANA du club.</summary>
    public string TimeZone { get; set; } = "America/Port-au-Prince";

    /// <summary>Heure locale d'envoi de l'exercice quotidien.</summary>
    public TimeOnly DailyExerciseTime { get; set; } = new(6, 0);

    /// <summary>Envoi réellement effectué ; à laisser désactivé tant qu'aucun transporteur n'est configuré.</summary>
    public bool DailyExerciseEnabled { get; set; } = true;

    /// <summary>Les commentaires des membres sont publiés sans modération a priori.</summary>
    public bool AutoApproveComments { get; set; }
}
