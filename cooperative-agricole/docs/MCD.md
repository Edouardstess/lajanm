# Modèle conceptuel de données

> Le MCD fourni avec le sujet est une image à joindre au dossier de remise.
> Cette page en donne la transcription utilisée pour construire les entités
> Room, afin que la correspondance MCD → code soit explicite.

## Entités et association

```
┌──────────────────────────────┐              ┌──────────────────────────────┐
│           PLANTEUR           │              │             PESEE            │
├──────────────────────────────┤              ├──────────────────────────────┤
│ code           (identifiant) │ 1        0,N │ id             (identifiant) │
│ nom                          │──────────────│ date                         │
│ prenom                       │   effectue   │ poids (kg)                   │
│ sexe                         │              │ observation                  │
│ date de naissance            │              │ code planteur   (clé étr.)   │
│ localite                     │              │                              │
└──────────────────────────────┘              └──────────────────────────────┘
```

Cardinalités : un planteur effectue **0 à N** pesées ; une pesée est effectuée
par **un et un seul** planteur.

## Traduction en Room

| Élément du MCD | Traduction |
|---|---|
| Entité `PLANTEUR` | `data class Planteur` annotée `@Entity(tableName = "planteurs")` |
| Entité `PESEE` | `data class Pesee` annotée `@Entity(tableName = "pesees")` |
| Identifiant `code` | `@PrimaryKey val code: String` — l'unicité est donc garantie par la base |
| Identifiant `id` | `@PrimaryKey(autoGenerate = true) val id: Long` |
| Association `1 : N` | `@ForeignKey` sur `Pesee.planteurCode` → `Planteur.code`, avec `CASCADE` en suppression et en mise à jour |
| Attribut `sexe` | énumération `Sexe`, stockée « M » / « F » via un `@TypeConverter` |
| Attributs de date | `Long` (millisecondes depuis l'epoch) : triables en SQL, sans convertisseur |
| Attribut `poids` | `Double`, exprimé en kilogrammes |

## Choix documentés

- **`observation` est facultative** (`String?`). Elle n'apparaît pas
  explicitement dans l'énoncé des écrans ; elle est conservée parce que le
  peseur a besoin d'une remarque libre (humidité, tri à refaire…). La retirer
  ne demanderait que de supprimer le champ de l'entité, de la migration et du
  formulaire.
- **Pas d'attribut « produit »** : le sujet ne décrit qu'un poids par pesée.
- **La version du schéma est `1`.** Toute modification ultérieure d'une entité
  impose d'incrémenter `version` dans `@Database` et d'écrire une migration ;
  les schémas exportés dans `app/schemas` servent de trace.
