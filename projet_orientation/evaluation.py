"""Partie D — Évaluation et comparaison des modèles.

Fournit les fonctions de mesure appelées par ``train.py`` et peut aussi être
exécuté seul pour réévaluer le modèle déjà sauvegardé :

    python evaluation.py
"""

from __future__ import annotations

import sys

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from sklearn.metrics import (
    accuracy_score,
    classification_report,
    confusion_matrix,
    f1_score,
    precision_score,
    recall_score,
)

import config


# ==========================================================================
# Métriques
# ==========================================================================
def calculer_metriques(y_vrai, y_predit) -> dict:
    """Métriques principales pour un problème multi-classes.

    La moyenne « macro » traite les quatre séries à poids égal : c'est la
    lecture pertinente ici, puisqu'aucune série ne doit être négligée.
    La moyenne « pondérée » tient compte des effectifs.
    """
    return {
        "accuracy": accuracy_score(y_vrai, y_predit),
        "precision_macro": precision_score(y_vrai, y_predit, average="macro", zero_division=0),
        "recall_macro": recall_score(y_vrai, y_predit, average="macro", zero_division=0),
        "f1_macro": f1_score(y_vrai, y_predit, average="macro", zero_division=0),
        "precision_ponderee": precision_score(y_vrai, y_predit, average="weighted", zero_division=0),
        "recall_pondere": recall_score(y_vrai, y_predit, average="weighted", zero_division=0),
        "f1_pondere": f1_score(y_vrai, y_predit, average="weighted", zero_division=0),
    }


def matrice_confusion(y_vrai, y_predit, etiquettes=None) -> pd.DataFrame:
    """Matrice de confusion lisible : lignes = réel, colonnes = prédit."""
    etiquettes = etiquettes or config.SERIES
    matrice = confusion_matrix(y_vrai, y_predit, labels=etiquettes)
    return pd.DataFrame(
        matrice,
        index=[f"réel {e}" for e in etiquettes],
        columns=[f"prédit {e}" for e in etiquettes],
    )


def rapport_classification(y_vrai, y_predit, etiquettes=None) -> str:
    return classification_report(
        y_vrai, y_predit, labels=etiquettes or config.SERIES, zero_division=0, digits=3
    )


def tableau_comparatif(resultats: dict) -> pd.DataFrame:
    """Construit le tableau comparatif demandé au point 4-D du sujet."""
    lignes = []
    for nom, donnees in resultats.items():
        ligne = {"Modèle": nom}
        ligne.update({
            "Accuracy": donnees["metriques"]["accuracy"],
            "Precision": donnees["metriques"]["precision_macro"],
            "Recall": donnees["metriques"]["recall_macro"],
            "F1-score": donnees["metriques"]["f1_macro"],
        })
        if "cv_moyenne" in donnees:
            ligne["CV (5 plis)"] = donnees["cv_moyenne"]
            ligne["CV écart-type"] = donnees["cv_ecart_type"]
        if "duree_entrainement" in donnees:
            ligne["Temps (s)"] = donnees["duree_entrainement"]
        lignes.append(ligne)

    tableau = pd.DataFrame(lignes).set_index("Modèle")
    return tableau.sort_values("F1-score", ascending=False)


def formater_tableau(tableau: pd.DataFrame) -> str:
    """Rendu console du tableau comparatif, arrondi à quatre décimales."""
    affichage = tableau.copy()
    for colonne in affichage.columns:
        affichage[colonne] = affichage[colonne].map(lambda v: f"{v:.4f}")
    return affichage.to_string()


# ==========================================================================
# Figures
# ==========================================================================
def tracer_comparaison(tableau: pd.DataFrame, chemin=None):
    """Diagramme en barres groupées des quatre métriques par modèle."""
    chemin = chemin or config.DOSSIER_REPORTS / "06_comparaison_modeles.png"
    metriques = ["Accuracy", "Precision", "Recall", "F1-score"]
    tableau = tableau.sort_values("F1-score")

    positions = np.arange(len(tableau))
    largeur = 0.2
    couleurs = ["#4C78A8", "#54A24B", "#EECA3B", "#E45756"]

    fig, ax = plt.subplots(figsize=(11, 5.5))
    for decalage, (metrique, couleur) in enumerate(zip(metriques, couleurs)):
        ax.bar(
            positions + (decalage - 1.5) * largeur,
            tableau[metrique],
            largeur,
            label=metrique,
            color=couleur,
        )

    ax.set_xticks(positions)
    ax.set_xticklabels(tableau.index, rotation=12, ha="right")
    ax.set_ylim(0, 1.05)
    ax.set_ylabel("Score sur le jeu de test")
    ax.set_title("Comparaison des modèles de classification")
    ax.legend(ncol=4, loc="lower right")
    ax.grid(axis="y", alpha=0.3)
    fig.tight_layout()
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    return chemin


