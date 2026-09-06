using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Media;

/// <summary>
/// Fichier stocké (photo, vidéo, document). Le stockage physique est abstrait :
/// système de fichiers local en développement, objet S3/MinIO en production.
/// </summary>
public class MediaItem : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;

    /// <summary>Chemin ou URL public servi au navigateur.</summary>
    public string Url { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    public MediaType Type { get; set; } = MediaType.Image;

    public string? ContentType { get; set; }

    public long SizeInBytes { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    /// <summary>Texte alternatif : obligatoire pour l'accessibilité (WCAG 2.2 AA).</summary>
    public string? AltText { get; set; }

    public string? Caption { get; set; }

    public int? AlbumId { get; set; }

    public Album? Album { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Empreinte SHA-256 du fichier : évite les doublons de téléversement.</summary>
    public string? Checksum { get; set; }

    public string SizeLabel => SizeInBytes switch
    {
        < 1024 => $"{SizeInBytes} o",
        < 1024 * 1024 => $"{SizeInBytes / 1024.0:0.#} Ko",
        _ => $"{SizeInBytes / (1024.0 * 1024.0):0.##} Mo"
    };
}
