using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Club;

/// <summary>Question fréquente affichée sur la page d'aide.</summary>
public class FaqItem : AuditableEntity
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    /// <summary>Regroupement : « Adhésion », « Tournois », « Exercices »…</summary>
    public string Category { get; set; } = "Général";

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public int ViewCount { get; set; }
}
