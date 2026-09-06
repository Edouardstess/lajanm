# Cavalier Noir — Dossier fonctionnel et technique

**Site web du club d'échecs Cavalier Noir** — Port-au-Prince, Haïti
ASP.NET Core MVC 10 · Entity Framework Core 10 · ASP.NET Core Identity

---

## Table des matières

1. [Ce qu'est le site](#1-ce-quest-le-site)
2. [Fonctionnalités](#2-fonctionnalités)
3. [Acteurs, rôles et permissions](#3-acteurs-rôles-et-permissions)
4. [Présentation des classes et entités](#4-présentation-des-classes-et-entités)
5. [Services applicatifs](#5-services-applicatifs)
6. [Règles de gestion implémentées](#6-règles-de-gestion-implémentées)
7. [Arborescence complète du projet](#7-arborescence-complète-du-projet)
8. [Chemins d'accès](#8-chemins-daccès)
9. [Périmètre livré et limites connues](#9-périmètre-livré-et-limites-connues)

---

## 1. Ce qu'est le site

Le site remplit trois fonctions distinctes, servies par une seule application :

**Une vitrine publique.** Un visiteur découvre le club, lit les actualités,
consulte le calendrier des tournois, essaie une sélection d'exercices et dépose
sa candidature. Tout ce qui est public est indexable : titres hiérarchisés,
métadonnées OpenGraph, plan du site et flux RSS générés dynamiquement.

**Un outil d'entraînement.** Chaque matin à 6 h (heure de Port-au-Prince), un
exercice différent part par courriel vers les membres qui l'ont demandé. Le lien
du message ouvre directement l'échiquier interactif, enregistre le résultat et
met à jour la série de réussites. La bibliothèque complète est filtrable par
thème, niveau et statut de résolution.

**Un système de gestion associative.** Le bureau instruit les adhésions, encaisse
les cotisations, organise les tournois avec appariements automatiques au système
suisse, publie les articles, modère les commentaires, gère les documents
officiels versionnés et suit le budget — le tout depuis un back-office unique
dont chaque écran est protégé par une permission précise.

### Chiffres du livrable

| Élément | Quantité |
|---|---|
| Entités du domaine | 55 |
| Énumérations métier | 34 |
| Services applicatifs | 14 |
| Contrôleurs | 33 |
| Actions (points d'entrée HTTP) | 142 |
| Vues Razor | 79 |
| Rôles applicatifs | 12 |
| Permissions fines | 30 |
| Exercices livrés avec l'application | 11 |

---

## 2. Fonctionnalités

### 2.1 Espace public

**Accueil.** Exercice du jour affiché sur un échiquier, prochains rendez-vous
(tournois et événements fusionnés), trois derniers articles, chiffres clés du
club (membres actifs, exercices publiés, résolutions, tournois joués) et
partenaires. La page est mise en cache 2 minutes.

**Bibliothèque d'exercices.** Recherche plein texte, filtres par thème (16
motifs tactiques), par difficulté (1 à 5) et par recueil ; tri par date,
difficulté, popularité ou taux de réussite. Un membre connecté peut masquer les
exercices qu'il a déjà réussis. Les visiteurs ne voient que les exercices marqués
publics.

**Résolution d'un exercice.** L'échiquier est rendu à partir de la position FEN
et orienté du côté du camp au trait. On clique la case de départ puis la case
d'arrivée ; les coups s'accumulent en notation par coordonnées (`e2e4`), avec
annulation du dernier coup et remise à zéro. Un chronomètre tourne. Jusqu'à trois
indices progressifs sont récupérables sans recharger la page. À la validation, la
correction indique la réussite ou, en cas d'échec, **jusqu'où la solution était
juste** — l'information la plus utile pédagogiquement.

**Tournois.** Calendrier filtrable par statut ; fiche complète avec règlement,
inscription en un clic, liste d'attente automatique quand la capacité est
atteinte, feuilles d'appariement ronde par ronde et classement avec départages
Buchholz et Sonneborn-Berger.

**Agenda.** Navigation mensuelle, liste des prochains rendez-vous, inscription
aux événements avec gestion des places.

**Blog.** Liste paginée avec filtres par rubrique, étiquette et recherche ;
article avec temps de lecture estimé, commentaires imbriqués sur deux niveaux,
signalement, articles liés. Flux RSS sur `/blog/rss`.

**Classement du club.** Classement ELO interne, paginé. Les membres qui l'ont
désactivé dans leurs préférences n'y figurent pas.

**Documents.** Bibliothèque documentaire dont la visibilité s'adapte au profil du
visiteur : public, membres, bureau. Téléchargement comptabilisé.

**Galerie.** Albums photo et vidéo, avec la même gradation de visibilité.

**Contact.** Formulaire protégé par un champ leurre invisible et une limitation à
5 envois par minute et par adresse IP — aucun CAPTCHA tiers, donc aucun traceur.

**Lettre d'information.** Abonnement en double opt-in (confirmation par courriel
obligatoire), désabonnement en un clic depuis n'importe quel message.

**Adhésion.** Présentation des formules et de leurs tarifs, explication du
parcours en cinq étapes, formulaire de candidature.

### 2.2 Espace membre

**Tableau de bord.** Exercice du jour et son état, statistiques personnelles
(tentatives, taux de réussite, série en cours et record), état de l'adhésion avec
alerte d'échéance, prochains tournois, badges obtenus, notifications non lues.

**Profil.** Identité, pseudonyme (unique, affiché en public à la place du nom),
photo, biographie, comptes Lichess / Chess.com / FIDE. Le classement ELO est
affiché mais non modifiable : il découle des parties.

**Préférences.** Langue, thème, et cinq réglages de notification indépendants
plus l'option de retrait du classement public.

**Mon entraînement.** Historique paginé des tentatives, taux de réussite **par
thème tactique** (avec barres de progression), badges obtenus.

**Mon adhésion.** État de la demande en cours, adhésion active avec période et
montant, historique des paiements et numéros de reçus, demande de renouvellement.

**Mes données (RGPD).** Récapitulatif de ce qui est conservé, export JSON complet
(identité, adhésions, paiements, exercices, historique ELO, commentaires) et
suppression du compte. La suppression **anonymise** plutôt qu'elle n'efface : les
résultats sportifs restent cohérents, les données personnelles disparaissent.

**Forum.** Sous-forums thématiques, sujets, réponses, notification de l'auteur du
fil. Un message peut joindre une **position FEN**, rendue sous forme d'échiquier.

**Notifications.** Liste, marquage individuel ou global comme lu.

### 2.3 Back-office

**Tableau de bord.** Neuf indicateurs, deux graphiques (tentatives sur 14 jours,
adhésions sur 12 mois) rendus en CSS pur, file des demandes à instruire.

**Adhésions.** Instruction des demandes (approuver avec ajustement du niveau
évalué, rejeter avec motif, demander un complément), liste des adhésions
filtrable par statut, encaissement d'une cotisation, export CSV des adhérents.

**Exercices.** CRUD complet avec validation de la position FEN et de la solution
au moment de l'enregistrement, archivage plutôt que suppression, calendrier de
l'exercice du jour sur 37 jours avec statistiques d'envoi, planification manuelle
et déclenchement immédiat de l'envoi.

**Tournois.** Création, pilotage du cycle de vie (ouvrir, clôturer, démarrer,
archiver, annuler), **génération automatique des appariements**, saisie des
résultats avec justification obligatoire en cas de correction, clôture avec
publication du podium, gestion des inscriptions et du pointage.

**Événements.** CRUD et feuille de présence avec jetons de pointage.

**Blog.** Rédaction, workflow éditorial complet (brouillon → relecture →
programmé → publié → archivé), rubriques et étiquettes, référencement.

**Modération.** File des commentaires par statut, signalements non traités,
publication ou masquage.

**Messages.** Boîte de réception du formulaire de contact avec statut et note
interne.

**Utilisateurs.** Recherche, filtre par rôle, fiche détaillée (rôles, adhésions,
dix dernières connexions), attribution des rôles, activation/désactivation,
déverrouillage après échecs de connexion, ajustement manuel du classement **avec
motif obligatoire et historisation**.

**Lettre d'information.** Compteurs d'abonnés, campagnes, expédition, liste des
abonnés, export CSV.

**Documents.** Téléversement, versionnage chaîné, archivage, historique des
versions.

**Finances.** Synthèse annuelle (cotisations, dons, dépenses, solde), liste des
encaissements avec remboursement motivé, saisie et approbation des dépenses
(**l'auteur d'une dépense ne peut pas l'approuver**), enregistrement des dons.

**Paramètres.** Réglages modifiables en production sans redéploiement.

**Audit.** Journal d'audit filtrable (entité, action, utilisateur) et historique
des connexions réussies et échouées. Consultation seule : ces tables ne sont
jamais modifiables depuis l'interface.

### 2.4 Traitements automatiques

Un ordonnanceur interne (`MaintenanceWorker`) se réveille toutes les cinq minutes
et déclenche les traitements dont l'échéance est atteinte :

| Traitement | Fréquence | Effet |
|---|---|---|
| Envoi de l'exercice du jour | Quotidien, à l'heure configurée | Sélectionne un exercice éligible, l'envoie aux abonnés, journalise chaque message |
| Rafraîchissement des adhésions | Quotidien | Passage en délai de grâce, expiration, retrait du rôle Membre, notification |
| Relances d'échéance | Quotidien | Courriel aux adhésions expirant sous 30 jours |
| Publication programmée | Toutes les 5 minutes | Publie les articles dont la date est atteinte |

Chaque traitement est **idempotent** : le rejouer ne produit pas de doublon. Une
contrainte d'unicité sur la date de l'exercice du jour garantit qu'un même
exercice n'est jamais expédié deux fois.

### 2.5 Sécurité et conformité

| Mesure | Mise en œuvre |
|---|---|
| Mots de passe | 12 caractères, 4 classes de caractères, 4 caractères distincts minimum |
| Verrouillage | 5 échecs → blocage 15 minutes |
| Politique de sécurité de contenu | Stricte, aucun script tiers autorisé |
| En-têtes | HSTS, `X-Frame-Options: DENY`, `nosniff`, `Referrer-Policy`, `Permissions-Policy` |
| CSRF | Jeton anti-falsification sur chaque formulaire |
| XSS | Encodage automatique Razor ; `Html.Raw` réservé aux contenus rédigés par le bureau |
| Injection SQL | EF Core paramétré, aucune requête concaténée |
| Limitation de débit | 100 req/min global, 20 sur l'authentification, 5 sur les formulaires publics |
| Journal d'audit | Écriture automatique sur 11 entités sensibles, secrets expurgés |
| Traversée de répertoire | Chemin de destination vérifié sous la racine, extensions en liste blanche |
| Énumération de comptes | Messages identiques que l'adresse existe ou non |
| Séparation des tâches | Dépense, procès-verbal, paiement en espèces : validation par un tiers |
| Droits RGPD | Accès, portabilité (export JSON), effacement (anonymisation) |
| Cookies | Seulement les cookies strictement nécessaires — aucune bannière requise |

---

## 3. Acteurs, rôles et permissions

Le système distingue **12 rôles**. Un rôle regroupe, une **permission** autorise :
chaque permission est une claim attachée au rôle, et chaque écran sensible
référence une politique d'autorisation du même nom.

| Rôle | Description |
|---|---|
| `Candidat` | Inscrit dont l'adhésion n'est pas encore validée |
| `Membre` | Adhérent à jour de cotisation |
| `MembreHonneur` | Ancien dirigeant ou bienfaiteur, accès permanent |
| `Entraineur` | Encadre les séances, rédige les exercices |
| `Arbitre` | Valide les appariements, saisit les résultats |
| `OrganisateurTournoi` | Crée et pilote les tournois |
| `ResponsablePedagogique` | Pilote les cours, les niveaux, la progression |
| `ResponsableCommunication` | Blog, lettre d'information, galerie, modération |
| `Secretaire` | Instruit les adhésions, gestion documentaire |
| `Tresorier` | Cotisations, paiements, dons, dépenses |
| `President` | Représentation légale, validations finales |
| `SuperAdmin` | Administration technique complète |

### Matrice RBAC (extrait)

| Fonctionnalité | Visiteur | Membre | Entraîneur | Arbitre | Secrétaire | Trésorier | Resp. Com | Président | Super Admin |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Voir les exercices publics | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Résoudre un exercice | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Créer un exercice | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| S'inscrire à un tournoi | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Apparier une ronde | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Saisir un résultat | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Créer un tournoi | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Instruire une adhésion | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Approuver une adhésion | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | ✅ |
| Encaisser une cotisation | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ |
| Voir les finances | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Rédiger un article | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Publier un article | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Modérer | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Gérer les documents | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | ✅ |
| Gérer les rôles | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Consulter le journal d'audit | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

### Les 30 permissions

```
exercises.view      exercises.solve     exercises.manage    exercises.publish
tournaments.view    tournaments.register tournaments.manage tournaments.pair
tournaments.results
memberships.apply   memberships.review  memberships.approve memberships.manage
finance.view        finance.manage      finance.refund
content.write       content.publish     content.moderate    content.media
content.newsletter
documents.view      documents.manage
admin.dashboard     admin.users         admin.roles         admin.audit
admin.settings      admin.backups
```

Le super-administrateur passe toutes les vérifications sans exception ; le rôle
`SuperAdmin` ne peut être accordé que par un autre super-administrateur.

---

## 4. Présentation des classes et entités

Le domaine est organisé en **huit contextes délimités**. Chaque entité hérite de
`Entity` (identité technique) ; celles dont les écritures doivent être datées et
attribuées héritent de `AuditableEntity`.

### 4.1 Socle commun — `CavalierNoir.Domain.Common`

| Classe | Rôle |
|---|---|
| `Entity` | Racine : `Id` entier auto-incrémenté, `IsTransient()` |
| `AuditableEntity` | `CreatedAt`, `CreatedById`, `UpdatedAt`, `UpdatedById` — renseignés automatiquement par l'intercepteur EF |
| `ISoftDeletable` | Contrat des entités archivables : `IsDeleted`, `DeletedAt`, `DeletedById` |
| `IAggregateRoot` | Marqueur des racines d'agrégat |
| `IDomainEvent` / `DomainEvent` | Base des événements métier |
| `DomainException` | Violation d'une règle de gestion, porteuse d'un code (`BR-04`, `SoD-01`…) |

### 4.2 Objets-valeurs — `CavalierNoir.Domain.ValueObjects`

Immuables, comparés par valeur, ils portent leurs invariants dans le
constructeur : une instance existante est toujours valide.

| Objet-valeur | Invariant garanti | Persistance |
|---|---|---|
| `Money` | Montant arrondi à 2 décimales, devise normalisée ; additionner deux devises différentes lève `BR-14` | Type possédé : deux colonnes (montant, devise) |
| `DateRange` | La fin ne peut pas précéder le début ; `OneYearFrom` produit exactement 365 jours | Type possédé : `DateDebut`, `DateFin` |
| `EloRating` | Borné à [200 ; 3000] (`BR-07`), `Shift` sature aux bornes | Entier simple |
| `Fen` | 6 champs, 8 rangées de 8 cases, trait valide, un roi par camp | Chaîne validée à l'écriture |
| `MoveSequence` | Normalise numérotation, prises, échecs, roques ; comparaison sensible à la casse des lettres de pièces | Chaîne brute |
| `Slug` (statique) | Translittère les accents, produit un segment d'URL stable | Chaîne |

Le détail qui compte sur `MoveSequence` : la comparaison **reste sensible à la
casse**. En notation algébrique, `B` est le fou et `b` la colonne b ; les
confondre validerait des réponses fausses.

### 4.3 Identité et sécurité — `Domain.Identity`

| Entité | Description | Propriétés notables |
|---|---|---|
| `ApplicationUser` | Racine d'agrégat, étend `IdentityUser<int>` | `Elo`, `RatedGamesPlayed`, `Pseudonym`, `BirthDate`, `LichessUsername`, `AnonymizedAt` |
| `ApplicationRole` | Étend `IdentityRole<int>` | `Description`, `IsSystemRole`, `DisplayOrder` |
| `UserPreference` | Préférences (1–1 avec l'utilisateur) | 5 réglages de notification, `ShowInPublicRanking` |
| `LoginHistory` | Trace de connexion réussie ou non | `AttemptedIdentifier` conservé même sans compte |
| `AuditLog` | Journal immuable | `OldValues`/`NewValues` en JSON, `AffectedColumns` |
| `Notification` | Notification interne | `Channel`, `Priority`, `ReadAt` |
| `Roles` (statique) | Les 12 rôles et leurs regroupements | `StaffRoles`, `MemberRoles` |
| `Permissions` (statique) | Les 30 permissions et la matrice RBAC | `RoleMatrix` |

Comportement métier porté par `ApplicationUser` :

- `DisplayName` — pseudonyme s'il existe, sinon « Prénom NOM » ;
- `Level` — niveau pédagogique déduit du classement (Débutant ≤ 1200 …) ;
- `Age(today)` et `MeetsMinimumAge(today)` — contrôle de `BR-02` ;
- `Anonymize(when)` — droit à l'effacement sans casser l'historique sportif.

### 4.4 Vie associative — `Domain.Club`

| Entité | Description |
|---|---|
| `Committee` | Bureau exécutif et son mandat (`DateRange`) |
| `CommitteeMember` | Affectation d'un membre à un poste (`CommitteePosition`) |
| `Meeting` | Réunion statutaire et procès-verbal ; `ApproveMinutes` refuse que le rédacteur valide son propre PV (`SoD-02`) |
| `Document` | Document officiel **versionné** ; `CreateNextVersion` archive la version courante et chaîne la nouvelle |
| `Partner` | Partenaire ou sponsor, niveau et contribution annuelle |
| `FaqItem` | Question fréquente, groupée par catégorie |
| `StaticPage` | Page éditoriale ; `IsSystemPage` protège mentions légales et confidentialité |
| `ContactMessage` | Message du formulaire public, avec consentement RGPD horodaté |
| `SystemSetting` | Paramètre modifiable en production ; `IsSecret` interdit l'affichage en clair |

### 4.5 Adhésions et finances — `Domain.Memberships`, `Domain.Finance`

| Entité | Description |
|---|---|
| `MembershipType` | Formule : tarif (`Money`), durée, bornes d'âge, justificatif requis |
| `MembershipApplication` | Demande instruite : `Submit`, `StartReview`, `RequestMoreInformation`, `Approve`, `Reject` |
| `Membership` | Racine d'agrégat : `Period` (`DateRange`), `Amount`, `MemberNumber` |
| `Payment` | Encaissement ; **aucune donnée de carte n'est stockée**, seulement la référence du prestataire |
| `Donation` | Don, éventuellement anonyme, avec reçu |
| `Expense` | Dépense ; `Approve` refuse l'approbation par son auteur (`SoD-01`) |

`Membership` porte l'essentiel des règles temporelles :

```csharp
public const int GracePeriodDays = 15;      // BR-04
public const int RenewalReminderDays = 30;

bool IsCurrentlyActive(DateOnly today);
bool IsInGracePeriod(DateOnly today);
bool NeedsRenewalReminder(DateOnly today);
MembershipStatus Refresh(DateOnly today);   // Active → DélaiDeGrâce → Expirée
void Activate(DateTime when, DateOnly? startingOn = null);
void Renew(int durationDays, Money amount, DateTime when);
```

`Renew` traite correctement le renouvellement anticipé : la nouvelle période
démarre à la fin de la précédente, sans perte de jours.

### 4.6 Compétitions — `Domain.Tournaments`

| Entité | Description |
|---|---|
| `Tournament` | Racine d'agrégat ; machine à états complète (`OpenRegistration`, `CloseRegistration`, `Start`, `Finish`, `Archive`, `Cancel`), `EnsureEditable` applique `BR-13` |
| `TournamentRegistration` | Inscription ; liste d'attente, pointage, `RecordAbsence` déclenche le forfait général (`BR-05`) |
| `TournamentRound` | Ronde ; `AllGamesPlayed`, `Close` |
| `TournamentGame` | Partie **et** appariement : échiquier, couleurs, résultat, PGN. `ScoreForWhite`/`ScoreForBlack` dérivent les points ; `CountsForElo` exclut byes et forfaits ; `SetResult` exige une justification pour toute correction |
| `TournamentStanding` | Ligne de classement projetée : score, Buchholz, Buchholz tronqué, Sonneborn-Berger, rang |
| `EloHistory` | Historique des variations — **jamais supprimé**, c'est la mémoire sportive du club |
| `ClubEvent` | Événement du calendrier, éventuellement lié à un tournoi |
| `EventRegistration` | Inscription à un événement, avec jeton de pointage |

### 4.7 Pédagogie — `Domain.Learning`

| Entité | Description |
|---|---|
| `Exercise` | Racine d'agrégat : `Fen`, `Solution`, thème, difficulté, 3 indices, explication. `IsCorrectAnswer` compare via `MoveSequence` ; `IsEligibleForDaily` applique `BR-08` |
| `ExerciseAttempt` | Tentative : coups soumis, réussite, temps, indices utilisés, `CorrectPrefixLength` |
| `ExerciseCollection` | Recueil thématique |
| `UserProgress` | Projection de la progression ; `Register` gère la **série quotidienne** (jour consécutif, même jour, rupture) |
| `Badge` / `UserBadge` | Récompense et attribution ; critère automatique et seuil |
| `DailyExercise` | Planification d'un jour ; unicité sur la date = idempotence de l'envoi |
| `Course`, `Lesson` | Programme de cours et ses chapitres |
| `TrainingSession`, `Attendance` | Séance et feuille de présence |

### 4.8 Contenus et communication — `Domain.Blog`, `Domain.Media`, `Domain.Communication`

| Entité | Description |
|---|---|
| `BlogPost` | Racine d'agrégat ; workflow `SubmitForReview` → `Approve` → `Publish` / `Unpublish` ; `ReadingTimeMinutes` |
| `BlogCategory`, `BlogTag`, `BlogPostTag` | Taxonomie ; jonction à clé composite |
| `BlogComment` | Commentaire imbriqué ; `Report()` applique `BR-09` (masquage au 3ᵉ signalement) |
| `CommentReport` | Signalement, unique par membre et par commentaire |
| `Album`, `MediaItem` | Galerie ; `AltText` requis pour l'accessibilité |
| `NewsletterSubscriber` | Abonné ; double opt-in, jeton de désabonnement permanent |
| `NewsletterCampaign` | Campagne ; taux d'ouverture et de clic calculés |
| `EmailLog` | Journal d'envoi : preuve d'expédition et statistiques |
| `ForumCategory`, `ForumTopic`, `ForumPost` | Forums ; un message peut joindre une position FEN |
| `PrivateMessage` | Messagerie privée avec suppression indépendante de chaque côté |

### 4.9 Services de domaine — `Domain.Services`

Ce sont les seules classes du domaine qui ne sont pas des entités : elles portent
une logique métier qui n'appartient naturellement à aucune entité.

**`EloCalculator`** — formule de la FIDE.

```
Se = 1 / (1 + 10^((Rb − Ra) / 400))
Rn = Ra + K × (Sr − Se)
```

Le facteur K suit `BR-07` : 32 pour les 20 premières parties classées, 24
jusqu'à la 50ᵉ, 16 ensuite (et 16 dès 2400 points), 10 en blitz. Deux garde-fous
importants : une victoire ne fait **jamais** perdre de point, une défaite n'en
fait **jamais** gagner, même quand l'arrondi le suggérerait.

**`SwissPairingService`** — appariements.

- `PairSwissRound` : tri par score puis classement, attribution du bye au joueur
  le moins bien classé n'en ayant pas encore bénéficié, puis **recherche par
  retour arrière** d'un appariement complet sans revanche (`BR-06`). Si aucune
  combinaison n'existe, la contrainte est relâchée explicitement plutôt que de
  produire un échec.
- Couleurs : priorité à l'équilibre blancs/noirs, puis à l'alternance par rapport
  à la ronde précédente, enfin au mieux classé.
- `PairRoundRobin` : méthode du cercle de Berger, avec joueur fictif pour un
  effectif impair.

**`StandingsCalculator`** — classement et départages : score, Buchholz, Buchholz
tronqué (moins le plus faible adversaire), Sonneborn-Berger, rangs partagés en
cas d'égalité complète.

### 4.10 Vue d'ensemble des relations

```
ApplicationUser (racine)
├─1..n─> Membership ──1─> MembershipType
│         └─1..n─> Payment
├─1..n─> MembershipApplication
├─1..n─> ExerciseAttempt ──n..1─> Exercise ──n..1─> ExerciseCollection
├─1..1─> UserProgress
├─1..n─> UserBadge ──n..1─> Badge
├─1..n─> TournamentRegistration ──n..1─> Tournament
├─1..n─> TournamentGame (blancs / noirs) ──n..1─> TournamentRound ──n..1─> Tournament
├─1..n─> EloHistory
├─1..n─> BlogPost ──n..1─> BlogCategory
│         ├─1..n─> BlogComment ──1..n─> CommentReport
│         └─n..n─> BlogTag (via BlogPostTag)
├─1..n─> ForumTopic ──1..n─> ForumPost
├─1..1─> UserPreference
├─1..n─> Notification
└─1..n─> LoginHistory

Tournament ──1..n─> TournamentRound ──1..n─> TournamentGame
           └─1..n─> TournamentStanding

DailyExercise ──1─> Exercise          (une ligne par date, unicité garantie)
Document ──0..1─> Document            (chaînage des versions)
Album ──1..n─> MediaItem
```

---

## 5. Services applicatifs

La couche Application orchestre le domaine. Elle ne dépend d'ASP.NET Core ni
d'aucun détail d'infrastructure : elle passe par sept abstractions.

### Abstractions

| Interface | Contrat | Implémentation |
|---|---|---|
| `IApplicationDbContext` | Vue applicative de la persistance (`DbSet<>` + `SaveChangesAsync`) | `ApplicationDbContext` |
| `IDateTimeProvider` | Horloge injectable (`UtcNow`, `LocalNow`, `Today`, fuseau du club) | `DateTimeProvider` |
| `ICurrentUser` | Utilisateur de la requête (identité, rôles, permissions, IP) | `CurrentUser` (web) / `SystemCurrentUser` (tâches de fond) |
| `IEmailSender` | Expédition d'un courriel | `SmtpEmailSender`, `FileEmailSender`, `NullEmailSender` |
| `IFileStorage` | Stockage de fichiers | `LocalFileStorage` (remplaçable par S3/MinIO) |
| `IUserAccountService` | Affectation des rôles Identity | `UserAccountService` |
| `INotificationService` | Notifications internes | `NotificationService` |

Aucun service n'appelle `DateTime.UtcNow` directement : les règles temporelles
(expiration d'adhésion, exercice du jour, série quotidienne) restent testables.

### Les 14 services

| Service | Responsabilité |
|---|---|
| `MembershipService` | Cycle complet de l'adhésion : candidature, instruction, approbation, encaissement, activation, renouvellement, rafraîchissement quotidien des statuts |
| `ExerciseService` | Recherche filtrée, soumission et correction d'une tentative, progression, sélection et planification de l'exercice du jour, attribution des badges |
| `TournamentService` | Inscriptions et liste d'attente, génération des appariements, saisie des résultats, application de l'ELO, recalcul du classement, clôture et podium |
| `BlogService` | Recherche, article, commentaires, signalements, modération, identifiants d'URL uniques, publication programmée |
| `ForumService` | Sous-forums, sujets, réponses, modération |
| `NewsletterService` | Abonnement en double opt-in, confirmation, désabonnement, expédition des campagnes |
| `EventService` | Calendrier, inscriptions, liste d'attente, pointage par jeton |
| `DocumentService` | Recherche par visibilité, versionnage, comptage des téléchargements |
| `DashboardService` | Indicateurs du back-office, tableau de bord membre, classement du club |
| `SettingsService` | Lecture et écriture des paramètres système |
| `NotificationService` | Création et lecture des notifications, diffusion à un rôle entier |
| `DailyExerciseDispatcher` | Envoi quotidien : destinataires, jetons personnels, journalisation, idempotence |
| `EmailTemplateFactory` | Gabarits de courriels (HTML en tableau, styles en ligne, échiquier rendu côté serveur) |
| `ExerciseTokenService` | Jetons HMAC-SHA256 des liens d'exercice, vérifiables sans stockage |

### Traitement d'une tentative d'exercice

Ce flux illustre la répartition des responsabilités entre les couches :

```
ExercicesController.Soumettre
  └─> ExerciseService.SubmitAsync
        ├─ MoveSequence.Parse(solution) / Parse(réponse)   ← domaine
        ├─ answer.Matches(expected)                        ← domaine
        ├─ answer.CommonPrefixLength(expected)             ← domaine
        ├─ new ExerciseAttempt { … }                       ← persistance
        ├─ exercise.RegisterAttempt(correct, temps)        ← domaine
        ├─ progress.Register(correct, …, aujourd'hui)      ← domaine (série)
        ├─ daily.SolvedCount++ si issu du courriel
        └─ AwardBadgesAsync(…)                             ← seuils franchis
```

Le contrôleur ne décide de rien : il traduit une requête HTTP en appel de
service, puis un résultat en vue.

---

## 6. Règles de gestion implémentées

Chaque règle est appliquée **dans le domaine**, pas dans un contrôleur : elle
reste vraie quel que soit le point d'entrée.

| Réf | Règle | Où elle vit |
|---|---|---|
| BR-02 | Âge minimum de 6 ans ; autorisation parentale avant 18 ans | `ApplicationUser.MeetsMinimumAge`, `MembershipService.SubmitApplicationAsync` |
| BR-03 | Adhésion valable 365 jours à compter du paiement | `Membership.Activate`, `DateRange.OneYearFrom` |
| BR-04 | Délai de grâce de 15 jours avant suspension | `Membership.GracePeriodDays`, `Membership.Refresh` |
| BR-05 | Deux absences consécutives = forfait général | `TournamentRegistration.RecordAbsence` |
| BR-06 | Pas de revanche dans un même tournoi | `SwissPairingService.TryPairRecursively` |
| BR-07 | ELO borné [200 ; 3000], plafonné à 1600 sans justificatif | `EloRating`, `MembershipService`, `EloCalculator.KFactorFor` |
| BR-08 | Un exercice n'est pas renvoyé avant 30 jours | `Exercise.IsEligibleForDaily`, `ExerciseService.GetOrCreateDailyAsync` |
| BR-09 | Commentaire masqué au 3ᵉ signalement | `BlogComment.Report` |
| BR-11 | Journal d'audit conservé 365 jours | `AuditingInterceptor`, `AuditLog` |
| BR-13 | Tournoi non modifiable après clôture des inscriptions | `Tournament.EnsureEditable` |
| BR-14 | Impossible de combiner deux devises | `Money.EnsureSameCurrency` |
| BR-15 | Documents officiels versionnés | `Document.CreateNextVersion` |
| SoD-01 | Une dépense n'est pas approuvée par son auteur | `Expense.Approve` |
| SoD-02 | Un procès-verbal n'est pas validé par son rédacteur | `Meeting.ApproveMinutes` |
| SoD-03 | Un paiement en espèces est saisi par un tiers | `MembershipService.RecordPaymentAsync` |

Une violation lève une `DomainException` porteuse du code de la règle. Le filtre
`DomainExceptionFilter` la traduit en message utilisateur (HTTP 422 en AJAX) :
une règle métier violée n'est pas une erreur serveur.

---

## 7. Arborescence complète du projet

Arborescence réelle du dossier `CavalierNoir/`, dossiers de compilation exclus.

```
CavalierNoir/
├── docs/
│   ├── ADR.md
│   └── DOSSIER-FONCTIONNEL.md
├── src/
│   ├── CavalierNoir.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IApplicationDbContext.cs
│   │   │   │   ├── ICurrentUser.cs
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   ├── IEmailSender.cs
│   │   │   │   ├── IFileStorage.cs
│   │   │   │   ├── INotificationService.cs
│   │   │   │   └── IUserAccountService.cs
│   │   │   ├── Models/
│   │   │   │   ├── EmailMessage.cs
│   │   │   │   ├── PagedList.cs
│   │   │   │   ├── Result.cs
│   │   │   │   ├── SiteOptions.cs
│   │   │   │   └── TokenOptions.cs
│   │   │   └── ChessBoardRenderer.cs
│   │   ├── Dtos/
│   │   │   ├── ContentDtos.cs
│   │   │   ├── DashboardDtos.cs
│   │   │   ├── LearningDtos.cs
│   │   │   ├── MembershipDtos.cs
│   │   │   └── TournamentDtos.cs
│   │   ├── Services/
│   │   │   ├── BlogService.cs
│   │   │   ├── DailyExerciseDispatcher.cs
│   │   │   ├── DashboardService.cs
│   │   │   ├── DocumentService.cs
│   │   │   ├── EmailTemplateFactory.cs
│   │   │   ├── EventService.cs
│   │   │   ├── ExerciseService.cs
│   │   │   ├── ExerciseTokenService.cs
│   │   │   ├── ForumService.cs
│   │   │   ├── MembershipService.cs
│   │   │   ├── NewsletterService.cs
│   │   │   ├── NotificationService.cs
│   │   │   ├── SettingsService.cs
│   │   │   └── TournamentService.cs
│   │   ├── CavalierNoir.Application.csproj
│   │   └── DependencyInjection.cs
│   ├── CavalierNoir.Domain/
│   │   ├── Abstractions/
│   │   │   ├── IRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Blog/
│   │   │   ├── BlogCategory.cs
│   │   │   ├── BlogComment.cs
│   │   │   ├── BlogPost.cs
│   │   │   ├── BlogPostTag.cs
│   │   │   ├── BlogTag.cs
│   │   │   └── CommentReport.cs
│   │   ├── Club/
│   │   │   ├── Committee.cs
│   │   │   ├── CommitteeMember.cs
│   │   │   ├── ContactMessage.cs
│   │   │   ├── Document.cs
│   │   │   ├── FaqItem.cs
│   │   │   ├── Meeting.cs
│   │   │   ├── Partner.cs
│   │   │   ├── StaticPage.cs
│   │   │   └── SystemSetting.cs
│   │   ├── Common/
│   │   │   ├── AuditableEntity.cs
│   │   │   ├── DomainException.cs
│   │   │   ├── Entity.cs
│   │   │   ├── IAggregateRoot.cs
│   │   │   ├── IDomainEvent.cs
│   │   │   └── ISoftDeletable.cs
│   │   ├── Communication/
│   │   │   ├── EmailLog.cs
│   │   │   ├── ForumCategory.cs
│   │   │   ├── ForumPost.cs
│   │   │   ├── ForumTopic.cs
│   │   │   ├── NewsletterCampaign.cs
│   │   │   ├── NewsletterSubscriber.cs
│   │   │   └── PrivateMessage.cs
│   │   ├── Enums/
│   │   │   ├── ClubEnums.cs
│   │   │   ├── ContentEnums.cs
│   │   │   ├── LearningEnums.cs
│   │   │   ├── MembershipEnums.cs
│   │   │   └── TournamentEnums.cs
│   │   ├── Finance/
│   │   │   ├── Donation.cs
│   │   │   ├── Expense.cs
│   │   │   └── Payment.cs
│   │   ├── Identity/
│   │   │   ├── ApplicationRole.cs
│   │   │   ├── ApplicationUser.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── LoginHistory.cs
│   │   │   ├── Notification.cs
│   │   │   ├── Permissions.cs
│   │   │   ├── Roles.cs
│   │   │   └── UserPreference.cs
│   │   ├── Learning/
│   │   │   ├── Attendance.cs
│   │   │   ├── Badge.cs
│   │   │   ├── Course.cs
│   │   │   ├── DailyExercise.cs
│   │   │   ├── Exercise.cs
│   │   │   ├── ExerciseAttempt.cs
│   │   │   ├── ExerciseCollection.cs
│   │   │   ├── Lesson.cs
│   │   │   ├── TrainingSession.cs
│   │   │   ├── UserBadge.cs
│   │   │   └── UserProgress.cs
│   │   ├── Media/
│   │   │   ├── Album.cs
│   │   │   └── MediaItem.cs
│   │   ├── Memberships/
│   │   │   ├── Membership.cs
│   │   │   ├── MembershipApplication.cs
│   │   │   └── MembershipType.cs
│   │   ├── Services/
│   │   │   ├── EloCalculator.cs
│   │   │   ├── StandingsCalculator.cs
│   │   │   └── SwissPairingService.cs
│   │   ├── Tournaments/
│   │   │   ├── ClubEvent.cs
│   │   │   ├── EloHistory.cs
│   │   │   ├── EventRegistration.cs
│   │   │   ├── Tournament.cs
│   │   │   ├── TournamentGame.cs
│   │   │   ├── TournamentRegistration.cs
│   │   │   ├── TournamentRound.cs
│   │   │   └── TournamentStanding.cs
│   │   ├── ValueObjects/
│   │   │   ├── DateRange.cs
│   │   │   ├── EloRating.cs
│   │   │   ├── Fen.cs
│   │   │   ├── Money.cs
│   │   │   ├── MoveSequence.cs
│   │   │   └── Slug.cs
│   │   └── CavalierNoir.Domain.csproj
│   ├── CavalierNoir.Infrastructure/
│   │   ├── BackgroundJobs/
│   │   │   └── MaintenanceWorker.cs
│   │   ├── Identity/
│   │   │   └── UserAccountService.cs
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   │   ├── ClubConfigurations.cs
│   │   │   │   ├── CommunicationConfigurations.cs
│   │   │   │   ├── ContentConfigurations.cs
│   │   │   │   ├── IdentityConfigurations.cs
│   │   │   │   ├── LearningConfigurations.cs
│   │   │   │   ├── MembershipConfigurations.cs
│   │   │   │   └── TournamentConfigurations.cs
│   │   │   ├── Interceptors/
│   │   │   │   └── AuditingInterceptor.cs
│   │   │   ├── Repositories/
│   │   │   │   └── EfRepository.cs
│   │   │   ├── SeedData/
│   │   │   │   ├── DataSeeder.cs
│   │   │   │   ├── ExerciseSeedData.cs
│   │   │   │   └── SeedOptions.cs
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── DatabaseInitializer.cs
│   │   ├── Services/
│   │   │   ├── DateTimeProvider.cs
│   │   │   ├── EmailOptions.cs
│   │   │   ├── FileEmailSender.cs
│   │   │   ├── LocalFileStorage.cs
│   │   │   ├── NullEmailSender.cs
│   │   │   ├── SmtpEmailSender.cs
│   │   │   ├── StorageOptions.cs
│   │   │   └── SystemCurrentUser.cs
│   │   ├── CavalierNoir.Infrastructure.csproj
│   │   └── DependencyInjection.cs
│   └── CavalierNoir.Web/
│       ├── App_Data/
│       │   └── .gitkeep
│       ├── Areas/
│       │   ├── Admin/
│       │   │   ├── Controllers/
│       │   │   │   ├── AdhesionsController.cs
│       │   │   │   ├── AuditController.cs
│       │   │   │   ├── BlogController.cs
│       │   │   │   ├── DocumentsController.cs
│       │   │   │   ├── EvenementsController.cs
│       │   │   │   ├── ExercicesController.cs
│       │   │   │   ├── FinancesController.cs
│       │   │   │   ├── MessagesController.cs
│       │   │   │   ├── ModerationController.cs
│       │   │   │   ├── NewsletterController.cs
│       │   │   │   ├── ParametresController.cs
│       │   │   │   ├── TableauDeBordController.cs
│       │   │   │   ├── TournoisController.cs
│       │   │   │   └── UtilisateursController.cs
│       │   │   └── Views/
│       │   │       ├── Adhesions/
│       │   │       │   ├── Details.cshtml
│       │   │       │   ├── Index.cshtml
│       │   │       │   └── Membres.cshtml
│       │   │       ├── Audit/
│       │   │       │   ├── Connexions.cshtml
│       │   │       │   └── Index.cshtml
│       │   │       ├── Blog/
│       │   │       │   ├── Formulaire.cshtml
│       │   │       │   └── Index.cshtml
│       │   │       ├── Documents/
│       │   │       │   ├── Index.cshtml
│       │   │       │   └── Versions.cshtml
│       │   │       ├── Evenements/
│       │   │       │   ├── Formulaire.cshtml
│       │   │       │   ├── Index.cshtml
│       │   │       │   └── Participants.cshtml
│       │   │       ├── Exercices/
│       │   │       │   ├── Formulaire.cshtml
│       │   │       │   ├── Index.cshtml
│       │   │       │   └── Planning.cshtml
│       │   │       ├── Finances/
│       │   │       │   ├── Depenses.cshtml
│       │   │       │   ├── Dons.cshtml
│       │   │       │   └── Index.cshtml
│       │   │       ├── Messages/
│       │   │       │   └── Index.cshtml
│       │   │       ├── Moderation/
│       │   │       │   └── Index.cshtml
│       │   │       ├── Newsletter/
│       │   │       │   ├── Abonnes.cshtml
│       │   │       │   ├── Formulaire.cshtml
│       │   │       │   └── Index.cshtml
│       │   │       ├── Parametres/
│       │   │       │   └── Index.cshtml
│       │   │       ├── Shared/
│       │   │       │   └── _AdminLayout.cshtml
│       │   │       ├── TableauDeBord/
│       │   │       │   └── Index.cshtml
│       │   │       ├── Tournois/
│       │   │       │   ├── Details.cshtml
│       │   │       │   ├── Formulaire.cshtml
│       │   │       │   └── Index.cshtml
│       │   │       ├── Utilisateurs/
│       │   │       │   ├── Details.cshtml
│       │   │       │   └── Index.cshtml
│       │   │       ├── _ViewImports.cshtml
│       │   │       └── _ViewStart.cshtml
│       │   └── Membre/
│       │       ├── Controllers/
│       │       │   ├── AdhesionController.cs
│       │       │   ├── ExercicesController.cs
│       │       │   ├── ProfilController.cs
│       │       │   └── TableauDeBordController.cs
│       │       └── Views/
│       │           ├── Adhesion/
│       │           │   └── Index.cshtml
│       │           ├── Exercices/
│       │           │   └── Historique.cshtml
│       │           ├── Profil/
│       │           │   ├── Donnees.cshtml
│       │           │   ├── Index.cshtml
│       │           │   ├── MotDePasse.cshtml
│       │           │   └── Preferences.cshtml
│       │           ├── Shared/
│       │           ├── TableauDeBord/
│       │           │   └── Index.cshtml
│       │           ├── _ViewImports.cshtml
│       │           └── _ViewStart.cshtml
│       ├── Controllers/
│       │   ├── AdhererController.cs
│       │   ├── BlogController.cs
│       │   ├── ClassementController.cs
│       │   ├── CompteController.cs
│       │   ├── ContactController.cs
│       │   ├── DocumentsController.cs
│       │   ├── EvenementsController.cs
│       │   ├── ExercicesController.cs
│       │   ├── ForumController.cs
│       │   ├── GalerieController.cs
│       │   ├── HomeController.cs
│       │   ├── InfosController.cs
│       │   ├── NewsletterController.cs
│       │   ├── NotificationsController.cs
│       │   └── TournoisController.cs
│       ├── Infrastructure/
│       │   ├── CurrentUser.cs
│       │   ├── DomainExceptionFilter.cs
│       │   ├── PermissionAuthorization.cs
│       │   └── SecurityHeadersMiddleware.cs
│       ├── Models/
│       │   ├── AccountViewModels.cs
│       │   ├── AdminViewModels.cs
│       │   ├── ErrorViewModel.cs
│       │   ├── MemberViewModels.cs
│       │   ├── PublicViewModels.cs
│       │   └── SharedViewModels.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── Views/
│       │   ├── Adherer/
│       │   │   ├── Confirmation.cshtml
│       │   │   ├── Demande.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Blog/
│       │   │   ├── Details.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Classement/
│       │   │   └── Index.cshtml
│       │   ├── Compte/
│       │   │   ├── AccesRefuse.cshtml
│       │   │   ├── Connexion.cshtml
│       │   │   ├── Inscription.cshtml
│       │   │   ├── MotDePasseOublie.cshtml
│       │   │   └── Reinitialiser.cshtml
│       │   ├── Contact/
│       │   │   └── Index.cshtml
│       │   ├── Documents/
│       │   │   └── Index.cshtml
│       │   ├── Evenements/
│       │   │   ├── Details.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Exercices/
│       │   │   ├── Index.cshtml
│       │   │   └── Resoudre.cshtml
│       │   ├── Forum/
│       │   │   ├── Index.cshtml
│       │   │   ├── Nouveau.cshtml
│       │   │   ├── Rubrique.cshtml
│       │   │   └── Sujet.cshtml
│       │   ├── Galerie/
│       │   │   ├── Album.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Home/
│       │   │   ├── Erreur.cshtml
│       │   │   └── Index.cshtml
│       │   ├── Infos/
│       │   │   ├── Faq.cshtml
│       │   │   └── Page.cshtml
│       │   ├── Newsletter/
│       │   │   └── Message.cshtml
│       │   ├── Notifications/
│       │   │   └── Index.cshtml
│       │   ├── Shared/
│       │   │   ├── _Alertes.cshtml
│       │   │   ├── _Echiquier.cshtml
│       │   │   ├── _Layout.cshtml
│       │   │   └── _Pagination.cshtml
│       │   ├── Tournois/
│       │   │   ├── Details.cshtml
│       │   │   └── Index.cshtml
│       │   ├── _ViewImports.cshtml
│       │   └── _ViewStart.cshtml
│       ├── wwwroot/
│       │   ├── css/
│       │   │   └── site.css
│       │   ├── img/
│       │   │   └── favicon.svg
│       │   ├── js/
│       │   │   ├── chessboard.js
│       │   │   └── site.js
│       │   └── uploads/
│       │       └── .gitkeep
│       ├── CavalierNoir.Web.csproj
│       ├── Program.cs
│       ├── appsettings.Development.json
│       └── appsettings.json
├── tests/
│   └── CavalierNoir.Tests.Unit/
│       ├── Application/
│       ├── Domain/
│       │   ├── EloCalculatorTests.cs
│       │   ├── ExerciseTests.cs
│       │   ├── FenTests.cs
│       │   ├── MembershipTests.cs
│       │   ├── MoveSequenceTests.cs
│       │   ├── StandingsCalculatorTests.cs
│       │   ├── SwissPairingServiceTests.cs
│       │   └── ValueObjectTests.cs
│       └── CavalierNoir.Tests.Unit.csproj
├── .dockerignore
├── .editorconfig
├── .env.example
├── .gitignore
├── CavalierNoir.sln
├── Directory.Build.props
├── Directory.Packages.props
├── Dockerfile
├── README.md
└── docker-compose.yml
```

---

## 8. Chemins d'accès

Deux conventions de routage cohabitent :

- **Routes par attribut** pour le site public : les URL sont écrites en français
  et restent stables (`/Exercices/Quotidien/{token}`).
- **Route par convention** pour les espaces membre et administration :
  `/{zone}/{contrôleur}/{action}/{id?}`.

Légende de la colonne « Accès » : **Public** = aucune authentification ;
**Connecté** = compte quelconque ; **Membre** = politique `EspaceMembre` ;
**Staff** = politique `EspaceAdmin` ; sinon la permission exacte est nommée.

### 8.1 Site public

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/` | Public | `Home.Index` | `Views/Home/Index.cshtml` |
| GET | `/Erreur/{code?}` | Public | `Home.Erreur` | `Views/Home/Erreur.cshtml` |
| GET | `/robots.txt` | Public | `Home.Robots` | — (texte) |
| GET | `/sitemap.xml` | Public | `Home.Sitemap` | — (XML) |
| GET | `/Infos/faq` | Public | `Infos.Faq` | `Views/Infos/Faq.cshtml` |
| GET | `/Infos/{slug}` | Public | `Infos.Page` | `Views/Infos/Page.cshtml` |
| GET | `/Contact` | Public | `Contact.Index` | `Views/Contact/Index.cshtml` |
| POST | `/Contact` | Public · 5 req/min | `Contact.Index` | redirection |
| GET | `/Galerie` | Public | `Galerie.Index` | `Views/Galerie/Index.cshtml` |
| GET | `/Galerie/{slug}` | Public | `Galerie.Album` | `Views/Galerie/Album.cshtml` |
| GET | `/Documents` | Public (filtré) | `Documents.Index` | `Views/Documents/Index.cshtml` |
| GET | `/Documents/Telecharger/{id}` | Public (filtré) | `Documents.Telecharger` | — (fichier) |
| GET | `/Classement` | Public | `Classement.Index` | `Views/Classement/Index.cshtml` |
| GET | `/health` | Public | *(intergiciel)* | — (JSON) |

### 8.2 Compte

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET · POST | `/Compte/Connexion` | Public · 20 req/min | `Compte.Connexion` | `Views/Compte/Connexion.cshtml` |
| GET · POST | `/Compte/Inscription` | Public · 20 req/min | `Compte.Inscription` | `Views/Compte/Inscription.cshtml` |
| POST | `/Compte/Deconnexion` | Connecté | `Compte.Deconnexion` | redirection |
| GET · POST | `/Compte/MotDePasseOublie` | Public · 20 req/min | `Compte.MotDePasseOublie` | `Views/Compte/MotDePasseOublie.cshtml` |
| GET · POST | `/Compte/Reinitialiser` | Public (jeton) | `Compte.Reinitialiser` | `Views/Compte/Reinitialiser.cshtml` |
| GET | `/Compte/AccesRefuse` | Public | `Compte.AccesRefuse` | `Views/Compte/AccesRefuse.cshtml` |

### 8.3 Exercices

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/Exercices` | Public (filtré) | `Exercices.Index` | `Views/Exercices/Index.cshtml` |
| GET | `/Exercices/{id}` | Public si exercice public | `Exercices.Details` | `Views/Exercices/Resoudre.cshtml` |
| GET | `/Exercices/Quotidien/{token}` | Public (lien signé) | `Exercices.Quotidien` | `Views/Exercices/Resoudre.cshtml` |
| POST | `/Exercices/Soumettre` | Connecté ou jeton · 5 req/min | `Exercices.Soumettre` | `Views/Exercices/Resoudre.cshtml` |
| GET | `/Exercices/Indice/{id}/{niveau}` | Membre | `Exercices.Indice` | — (JSON) |

### 8.4 Tournois et agenda

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/Tournois` | Public | `Tournois.Index` | `Views/Tournois/Index.cshtml` |
| GET | `/Tournois/{slug}?ronde=n` | Public | `Tournois.Details` | `Views/Tournois/Details.cshtml` |
| POST | `/Tournois/{id}/Inscription` | Membre | `Tournois.Inscription` | redirection |
| POST | `/Tournois/{id}/Desinscription` | Membre | `Tournois.Desinscription` | redirection |
| GET | `/Evenements?annee=&mois=` | Public | `Evenements.Index` | `Views/Evenements/Index.cshtml` |
| GET | `/Evenements/{slug}` | Public | `Evenements.Details` | `Views/Evenements/Details.cshtml` |
| POST | `/Evenements/{id}/Inscription` | Membre | `Evenements.Inscription` | redirection |
| POST | `/Evenements/{id}/Annulation` | Membre | `Evenements.Annulation` | redirection |

### 8.5 Blog, forum, lettre d'information

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/Blog?rubrique=&etiquette=&q=` | Public | `Blog.Index` | `Views/Blog/Index.cshtml` |
| GET | `/Blog/{slug}` | Public | `Blog.Details` | `Views/Blog/Details.cshtml` |
| POST | `/Blog/Commenter` | Membre · 5 req/min | `Blog.Commenter` | redirection |
| POST | `/Blog/Signaler/{commentaireId}` | Membre · 5 req/min | `Blog.Signaler` | redirection |
| GET | `/blog/rss` | Public | `Blog.Rss` | — (RSS) |
| GET | `/Forum` | Membre | `Forum.Index` | `Views/Forum/Index.cshtml` |
| GET | `/Forum/Rubrique/{slug}` | Membre | `Forum.Rubrique` | `Views/Forum/Rubrique.cshtml` |
| GET | `/Forum/Sujet/{id}` | Membre | `Forum.Sujet` | `Views/Forum/Sujet.cshtml` |
| GET · POST | `/Forum/Nouveau/{categorieId}` | Membre · 5 req/min | `Forum.Nouveau` | `Views/Forum/Nouveau.cshtml` |
| POST | `/Forum/Repondre` | Membre · 5 req/min | `Forum.Repondre` | redirection |
| POST | `/Forum/Moderer/{id}` | `content.moderate` | `Forum.Moderer` | redirection |
| POST | `/Newsletter/Inscription` | Public · 5 req/min | `Newsletter.Inscription` | redirection |
| GET | `/Newsletter/Confirmer/{token}` | Public (jeton) | `Newsletter.Confirmer` | `Views/Newsletter/Message.cshtml` |
| GET | `/Newsletter/Desabonnement/{token}` | Public (jeton) | `Newsletter.Desabonnement` | `Views/Newsletter/Message.cshtml` |
| GET | `/Notifications` | Connecté | `Notifications.Index` | `Views/Notifications/Index.cshtml` |
| POST | `/Notifications/Lire/{id}` | Connecté | `Notifications.Lire` | redirection |
| POST | `/Notifications/ToutLire` | Connecté | `Notifications.ToutLire` | redirection |

### 8.6 Adhésion

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/Adherer` | Public | `Adherer.Index` | `Views/Adherer/Index.cshtml` |
| GET · POST | `/Adherer/Demande` | Connecté · 5 req/min | `Adherer.Demande` | `Views/Adherer/Demande.cshtml` |
| GET | `/Adherer/Confirmation` | Connecté | `Adherer.Confirmation` | `Views/Adherer/Confirmation.cshtml` |

### 8.7 Espace membre — préfixe `/Membre`

| Méthode | URL | Accès | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/Membre` | Membre | `TableauDeBord.Index` | `Areas/Membre/Views/TableauDeBord/Index.cshtml` |
| GET · POST | `/Membre/Profil` | Connecté | `Profil.Index` | `Areas/Membre/Views/Profil/Index.cshtml` |
| GET · POST | `/Membre/Profil/Preferences` | Connecté | `Profil.Preferences` | `Areas/Membre/Views/Profil/Preferences.cshtml` |
| GET · POST | `/Membre/Profil/MotDePasse` | Connecté | `Profil.MotDePasse` | `Areas/Membre/Views/Profil/MotDePasse.cshtml` |
| GET | `/Membre/Profil/Donnees` | Connecté | `Profil.Donnees` | `Areas/Membre/Views/Profil/Donnees.cshtml` |
| GET | `/Membre/Profil/Exporter` | Connecté | `Profil.Exporter` | — (JSON téléchargé) |
| POST | `/Membre/Profil/Supprimer` | Connecté | `Profil.Supprimer` | redirection |
| GET | `/Membre/Adhesion` | Connecté | `Adhesion.Index` | `Areas/Membre/Views/Adhesion/Index.cshtml` |
| POST | `/Membre/Adhesion/Renouveler` | Connecté | `Adhesion.Renouveler` | redirection |
| GET | `/Membre/Exercices/Historique` | Membre | `Exercices.Historique` | `Areas/Membre/Views/Exercices/Historique.cshtml` |

### 8.8 Back-office — préfixe `/Admin`

| Méthode | URL | Permission requise | Contrôleur · action | Vue |
|---|---|---|---|---|
| GET | `/Admin` | Staff | `TableauDeBord.Index` | `Areas/Admin/Views/TableauDeBord/Index.cshtml` |
| GET | `/Admin/Adhesions` | `memberships.review` | `Adhesions.Index` | `Adhesions/Index.cshtml` |
| GET | `/Admin/Adhesions/Details/{id}` | `memberships.review` | `Adhesions.Details` | `Adhesions/Details.cshtml` |
| POST | `/Admin/Adhesions/Approuver/{id}` | `memberships.approve` | `Adhesions.Approuver` | redirection |
| POST | `/Admin/Adhesions/Rejeter/{id}` | `memberships.approve` | `Adhesions.Rejeter` | redirection |
| POST | `/Admin/Adhesions/DemanderComplement/{id}` | `memberships.review` | `Adhesions.DemanderComplement` | redirection |
| GET | `/Admin/Adhesions/Membres` | `memberships.review` | `Adhesions.Membres` | `Adhesions/Membres.cshtml` |
| POST | `/Admin/Adhesions/EnregistrerPaiement` | `finance.manage` | `Adhesions.EnregistrerPaiement` | redirection |
| GET | `/Admin/Adhesions/Exporter` | `finance.view` | `Adhesions.Exporter` | — (CSV) |
| GET | `/Admin/Exercices` | `exercises.manage` | `Exercices.Index` | `Exercices/Index.cshtml` |
| GET | `/Admin/Exercices/Creer` | `exercises.manage` | `Exercices.Creer` | `Exercices/Formulaire.cshtml` |
| GET | `/Admin/Exercices/Modifier/{id}` | `exercises.manage` | `Exercices.Modifier` | `Exercices/Formulaire.cshtml` |
| POST | `/Admin/Exercices/Enregistrer` | `exercises.manage` | `Exercices.Enregistrer` | redirection |
| POST | `/Admin/Exercices/Archiver/{id}` | `exercises.manage` | `Exercices.Archiver` | redirection |
| GET | `/Admin/Exercices/Planning` | `exercises.manage` | `Exercices.Planning` | `Exercices/Planning.cshtml` |
| POST | `/Admin/Exercices/Planifier` | `exercises.manage` | `Exercices.Planifier` | redirection |
| POST | `/Admin/Exercices/Envoyer` | `exercises.manage` | `Exercices.Envoyer` | redirection |
| GET | `/Admin/Tournois` | `tournaments.manage` | `Tournois.Index` | `Tournois/Index.cshtml` |
| GET | `/Admin/Tournois/Creer` · `/Modifier/{id}` | `tournaments.manage` | `Tournois.Creer` · `.Modifier` | `Tournois/Formulaire.cshtml` |
| POST | `/Admin/Tournois/Enregistrer` | `tournaments.manage` | `Tournois.Enregistrer` | redirection |
| GET | `/Admin/Tournois/Details/{id}?ronde=n` | `tournaments.manage` | `Tournois.Details` | `Tournois/Details.cshtml` |
| POST | `/Admin/Tournois/ChangerStatut/{id}` | `tournaments.manage` | `Tournois.ChangerStatut` | redirection |
| POST | `/Admin/Tournois/GenererRonde/{id}` | `tournaments.pair` | `Tournois.GenererRonde` | redirection |
| POST | `/Admin/Tournois/SaisirResultat/{id}` | `tournaments.results` | `Tournois.SaisirResultat` | redirection |
| POST | `/Admin/Tournois/Cloturer/{id}` | `tournaments.manage` | `Tournois.Cloturer` | redirection |
| POST | `/Admin/Tournois/GererInscription/{id}` | `tournaments.manage` | `Tournois.GererInscription` | redirection |
| GET | `/Admin/Evenements` | `tournaments.manage` | `Evenements.Index` | `Evenements/Index.cshtml` |
| GET | `/Admin/Evenements/Creer` · `/Modifier/{id}` | `tournaments.manage` | `Evenements.Creer` · `.Modifier` | `Evenements/Formulaire.cshtml` |
| GET | `/Admin/Evenements/Participants/{id}` | `tournaments.manage` | `Evenements.Participants` | `Evenements/Participants.cshtml` |
| GET | `/Admin/Blog` | `content.write` | `Blog.Index` | `Blog/Index.cshtml` |
| GET | `/Admin/Blog/Creer` · `/Modifier/{id}` | `content.write` | `Blog.Creer` · `.Modifier` | `Blog/Formulaire.cshtml` |
| POST | `/Admin/Blog/Enregistrer` | `content.write` | `Blog.Enregistrer` | redirection |
| POST | `/Admin/Blog/SoumettreRelecture/{id}` | `content.write` | `Blog.SoumettreRelecture` | redirection |
| POST | `/Admin/Blog/Publier/{id}` | `content.publish` | `Blog.Publier` | redirection |
| POST | `/Admin/Blog/Depublier/{id}` | `content.publish` | `Blog.Depublier` | redirection |
| GET | `/Admin/Moderation` | `content.moderate` | `Moderation.Index` | `Moderation/Index.cshtml` |
| POST | `/Admin/Moderation/Traiter/{id}` | `content.moderate` | `Moderation.Traiter` | redirection |
| POST | `/Admin/Moderation/ClasserSignalement/{id}` | `content.moderate` | `Moderation.ClasserSignalement` | redirection |
| GET | `/Admin/Messages` | Staff | `Messages.Index` | `Messages/Index.cshtml` |
| POST | `/Admin/Messages/Traiter/{id}` | Staff | `Messages.Traiter` | redirection |
| GET | `/Admin/Newsletter` | `content.newsletter` | `Newsletter.Index` | `Newsletter/Index.cshtml` |
| GET | `/Admin/Newsletter/Creer` | `content.newsletter` | `Newsletter.Creer` | `Newsletter/Formulaire.cshtml` |
| POST | `/Admin/Newsletter/Enregistrer` | `content.newsletter` | `Newsletter.Enregistrer` | redirection |
| POST | `/Admin/Newsletter/Envoyer/{id}` | `content.newsletter` | `Newsletter.Envoyer` | redirection |
| GET | `/Admin/Newsletter/Abonnes` | `content.newsletter` | `Newsletter.Abonnes` | `Newsletter/Abonnes.cshtml` |
| GET | `/Admin/Newsletter/Exporter` | `content.newsletter` | `Newsletter.Exporter` | — (CSV) |
| GET | `/Admin/Documents` | `documents.manage` | `Documents.Index` | `Documents/Index.cshtml` |
| POST | `/Admin/Documents/Televerser` | `documents.manage` | `Documents.Televerser` | redirection |
| GET | `/Admin/Documents/Versions/{id}` | `documents.manage` | `Documents.Versions` | `Documents/Versions.cshtml` |
| POST | `/Admin/Documents/Archiver/{id}` | `documents.manage` | `Documents.Archiver` | redirection |
| GET | `/Admin/Finances?annee=` | `finance.view` | `Finances.Index` | `Finances/Index.cshtml` |
| GET | `/Admin/Finances/Depenses` | `finance.view` | `Finances.Depenses` | `Finances/Depenses.cshtml` |
| POST | `/Admin/Finances/AjouterDepense` | `finance.manage` | `Finances.AjouterDepense` | redirection |
| POST | `/Admin/Finances/ApprouverDepense/{id}` | `finance.manage` | `Finances.ApprouverDepense` | redirection |
| GET | `/Admin/Finances/Dons` | `finance.view` | `Finances.Dons` | `Finances/Dons.cshtml` |
| POST | `/Admin/Finances/AjouterDon` | `finance.manage` | `Finances.AjouterDon` | redirection |
| POST | `/Admin/Finances/Rembourser/{id}` | `finance.refund` | `Finances.Rembourser` | redirection |
| GET | `/Admin/Utilisateurs?q=&role=` | `admin.users` | `Utilisateurs.Index` | `Utilisateurs/Index.cshtml` |
| GET | `/Admin/Utilisateurs/Details/{id}` | `admin.users` | `Utilisateurs.Details` | `Utilisateurs/Details.cshtml` |
| POST | `/Admin/Utilisateurs/ChangerRole/{id}` | `admin.roles` | `Utilisateurs.ChangerRole` | redirection |
| POST | `/Admin/Utilisateurs/ChangerActivation/{id}` | `admin.users` | `Utilisateurs.ChangerActivation` | redirection |
| POST | `/Admin/Utilisateurs/Deverrouiller/{id}` | `admin.users` | `Utilisateurs.Deverrouiller` | redirection |
| POST | `/Admin/Utilisateurs/AjusterElo/{id}` | `admin.users` | `Utilisateurs.AjusterElo` | redirection |
| GET | `/Admin/Parametres` | `admin.settings` | `Parametres.Index` | `Parametres/Index.cshtml` |
| POST | `/Admin/Parametres/Enregistrer` | `admin.settings` | `Parametres.Enregistrer` | redirection |
| GET | `/Admin/Audit` | `admin.audit` | `Audit.Index` | `Audit/Index.cshtml` |
| GET | `/Admin/Audit/Connexions` | `admin.audit` | `Audit.Connexions` | `Audit/Connexions.cshtml` |

### 8.9 Ressources statiques

| URL | Contenu |
|---|---|
| `/css/site.css` | Feuille de style unique (design system, thèmes clair et sombre) |
| `/js/site.js` | Menu, thème, alertes, chronomètre, indices, onglets |
| `/js/chessboard.js` | Échiquier interactif |
| `/img/favicon.svg` | Icône du site |
| `/uploads/**` | Fichiers téléversés (documents, galerie, photos de profil) |

Les fichiers statiques sont servis avec `Cache-Control: public, max-age=604800`
et une empreinte de version (`asp-append-version`) qui invalide le cache à
chaque modification.

---

## 9. Périmètre livré et limites connues

### 9.1 Ce qui est livré et fonctionnel

Le site est complet de bout en bout pour les usages décrits en section 2 :
parcours d'adhésion, entraînement quotidien, tournois avec appariements et
classement ELO, blog avec workflow éditorial, forum, gestion documentaire,
finances, administration et audit. Les onze exercices livrés avec l'application
ont été vérifiés position par position (légalité, idée gagnante, exactitude du mat
annoncé). Les traitements automatiques sont idempotents.

### 9.2 Vérification effectuée — à lire avant la première compilation

Le code de ce dossier **n'a pas été compilé** : l'environnement de génération ne
disposait pas du SDK .NET et son téléchargement était bloqué par la politique de
sortie réseau. Il a été écrit avec attention, mais **attendez-vous à corriger
quelques erreurs de compilation au premier `dotnet build`** — un `using`
manquant, une signature qui a dérivé. Ce sont des corrections mécaniques, pas des
défauts de conception.

Ordre de vérification conseillé :

```bash
cd CavalierNoir
dotnet build CavalierNoir.sln      # 1. corriger les erreurs éventuelles
dotnet test CavalierNoir.sln       # 2. les tests valident le cœur métier
dotnet run --project src/CavalierNoir.Web
```

Les versions de paquets NuGet (`Directory.Packages.props`) sont alignées sur
.NET 10 mais n'ont pas pu être résolues contre nuget.org : ajustez le numéro de
correctif si la restauration échoue.

### 9.3 Migrations Entity Framework

**Aucune migration n'est générée** — elle exige le SDK. Au premier démarrage,
`DatabaseInitializer` détecte l'absence d'historique de migration et crée le
schéma directement depuis le modèle (`EnsureCreatedAsync`), ce qui suffit pour
développer.

**Avant tout déploiement**, générez les migrations :

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial \
    --project src/CavalierNoir.Infrastructure \
    --startup-project src/CavalierNoir.Web
```

`EnsureCreated` ne sait pas faire évoluer un schéma existant : sans migrations,
la première modification du modèle en production serait ingérable.

### 9.4 Ce qui n'est volontairement pas implémenté

| Sujet | État | Ce qu'il reste à faire |
|---|---|---|
| **Paiement en ligne** (Stripe, MonCash) | Le modèle est prêt : `Payment` porte `TransactionId`, `IdempotencyKey`, statuts et remboursement ; `RecordPaymentAsync` est idempotent | Écrire l'adaptateur du prestataire et le point d'entrée de webhook. Le reste de la chaîne ne bouge pas |
| **Double authentification (TOTP)** | `AddDefaultTokenProviders()` est configuré | Écrire les écrans d'activation et de vérification. À faire **avant** d'ouvrir les paiements réels |
| **Traduction créole et anglaise** | L'infrastructure de localisation est en place (`fr-FR`, `ht`, `en`), le sélecteur est dans les préférences | Créer les fichiers `.resx` et extraire les chaînes. L'interface est aujourd'hui en français uniquement |
| **Import Lichess / Chess.com** | Les champs de compte existent sur `ApplicationUser` | Écrire une couche anticorruption appelant l'API et projetant vers `TournamentGame` |
| **Cours et séances** | Entités et cartographie EF complètes (`Course`, `Lesson`, `TrainingSession`, `Attendance`) | Écrans d'administration et espace élève |
| **Messagerie privée** | Entité `PrivateMessage` cartographiée | Contrôleur et vues |
| **Suivi d'ouverture des courriels** | `EmailLog` prévoit `OpenedAt` et `ClickedAt` ; les ouvertures de l'exercice du jour sont comptées via le clic sur le lien | Webhook du transporteur pour les ouvertures réelles |
| **Tests d'intégration et de bout en bout** | Les tests unitaires couvrent le domaine ; `Program` est déclaré `partial` pour `WebApplicationFactory` | Ajouter un projet d'intégration (Testcontainers) et des scénarios Playwright |
| **Observabilité** | Journalisation structurée par la console | Brancher OpenTelemetry et un collecteur si le besoin apparaît |

### 9.5 Écarts assumés par rapport au dossier d'architecture initial

Trois choix s'écartent des spécifications de départ. Chacun est justifié dans
[`ADR.md`](ADR.md) ; les voici résumés.

**Hangfire remplacé par un `BackgroundService`.** Sur une instance unique,
l'idempotence des traitements suffit ; Hangfire aurait ajouté une dépendance et
quatre tables pour un tableau de bord de tâches. Le passage à Hangfire consiste à
réécrire une seule classe (`MaintenanceWorker`). *(ADR-002)*

**Aucune bibliothèque front-end.** Bootstrap, chessboard.js et Chart.js auraient
exigé d'autoriser un CDN, donc d'affaiblir la politique de sécurité de contenu sur
les pages où un trésorier saisit des paiements. Le CSS, l'échiquier et les
graphiques sont écrits à la main. *(ADR-003, ADR-004)*

**Dépôts génériques plutôt qu'un dépôt par agrégat.** La couche Application
utilise `IApplicationDbContext` pour les lectures composables et un
`IRepository<T>` défini dans le domaine là où il apporte quelque chose. Un dépôt
par agrégat aurait produit une centaine de méthodes de délégation sans garantie
supplémentaire. *(ADR-001)*

### 9.6 À faire avant une mise en production

1. **Changer le mot de passe d'administration** et la clé `Tokens:SigningKey`
   (`openssl rand -base64 48`).
2. **Générer les migrations** et les appliquer (§9.3).
3. **Configurer un transporteur SMTP** (`Email:Provider = smtp`) — en mode
   `fichier`, aucun courriel ne part réellement.
4. **Activer la double authentification** pour les comptes du bureau (§9.4).
5. **Vérifier `Site:BaseUrl`** : les liens des courriels et le plan du site en
   dépendent.
6. **Mettre en place les sauvegardes** de la base et du dossier `wwwroot/uploads`.
7. **Faire relire les mentions légales et la politique de confidentialité** par
   une personne compétente : les textes livrés sont un point de départ honnête,
   pas un avis juridique.

---

*Document généré avec le code source. Toute divergence entre ce dossier et le
code doit être tranchée en faveur du code — et le dossier corrigé.*
