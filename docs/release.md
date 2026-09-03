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

## Où déployer quoi

Les deux applications n'ont pas les mêmes besoins, et une seule des deux
va sur Vercel.

| Composant | Hébergeur | Pourquoi |
|---|---|---|
| `apps/admin-web` (Next.js) | **Vercel** | C'est précisément son usage. `vercel.json` est prêt. |
| `services/api` (NestJS) | **Render** — `render.yaml` est prêt | Voir ci-dessous |

### Pourquoi l'API ne peut pas aller sur Vercel

Ce n'est pas une préférence, c'est une incompatibilité qui coûterait de
l'argent aux clients.

`TopupInitiationProcessor` est un **worker BullMQ** : quand MonCash est
indisponible, la demande de dépôt est mise en file et rejouée plus tard.
Vercel est serverless — un processus s'arrête dès la réponse envoyée, il
n'existe aucun consommateur permanent. **Les dépôts échoués seraient mis
en file et jamais rejoués** : le client a payé chez MonCash, et son solde
n'est jamais crédité.

S'ajoutent deux problèmes classiques du serverless pour cette API : chaque
instance ouvre son propre pool TypeORM, ce qui épuise les connexions
Postgres sans pooler dédié, et les migrations n'ont aucun moment naturel
pour s'exécuter.

### Pourquoi Render plutôt que Railway ou Fly.io

- **Fly.io** : son Postgres n'est pas managé — les sauvegardes sont à la
  charge de l'utilisateur. Inacceptable pour un registre comptable.
- **Railway** : très bon, mais sa configuration-as-code est limitée ; le
  déploiement se décrit alors en suite de clics plutôt qu'en fichier
  versionné.
- **Render** : `render.yaml` décrit toute l'infrastructure dans le dépôt,
  son Postgres managé sauvegarde automatiquement, et son
  `preDeployCommand` applique les migrations **avant** que la nouvelle
  version ne prenne le trafic — exactement le problème que pose une image
  qui démarrerait contre un schéma absent.

### Déployer l'API sur Render

Render → **New → Blueprint** → sélectionner ce dépôt. `render.yaml` crée
d'un coup l'API, PostgreSQL et Redis, en région Virginie (la plus proche
d'Haïti).

Rien n'est à saisir pour que le premier déploiement réussisse : les cinq
variables obligatoires (`NODE_ENV`, `PORT`, `DATABASE_URL`, `REDIS_URL`,
`JWT_SECRET`) sont fournies automatiquement — `JWT_SECRET` est généré par
Render, donc jamais choisi par un humain ni recopié d'un autre
environnement. Séquence vérifiée en local à l'identique : 11 migrations
appliquées par le `preDeployCommand`, API démarrée sans erreur, `/health`
à 200, inscription/connexion/solde fonctionnels.

À renseigner ensuite dans l'interface Render (tous optionnels au
démarrage) :

- `CORS_ORIGINS` — l'origine du back-office Vercel, une fois celui-ci
  déployé. Tant qu'elle est vide, l'API reflète l'origine appelante, ce
  qui ne convient qu'en développement.
- `MONCASH_CLIENT_ID`, `MONCASH_CLIENT_SECRET`, `MONCASH_WEBHOOK_SECRET` —
  identifiants **sandbox** tant que le statut FSP n'est pas clarifié.
  `MONCASH_BASE_URL` pointe déjà sur le sandbox.

Le plan Postgres retenu est `basic-256mb`, pas le plan gratuit : ce
dernier n'offre aucune sauvegarde et expire après 90 jours, ce qui exclut
d'y placer de vraies écritures comptables, même en staging.

Redis est configuré en `noeviction` : une file BullMQ purgée sous pression
mémoire, c'est un dépôt client jamais rejoué, donc jamais crédité.

### Déployer le back-office sur Vercel

1. Vercel → New Project → importer le dépôt
2. **Root Directory : `apps/admin-web`**, et activer « Include source files
   outside of the Root Directory » (le monorepo utilise les workspaces npm)
3. Variable d'environnement : `NEXT_PUBLIC_API_BASE_URL` = l'URL publique
   de votre API
4. Ajouter l'origine Vercel à `CORS_ORIGINS` côté API, sinon le navigateur
   bloque chaque requête avant qu'elle n'atteigne un contrôleur

`vercel.json` fixe déjà les en-têtes de sécurité (`X-Frame-Options: DENY`
contre le clickjacking, HSTS, `noindex` — une console d'exploitation n'a
rien à faire dans un moteur de recherche).

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

### Obtenir un APK sans aucun compte (recommandé pour démarrer)

Le workflow **« APK Android (build locale, sans Expo) »** compile l'APK
directement sur un runner GitHub, dont le SDK Android est préinstallé.
Aucun compte Expo, aucun jeton, aucun compte Apple.

Actions → *APK Android* → Run workflow → l'APK se télécharge dans la
section « Artifacts » du run.

Par défaut il est signé avec une clé **éphémère**, régénérée à chaque run :
parfait pour installer et tester, inutilisable pour le Play Store, car
Android refuse de mettre à jour une application dont la signature a changé.
Pour une clé stable, ajoutez les secrets `ANDROID_KEYSTORE_BASE64`,
`ANDROID_KEYSTORE_PASSWORD` et `ANDROID_KEY_ALIAS`.

En local, la même chose demande le SDK Android (≈ 3 Go) :

```bash
cd apps/mobile
LAJANM_API_URL=https://api.VOTRE-DOMAINE.ht npx expo prebuild --platform android
cd android && ./gradlew assembleRelease
```

Il n'existe **aucun équivalent pour iOS sans macOS ni compte Apple.**

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
