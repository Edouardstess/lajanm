namespace CavalierNoir.Domain.Common;

/// <summary>
/// Entité dont les créations et modifications sont horodatées et attribuées.
/// Les colonnes <see cref="CreatedById"/> / <see cref="UpdatedById"/> référencent
/// logiquement un utilisateur mais ne portent pas de clé étrangère afin d'éviter
/// les chemins de suppression en cascade multiples.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedById { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedById { get; set; }
}
