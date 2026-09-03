# Mise en production et génération des installeurs

> **Rappel de gouvernance.** Voir `docs/audit-readiness.md` : le statut FSP
> (Circulaire BRH n°121) doit être clarifié avant toute mise en production
> **avec des fonds réels**. Tout ce qui suit s'applique à l'identique pour
> un déploiement sandbox/staging — rien de ce travail n'est perdu. Ne
> branchez les identifiants MonCash de production qu'une fois le statut
> réglé.

## L'ordre compte

Un installeur ne sert à rien tant que l'API n'est pas déployée : l'URL de
l'API est **compilée dans le binaire**. Construire l'APK avant d'avoir
l'URL finale oblige à tout reconstruire.

```
1. Déployer l'API (URL HTTPS publique)   ← bloquant
2. Renseigner cette URL dans eas.json
3. Construire les installeurs
4. Publier sur les stores
```

## Étape 1 — Déployer l'API

Il vous faut, chez l'hébergeur de votre choix :

| Ressource | Notes |
|---|---|
| Un hôte pour l'API | `services/api/Dockerfile` est prêt |
| PostgreSQL managé | Sauvegardes automatiques obligatoires — c'est le registre comptable |
| Redis managé | Files BullMQ (retries MonCash) |
| Un domaine + TLS | ex. `api.lajanm.example` |

Variables d'environnement : voir `services/api/.env.production.example`.
Elles ne sont **jamais** commitées — injectez-les via le gestionnaire de
secrets de votre plateforme.

À ne pas oublier en production :

- `CORS_ORIGINS` doit être une liste explicite (l'origine du back-office),
  jamais laissée vide — vide = « refléter l'origine appelante », correct en
  dev seulement.
- `JWT_SECRET` propre à la production, jamais copié depuis staging.
- Appliquer les migrations au déploiement, **depuis l'image de
  production** : `npm run migration:run:prod`. La variante `migration:run`
  ne fonctionne qu'en développement : elle passe par ts-node et les
  sources TypeScript, tous deux absents de l'image (`npm ci --omit=dev`,
  et seul `dist` est copié). La variante `:prod` vise le data-source
  compilé et le CLI `typeorm`, qui est une dépendance de production.
  Lancez-la **avant** de démarrer l'API : sans schéma, chaque requête
  échoue.
- Créer le premier compte back-office (il n'y a pas d'auto-inscription) :
  `npm run seed:admin -w @lajanm/api -- --email=... --password=... --role=admin`

## Étape 2 — Renseigner les valeurs qui vous appartiennent

Deux endroits, et ce sont les seuls :

**`apps/mobile/eas.json`** — remplacez les URL d'exemple :

```json
"production": { "env": { "LAJANM_API_URL": "https://api.VOTRE-DOMAINE.ht" } }
"staging":    { "env": { "LAJANM_API_URL": "https://api-staging.VOTRE-DOMAINE.ht" } }
```

**`apps/mobile/app.config.ts`** — les identifiants d'application :

```ts
const IOS_BUNDLE_IDENTIFIER = 'com.lajanm.app';  // → votre domaine inversé
const ANDROID_PACKAGE       = 'com.lajanm.app';
```

⚠️ Ces identifiants sont **définitifs**. Une fois l'app publiée, en changer
crée une application *différente* dans les stores : les utilisateurs déjà
installés ne reçoivent plus les mises à jour. Fixez-les avant la première
soumission.

Le build échoue volontairement si `LAJANM_API_URL` est absente, ou si elle
n'est pas en `https://` (hors localhost) — pour qu'aucun installeur ne
parte avec une URL morte ou un jeton transitant en clair.

## Étape 3 — Construire

### Ce qu'il vous faut (je ne peux pas le fournir à votre place)

| Élément | Coût | Pourquoi |
|---|---|---|
| Compte Expo | gratuit | Lance les builds EAS |
| Compte Google Play Developer | 25 $ une fois | Publier l'Android |
| Compte Apple Developer | 99 $/an | **Obligatoire même pour un simple .ipa** — Apple ne signe rien sans |

Un build iOS exige macOS + Xcode, ou le cloud EAS. Sans Mac, EAS est la
seule voie.

### Commandes

```bash
npm install -g eas-cli
eas login
cd apps/mobile
eas build:configure          # crée le projet côté Expo

# APK de test, installable directement sur un téléphone Android
eas build --profile staging --platform android

# Binaires de production pour les stores (.aab + .ipa)
eas build --profile production --platform all
```

EAS génère et conserve les clés de signature. Le lien de téléchargement
s'affiche à la fin du build.

### Alternative Android sans compte Expo

Un APK peut se construire localement, mais il faut le SDK Android
(≈ 3 Go) et gérer soi-même la clé de signature :

```bash
cd apps/mobile
LAJANM_API_URL=https://api.VOTRE-DOMAINE.ht npx expo prebuild --platform android
cd android && ./gradlew assembleRelease
```

Il n'existe **aucun équivalent pour iOS sans macOS.**

## Étape 4 — Publier

```bash
eas submit --profile production --platform android
eas submit --profile production --platform ios
```

Prévoir pour la revue des stores : politique de confidentialité, captures
d'écran, et une justification de l'usage de la caméra (KYC) — la chaîne de
permission est déjà déclarée dans `app.config.ts`.

## Avant la première mise en production réelle

- [ ] Statut FSP clarifié (bloquant — voir `docs/audit-readiness.md`)
- [ ] `CORS_ORIGINS` en liste explicite
- [ ] Sauvegardes PostgreSQL vérifiées **par une restauration de test**
- [ ] Identifiants MonCash de production (et non sandbox)
- [ ] Les lacunes connues de `docs/audit-readiness.md` arbitrées — en
      particulier l'absence de limitation de débit sur les endpoints d'auth
- [ ] Audit de sécurité externe
