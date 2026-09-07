"""Parties B, C et E — Prétraitement, entraînement, sélection et sauvegarde.

Exécution :

    python train.py            # entraînement complet
    python train.py --rapide   # sans recherche d'hyperparamètres

Le script entraîne cinq algorithmes de classification, les compare par
validation croisée et sur un jeu de test tenu à l'écart, retient le meilleur,
puis sauvegarde le pipeline complet (nettoyage + encodages + modèle) avec
Joblib. L'application PyQt se contente ensuite de charger ce fichier.
"""

from __future__ import annotations

import argparse
import json
import platform
import sys
import time
import warnings
from datetime import datetime

# SVC(probability=True) est déprécié à partir de scikit-learn 1.9 mais reste la
# façon standard d'obtenir des probabilités avec un SVM sur les versions
# antérieures. On masque l'avertissement pour garder une sortie lisible.
warnings.filterwarnings(
    "ignore", message=".*probability.*deprecated.*", category=FutureWarning
)

import joblib
import numpy as np
import pandas as pd
import sklearn
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.model_selection import GridSearchCV, StratifiedKFold, cross_val_score, train_test_split
from sklearn.neighbors import KNeighborsClassifier
from sklearn.svm import SVC
from sklearn.tree import DecisionTreeClassifier

import config
import evaluation
from preprocessing import charger_donnees, construire_pipeline, separer_x_y


LARGEUR = 78


def titre(texte: str) -> None:
    print()
    print("=" * LARGEUR)
    print(texte.upper().center(LARGEUR))
    print("=" * LARGEUR)


def section(texte: str) -> None:
    print()
    print(f"--- {texte} " + "-" * max(0, LARGEUR - len(texte) - 5))


# ==========================================================================
# Partie C — les modèles candidats
# ==========================================================================
def definir_modeles() -> dict:
    """Cinq algorithmes, choisis pour couvrir des familles différentes.

    Le sujet en demande au moins trois ; en retenir cinq permet de comparer
    des hypothèses de nature réellement distincte plutôt que des variantes
    d'une même idée.
    """
    graine = config.GRAINE_ALEATOIRE
    return {
        "Logistic Regression": LogisticRegression(max_iter=2000, random_state=graine),
        "Decision Tree": DecisionTreeClassifier(
            max_depth=8, min_samples_leaf=10, random_state=graine
        ),
        "Random Forest": RandomForestClassifier(
            n_estimators=300, min_samples_leaf=2, n_jobs=-1, random_state=graine
        ),
        "K-Nearest Neighbors": KNeighborsClassifier(n_neighbors=15, weights="distance"),
        "Support Vector Machine": SVC(
            kernel="rbf", C=1.0, probability=True, random_state=graine
        ),
    }


JUSTIFICATIONS = {
    "Logistic Regression": (
        "Modèle linéaire de référence. Rapide, entièrement interprétable — chaque\n"
        "    coefficient dit dans quel sens une variable pousse vers une série — et\n"
        "    il fournit nativement des probabilités, ce que l'interface doit afficher."
    ),
    "Decision Tree": (
        "Capture les règles en escalier et les interactions entre variables sans\n"
        "    aucune mise à l'échelle. Son arbre se lit comme une suite de questions,\n"
        "    ce qui correspond bien au raisonnement d'un conseiller d'orientation.\n"
        "    Seul, il surapprend facilement, d'où sa profondeur bornée."
    ),
    "Random Forest": (
        "Moyenne de nombreux arbres décorrélés : conserve la souplesse de l'arbre\n"
        "    en supprimant l'essentiel de son surapprentissage. Robuste aux valeurs\n"
        "    extrêmes résiduelles, tolérant aux variables peu utiles, et il fournit\n"
        "    une mesure d'importance des variables exploitable dans l'application."
    ),
    "K-Nearest Neighbors": (
        "Approche par similarité : un élève reçoit la série majoritaire parmi les\n"
        "    profils qui lui ressemblent le plus. C'est exactement l'intuition d'une\n"
        "    orientation « par comparaison avec les promotions précédentes ».\n"
        "    Exige des variables normalisées, ce que le pipeline garantit."
    ),
    "Support Vector Machine": (
        "Cherche la frontière de séparation la plus large possible entre séries.\n"
        "    Le noyau RBF gère les frontières non linéaires et le modèle se comporte\n"
        "    bien sur un échantillon de quelques milliers de lignes."
    ),
}


