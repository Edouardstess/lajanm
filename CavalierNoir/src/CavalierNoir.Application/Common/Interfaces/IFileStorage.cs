namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>
/// Stockage de fichiers téléversés. Implémenté sur le système de fichiers local ;
/// remplaçable par un stockage objet (MinIO / S3) sans toucher aux services.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Enregistre un flux et renvoie l'URL publique relative du fichier créé.
    /// </summary>
    /// <param name="container">Dossier logique : « documents », « galerie », « profils ».</param>
    Task<StoredFile> SaveAsync(
        Stream content,
        string fileName,
        string container,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string relativeUrl, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default);

    bool Exists(string relativeUrl);
}

/// <summary>Fichier persisté par <see cref="IFileStorage"/>.</summary>
/// <param name="RelativeUrl">URL publique relative, par exemple « /uploads/documents/statuts.pdf ».</param>
/// <param name="FileName">Nom de fichier assaini.</param>
/// <param name="SizeInBytes">Taille en octets.</param>
/// <param name="ContentType">Type MIME détecté.</param>
/// <param name="Checksum">Empreinte SHA-256 en hexadécimal.</param>
public readonly record struct StoredFile(
    string RelativeUrl,
    string FileName,
    long SizeInBytes,
    string ContentType,
    string Checksum);
