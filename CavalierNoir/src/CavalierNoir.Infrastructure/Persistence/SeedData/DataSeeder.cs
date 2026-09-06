using System.Security.Claims;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Club;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.Memberships;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Infrastructure.Persistence.SeedData;

/// <summary>
/// Amorçage de la base : rôles, permissions, compte d'administration, formules
/// d'adhésion, badges, pages éditoriales, rubriques et bibliothèque d'exercices.
/// L'opération est idempotente : elle peut être rejouée à chaque démarrage sans
/// dupliquer quoi que ce soit.
/// </summary>
public sealed class DataSeeder(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(SeedOptions options, SiteOptions site, CancellationToken ct = default)
    {
        await SeedRolesAndPermissionsAsync(ct);
        await SeedAdministratorAsync(options, ct);
        await SeedSettingsAsync(site, ct);
        await SeedMembershipTypesAsync(site, ct);
        await SeedBadgesAsync(ct);
        await SeedStaticPagesAsync(site, ct);
        await SeedFaqAsync(ct);
        await SeedBlogTaxonomyAsync(ct);
        await SeedForumAsync(ct);
        await SeedExercisesAsync(ct);

        logger.LogInformation("Amorçage de la base terminé.");
    }

    /// <summary>Crée les 12 rôles et attache à chacun ses permissions (matrice RBAC).</summary>
    private async Task SeedRolesAndPermissionsAsync(CancellationToken ct)
    {
        foreach (var (name, description, order) in Roles.All)
        {
            var role = await roleManager.FindByNameAsync(name);

            if (role is null)
            {
                role = new ApplicationRole(name)
                {
                    Description = description,
                    IsSystemRole = true,
                    DisplayOrder = order
                };

                var created = await roleManager.CreateAsync(role);
                if (!created.Succeeded)
                {
                    logger.LogError(
                        "Création du rôle {Role} impossible : {Errors}",
                        name,
                        string.Join(", ", created.Errors.Select(e => e.Description)));
                    continue;
                }
            }

            if (!Permissions.RoleMatrix.TryGetValue(name, out var permissions))
            {
                continue;
            }

            var existing = await roleManager.GetClaimsAsync(role);
            foreach (var permission in permissions)
            {
                if (existing.Any(c => c.Type == Permissions.ClaimType && c.Value == permission))
                {
                    continue;
                }

                await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            }
        }
    }

    /// <summary>Crée le compte super-administrateur s'il n'existe pas.</summary>
    private async Task SeedAdministratorAsync(SeedOptions options, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(options.AdminEmail);
        if (existing is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = options.AdminEmail,
            Email = options.AdminEmail,
            EmailConfirmed = true,
            FirstName = options.AdminFirstName,
            LastName = options.AdminLastName,
            Pseudonym = "cavalier-noir",
            IsActive = true,
            Elo = 1800,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, options.AdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Création du compte d'administration impossible : {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
        await userManager.AddToRoleAsync(admin, Roles.President);

        context.UserPreferences.Add(new UserPreference { UserId = admin.Id });
        context.UserProgress.Add(new UserProgress { UserId = admin.Id });
        await context.SaveChangesAsync(ct);

        logger.LogWarning(
            "Compte d'administration créé pour {Email}. Changez le mot de passe dès la première connexion.",
            options.AdminEmail);
    }

    private async Task SeedSettingsAsync(SiteOptions site, CancellationToken ct)
    {
        var defaults = new (string Key, string? Value, string Group, string Type, string Description)[]
        {
            ("club.nom", site.Name, "Club", "text", "Nom affiché du club."),
            ("club.slogan", site.Tagline, "Club", "text", "Slogan affiché sous le logo."),
            ("club.adresse", site.Address, "Club", "text", "Adresse postale du siège."),
            ("club.email", site.ContactEmail, "Club", "text", "Adresse de contact publique."),
            ("club.telephone", site.Phone, "Club", "text", "Numéro de téléphone public."),
            ("club.fondation", "2019", "Club", "number", "Année de fondation du club."),
            ("reseaux.facebook", site.Facebook, "Réseaux", "text", "Page Facebook."),
            ("reseaux.instagram", site.Instagram, "Réseaux", "text", "Compte Instagram."),
            ("reseaux.youtube", site.YouTube, "Réseaux", "text", "Chaîne YouTube."),
            ("exercice.heureEnvoi", "06:00", "Emails", "time", "Heure locale d'envoi de l'exercice du jour."),
            ("exercice.delaiRepetition", "30", "Emails", "number", "Jours avant qu'un exercice puisse être renvoyé."),
            ("adhesion.delaiGrace", "15", "Adhésions", "number", "Jours de grâce après expiration."),
            ("adhesion.relance", "30", "Adhésions", "number", "Jours avant échéance pour la relance."),
            ("blog.moderationAPriori", "true", "Contenus", "bool", "Les commentaires sont validés avant publication."),
            ("blog.seuilSignalement", "3", "Contenus", "number", "Signalements avant masquage automatique."),
            ("seo.metaDescription",
                "Club d'échecs Cavalier Noir : tournois, exercices quotidiens, cours et communauté à Port-au-Prince.",
                "SEO", "text", "Description par défaut des pages."),
            ("tournoi.eloInitialMax", "1600", "Tournois", "number", "Classement initial maximal sans justificatif.")
        };

        var existingKeys = await context.SystemSettings.Select(s => s.Key).ToListAsync(ct);
        var added = 0;

        foreach (var (key, value, group, type, description) in defaults)
        {
            if (existingKeys.Contains(key))
            {
                continue;
            }

            context.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = value,
                Group = group,
                DataType = type,
                Description = description
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync(ct);
        }
    }

    private async Task SeedMembershipTypesAsync(SiteOptions site, CancellationToken ct)
    {
        if (await context.MembershipTypes.AnyAsync(ct))
        {
            return;
        }

        var currency = site.DefaultCurrency;

        context.MembershipTypes.AddRange(
            new MembershipType
            {
                Name = "Adhésion jeune",
                Code = "JEUNE",
                Description = "Pour les moins de 18 ans. Accès aux entraînements du samedi et aux tournois jeunes.",
                Price = new Money(750m, currency),
                DurationDays = 365,
                MaximumAge = 17,
                RequiresProof = true,
                DisplayOrder = 10
            },
            new MembershipType
            {
                Name = "Adhésion étudiante",
                Code = "ETUDIANT",
                Description = "Tarif réduit sur présentation d'une carte d'étudiant en cours de validité.",
                Price = new Money(1000m, currency),
                DurationDays = 365,
                MinimumAge = 18,
                RequiresProof = true,
                DisplayOrder = 20
            },
            new MembershipType
            {
                Name = "Adhésion plein tarif",
                Code = "STANDARD",
                Description = "Accès complet : entraînements, tournois internes, exercices, forums et bibliothèque.",
                Price = new Money(1500m, currency),
                DurationDays = 365,
                MinimumAge = 18,
                DisplayOrder = 30
            },
            new MembershipType
            {
                Name = "Adhésion famille",
                Code = "FAMILLE",
                Description = "Jusqu'à quatre membres d'un même foyer.",
                Price = new Money(3000m, currency),
                DurationDays = 365,
                DisplayOrder = 40
            },
            new MembershipType
            {
                Name = "Membre de soutien",
                Code = "SOUTIEN",
                Description = "Vous soutenez le développement des échecs en Haïti au-delà de la cotisation ordinaire.",
                Price = new Money(5000m, currency),
                DurationDays = 365,
                DisplayOrder = 50
            });

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedBadgesAsync(CancellationToken ct)
    {
        if (await context.Badges.AnyAsync(ct))
        {
            return;
        }

        context.Badges.AddRange(
            new Badge
            {
                Name = "Premier pas",
                Code = "PREMIER_PAS",
                Description = "Vous avez résolu votre premier exercice.",
                Icon = "star",
                Color = "or",
                Criterion = BadgeCriterion.ExercicesResolus,
                Threshold = 1,
                Points = 5,
                DisplayOrder = 10
            },
            new Badge
            {
                Name = "Tacticien",
                Code = "TACTICIEN",
                Description = "Dix exercices résolus.",
                Icon = "lightning",
                Color = "or",
                Criterion = BadgeCriterion.ExercicesResolus,
                Threshold = 10,
                Points = 15,
                DisplayOrder = 20
            },
            new Badge
            {
                Name = "Stratège",
                Code = "STRATEGE",
                Description = "Cinquante exercices résolus.",
                Icon = "diagram-3",
                Color = "argent",
                Criterion = BadgeCriterion.ExercicesResolus,
                Threshold = 50,
                Points = 40,
                DisplayOrder = 30
            },
            new Badge
            {
                Name = "Maître des tactiques",
                Code = "MAITRE_TACTIQUE",
                Description = "Deux cents exercices résolus.",
                Icon = "mortarboard",
                Color = "or",
                Criterion = BadgeCriterion.ExercicesResolus,
                Threshold = 200,
                Points = 100,
                DisplayOrder = 40
            },
            new Badge
            {
                Name = "Régularité",
                Code = "SERIE_7",
                Description = "Sept jours consécutifs d'entraînement.",
                Icon = "calendar-check",
                Color = "vert",
                Criterion = BadgeCriterion.SerieQuotidienne,
                Threshold = 7,
                Points = 20,
                DisplayOrder = 50
            },
            new Badge
            {
                Name = "Discipline de fer",
                Code = "SERIE_30",
                Description = "Trente jours consécutifs d'entraînement.",
                Icon = "fire",
                Color = "rouge",
                Criterion = BadgeCriterion.SerieQuotidienne,
                Threshold = 30,
                Points = 60,
                DisplayOrder = 60
            },
            new Badge
            {
                Name = "Compétiteur",
                Code = "COMPETITEUR",
                Description = "Participation à trois tournois du club.",
                Icon = "trophy",
                Color = "or",
                Criterion = BadgeCriterion.TournoisJoues,
                Threshold = 3,
                Points = 30,
                DisplayOrder = 70
            },
            new Badge
            {
                Name = "Fidélité",
                Code = "FIDELITE",
                Description = "Trois années d'adhésion consécutives.",
                Icon = "heart",
                Color = "argent",
                Criterion = BadgeCriterion.AncienneteAdhesion,
                Threshold = 3,
                Points = 50,
                DisplayOrder = 80
            });

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedStaticPagesAsync(SiteOptions site, CancellationToken ct)
    {
        if (await context.StaticPages.AnyAsync(ct))
        {
            return;
        }

        context.StaticPages.AddRange(
            new StaticPage
            {
                Title = "Le club",
                Slug = "le-club",
                ShowInMenu = true,
                DisplayOrder = 10,
                MetaDescription = "Histoire, valeurs et fonctionnement du club d'échecs Cavalier Noir.",
                Content = """
                          <h2>Notre histoire</h2>
                          <p>Le Cavalier Noir est né de quelques parties disputées le samedi après-midi,
                          entre amis, autour d'un échiquier posé sur une table de fortune. De ces
                          rendez-vous informels est née l'idée d'un véritable club : un lieu où l'on
                          apprend, où l'on progresse, et où l'on transmet.</p>

                          <h2>Notre mission</h2>
                          <p>Faire des échecs un outil éducatif accessible à tous en Haïti. Le jeu
                          apprend la patience, la responsabilité de ses choix et le respect de
                          l'adversaire — trois qualités qui dépassent largement les soixante-quatre
                          cases.</p>

                          <h2>Nos activités</h2>
                          <ul>
                            <li>Entraînements hebdomadaires encadrés, par niveau ;</li>
                            <li>Tournois internes et opens nationaux ;</li>
                            <li>Exercice quotidien envoyé par courriel à tous les membres ;</li>
                            <li>Cours d'initiation pour les scolaires ;</li>
                            <li>Analyse collective des parties du week-end.</li>
                          </ul>

                          <h2>Nous rejoindre</h2>
                          <p>L'adhésion est ouverte à partir de six ans, sans niveau minimum requis.
                          Débutant complet ou joueur classé, vous trouverez au club un adversaire à
                          votre mesure et un entraîneur pour vous accompagner.</p>
                          """
            },
            new StaticPage
            {
                Title = "Mentions légales",
                Slug = "mentions-legales",
                ShowInFooter = true,
                IsSystemPage = true,
                DisplayOrder = 100,
                Content = $"""
                           <h2>Éditeur du site</h2>
                           <p>{site.Name}, association sportive.<br>
                           Siège : {site.Address}<br>
                           Contact : {site.ContactEmail}</p>

                           <h2>Directeur de la publication</h2>
                           <p>Le président en exercice de l'association.</p>

                           <h2>Hébergement</h2>
                           <p>Le site est hébergé sur un serveur dédié. Les coordonnées complètes de
                           l'hébergeur sont communiquées sur simple demande à l'adresse de contact
                           ci-dessus.</p>

                           <h2>Propriété intellectuelle</h2>
                           <p>Les contenus publiés sur ce site (articles, exercices, photographies)
                           sont la propriété de l'association ou de leurs auteurs respectifs. Toute
                           reproduction sans autorisation est interdite. Les positions d'échecs
                           relèvent du domaine public.</p>
                           """
            },
            new StaticPage
            {
                Title = "Politique de confidentialité",
                Slug = "confidentialite",
                ShowInFooter = true,
                IsSystemPage = true,
                DisplayOrder = 110,
                Content = """
                          <h2>Données que nous collectons</h2>
                          <p>Nous collectons uniquement ce qui est nécessaire à la vie de
                          l'association : identité, coordonnées, date de naissance (pour les
                          catégories d'âge), classement et historique sportif, ainsi que les données
                          liées à votre cotisation.</p>

                          <h2>Ce que nous ne faisons pas</h2>
                          <p>Nous ne vendons aucune donnée, nous n'utilisons ni régie publicitaire ni
                          traceur tiers, et nous ne conservons aucune donnée bancaire : les paiements
                          en ligne sont traités par un prestataire agréé.</p>

                          <h2>Durées de conservation</h2>
                          <ul>
                            <li>Compte inactif : anonymisation après deux ans ;</li>
                            <li>Journaux de connexion et d'audit : un an ;</li>
                            <li>Pièces comptables : dix ans (obligation légale) ;</li>
                            <li>Résultats sportifs : conservés sans limite, sous forme anonymisée si
                            vous supprimez votre compte.</li>
                          </ul>

                          <h2>Vos droits</h2>
                          <p>Depuis votre espace membre, vous pouvez à tout moment consulter et
                          rectifier vos données, exporter l'ensemble de vos informations, et demander
                          la suppression de votre compte. Une demande de suppression est traitée sous
                          trente jours.</p>

                          <h2>Cookies</h2>
                          <p>Le site dépose uniquement les cookies nécessaires à son fonctionnement :
                          session d'authentification, jeton anti-CSRF et préférence de thème. Aucun
                          consentement n'est requis pour ces cookies strictement nécessaires, et
                          aucun autre n'est déposé.</p>
                          """
            },
            new StaticPage
            {
                Title = "Palmarès",
                Slug = "palmares",
                ShowInMenu = true,
                DisplayOrder = 20,
                Content = """
                          <p>Le palmarès du club est alimenté automatiquement à l'issue de chaque
                          tournoi organisé sur la plateforme. Les résultats antérieurs à la mise en
                          service du site sont saisis par le secrétariat.</p>
                          """
            });

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedFaqAsync(CancellationToken ct)
    {
        if (await context.FaqItems.AnyAsync(ct))
        {
            return;
        }

        context.FaqItems.AddRange(
            new FaqItem
            {
                Category = "Adhésion",
                DisplayOrder = 10,
                Question = "Faut-il déjà savoir jouer pour adhérer ?",
                Answer = "Non. Le club accueille les débutants complets : une séance d'initiation "
                         + "est proposée chaque premier samedi du mois, matériel fourni."
            },
            new FaqItem
            {
                Category = "Adhésion",
                DisplayOrder = 20,
                Question = "À partir de quel âge peut-on s'inscrire ?",
                Answer = "À partir de six ans. Pour les mineurs, une autorisation parentale est "
                         + "demandée lors de l'inscription en ligne."
            },
            new FaqItem
            {
                Category = "Adhésion",
                DisplayOrder = 30,
                Question = "Comment payer sa cotisation ?",
                Answer = "En ligne par carte, par transfert mobile, ou en espèces auprès du "
                         + "trésorier lors d'une séance. Un reçu est émis dans tous les cas."
            },
            new FaqItem
            {
                Category = "Exercices",
                DisplayOrder = 40,
                Question = "Comment fonctionne l'exercice du jour ?",
                Answer = "Chaque matin à 6 h, un exercice différent est envoyé par courriel aux "
                         + "membres qui l'ont demandé. Le lien du courriel ouvre directement "
                         + "l'échiquier interactif et enregistre votre résultat."
            },
            new FaqItem
            {
                Category = "Exercices",
                DisplayOrder = 50,
                Question = "Sous quelle forme saisir la solution ?",
                Answer = "Il suffit de déplacer les pièces sur l'échiquier : le site enregistre "
                         + "votre coup. Vous pouvez aussi le saisir au clavier, par exemple « e2e4 »."
            },
            new FaqItem
            {
                Category = "Tournois",
                DisplayOrder = 60,
                Question = "Comment sont calculés les appariements ?",
                Answer = "Les tournois utilisent le système suisse : les joueurs de même score se "
                         + "rencontrent, sans jamais rejouer un adversaire déjà affronté, et les "
                         + "couleurs sont alternées autant que possible."
            },
            new FaqItem
            {
                Category = "Tournois",
                DisplayOrder = 70,
                Question = "Qu'est-ce que le classement ELO interne ?",
                Answer = "C'est un classement calculé selon la formule de la FIDE à partir des "
                         + "parties jouées au club. Un nouveau membre démarre entre 1200 et 1600 "
                         + "selon son niveau déclaré, puis son classement évolue à chaque partie."
            },
            new FaqItem
            {
                Category = "Compte",
                DisplayOrder = 80,
                Question = "Comment supprimer mon compte ?",
                Answer = "Depuis votre espace membre, rubrique « Mes données ». Vos informations "
                         + "personnelles sont effacées ; vos résultats sportifs sont conservés sous "
                         + "forme anonymisée pour préserver l'historique des tournois."
            });

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedBlogTaxonomyAsync(CancellationToken ct)
    {
        if (await context.BlogCategories.AnyAsync(ct))
        {
            return;
        }

        context.BlogCategories.AddRange(
            new BlogCategory { Name = "Vie du club", Slug = "vie-du-club", Color = "#d4af37", DisplayOrder = 10 },
            new BlogCategory { Name = "Analyses de parties", Slug = "analyses", Color = "#2980b9", DisplayOrder = 20 },
            new BlogCategory { Name = "Pédagogie", Slug = "pedagogie", Color = "#27ae60", DisplayOrder = 30 },
            new BlogCategory { Name = "Tournois", Slug = "tournois", Color = "#c0392b", DisplayOrder = 40 },
            new BlogCategory { Name = "Portraits", Slug = "portraits", Color = "#8e44ad", DisplayOrder = 50 });

        context.BlogTags.AddRange(
            new BlogTag { Name = "Ouvertures", Slug = "ouvertures" },
            new BlogTag { Name = "Finales", Slug = "finales" },
            new BlogTag { Name = "Tactique", Slug = "tactique" },
            new BlogTag { Name = "Débutants", Slug = "debutants" },
            new BlogTag { Name = "Jeunes", Slug = "jeunes" },
            new BlogTag { Name = "Haïti", Slug = "haiti" });

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedForumAsync(CancellationToken ct)
    {
        if (await context.ForumCategories.AnyAsync(ct))
        {
            return;
        }

        context.ForumCategories.AddRange(
            new ForumCategory
            {
                Name = "Annonces du club",
                Slug = "annonces",
                Description = "Informations officielles du bureau. Lecture pour tous, écriture réservée.",
                Icon = "megaphone",
                Visibility = Visibility.Membres,
                IsLocked = true,
                DisplayOrder = 10
            },
            new ForumCategory
            {
                Name = "Débutants",
                Slug = "debutants",
                Description = "Aucune question n'est bête. Posez-les ici.",
                Icon = "question-circle",
                Visibility = Visibility.Membres,
                DisplayOrder = 20
            },
            new ForumCategory
            {
                Name = "Ouvertures et théorie",
                Slug = "ouvertures",
                Description = "Répertoires, nouveautés, discussions théoriques.",
                Icon = "book",
                Visibility = Visibility.Membres,
                DisplayOrder = 30
            },
            new ForumCategory
            {
                Name = "Analyse de parties",
                Slug = "analyses",
                Description = "Partagez vos parties, joignez la position et recevez des retours.",
                Icon = "grid-3x3",
                Visibility = Visibility.Membres,
                DisplayOrder = 40
            },
            new ForumCategory
            {
                Name = "Vie du club",
                Slug = "vie-du-club",
                Description = "Covoiturage, matériel, organisation, propositions.",
                Icon = "people",
                Visibility = Visibility.Membres,
                DisplayOrder = 50
            });

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedExercisesAsync(CancellationToken ct)
    {
        if (await context.Exercises.AnyAsync(ct))
        {
            return;
        }

        var collection = new ExerciseCollection
        {
            Title = "Les fondamentaux",
            Slug = "les-fondamentaux",
            Description = "Onze positions à connaître par cœur : mats élémentaires et motifs tactiques de base.",
            Level = PlayerLevel.Debutant,
            IsPublished = true,
            DisplayOrder = 10
        };

        context.ExerciseCollections.Add(collection);
        await context.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;

        foreach (var seed in ExerciseSeedData.All)
        {
            if (!Fen.IsValid(seed.Fen))
            {
                logger.LogError("Position FEN invalide dans les données d'amorçage : {Title}.", seed.Title);
                continue;
            }

            context.Exercises.Add(new Exercise
            {
                Title = seed.Title,
                Fen = seed.Fen,
                Solution = seed.Solution,
                Theme = seed.Theme,
                Difficulty = seed.Difficulty,
                Hint1 = seed.Hint1,
                Hint2 = seed.Hint2,
                Hint3 = seed.Hint3,
                Explanation = seed.Explanation,
                IsPublished = true,
                IsPublic = seed.IsPublic,
                CollectionId = collection.Id,
                EstimatedTimeSeconds = 60 * (int)seed.Difficulty,
                Source = "Bibliothèque du club",
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("{Count} exercices de référence insérés.", ExerciseSeedData.All.Count);
    }
}
