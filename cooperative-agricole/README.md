# Coopérative Agricole — Gestion des planteurs et des pesées

Application Android native (Kotlin) de gestion d'une coopérative agricole,
construite selon l'architecture **MVVM** et la persistance locale **Room**.

Projet d'évaluation finale — UNITECH, *Projet Intra Android*, niveaux 3 et 4
Sciences Informatiques.

---

## 1. Présentation du projet

Une coopérative agricole doit tenir à jour deux informations : **qui sont ses
planteurs** et **quelles quantités chacun a livrées**. Cette application
remplace le cahier de pesée papier : elle enregistre les planteurs, consigne
chaque pesée en la rattachant à son planteur, et calcule automatiquement les
totaux par planteur et pour la coopérative entière.

Tout fonctionne **hors ligne** : les données vivent dans une base SQLite
locale gérée par Room, sans serveur ni connexion Internet.

## 2. Objectifs

**Objectifs fonctionnels**

- Gérer les planteurs : créer, consulter, modifier, supprimer, rechercher.
- Gérer les pesées : enregistrer, consulter, modifier, supprimer.
- Rattacher obligatoirement chaque pesée à un planteur.
- Consulter la fiche d'un planteur avec l'historique et la synthèse de ses
  pesées (nombre, poids total, dernière pesée).
- Empêcher la saisie de données incohérentes et confirmer les suppressions.

**Objectifs techniques**

- Respecter la séparation View → ViewModel → Repository → DAO → Room.
- Exposer les données par des flux observables (`StateFlow`), de sorte que
  l'interface se mette à jour d'elle-même après chaque écriture.
- N'écrire aucune requête SQL ailleurs que dans les DAO.
- Garder les règles métier testables sans émulateur.

## 3. Fonctionnalités développées

| Écran | Fonctionnalités |
|---|---|
| **Accueil** | Accès aux planteurs et aux pesées ; compteurs (nombre de planteurs, nombre de pesées, poids total) mis à jour en direct |
| **Liste des planteurs** | Liste en `RecyclerView` (nom complet, localité, sexe, code), recherche sur le code, le nom, le prénom et la localité, états vides distincts « aucun planteur » / « aucun résultat » |
| **Formulaire planteur** | Création et modification ; sélecteur de date de naissance ; choix du sexe ; validation champ par champ ; confirmation avant d'abandonner une saisie |
| **Fiche du planteur** | Informations complètes (code, nom complet, localité, sexe, date de naissance et âge), synthèse (nombre de pesées, poids total, dernière pesée), historique des pesées, actions modifier / supprimer, ajout direct d'une pesée pour ce planteur |
| **Liste des pesées** | Toutes les pesées, les plus récentes en tête, avec le planteur, la date, le poids et l'observation ; totaux en en-tête ; menu modifier / supprimer par ligne |
| **Formulaire pesée** | Création et modification ; sélection du planteur dans une liste déroulante alimentée par la base ; sélecteur de date ; poids décimal ; observation facultative |

**Contraintes fonctionnelles tenues**

- *Unicité du code planteur* : le code est la clé primaire de la table, et
  l'application vérifie sa disponibilité avant d'insérer.
- *Association obligatoire pesée → planteur* : garantie par une clé étrangère
  en base **et** par une liste déroulante côté interface.
- *Validation des saisies* : règles centralisées, message d'erreur affiché
  sous le champ fautif.
- *Confirmation avant suppression* : boîte de dialogue qui annonce les
  conséquences (nombre de pesées supprimées en cascade).
- *Données de démonstration* : 10 planteurs et 23 pesées insérés à la première
  ouverture, dont un planteur sans pesée pour montrer l'état vide.

## 4. Architecture MVVM

```
┌──────────────────────────────┐
│ VIEW — Activity + XML        │  affiche l'état, transmet les gestes
└──────────────┬───────────────┘
               │  observe (StateFlow) / appelle
┌──────────────▼───────────────┐
│ VIEWMODEL                    │  état de l'écran, validation, événements
└──────────────┬───────────────┘
               │
┌──────────────▼───────────────┐
│ REPOSITORY                   │  point d'accès unique aux données
└──────────────┬───────────────┘
               │
┌──────────────▼───────────────┐
│ DAO                          │  les requêtes, et rien d'autre
└──────────────┬───────────────┘
               │
┌──────────────▼───────────────┐
│ ROOM / SQLite                │  la base locale
└──────────────────────────────┘
```

**View** (`MainActivity`, `*ListActivity`, `*FormActivity`,
`PlanteurDetailActivity`) — ne contient aucune règle métier ni aucun accès aux
données. Elle affiche ce que le ViewModel émet et lui transmet les actions de
l'utilisateur. Le *view binding* remplace `findViewById`.

