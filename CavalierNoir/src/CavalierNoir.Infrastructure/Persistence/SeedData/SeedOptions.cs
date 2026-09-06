namespace CavalierNoir.Infrastructure.Persistence.SeedData;

/// <summary>
/// Amorçage de la base (section « Seed »). Le mot de passe du compte
/// d'administration ne doit jamais être laissé à sa valeur par défaut en
/// production : il est fourni par variable d'environnement ou coffre de secrets.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Applique les migrations et insère les données de référence au démarrage.</summary>
    public bool Enabled { get; set; } = true;

    public string AdminEmail { get; set; } = "admin@cavaliernoir.ht";

    public string AdminPassword { get; set; } = "CavalierNoir!2026";

    public string AdminFirstName { get; set; } = "Super";

    public string AdminLastName { get; set; } = "Administrateur";

    /// <summary>
    /// Insère des membres, un tournoi et des articles de démonstration.
    /// À laisser désactivé en production.
    /// </summary>
    public bool CreateDemoData { get; set; }
}
