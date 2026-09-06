namespace CavalierNoir.Domain.Identity;

/// <summary>
/// Catalogue des permissions fines. Chaque permission est stockée comme une claim
/// de type <see cref="ClaimType"/> attachée à un rôle, et référencée par une
/// <c>AuthorizationPolicy</c> du même nom côté web.
/// </summary>
public static class Permissions
{
    public const string ClaimType = "cavaliernoir.permission";

    public static class Exercises
    {
        public const string View = "exercises.view";
        public const string Solve = "exercises.solve";
        public const string Manage = "exercises.manage";
        public const string Publish = "exercises.publish";
    }

    public static class Tournaments
    {
        public const string View = "tournaments.view";
        public const string Register = "tournaments.register";
        public const string Manage = "tournaments.manage";
        public const string Pair = "tournaments.pair";
        public const string EnterResults = "tournaments.results";
    }

    public static class MembershipsPermissions
    {
        public const string Apply = "memberships.apply";
        public const string Review = "memberships.review";
        public const string Approve = "memberships.approve";
        public const string Manage = "memberships.manage";
    }

    public static class Finance
    {
        public const string View = "finance.view";
        public const string Manage = "finance.manage";
        public const string Refund = "finance.refund";
    }

    public static class Content
    {
        public const string Write = "content.write";
        public const string Publish = "content.publish";
        public const string Moderate = "content.moderate";
        public const string ManageMedia = "content.media";
        public const string SendNewsletter = "content.newsletter";
    }

    public static class Documents
    {
        public const string View = "documents.view";
        public const string Manage = "documents.manage";
    }

    public static class Administration
    {
        public const string ViewDashboard = "admin.dashboard";
        public const string ManageUsers = "admin.users";
        public const string ManageRoles = "admin.roles";
        public const string ViewAudit = "admin.audit";
        public const string ManageSettings = "admin.settings";
        public const string ManageBackups = "admin.backups";
    }

    /// <summary>Toutes les permissions, pour l'écran d'affectation des rôles.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Exercises.View, Exercises.Solve, Exercises.Manage, Exercises.Publish,
        Tournaments.View, Tournaments.Register, Tournaments.Manage, Tournaments.Pair, Tournaments.EnterResults,
        MembershipsPermissions.Apply, MembershipsPermissions.Review, MembershipsPermissions.Approve, MembershipsPermissions.Manage,
        Finance.View, Finance.Manage, Finance.Refund,
        Content.Write, Content.Publish, Content.Moderate, Content.ManageMedia, Content.SendNewsletter,
        Documents.View, Documents.Manage,
        Administration.ViewDashboard, Administration.ManageUsers, Administration.ManageRoles,
        Administration.ViewAudit, Administration.ManageSettings, Administration.ManageBackups
    ];

    /// <summary>
    /// Matrice RBAC de référence (§6 du référentiel v5.0) : permissions accordées
    /// par rôle au moment de l'amorçage de la base.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> RoleMatrix { get; } = new Dictionary<string, string[]>
    {
        [Roles.Candidat] =
        [
            Exercises.View, Tournaments.View, MembershipsPermissions.Apply
        ],
        [Roles.Membre] =
        [
            Exercises.View, Exercises.Solve, Tournaments.View, Tournaments.Register,
            MembershipsPermissions.Apply, Documents.View
        ],
        [Roles.MembreHonneur] =
        [
            Exercises.View, Exercises.Solve, Tournaments.View, Tournaments.Register, Documents.View
        ],
        [Roles.Entraineur] =
        [
            Exercises.View, Exercises.Solve, Exercises.Manage, Exercises.Publish,
            Tournaments.View, Tournaments.Register, Documents.View, Administration.ViewDashboard
        ],
        [Roles.Arbitre] =
        [
            Exercises.View, Exercises.Solve, Tournaments.View, Tournaments.Pair,
            Tournaments.EnterResults, Administration.ViewDashboard
        ],
        [Roles.OrganisateurTournoi] =
        [
            Exercises.View, Tournaments.View, Tournaments.Manage, Tournaments.Pair,
            Tournaments.EnterResults, Administration.ViewDashboard
        ],
        [Roles.ResponsablePedagogique] =
        [
            Exercises.View, Exercises.Solve, Exercises.Manage, Exercises.Publish,
            Tournaments.View, Documents.View, Administration.ViewDashboard
        ],
        [Roles.ResponsableCommunication] =
        [
            Exercises.View, Tournaments.View, Content.Write, Content.Publish, Content.Moderate,
            Content.ManageMedia, Content.SendNewsletter, Administration.ViewDashboard
        ],
        [Roles.Secretaire] =
        [
            Exercises.View, Tournaments.View, MembershipsPermissions.Review, MembershipsPermissions.Approve,
            MembershipsPermissions.Manage, Documents.View, Documents.Manage, Finance.View,
            Administration.ViewDashboard, Administration.ManageUsers
        ],
        [Roles.Tresorier] =
        [
            Exercises.View, Tournaments.View, Finance.View, Finance.Manage, Finance.Refund,
            MembershipsPermissions.Review, Documents.View, Administration.ViewDashboard
        ],
        [Roles.President] =
        [
            Exercises.View, Exercises.Solve, Tournaments.View, Tournaments.Manage,
            MembershipsPermissions.Review, MembershipsPermissions.Approve, MembershipsPermissions.Manage,
            Finance.View, Content.Write, Content.Publish, Content.Moderate,
            Documents.View, Documents.Manage, Administration.ViewDashboard, Administration.ManageUsers
        ],
        [Roles.SuperAdmin] = [.. All]
    };
}
