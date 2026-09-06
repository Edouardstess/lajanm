using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Blog;

/// <summary>Rubrique éditoriale du blog.</summary>
public class BlogCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Couleur d'accent de la rubrique (code hexadécimal).</summary>
    public string? Color { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}
