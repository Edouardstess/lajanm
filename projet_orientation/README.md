# Système d'aide à l'orientation scolaire — Nouveau Secondaire

Projet IA2 — UNITECH, niveaux 3 et 4 Sciences Informatiques.

Application locale qui analyse le profil académique d'un élève (notes,
motivation, centres d'intérêt, aptitude logique) et propose la série du
Nouveau Secondaire la plus compatible : **SMP**, **SVT**, **SES** ou **LLA**.

> **Avertissement.** Cet outil est une **aide à la décision**. Il ne constitue
> pas un dispositif officiel d'orientation scolaire. Le jeu de données
> d'entraînement est simulé à des fins pédagogiques : les résultats obtenus ne
> valident aucune méthode réelle d'orientation des élèves en Haïti.

---

## 1. Installation

Python 3.9 ou plus récent est requis.

```bash
cd projet_orientation

# Environnement virtuel (recommandé)
python -m venv .venv
source .venv/bin/activate          # Windows :  .venv\Scripts\activate

pip install -r requirements.txt
```

L'interface fonctionne avec **PyQt5 ou PyQt6** : le module `qt_compat.py`
détecte automatiquement celle qui est installée. `requirements.txt` installe
PyQt5 par défaut ; pour PyQt6, décommentez la ligne correspondante.

Aucune connexion Internet n'est nécessaire après l'installation : tout
s'exécute localement, sans API.

## 2. Entraîner le modèle

```bash
python train.py
```

Le script enchaîne le prétraitement, entraîne cinq algorithmes de
classification, les compare, retient le meilleur et l'enregistre :

| Fichier produit                    | Contenu                                        |
| ---------------------------------- | ---------------------------------------------- |
| `model/modele_orientation.joblib`  | pipeline complet (prétraitement + classifieur) |
| `model/metadonnees_modele.json`    | métriques, versions, importances des variables |
| `model/comparaison_modeles.csv`    | tableau comparatif des cinq modèles            |
| `reports/06…08_*.png`              | comparaison, matrices de confusion, importances |

Durée : environ 10 secondes sur une machine ordinaire.

Options :

```bash
python train.py --rapide     # saute la recherche d'hyperparamètres
```

## 3. Lancer l'application

```bash
python application.py
```

L'application **charge** le fichier `.joblib` et n'effectue jamais de nouvel
entraînement. Si le modèle est absent, un message invite à lancer `train.py`.

## 4. Utiliser le système

L'interface comporte trois onglets.

### Onglet « Orientation »

1. Saisir les cinq notes sur 100 (mathématiques, physique, SVT, français,
   histoire). La **moyenne générale se calcule automatiquement**.
2. Renseigner l'aptitude logique (1 à 5), le niveau de motivation, le centre
   d'intérêt et l'intérêt pour l'informatique. Ces trois derniers champs
   acceptent « non renseigné » : le modèle sait traiter l'information
   manquante, mais prévient que la prédiction sera moins fiable.
3. Cliquer sur **Prédire l'orientation**.

Le panneau de droite affiche la série recommandée, les probabilités des
quatre séries sous forme de barres, une explication des facteurs qui ont pesé
pour ce profil précis, et une sortie texte détaillée.

Boutons annexes : **Réinitialiser** remet le formulaire à zéro ;
**Profil d'exemple** le remplit avec un profil cohérent tiré au hasard.

### Onglet « Historique »

Chaque prédiction est enregistrée dans une base **SQLite** locale
(`model/historique_orientations.sqlite`). L'onglet permet de consulter,
supprimer, exporter en CSV ou vider l'historique.

### Onglet « Modèle »

Algorithme retenu, métriques, comparaison des cinq modèles, variables les
plus influentes et toutes les figures produites par l'analyse.

## 5. Déploiement web (optionnel)

En plus de l'application PyQt, le modèle peut être exposé sous forme d'API
HTTP avec une page web de saisie :

```bash
pip install -r web/requirements.txt
python web/serveur.py          # http://127.0.0.1:8000
```

