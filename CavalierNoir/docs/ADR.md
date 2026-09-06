# Décisions d'architecture — Cavalier Noir

Ce document consigne les décisions structurantes, ce qui a été écarté et
pourquoi. Une décision sans alternative examinée n'est pas une décision.

---

## ADR-001 — Architecture en couches avec `IApplicationDbContext`

**Contexte.** Le cœur métier (ELO, appariements, cycle des adhésions) est
suffisamment riche pour mériter d'être isolé de la persistance et du web.

**Décision.** Quatre projets, dépendances dirigées vers l'intérieur :
`Web → Infrastructure → Application → Domain`. Le domaine ne référence ni EF Core
ni ASP.NET Core. La couche Application accède à la persistance par
`IApplicationDbContext`, une interface exposant les `DbSet<>`.

**Alternative écartée.** Un dépôt par agrégat, avec une méthode par requête. Cela
aurait ajouté une centaine de méthodes de délégation sans rien garantir de plus :
les requêtes de lecture (tableau de bord, recherche filtrée, classement) sont
naturellement composables en LINQ et n'ont pas d'invariant à protéger.

**Conséquence assumée.** La couche Application référence
`Microsoft.EntityFrameworkCore`. C'est un couplage à une abstraction de requête,
pas à un fournisseur : le domaine, lui, reste totalement indépendant. Un dépôt
générique (`IRepository<T>`) reste défini dans le domaine et implémenté dans
l'infrastructure, pour les cas où il apporte quelque chose.

---

## ADR-002 — Un ordonnanceur interne plutôt que Hangfire

**Contexte.** Trois traitements périodiques : exercice quotidien, maintenance des
adhésions, publication programmée.

**Décision.** Un `BackgroundService` (`MaintenanceWorker`) qui se réveille toutes
les cinq minutes et déclenche ce qui est dû.

**Alternative écartée.** Hangfire, qui apporterait un tableau de bord et la
reprise sur échec — au prix d'une dépendance, de quatre tables supplémentaires et
d'une configuration de sérialisation.

**Pourquoi ce choix.** Pour un club sur une seule instance, l'idempotence des
traitements suffit : une contrainte d'unicité sur la date de l'exercice du jour
garantit qu'un rejeu n'envoie rien deux fois. Le passage à Hangfire (verrou
distribué en multi-instance) consiste à réécrire cette seule classe, sans toucher
aux services applicatifs qu'elle appelle.

---

## ADR-003 — Aucune dépendance front-end

**Contexte.** Le site a besoin d'une mise en page responsive, d'un échiquier
interactif et de deux graphiques.

**Décision.** CSS et JavaScript écrits à la main, aucun paquet, aucun CDN.

**Alternative écartée.** Bootstrap + chessboard.js + Chart.js par CDN.

**Pourquoi ce choix.** La politique de sécurité de contenu du site interdit toute
ressource tierce (`script-src 'self'`) : autoriser un CDN reviendrait à confier
l'exécution de code à un tiers sur les pages où un trésorier saisit des
paiements. Les polices sont des piles système, ce qui supprime aussi toute
requête vers un service de polices — utile sur les connexions lentes visées.
Le coût est d'environ 630 lignes de CSS et 440 de JavaScript, sans minification
ni chaîne de compilation à maintenir.

---

## ADR-004 — Un échiquier sans moteur d'échecs

**Contexte.** Il faut afficher une position et enregistrer les coups du membre.

**Décision.** Le composant client rend la position FEN et déplace ce qu'on lui
demande de déplacer. Il ne connaît pas les règles du jeu. La correction est faite
côté serveur par comparaison de suites de coups normalisées.

**Alternative écartée.** Embarquer `chess.js` pour valider la légalité des coups
côté client.

**Pourquoi ce choix.** La vérité doit rester côté serveur, où elle n'est pas
manipulable : un contrôle client seul serait contournable en une ligne de console.
Et la notation par coordonnées (`e2e4`) évite le piège de la notation algébrique
localisée — un membre francophone tape « Ta8 » là où la base stocke « Ra8 ».

---

## ADR-005 — SQLite par défaut, SQL Server en production

**Contexte.** Un contributeur doit pouvoir lancer le site sans installer de
serveur de base de données.

**Décision.** Le fournisseur est choisi par configuration (`Database:Provider`) :
SQLite par défaut, SQL Server en production via `docker compose`.

**Conséquence assumée.** Les requêtes évitent les constructions non traduisibles
par les deux fournisseurs (pas de `DateOnly.FromDateTime` dans un tri, par
exemple). Les migrations doivent être générées par fournisseur avant tout
déploiement.

---

## ADR-006 — Le mot de passe n'est jamais le seul rempart

**Contexte.** Un compte de trésorier compromis donne accès aux données
financières.

**Décision.** Politique à 12 caractères et 4 classes, verrouillage après 5
échecs, limitation de débit à 20 requêtes par minute sur l'authentification,
journal de toutes les tentatives avec IP et agent, messages d'erreur identiques
que l'adresse existe ou non.

**Ce qui n'est pas livré.** La double authentification par TOTP. ASP.NET Core
Identity la fournit (`AddDefaultTokenProviders` est déjà configuré) : il reste à
écrire les écrans d'activation et de vérification. C'est le premier chantier de
sécurité à ouvrir avant une mise en production avec des paiements réels.

---

## ADR-007 — Suppression de compte : anonymiser plutôt qu'effacer

**Contexte.** Le droit à l'effacement s'oppose à l'intégrité de l'historique
sportif : supprimer un joueur casserait les appariements et les classements de
tous les tournois auxquels il a participé.

**Décision.** `ApplicationUser.Anonymize` efface les données personnelles
(identité, coordonnées, biographie, comptes de jeu) et neutralise le compte, en
conservant l'identifiant technique et les résultats.

**Base.** Le RGPD admet la conservation lorsqu'elle est nécessaire à
l'établissement de droits — ici, la régularité des compétitions passées. La
politique de confidentialité du site l'annonce explicitement.

---

## ADR-008 — Formulaires : champ leurre plutôt que CAPTCHA

**Contexte.** Le formulaire de contact et l'abonnement à la lettre d'information
sont ouverts aux visiteurs non authentifiés.

**Décision.** Un champ invisible que les robots remplissent, plus une limitation
à 5 envois par minute et par IP.

**Alternative écartée.** reCAPTCHA, qui exige un script tiers (interdit par la
politique de sécurité de contenu) et transmet des données de navigation à un
tiers sans consentement — ce qui obligerait à afficher une bannière de cookies
sur un site qui, en l'état, n'en a pas besoin.
