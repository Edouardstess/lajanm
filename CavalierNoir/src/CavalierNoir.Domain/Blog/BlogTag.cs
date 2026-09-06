using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Blog;

/// <summary>Étiquette libre appliquée aux articles.</summary>
public class BlogTag : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public ICollection<BlogPostTag> PostTags { get; set; } = new List<BlogPostTag>();
}