Le service est conteneurisé (`web/Dockerfile`) et déclaré dans le
`render.yaml` du dépôt sous le nom `orientation-ns`. Voir
[`web/README.md`](web/README.md) pour le détail.

**Ce déploiement est un ajout, pas un remplacement.** L'énoncé exige que
l'application PyQt fonctionne localement, sans API ni connexion Internet :
c'est le cas, `application.py` n'appelle jamais ce service.

## 6. Analyse exploratoire

```bash
python exploration.py
```

Produit `reports/exploration.txt` et les figures `reports/01…05_*.png`.

## 7. Réévaluer un modèle déjà entraîné

```bash
python evaluation.py
```

Recharge le fichier `.joblib` et réaffiche ses métriques sur le même jeu de
test qu'à l'entraînement (découpage reproduit à l'identique via la graine
aléatoire).

## 8. Organisation du projet

```
projet_orientation/
│
├── data/
│   ├── dataset_orientation_NS_3000.csv     # jeu de données (3 000 élèves)
│   └── dataset_orientation_NS_3000.xlsx    # version Excel d'origine
│
├── model/
│   ├── modele_orientation.joblib           # modèle sauvegardé (livrable)
│   ├── metadonnees_modele.json
│   ├── comparaison_modeles.csv
│   └── historique_orientations.sqlite      # créé à la première prédiction
│
├── reports/                                # figures + rapport d'exploration
│
├── web/                                    # déploiement (optionnel)
│   ├── serveur.py                          # API FastAPI + page web
│   ├── page.html
│   ├── Dockerfile
│   ├── requirements.txt
│   └── README.md
│
├── config.py          # chemins, colonnes, bornes, constantes partagées
├── preprocessing.py   # chargement, nettoyage, encodages, pipeline
├── exploration.py     # Partie A — analyse exploratoire
├── train.py           # Parties B, C, E — entraînement, sélection, sauvegarde
├── evaluation.py      # Partie D — métriques, matrices, figures
├── database.py        # historique SQLite
├── qt_compat.py       # compatibilité PyQt5 / PyQt6
├── application.py     # interface graphique
├── requirements.txt
├── README.md
└── RAPPORT.md         # rapport détaillé du projet
```

## 9. Résultats obtenus

Cinq algorithmes comparés sur le même découpage (2 400 élèves d'entraînement,
600 de test, stratifié, graine 42) :

| Modèle                 | Accuracy | Precision | Recall | F1-score |
| ---------------------- | -------: | --------: | -----: | -------: |
| **Logistic Regression**| **0,9617** | **0,9627** | **0,9626** | **0,9623** |
| Support Vector Machine |   0,9600 |    0,9611 | 0,9610 |   0,9606 |
| Random Forest          |   0,9533 |    0,9547 | 0,9545 |   0,9540 |
| Decision Tree          |   0,9400 |    0,9418 | 0,9414 |   0,9410 |
| K-Nearest Neighbors    |   0,9367 |    0,9384 | 0,9371 |   0,9370 |

Précision, rappel et F1 sont des moyennes **macro** (les quatre séries
comptent à poids égal). Le modèle retenu est la **régression logistique**.

Le détail — analyse des données, choix de prétraitement, justification du
modèle et limites du système — figure dans [`RAPPORT.md`](RAPPORT.md).

## 10. Résolution de problèmes

| Symptôme | Cause probable et solution |
| --- | --- |
| `Le fichier modele_orientation.joblib est absent` | Lancez `python train.py` avant `python application.py`. |
| `ImportError: Aucune version de PyQt n'est installée` | `pip install PyQt5` (ou `PyQt6`). |
| `ModuleNotFoundError: No module named 'preprocessing'` | Lancez les scripts depuis le dossier `projet_orientation/`. |
| L'interface ne s'ouvre pas sur un serveur sans écran | PyQt a besoin d'un environnement graphique. En test : `QT_QPA_PLATFORM=offscreen`. |
| `openpyxl` manquant | Nécessaire uniquement pour lire le `.xlsx` ; le CSV suffit sinon. |