def tracer_matrices_confusion(resultats: dict, chemin=None):
    """Une matrice de confusion par modèle, en valeurs absolues."""
    chemin = chemin or config.DOSSIER_REPORTS / "07_matrices_confusion.png"
    noms = list(resultats)
    colonnes = min(3, len(noms))
    lignes = int(np.ceil(len(noms) / colonnes))

    fig, axes = plt.subplots(lignes, colonnes, figsize=(4.6 * colonnes, 4.3 * lignes))
    axes = np.atleast_1d(axes).ravel()

    for ax, nom in zip(axes, noms):
        matrice = confusion_matrix(
            resultats[nom]["y_test"], resultats[nom]["y_predit"], labels=config.SERIES
        )
        ax.imshow(matrice, cmap="Blues")
        ax.set_xticks(range(len(config.SERIES)), config.SERIES)
        ax.set_yticks(range(len(config.SERIES)), config.SERIES)
        ax.set_xlabel("Prédit")
        ax.set_ylabel("Réel")
        ax.set_title(f"{nom}\nexactitude {resultats[nom]['metriques']['accuracy']:.3f}",
                     fontsize=10)
        seuil = matrice.max() / 2
        for i in range(matrice.shape[0]):
            for j in range(matrice.shape[1]):
                ax.text(
                    j, i, int(matrice[i, j]),
                    ha="center", va="center", fontsize=10,
                    color="white" if matrice[i, j] > seuil else "black",
                )

    for ax in axes[len(noms):]:
        ax.axis("off")

    fig.suptitle("Matrices de confusion sur le jeu de test", fontsize=13)
    fig.tight_layout()
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    return chemin


def tracer_importances(importances: pd.Series, titre: str, chemin=None):
    """Importance des variables du modèle retenu (bonus du sujet)."""
    chemin = chemin or config.DOSSIER_REPORTS / "08_importance_variables.png"
    importances = importances.sort_values().tail(20)

    fig, ax = plt.subplots(figsize=(9, max(4.5, 0.32 * len(importances))))
    ax.barh(importances.index, importances.values, color="#4C78A8")
    ax.set_title(titre)
    ax.set_xlabel("Importance relative")
    fig.tight_layout()
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    return chemin


# ==========================================================================
# Exécution autonome : réévalue le modèle sauvegardé
# ==========================================================================
def main() -> int:
    import joblib
    from sklearn.model_selection import train_test_split

    from preprocessing import charger_donnees, separer_x_y

    if not config.CHEMIN_MODELE.exists():
        print(
            f"Modèle introuvable : {config.CHEMIN_MODELE}\n"
            "Lancez d'abord :  python train.py"
        )
        return 1

    modele = joblib.load(config.CHEMIN_MODELE)
    X, y = separer_x_y(charger_donnees())

    # Même découpage qu'à l'entraînement (graine et stratification identiques).
    _, X_test, _, y_test = train_test_split(
        X, y,
        test_size=config.PROPORTION_TEST,
        random_state=config.GRAINE_ALEATOIRE,
        stratify=y,
    )

    y_predit = modele.predict(X_test)

    print("=" * 78)
    print("RÉÉVALUATION DU MODÈLE SAUVEGARDÉ".center(78))
    print("=" * 78)
    print(f"\nFichier : {config.CHEMIN_MODELE.name}")
    print(f"Jeu de test : {len(X_test)} élèves\n")

    metriques = calculer_metriques(y_test, y_predit)
    for nom, valeur in metriques.items():
        print(f"  {nom:<20} {valeur:.4f}")

    print("\n--- Matrice de confusion ---")
    print(matrice_confusion(y_test, y_predit).to_string())

    print("\n--- Rapport par série ---")
    print(rapport_classification(y_test, y_predit))
    return 0


if __name__ == "__main__":
    sys.exit(main())
