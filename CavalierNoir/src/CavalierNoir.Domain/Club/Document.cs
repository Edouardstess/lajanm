using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Club;

/// <summary>
/// Document officiel versionné (statuts, règlement intérieur, procès-verbal,
/// rapport d'activité). Les versions successives sont chaînées par
/// <see cref="PreviousVersionId"/>.
/// </summary>
public class Document : AuditableEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long SizeInBytes { get; set; }

    public int Version { get; set; } = 1;

    public int? PreviousVersionId { get; set; }

    public Document? PreviousVersion { get; set; }

    public DocumentCategoryCode Category { get; set; } = DocumentCategoryCode.Autre;

    public Visibility Visibility { get; set; } = Visibility.Membres;

    public DataClassification Classification { get; set; } = DataClassification.Interne;

    public DateOnly? EffectiveDate { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public string? ArchiveReason { get; set; }

    public int DownloadCount { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }

    public string SizeLabel => SizeInBytes switch
    {
        < 1024 => $"{SizeInBytes} o",
        < 1024 * 1024 => $"{SizeInBytes / 1024.0:0.#} Ko",
        _ => $"{SizeInBytes / (1024.0 * 1024.0):0.##} Mo"
    };

    /// <summary>Crée la version suivante en conservant l'historique.</summary>
    public Document CreateNextVersion(string fileUrl, long sizeInBytes, int authorId, DateTime when)
    {
        IsArchived = true;
        ArchivedAt = when;
        ArchiveReason = "Remplacé par une version plus récente.";

        return new Document
        {
            Title = Title,
            Description = Description,
            FileUrl = fileUrl,
            ContentType = ContentType,
            SizeInBytes = sizeInBytes,
            Version = Version + 1,
            PreviousVersionId = Id,
            Category = Category,
            Visibility = Visibility,
            Classification = Classification,
            EffectiveDate = DateOnly.FromDateTime(when),
            CreatedById = authorId,
            CreatedAt = when
        };
    }
}
