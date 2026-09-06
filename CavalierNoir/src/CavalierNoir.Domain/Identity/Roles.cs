namespace CavalierNoir.Domain.Identity;

/// <summary>
/// Catalogue des 12 rôles applicatifs (§3 du DAL). Les constantes sont utilisées
/// telles quelles dans les attributs <c>[Authorize(Roles = ...)]</c>.
/// </summary>
public static class Roles
{
    public const string Candidat = "Candidat";
    public const string Membre = "Membre";
    public const string MembreHonneur = "MembreHonneur";
    public const string Entraineur = "Entraineur";
    public const string Arbitre = "Arbitre";
    public const string OrganisateurTournoi = "OrganisateurTournoi";
    public const string ResponsablePedagogique = "ResponsablePedagogique";
    public const string ResponsableCommunication = "ResponsableCommunication";
    public const string Secretaire = "Secretaire";
    public const string Tresorier = "Tresorier";
    public const string President = "President";
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Rôles donnant accès au back-office (/Admin).</summary>
    public const string StaffRoles =
        Entraineur + "," + Arbitre + "," + OrganisateurTournoi + "," + ResponsablePedagogique + "," +
        ResponsableCommunication + "," + Secretaire + "," + Tresorier + "," + President + "," + SuperAdmin;

    /// <summary>Rôles donnant accès à l'espace membre (/Membre).</summary>
    public const string MemberRoles =
        Membre + "," + MembreHonneur + "," + Entraineur + "," + Arbitre + "," + OrganisateurTournoi + "," +
        ResponsablePedagogique + "," + ResponsableCommunication + "," + Secretaire + "," + Tresorier + "," +
        President + "," + SuperAdmin;

    public static IReadOnlyList<(string Name, string Description, int Order)> All { get; } =
    [
        (Candidat, "Inscrit dont l'adhésion n'est pas encore validée.", 10),
        (Membre, "Adhérent à jour de cotisation.", 20),
        (MembreHonneur, "Ancien dirigeant ou bienfaiteur, accès permanent.", 30),
        (Entraineur, "Encadre les séances et rédige les exercices.", 40),
        (Arbitre, "Valide les appariements et saisit les résultats.", 50),
        (OrganisateurTournoi, "Crée et pilote les tournois.", 60),
        (ResponsablePedagogique, "Pilote les cours, les niveaux et la progression.", 70),
        (ResponsableCommunication, "Gère le blog, la newsletter et la galerie.", 80),
        (Secretaire, "Instruit les adhésions et la gestion documentaire.", 90),
        (Tresorier, "Gère les cotisations, paiements, dons et dépenses.", 100),
        (President, "Représentation légale et validations finales.", 110),
        (SuperAdmin, "Administration technique complète.", 120)
    ];
}
