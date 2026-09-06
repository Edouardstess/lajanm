using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Learning;

/// <summary>Récompense attribuable automatiquement ou manuellement.</summary>
public class Badge : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Nom d'icône Bootstrap, par exemple « award ».</summary>
    public string Icon { get; set; } = "award";

    /// <summary>Couleur d'affichage (classe utilitaire CSS).</summary>
    public string Color { get; set; } = "or";

    public BadgeCriterion Criterion { get; set; } = BadgeCriterion.Manuel;

    /// <summary>Seuil déclencheur du critère automatique.</summary>
    public int Threshold { get; set; }

    public int Points { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public ICollection<UserBadge> AwardedTo { get; set; } = new List<UserBadge>();
}
