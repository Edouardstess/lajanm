using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Identity;

/// <summary>Notification destinée à un utilisateur, tous canaux confondus.</summary>
public class Notification : Entity
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>Lien relatif vers la ressource concernée, par exemple « /Tournois/12 ».</summary>
    public string? Url { get; set; }

    /// <summary>Nom d'icône Bootstrap, par exemple « trophy ».</summary>
    public string? Icon { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    public int Priority { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    public bool IsRead => ReadAt.HasValue;

    public void MarkAsRead(DateTime when) => ReadAt ??= when;
}
