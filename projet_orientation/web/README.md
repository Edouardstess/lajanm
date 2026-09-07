# Déploiement du modèle — service web

Ce dossier expose le modèle entraîné par `train.py` sous forme d'**API HTTP**
accompagnée d'une **page web** de saisie, afin qu'il soit utilisable depuis un
navigateur ou un téléphone, sans installer Python.

> **Le service est un ajout, pas un remplacement.** L'énoncé du projet exige
> que l'application PyQt fonctionne **localement, sans API et sans connexion
> Internet** (§ 8). Cette contrainte reste respectée : `application.py` est
> inchangée et n'appelle jamais ce service. C'est le même fichier
> `modele_orientation.joblib` qui sert aux deux.

---

## 1. Lancer en local

```bash
cd projet_orientation
pip install -r web/requirements.txt
python web/serveur.py
```

Puis ouvrir <http://127.0.0.1:8000>.

Équivalent avec rechargement automatique pendant le développement :

```bash
uvicorn web.serveur:app --reload
```

## 2. Points d'entrée

| Méthode | Chemin | Rôle |
| --- | --- | --- |
| `GET` | `/` | page web de saisie |
| `POST` | `/api/predire` | prédiction à partir d'un profil JSON |
| `GET` | `/api/modele` | modèle déployé, métriques, importances |
| `GET` | `/health` | sonde de disponibilité (utilisée par Render) |
| `GET` | `/docs` | documentation interactive générée par FastAPI |

### Exemple d'appel

```bash
curl -X POST http://127.0.0.1:8000/api/predire \
  -H 'Content-Type: application/json' \
  -d '{
        "math": 92, "physique": 88, "svt": 62,
        "francais": 58, "histoire": 55,
        "aptitude_logique": 5,
        "niveau_motivation": "Élevée",
        "centre_interet": "Informatique",
        "interesse_par_informatique": "Oui"
      }'
```

```json
{
  "serie_recommandee": "SMP",
  "libelle": "Sciences, Mathématiques et Physique",
  "probabilites": { "SMP": 1.0, "SVT": 0.0, "SES": 0.0, "LLA": 0.0 },
  "moyenne_generale": 71.0,
  "avertissements": [],
  "modele": "Logistic Regression"
}
```

Les trois champs `niveau_motivation`, `centre_interet` et
`interesse_par_informatique` acceptent `null` : le modèle traite l'absence
grâce à la modalité « Inconnu », et le champ `avertissements` signale la
perte de fiabilité qui en résulte.

Une note hors de `[0, 100]` ou une aptitude hors de `[1, 5]` est rejetée avec
un code **422** avant d'atteindre le modèle.

## 3. Conteneur Docker

```bash
cd projet_orientation
docker build -f web/Dockerfile -t orientation .
docker run --rm -p 8000:8000 orientation
```

L'image contient le modèle, `config.py` et `preprocessing.py` — ce dernier
est indispensable car le pipeline sérialisé référence la classe
`NettoyeurValeurs` qui y est définie. Elle tourne sous un utilisateur non
privilégié et n'écrit rien sur disque.

## 4. Déploiement sur Render

Le service est déclaré dans le `render.yaml` à la racine du dépôt, sous le nom
**`orientation-ns`**.

1. Sur Render : **New → Blueprint**, sélectionner ce dépôt.
2. Render lit `render.yaml` et propose les services ; `orientation-ns` en fait
   partie.
3. Valider. Le premier build prend quelques minutes.
4. L'URL publique est de la forme `https://orientation-ns.onrender.com`.

Aucune variable d'environnement, aucun secret et aucune base de données ne
sont nécessaires : le service est en lecture seule et sans état.

**À savoir sur le plan gratuit :** le service s'endort après environ 15 minutes
sans trafic, et son réveil prend environ 50 secondes. La première requête après
une pause semblera donc très lente. Passer en plan `starter` supprime la mise
en veille.

## 5. Déployer ailleurs

Le conteneur est standard et ne dépend de rien de spécifique à Render : il
suffit d'un hébergeur qui exécute une image Docker et fournit la variable
`PORT` (Fly.io, Railway, Google Cloud Run, une VM avec Docker…). Sur une
plateforme qui ne définit pas `PORT`, le service écoute sur 8000.

## 6. Épinglage des versions

`web/requirements.txt` fige scikit-learn, pandas, numpy et joblib sur les
versions ayant produit le modèle (voir `model/metadonnees_modele.json`).
Recharger un `.joblib` avec des versions différentes déclenche un
avertissement d'incompatibilité et ne garantit pas un comportement identique.

**Si vous relancez `train.py`** avec d'autres versions, reportez dans
`web/requirements.txt` celles inscrites dans les métadonnées, puis
reconstruisez l'image.

## 7. Limites de ce déploiement

- **Pas d'authentification.** Le service est public dès qu'il est déployé.
  Il ne collecte ni ne stocke aucune donnée personnelle — chaque requête est
  traitée en mémoire puis oubliée, et il n'y a pas d'équivalent de
  l'historique SQLite de l'application PyQt — mais n'importe qui connaissant
  l'URL peut l'interroger. Pour un usage en établissement, ajouter au minimum
  une clé d'API.
- **Aucune limitation de débit.** Rien n'empêche un client d'enchaîner les
  requêtes.
- **Modèle figé dans l'image.** Mettre à jour le modèle suppose de relancer
  `train.py`, de commiter le nouveau `.joblib` et de redéployer.
- **Les limites du modèle lui-même** restent celles décrites au § 10 du
  `RAPPORT.md` : le score de 96 % reflète largement la construction du jeu de
  données simulé. Les déployer ne les efface pas.