# Grilles volontairement courtes : l'objectif est d'affiner le modèle retenu,
# pas d'explorer exhaustivement l'espace des hyperparamètres.
GRILLES = {
    "Logistic Regression": {"classifieur__C": [0.1, 1.0, 10.0]},
    "Decision Tree": {
        "classifieur__max_depth": [5, 8, 12, None],
        "classifieur__min_samples_leaf": [1, 5, 10, 20],
    },
    "Random Forest": {
        "classifieur__n_estimators": [200, 400],
        "classifieur__max_depth": [None, 12],
        "classifieur__min_samples_leaf": [1, 2, 5],
    },
    "K-Nearest Neighbors": {
        "classifieur__n_neighbors": [5, 11, 15, 25, 35],
        "classifieur__weights": ["uniform", "distance"],
    },
    "Support Vector Machine": {
        "classifieur__C": [0.5, 1.0, 5.0],
        "classifieur__gamma": ["scale", 0.05],
    },
}


# ==========================================================================
# Importance des variables
# ==========================================================================
def extraire_importances(pipeline) -> pd.Series | None:
    """Importance des variables après encodage, si le modèle en expose une."""
    try:
        noms = pipeline.named_steps["preparation"].get_feature_names_out()
    except Exception:
        return None

    classifieur = pipeline.named_steps["classifieur"]

    if hasattr(classifieur, "feature_importances_"):
        valeurs = classifieur.feature_importances_
    elif hasattr(classifieur, "coef_"):
        # Multi-classes : on résume par la moyenne des valeurs absolues.
        valeurs = np.abs(classifieur.coef_).mean(axis=0)
    else:
        return None  # KNN et SVM-RBF n'exposent pas d'importance directe

    if len(valeurs) != len(noms):
        return None
    return pd.Series(valeurs, index=noms).sort_values(ascending=False)


