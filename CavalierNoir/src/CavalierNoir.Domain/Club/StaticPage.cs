using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Club;

/// <summary>
/// Page éditoriale gérée par le back-office : présentation, histoire, mentions
/// légales, politique de confidentialité, palmarès…
/// </summary>
public class StaticPage : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; } = true;

    /// <summary>Affichée dans le menu principal.</summary>
    public bool ShowInMenu { get; set; }

    /// <summary>Affichée dans le pied de page.</summary>
    public bool ShowInFooter { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Page structurelle non supprimable (mentions légales, confidentialité).</summary>
    public bool IsSystemPage { get; set; }
}
