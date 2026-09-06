namespace CavalierNoir.Domain.Blog;

/// <summary>Table de jonction article ↔ étiquette (clé composite).</summary>
public class BlogPostTag
{
    public int BlogPostId { get; set; }

    public BlogPost BlogPost { get; set; } = null!;

    public int BlogTagId { get; set; }

    public BlogTag BlogTag { get; set; } = null!;
}
