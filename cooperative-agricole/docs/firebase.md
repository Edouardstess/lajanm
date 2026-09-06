# Base distante Firebase

L'application fonctionne **sans Firebase** : elle utilise alors la seule base
Room locale, et l'accueil affiche « Base locale ». Déposer un fichier de
configuration Firebase active la synchronisation entre appareils, sans qu'une
ligne de code change.

## Ce que Firebase apporte ici

| | Room (local) | Firestore (distant) |
|---|---|---|
| Rôle | Source de vérité de l'interface | Miroir partagé entre appareils |
| Lecture par les écrans | Toujours | Jamais directement |
| Disponible hors réseau | Oui | Oui (cache Firestore) |
| Partagé entre téléphones | Non | Oui |

Ce partage des rôles est délibéré. Si les écrans lisaient Firestore
directement, chaque liste attendrait le réseau et l'application deviendrait
inutilisable sur une parcelle sans couverture — exactement là où les pesées se
font. Les écrans lisent donc Room, et la synchronisation travaille derrière.

## Activer la synchronisation

1. Créer un projet sur [console.firebase.google.com](https://console.firebase.google.com).
2. Y ajouter une application **Android** avec le nom de paquet exact :
   `com.example.cooperativeagricole`.
3. Télécharger le `google-services.json` proposé et le déposer dans
   `cooperative-agricole/app/`.
4. Dans la console, ouvrir **Firestore Database** et créer une base en
   *mode test* (le temps du développement).
5. Relancer une synchronisation Gradle, puis l'application. L'étiquette de
   l'accueil passe de « Base locale » à « Synchronisé ».

Le fichier `google-services.json` **n'est pas versionné** : il identifie votre
projet Firebase. Un modèle commenté est fourni dans
`app/google-services.json.exemple`.

## Comment l'activation est détectée

Le plugin `com.google.gms.google-services` n'est appliqué que si le fichier
existe (`app/build.gradle.kts`) ; sans cette garde, le projet ne compilerait
pas tant que Firebase n'est pas configuré. À l'exécution,
`FirebaseApp.initializeApp` renvoie `null` en l'absence de configuration :
`ConnexionFirebase` en déduit qu'il n'y a pas de base distante et le reste de
l'application l'ignore — les repositories reçoivent `null` et écrivent
seulement dans Room.

## Modèle de données distant

```
planteurs/{code}           nom, prenom, sexe, dateNaissance, localite
pesees/{cleDistante}       planteurCode, datePesee, poidsKg, observation
```

Deux points méritent d'être expliqués en soutenance :

- **`cleDistante`**. L'`id` d'une pesée est auto-incrémenté par SQLite : deux
  téléphones donneraient le même `id` à deux pesées différentes. Chaque pesée
  porte donc une clé tirée au hasard à sa création, qui l'identifie d'un
  appareil à l'autre. L'`id` du MCD reste l'identifiant local.
- **La cascade**. SQLite supprime les pesées d'un planteur supprimé, grâce à la
  clé étrangère. Firestore n'a pas de clés étrangères : `SourceDistante` refait
  ce travail explicitement, en supprimant les documents `pesees` du planteur.

## Sens de circulation

```
        écriture                                lecture
   ┌──────────────────┐                    ┌──────────────────┐
   │    Repository    │                    │      Écrans      │
   └────────┬─────────┘                    └────────▲─────────┘
            │ 1. Room (immédiat)                    │
            │ 2. Firestore (file d'attente)         │ Flow
            ▼                                       │
   ┌──────────────────┐   instantanés     ┌─────────┴────────┐
   │    Firestore     │ ────────────────▶ │       Room       │
   └──────────────────┘  Synchronisation  └──────────────────┘
```

Les écritures ne sont pas attendues : le SDK Firestore met à jour son cache
local tout de suite, met la requête en file d'attente et la rejoue à la
reconnexion. Bloquer l'interface le temps d'un aller-retour serveur ne
sécuriserait rien et gênerait la saisie hors réseau.

## Premier lancement sur un dépôt vide

Recopier littéralement un dépôt distant vide effacerait les données locales.
`SynchronisationCooperative` traite donc ce cas à part : au tout premier
instantané, si le distant est vide, l'application **téléverse** ce qu'elle a au
lieu d'effacer. C'est ce qui permet d'installer l'application sur un premier
téléphone sans perdre le jeu de démonstration.

Ce mécanisme suppose que les données locales existent déjà quand le premier
instantané arrive. C'est la raison pour laquelle le jeu de démonstration est
inséré **de façon synchrone**, dans la transaction qui crée la base
(`DonneesDemo`), et non dans une coroutine lancée après coup : la
synchronisation démarre au lancement de l'application, et aurait pris ce vide
passager pour l'état réel de l'appareil.

## Règles de sécurité

Le mode test ouvre la base à tous pendant 30 jours : c'est acceptable pour une
démonstration, pas au-delà. Une première restriction raisonnable consiste à
exiger une authentification :

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /{document=**} {
      allow read, write: if request.auth != null;
    }
  }
}
```

Elle suppose d'ajouter Firebase Authentication, qui sort du périmètre de ce
projet ; l'application affiche « Synchronisation indisponible » si le serveur
refuse la lecture.
