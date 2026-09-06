# Cavalier Noir

Site web du club d'échecs **Cavalier Noir** (Port-au-Prince, Haïti) :
vitrine publique, plateforme pédagogique (exercice quotidien, bibliothèque
tactique), gestion des tournois au système suisse avec classement ELO, et
back-office complet pour la vie associative — adhésions, finances, documents
officiels, communication.

Application **ASP.NET Core MVC 10**, architecture en couches (Clean
Architecture), Entity Framework Core 10, ASP.NET Core Identity.

---

## Démarrage rapide

```bash
cd CavalierNoir

# 1. Compiler
dotnet build CavalierNoir.sln

# 2. Lancer (SQLite, base créée et amorcée au premier démarrage)
dotnet run --project src/CavalierNoir.Web
```

Le site répond sur <https://localhost:7256>. Le premier démarrage crée le
schéma, les douze rôles, le compte d'administration, les formules d'adhésion,
les badges, les rubriques et onze exercices de référence.

**Compte d'administration par défaut** (à changer immédiatement) :

| Champ | Valeur |
|---|---|
| Adresse | `admin@cavaliernoir.ht` |
| Mot de passe | `CavalierNoir!2026` |

En production, ces valeurs sont fournies par les variables d'environnement
`Seed__AdminEmail` et `Seed__AdminPassword` ; la valeur par défaut du fichier
`appsettings.json` ne convient qu'au développement local.

## Tests

```bash
dotnet test CavalierNoir.sln
```

Les tests couvrent le cœur métier : formule ELO de la FIDE, appariement au
système suisse (dont l'interdiction des revanches), départages Buchholz et
Sonneborn-Berger, validation des positions FEN, comparaison des suites de
coups, cycle de vie des adhésions et objets-valeurs.

## Docker

```bash
cp .env.example .env    # puis renseignez les valeurs
docker compose up --build
```

La pile démarre l'application (port 8080) et SQL Server 2022. L'application
tourne sous un utilisateur non privilégié et expose `/health`.

## Structure

```
CavalierNoir/
├── src/
│   ├── CavalierNoir.Domain/           # Entités, objets-valeurs, règles métier
│   ├── CavalierNoir.Application/      # Services applicatifs, DTO, abstractions
│   ├── CavalierNoir.Infrastructure/   # EF Core, Identity, courriels, tâches de fond
│   └── CavalierNoir.Web/              # MVC : contrôleurs, vues, ressources statiques
├── tests/
│   └── CavalierNoir.Tests.Unit/       # Tests xUnit du domaine
└── docs/
    ├── DOSSIER-FONCTIONNEL.md         # Fonctionnalités, entités, arborescence, chemins d'accès
    └── ADR.md                         # Décisions d'architecture et leurs justifications
```

Les dépendances vont toujours vers l'intérieur : `Web → Infrastructure →
Application → Domain`. Le domaine ne référence ni EF Core, ni ASP.NET Core.

## Configuration

Tout se règle par configuration (`appsettings.json`, variables
d'environnement, coffre de secrets). Les sections utiles :

| Section | Rôle |
|---|---|
| `ConnectionStrings:Default` | Chaîne de connexion |
| `Database:Provider` | `Sqlite` (défaut) ou `SqlServer` |
| `Site` | Nom, URL publique, fuseau, heure d'envoi de l'exercice du jour |
| `Email` | Transporteur : `fichier` (développement), `smtp`, `aucun` |
| `Storage` | Emplacement et préfixe public des fichiers téléversés |
| `Tokens:SigningKey` | Clé HMAC des liens signés — **à remplacer en production** |
| `Seed` | Compte d'administration créé au premier démarrage |

En développement, `Email:Provider = fichier` écrit chaque courriel dans
`App_Data/emails/*.html` au lieu de l'expédier : les gabarits sont
vérifiables sans serveur SMTP et sans risque d'envoi réel.

## Documentation

- **[docs/DOSSIER-FONCTIONNEL.md](docs/DOSSIER-FONCTIONNEL.md)** — fonctionnalités
  détaillées, présentation des classes et entités, arborescence complète du
  projet et table des chemins d'accès (URL, rôle requis, contrôleur, vue).
- **[docs/ADR.md](docs/ADR.md)** — décisions d'architecture, ce qui a été retenu,
  ce qui a été écarté et pourquoi.
