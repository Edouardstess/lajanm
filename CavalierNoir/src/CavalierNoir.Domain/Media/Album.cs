using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Tournaments;

namespace CavalierNoir.Domain.Media;

/// <summary>Album de la galerie, généralement rattaché à un événement.</summary>
public class Album : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly? TakenOn { get; set; }

    public int? ClubEventId { get; set; }

    public ClubEvent? ClubEvent { get; set; }

    public int? CoverMediaId { get; set; }

    public MediaItem? CoverMedia { get; set; }

    public Visibility Visibility { get; set; } = Visibility.Public;

    public bool IsPublished { get; set; }

    public ICollection<MediaItem> Items { get; set; } = new List<MediaItem>();

    public int ItemCount => Items.Count;
}
