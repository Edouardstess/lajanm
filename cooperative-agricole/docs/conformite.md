# Conformité au cahier des charges

Où chaque exigence du sujet est satisfaite dans le code.

| Exigence | Réalisation |
|---|---|
| Architecture MVVM imposée | `ui/*` (View) → `*ViewModel` → `data/repository` → `data/local/dao` → `CooperativeDatabase` |
| Organisation du projet | Packages `data.local.dao`, `data.local.database`, `data.local.entity`, `data.repository`, `ui.planteur`, `ui.pesee` |
| Entity, DAO, Database, Repository | `Planteur`/`Pesee`, `PlanteurDao`/`PeseeDao`, `CooperativeDatabase`, `PlanteurRepository`/`PeseeRepository` |
| Insérer / consulter / modifier / supprimer / lister | `@Insert`, `@Query`, `@Update`, `@Delete` dans les deux DAO |
| Toutes les pesées d'un planteur | `PeseeDao.listerParPlanteur` |
| Nombre de pesées d'un planteur | `PeseeDao.compterParPlanteur` |
| Poids total d'un planteur | `PeseeDao.poidsTotalParPlanteur` |
| Au moins un niveau Repository | `PlanteurRepository`, `PeseeRepository` — les ViewModels n'atteignent jamais un DAO |
| `PlanteurViewModel` et `PeseeViewModel` | `ui/planteur/PlanteurViewModel.kt`, `ui/pesee/PeseeViewModel.kt`, chacun avec sa fabrique |
| Écran d'accueil avec accès aux deux modules | `MainActivity` + `activity_main.xml` |
| Amélioration facultative : compteurs | Nombre de planteurs, nombre de pesées et poids total sur l'accueil |
| Liste des planteurs en RecyclerView | `PlanteurListActivity` + `PlanteurAdapter` (`ListAdapter` + `DiffUtil`) |
| Affichage nom complet, localité, sexe | `item_planteur.xml` (le code est ajouté en complément) |
| Ajouter / modifier / supprimer un planteur | `PlanteurFormActivity`, suppression depuis `PlanteurDetailActivity` |
| Liste, ajout, modification, suppression des pesées | `PeseeListActivity`, `PeseeFormActivity` |
| Association d'une pesée à un planteur par un sélecteur | `MaterialAutoCompleteTextView` en liste déroulante, alimentée par `PeseeViewModel.planteurs` |
| Écran de détail : code, nom complet, localité, sexe, date de naissance | `PlanteurDetailActivity` + `partie_ligne_information.xml` |
| Détail : historique des pesées | `PeseeCompacteAdapter` dans `activity_planteur_detail.xml` |
| Détail : nombre de pesées, poids total, dernière pesée | Bloc « Synthèse des pesées » |
| Navigation entre les écrans | `Intent` + fabriques `intention(...)` ; hiérarchie déclarée par `parentActivityName` |
| Transmission des informations entre écrans | `EXTRA_CODE` (planteur), `EXTRA_ID` / `EXTRA_PLANTEUR` (pesée) |
| Données observables (LiveData ou StateFlow) | `StateFlow` alimenté par les `Flow` de Room, collecté avec `repeatOnLifecycle` |
| Unicité de l'identifiant du planteur | `@PrimaryKey` + `OnConflictStrategy.ABORT` + vérification `existe()` avant insertion |
| Association obligatoire d'une pesée à un planteur | `@ForeignKey` en base + validation `ValidationPesee` + liste déroulante |
| Validation des données saisies | `domain/validation`, couvert par des tests JUnit |
| Message en cas d'erreur | `TextInputLayout.error` sous le champ fautif ; `Toast` pour les échecs techniques |
| Confirmation avant suppression | `MaterialAlertDialogBuilder`, avec le nombre de pesées supprimées en cascade |
| Données de test suffisantes | `DonneesDemo` : 10 planteurs, 23 pesées, dont un planteur sans pesée |
| `README.md` en 8 sections | `README.md` |
| Copie du MCD | `docs/MCD.md` (joindre l'image fournie au dossier de remise) |

## Au-delà du cahier des charges

| Ajout | Réalisation |
|---|---|
| Thème clair et sombre | `values/colors.xml` et `values-night/colors.xml`, un seul thème à maintenir |
| Système de styles | Typographie et composants définis une fois dans `values/themes.xml`, jamais redéfinis dans les layouts |
| Icône de l'application | Icône adaptative dessinée pour le projet (`ic_launcher_foreground.xml`) |
| Compilation vérifiée en continu | Workflow GitHub Actions qui compile l'APK et exécute les tests à chaque envoi |