**ViewModel** (`PlanteurViewModel`, `PeseeViewModel`) — porte l'état de
l'écran et la logique de présentation. Il survit à la rotation de l'écran, ce
qui évite de recharger les données à chaque changement de configuration. Il ne
connaît ni `Activity`, ni `View`, ni `Context`.

**Repository** (`PlanteurRepository`, `PeseeRepository`) — centralise l'accès
aux données et ramène les résultats bruts de SQL à des valeurs directement
utilisables (un `SUM` vide devient `0.0`). Le ViewModel ignore que les données
viennent de Room : si la coopérative ajoutait demain un serveur distant, seuls
les repositories changeraient.

**DAO** (`PlanteurDao`, `PeseeDao`) — décrivent les opérations possibles sur
les tables ; Room en génère l'implémentation SQL à la compilation. Les
lectures observables renvoient un `Flow`, les écritures sont des fonctions
`suspend` exécutées hors du thread principal.

**Room** (`CooperativeDatabase`) — crée et ouvre la base SQLite, applique les
convertisseurs de types et fournit les DAO. Une seule instance pour toute
l'application.

### Comment les données arrivent jusqu'à l'écran

Une écriture (par exemple l'ajout d'une pesée) modifie la table `pesees`.
Room sait quelles requêtes observent cette table et **réémet** leurs résultats.
Le `Flow` du DAO traverse le Repository, le ViewModel le transforme en
`StateFlow`, et l'Activity, qui le collecte, se redessine. Aucun écran n'a
besoin de prévenir un autre : l'accueil, la liste et la fiche se mettent à jour
d'eux-mêmes.

## 5. Structure du projet

```
com.example.cooperativeagricole
│
├── CooperativeApplication.kt      Application + conteneur de dépendances
├── MainActivity.kt                Écran d'accueil
│
├── data
│   ├── local
│   │   ├── dao
│   │   │   ├── PlanteurDao.kt
│   │   │   └── PeseeDao.kt
│   │   ├── database
│   │   │   ├── CooperativeDatabase.kt   base Room (singleton)
│   │   │   ├── Convertisseurs.kt        Sexe ↔ colonne texte
│   │   │   └── DonneesDemo.kt           jeu de démonstration
│   │   └── entity
│   │       ├── Planteur.kt              entité + énumération Sexe
│   │       ├── Pesee.kt                 entité + clé étrangère
│   │       └── PeseeAvecPlanteur.kt     résultat de jointure
│   └── repository
│       ├── PlanteurRepository.kt
│       └── PeseeRepository.kt
│
├── domain
│   └── validation                 règles de saisie, sans dépendance Android
│       ├── Validation.kt          champs et motifs d'invalidité
│       ├── ValidationPlanteur.kt
│       └── ValidationPesee.kt
│
├── ui
│   ├── accueil
│   │   └── AccueilViewModel.kt    les compteurs de l'écran d'accueil
│   ├── planteur
│   │   ├── PlanteurViewModel.kt   + sa fabrique
│   │   ├── PlanteurAdapter.kt
│   │   ├── PlanteurListActivity.kt
│   │   ├── PlanteurFormActivity.kt
│   │   └── PlanteurDetailActivity.kt
│   └── pesee
│       ├── PeseeViewModel.kt      + sa fabrique
│       ├── PeseeAdapter.kt        liste générale (avec planteur)
│       ├── PeseeCompacteAdapter.kt historique d'un planteur
│       ├── PeseeListActivity.kt
│       └── PeseeFormActivity.kt
│
└── util
    ├── Dates.kt                   dates compatibles API 24
    ├── Formats.kt                 mise en forme des poids
    └── Ui.kt                      insets, collecte selon le cycle de vie,
                                   traduction des erreurs
```

## 6. Modèle de données

### Entité `Planteur` (table `planteurs`)

| Champ | Type | Rôle |
|---|---|---|
| `code` | `String` | **Clé primaire** — identifiant du planteur |
| `nom` | `String` | Nom de famille |
| `prenom` | `String` | Prénom |
| `sexe` | `Sexe` | `MASCULIN` / `FEMININ`, stocké « M » / « F » |
| `dateNaissance` | `Long` | Millisecondes depuis l'epoch |
| `localite` | `String` | Localité de rattachement |

`nomComplet` est une propriété calculée : elle n'occupe pas de colonne.

### Entité `Pesee` (table `pesees`)

