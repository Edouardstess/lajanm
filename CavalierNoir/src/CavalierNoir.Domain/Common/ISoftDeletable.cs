namespace CavalierNoir.Domain.Common;

/// <summary>
/// Contrat des entités archivables : elles ne sont jamais supprimées physiquement,
/// conformément à la politique de gouvernance des données (BR-15, conservation légale).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }

    int? DeletedById { get; set; }
}