# ==========================================================================
# Programme principal
# ==========================================================================
def main(argv=None) -> int:
    analyseur = argparse.ArgumentParser(description="Entraînement du modèle d'orientation")
    analyseur.add_argument(
        "--rapide", action="store_true",
        help="ignore la recherche d'hyperparamètres du modèle retenu",
    )
    arguments = analyseur.parse_args(argv)

    config.creer_dossiers()
    debut_total = time.perf_counter()

    # ---------------------------------------------------------------- B --
    titre("Partie B — Chargement et préparation")

    df = charger_donnees()
    print(f"Jeu de données  : {df.shape[0]} lignes x {df.shape[1]} colonnes")

    X, y = separer_x_y(df)
    print(f"Variables explicatives : {len(config.COLONNES_FEATURES)}")
    print(f"  {', '.join(config.COLONNES_FEATURES)}")
    print(f"Variable cible  : {config.COLONNE_CIBLE}")
    print(f"Colonne écartée : {config.COLONNE_ID} (identifiant, sans pouvoir prédictif)")

    section("Traitements appliqués dans le pipeline")
    print("  1. Notes hors [0, 100] et aptitude hors [1, 5] converties en manquantes")
    print("  2. Numériques      → imputation par la médiane, puis standardisation")
    print("  3. aptitude_logique→ imputation par la médiane, puis standardisation")
    print("  4. niveau_motivation → encodage ordinal (Faible < Moyenne < Élevée < Très élevée)")
    print("  5. interesse_par_informatique → one-hot (Non / Oui / Inconnu)")
    print("  6. centre_interet  → one-hot (6 modalités + Inconnu)")
    print("\n  Les manquants des variables textuelles deviennent une modalité")
    print("  « Inconnu » : imputer par le mode inventerait une réponse.")

    X_entrainement, X_test, y_entrainement, y_test = train_test_split(
        X, y,
        test_size=config.PROPORTION_TEST,
        random_state=config.GRAINE_ALEATOIRE,
        stratify=y,  # conserve la proportion des quatre séries de part et d'autre
    )
    section("Découpage entraînement / test")
    print(f"  entraînement : {len(X_entrainement)} élèves ({1 - config.PROPORTION_TEST:.0%})")
    print(f"  test         : {len(X_test)} élèves ({config.PROPORTION_TEST:.0%})")
    print("  découpage stratifié sur la série, graine fixée à "
          f"{config.GRAINE_ALEATOIRE} (résultats reproductibles)")

    # ---------------------------------------------------------------- C --
    titre("Partie C — Entraînement des modèles")

    modeles = definir_modeles()
    for nom, justification in JUSTIFICATIONS.items():
        print(f"\n  {nom}")
        print(f"    {justification}")

    validation = StratifiedKFold(
        n_splits=config.NB_PLIS_VALIDATION_CROISEE,
        shuffle=True,
        random_state=config.GRAINE_ALEATOIRE,
    )

    resultats = {}
    for nom, classifieur in modeles.items():
        section(nom)
        pipeline = construire_pipeline(classifieur)

        scores = cross_val_score(
            pipeline, X_entrainement, y_entrainement,
            cv=validation, scoring="f1_macro", n_jobs=-1,
        )
        print(f"  validation croisée (F1 macro) : {scores.mean():.4f} "
              f"± {scores.std():.4f}   {np.round(scores, 4)}")

        depart = time.perf_counter()
        pipeline.fit(X_entrainement, y_entrainement)
        duree = time.perf_counter() - depart

        y_predit = pipeline.predict(X_test)
        metriques = evaluation.calculer_metriques(y_test, y_predit)

        print(f"  test : exactitude {metriques['accuracy']:.4f} | "
              f"F1 macro {metriques['f1_macro']:.4f} | entraîné en {duree:.2f} s")

        resultats[nom] = {
            "pipeline": pipeline,
            "metriques": metriques,
            "cv_moyenne": scores.mean(),
            "cv_ecart_type": scores.std(),
            "duree_entrainement": duree,
            "y_test": y_test,
            "y_predit": y_predit,
        }

    # ---------------------------------------------------------------- D --
    titre("Partie D — Évaluation et comparaison")

    tableau = evaluation.tableau_comparatif(resultats)
    print()
    print(evaluation.formater_tableau(tableau))
    tableau.round(4).to_csv(config.CHEMIN_COMPARAISON, encoding="utf-8")
    print(f"\nTableau enregistré : {config.CHEMIN_COMPARAISON.relative_to(config.RACINE)}")

    section("Matrices de confusion")
    for nom, donnees in resultats.items():
        print(f"\n  {nom}")
        matrice = evaluation.matrice_confusion(donnees["y_test"], donnees["y_predit"])
        print("  " + matrice.to_string().replace("\n", "\n  "))

    # Choix du modèle : F1 macro sur le test, départagé par la validation
    # croisée puis par le temps d'entraînement (le plus rapide l'emporte).
    nom_retenu = max(
        resultats,
        key=lambda n: (
            round(resultats[n]["metriques"]["f1_macro"], 4),
            round(resultats[n]["cv_moyenne"], 4),
            -resultats[n]["duree_entrainement"],
        ),
    )
    section("Modèle retenu")
    print(f"  {nom_retenu}")
    print(f"    F1 macro (test)          : {resultats[nom_retenu]['metriques']['f1_macro']:.4f}")
    print(f"    Exactitude (test)        : {resultats[nom_retenu]['metriques']['accuracy']:.4f}")
    print(f"    F1 macro (val. croisée)  : {resultats[nom_retenu]['cv_moyenne']:.4f} "
          f"± {resultats[nom_retenu]['cv_ecart_type']:.4f}")
    print("\n  Critère : meilleur F1 macro sur le jeu de test, confirmé par la")
    print("  validation croisée. Le F1 macro est préféré à l'exactitude car il")
    print("  traite les quatre séries à poids égal.")

    print("\n  Rapport détaillé par série :")
    print("  " + evaluation.rapport_classification(
        resultats[nom_retenu]["y_test"], resultats[nom_retenu]["y_predit"]
    ).replace("\n", "\n  "))

    # ------------------------------------------------- affinage éventuel --
    pipeline_final = resultats[nom_retenu]["pipeline"]
    meilleurs_parametres = None

    if not arguments.rapide and nom_retenu in GRILLES:
        section(f"Recherche d'hyperparamètres — {nom_retenu}")
        recherche = GridSearchCV(
            construire_pipeline(definir_modeles()[nom_retenu]),
            GRILLES[nom_retenu],
            cv=validation,
            scoring="f1_macro",
            n_jobs=-1,
        )
        depart = time.perf_counter()
        recherche.fit(X_entrainement, y_entrainement)
        print(f"  {len(recherche.cv_results_['params'])} combinaisons testées "
              f"en {time.perf_counter() - depart:.1f} s")
        print(f"  meilleurs paramètres : {recherche.best_params_}")
        print(f"  F1 macro (val. croisée) : {recherche.best_score_:.4f}")

        f1_affine = evaluation.calculer_metriques(
            y_test, recherche.best_estimator_.predict(X_test)
        )["f1_macro"]
        f1_initial = resultats[nom_retenu]["metriques"]["f1_macro"]
        print(f"  F1 macro (test) : {f1_initial:.4f} → {f1_affine:.4f}")

        if f1_affine >= f1_initial:
            pipeline_final = recherche.best_estimator_
            meilleurs_parametres = {
                cle.replace("classifieur__", ""): valeur
                for cle, valeur in recherche.best_params_.items()
            }
            resultats[nom_retenu]["metriques"] = evaluation.calculer_metriques(
                y_test, pipeline_final.predict(X_test)
            )
            resultats[nom_retenu]["y_predit"] = pipeline_final.predict(X_test)
            print("  → version affinée conservée")
        else:
            print("  → réglages par défaut conservés (l'affinage ne fait pas mieux)")

    # -------------------------------------------------------- figures ----
    section("Figures")
    for chemin in (
        evaluation.tracer_comparaison(tableau),
        evaluation.tracer_matrices_confusion(resultats),
    ):
        print(f"  {chemin.relative_to(config.RACINE)}")

    importances = extraire_importances(pipeline_final)
    if importances is not None:
        chemin = evaluation.tracer_importances(
            importances, f"Importance des variables — {nom_retenu}"
        )
        print(f"  {chemin.relative_to(config.RACINE)}")
        print("\n  Dix variables les plus influentes :")
        for variable, valeur in importances.head(10).items():
            print(f"    {variable:<45} {valeur:.4f}")

    # ---------------------------------------------------------------- E --
    titre("Partie E — Sauvegarde du modèle")

    joblib.dump(pipeline_final, config.CHEMIN_MODELE)
    taille = config.CHEMIN_MODELE.stat().st_size / 1024
    print(f"  modèle : {config.CHEMIN_MODELE.relative_to(config.RACINE)} ({taille:.0f} Ko)")
    print("  le fichier contient le pipeline complet : nettoyage, imputations,")
    print("  encodages, normalisation et classifieur.")

    metadonnees = {
        "modele_retenu": nom_retenu,
        "hyperparametres_recherche": meilleurs_parametres,
        "date_entrainement": datetime.now().isoformat(timespec="seconds"),
        "classes": list(pipeline_final.classes_),
        "colonnes_attendues": config.COLONNES_FEATURES,
        "taille_entrainement": len(X_entrainement),
        "taille_test": len(X_test),
        "graine_aleatoire": config.GRAINE_ALEATOIRE,
        "metriques_test": {
            cle: round(float(valeur), 4)
            for cle, valeur in resultats[nom_retenu]["metriques"].items()
        },
        "validation_croisee_f1_macro": {
            "moyenne": round(float(resultats[nom_retenu]["cv_moyenne"]), 4),
            "ecart_type": round(float(resultats[nom_retenu]["cv_ecart_type"]), 4),
            "nb_plis": config.NB_PLIS_VALIDATION_CROISEE,
        },
        "comparaison": {
            nom: {cle: round(float(valeur), 4) for cle, valeur in donnees["metriques"].items()}
            for nom, donnees in resultats.items()
        },
        "importances_principales": (
            {k: round(float(v), 5) for k, v in importances.head(15).items()}
            if importances is not None else None
        ),
        "versions": {
            "python": platform.python_version(),
            "scikit_learn": sklearn.__version__,
            "pandas": pd.__version__,
            "numpy": np.__version__,
            "joblib": joblib.__version__,
        },
    }
    config.CHEMIN_METADONNEES.write_text(
        json.dumps(metadonnees, indent=2, ensure_ascii=False), encoding="utf-8"
    )
    print(f"  métadonnées : {config.CHEMIN_METADONNEES.relative_to(config.RACINE)}")

    # Vérification : le fichier rechargé doit prédire exactement pareil.
    section("Vérification du fichier sauvegardé")
    recharge = joblib.load(config.CHEMIN_MODELE)
    identiques = (recharge.predict(X_test) == pipeline_final.predict(X_test)).all()
    print(f"  prédictions identiques après rechargement : {'oui' if identiques else 'NON'}")
    if not identiques:
        print("  ATTENTION : incohérence de sérialisation.")
        return 1

    print(f"\nTerminé en {time.perf_counter() - debut_total:.1f} s.")
    print("Lancez l'interface :  python application.py")
    return 0


if __name__ == "__main__":
    sys.exit(main())