| Champ | Type | Rôle |
|---|---|---|
| `id` | `Long` | **Clé primaire** auto-générée |
| `planteurCode` | `String` | **Clé étrangère** vers `planteurs.code` |
| `datePesee` | `Long` | Date de la pesée |
| `poidsKg` | `Double` | Poids pesé, en kilogrammes |
| `observation` | `String?` | Remarque facultative |

### Relation

Un **planteur** possède **plusieurs pesées** ; une **pesée** appartient à
**un seul planteur** — association `1 : N`.

```
Planteur (1) ──────< (N) Pesee
   code                planteurCode
```

Elle est implémentée par une `ForeignKey` sur `planteurCode` :

- `onDelete = CASCADE` — supprimer un planteur supprime ses pesées, donc
  aucune pesée orpheline ne peut subsister ;
- `onUpdate = CASCADE` — si le code changeait, les pesées suivraient ;
- un `Index` sur `planteurCode` accélère toutes les requêtes par planteur.

Pour afficher le nom du planteur dans la liste générale des pesées, une
jointure SQL remplit l'objet `PeseeAvecPlanteur` : une seule requête au lieu
d'une par ligne affichée.

## 7. Technologies utilisées

- **Kotlin** 2.0.21
- **Android SDK** — `compileSdk` 36, `minSdk` 24, `targetSdk` 36
- **Room** 2.6.1 (Entity, DAO, Database, TypeConverter), génération par **KSP**
- **ViewModel** et **StateFlow** (coroutines Kotlin) pour l'observabilité
- **RecyclerView** avec `ListAdapter` et `DiffUtil`
- **Material Design 3** (thème clair et sombre)
- **View Binding**
- **JUnit 4** (tests unitaires JVM) et **AndroidX Test** (tests instrumentés Room)
- **Gradle** 8.13, AGP 8.13.2, catalogue de versions `libs.versions.toml`

## 8. Installation et exécution

**Prérequis** : Android Studio (version récente), JDK 17, et un émulateur ou
un appareil Android 7.0 (API 24) ou supérieur.

1. **Récupérer le projet** — décompresser l'archive, ou cloner le dépôt :
   ```bash
   git clone <url-du-depot>
   ```
2. **Ouvrir dans Android Studio** — *File ▸ Open…*, puis sélectionner le
   dossier `cooperative-agricole` (celui qui contient `settings.gradle.kts`).
3. **Synchroniser les dépendances** — Android Studio le propose
   automatiquement ; sinon *File ▸ Sync Project with Gradle Files*. La
   première synchronisation télécharge Gradle, l'AGP, Kotlin et Room : elle
   demande une connexion Internet.
4. **Compiler et exécuter** — choisir un appareil puis *Run ▸ Run 'app'*
   (`Maj + F10`).

En ligne de commande :

```bash
./gradlew assembleDebug      # produit app/build/outputs/apk/debug/app-debug.apk
./gradlew installDebug       # installe sur l'appareil connecté
./gradlew test               # tests unitaires (JVM)
./gradlew connectedAndroidTest   # tests des DAO (appareil ou émulateur requis)
```

Au premier lancement, la base est créée et le jeu de démonstration inséré.
Pour repartir d'une base vide : *Paramètres ▸ Applications ▸ Coopérative
Agricole ▸ Stockage ▸ Effacer les données*, ou désinstaller l'application.

---

## Points à savoir pour la soutenance

- **Du MCD aux entités Room** : chaque entité du MCD devient une `data class`
  annotée `@Entity` ; les attributs deviennent des colonnes, l'identifiant une
  `@PrimaryKey`, l'association `1 : N` une `@ForeignKey` portée par le côté
  « plusieurs » (la pesée).
- **Pourquoi un Repository** : pour que le ViewModel ne dépende pas de Room.
  Il centralise l'accès aux données et absorbe leurs particularités
  (`SUM` valant `NULL`).
- **Rôle du DAO** : déclarer les opérations ; Room écrit le SQL et vérifie les
  requêtes à la compilation — une faute de frappe dans une requête arrête la
  compilation, elle n'attend pas l'exécution.
- **Pourquoi `StateFlow`** : l'écran ne demande jamais « les données ont-elles
  changé ? » ; il est prévenu. Et `repeatOnLifecycle` interrompt la collecte
  dès que l'écran n'est plus visible.
- **Rôle de l'Adapter** : créer un petit nombre de vues et les recycler ;
  `DiffUtil` compare l'ancienne et la nouvelle liste pour n'animer que les
  lignes réellement modifiées.
- **Le code du planteur n'est pas modifiable** en modification : c'est la clé
  primaire et la cible des clés étrangères des pesées.
