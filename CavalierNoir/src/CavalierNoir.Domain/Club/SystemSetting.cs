using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Club;

/// <summary>
/// Paramètre de configuration modifiable en production sans redéploiement
/// (nom du club, adresse, réseaux sociaux, heure d'envoi de l'exercice…).
/// </summary>
public class SystemSetting : Entity
{
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? Description { get; set; }

    /// <summary>Regroupement d'affichage : « Club », « Emails », « SEO »…</summary>
    public string Group { get; set; } = "Général";

    /// <summary>« text », « number », « bool », « html », « color », « time ».</summary>
    public string DataType { get; set; } = "text";

    /// <summary>Valeur chiffrée au repos : ne jamais l'afficher en clair dans l'interface.</summary>
    public bool IsSecret { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedById { get; set; }
}
