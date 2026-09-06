namespace CavalierNoir.Infrastructure.Services;

/// <summary>Configuration du transporteur de courriels (section « Email »).</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>« smtp » (production), « fichier » (développement) ou « aucun ».</summary>
    public string Provider { get; set; } = "fichier";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    /// <summary>Dossier de dépôt des courriels quand <c>Provider</c> vaut « fichier ».</summary>
    public string DropFolder { get; set; } = "App_Data/emails";

    /// <summary>Délai maximal d'un envoi, en secondes.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Nombre de tentatives avant abandon (politique de résilience).</summary>
    public int MaxRetries { get; set; } = 3;
}
