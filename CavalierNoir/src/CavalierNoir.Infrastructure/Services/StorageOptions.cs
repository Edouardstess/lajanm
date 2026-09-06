namespace CavalierNoir.Infrastructure.Services;

/// <summary>Configuration du stockage de fichiers (section « Storage »).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Racine physique des fichiers téléversés. Un chemin relatif est résolu
    /// depuis le répertoire de contenu de l'application.
    /// </summary>
    public string RootPath { get; set; } = "wwwroot/uploads";

    /// <summary>Préfixe d'URL publique correspondant à <see cref="RootPath"/>.</summary>
    public string PublicPrefix { get; set; } = "/uploads";

    /// <summary>Taille maximale d'un fichier téléversé, en mégaoctets.</summary>
    public int MaxFileSizeMb { get; set; } = 10;
}
