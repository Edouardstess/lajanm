"""Partie A — Analyse exploratoire du jeu de données.

Exécution :

    python exploration.py

Le script affiche l'analyse dans la console, écrit une copie texte dans
``reports/exploration.txt`` et enregistre les figures dans ``reports/``.
"""

from __future__ import annotations

import sys

import matplotlib
matplotlib.use("Agg")  # rendu fichier : aucun affichage interactif requis
import matplotlib.pyplot as plt
import pandas as pd

import config
from preprocessing import charger_donnees


LARGEUR = 78


class Rapporteur:
    """Écrit simultanément à l'écran et dans un fichier texte."""

    def __init__(self, chemin):
        self.lignes = []
        self.chemin = chemin

    def ecrire(self, texte=""):
        print(texte)
        self.lignes.append(str(texte))

    def titre(self, texte):
        self.ecrire()
        self.ecrire("=" * LARGEUR)
        self.ecrire(texte.upper().center(LARGEUR))
        self.ecrire("=" * LARGEUR)

    def section(self, texte):
        self.ecrire()
        self.ecrire(f"--- {texte} " + "-" * max(0, LARGEUR - len(texte) - 5))

    def sauvegarder(self):
        self.chemin.write_text("\n".join(self.lignes), encoding="utf-8")


# ==========================================================================
# Analyses
# ==========================================================================
def decrire_structure(df: pd.DataFrame, rap: Rapporteur) -> None:
    rap.section("Dimensions")
    rap.ecrire(f"{df.shape[0]} lignes x {df.shape[1]} colonnes")

    rap.section("Cinq premières lignes")
    rap.ecrire(df.head().to_string())

    rap.section("Cinq dernières lignes")
    rap.ecrire(df.tail().to_string())

    rap.section("Types de données")
    types = pd.DataFrame({
        "type": df.dtypes.astype(str),
        "valeurs_uniques": df.nunique(),
    })
    rap.ecrire(types.to_string())

    rap.section("Doublons")
    rap.ecrire(f"lignes entièrement dupliquées : {df.duplicated().sum()}")
    rap.ecrire(
        f"identifiants dupliqués : {df[config.COLONNE_ID].duplicated().sum()} "
        f"({df[config.COLONNE_ID].nunique()} identifiants pour {len(df)} lignes)"
    )


def decrire_statistiques(df: pd.DataFrame, rap: Rapporteur) -> None:
    rap.section("Statistiques descriptives — variables numériques")
    rap.ecrire(df.describe().T.round(2).to_string())

    rap.section("Statistiques descriptives — variables textuelles")
    colonnes_texte = df.select_dtypes(include=["object", "string"]).columns
    rap.ecrire(df[colonnes_texte].describe().T.to_string())


def decrire_valeurs_manquantes(df: pd.DataFrame, rap: Rapporteur) -> pd.DataFrame:
    rap.section("Valeurs manquantes")
    nb = df.isna().sum()
    tableau = pd.DataFrame({
        "manquants": nb,
        "pourcentage": (nb / len(df) * 100).round(2),
    })
    tableau = tableau[tableau["manquants"] > 0].sort_values(
        "manquants", ascending=False
    )
    if tableau.empty:
        rap.ecrire("Aucune valeur manquante.")
    else:
        rap.ecrire(tableau.to_string())
        rap.ecrire()
        rap.ecrire(f"Total de cellules manquantes : {int(nb.sum())}")
        complet = df.dropna()
        rap.ecrire(
            f"Lignes complètes : {len(complet)} / {len(df)} "
            f"({len(complet) / len(df) * 100:.1f} %)"
        )
    return tableau


def decrire_valeurs_aberrantes(df: pd.DataFrame, rap: Rapporteur) -> pd.DataFrame:
    """Deux lectures complémentaires des anomalies.

    1. Les valeurs *impossibles* : une note hors de l'intervalle [0, 100] ou
       une aptitude hors de [1, 5] ne peut pas exister, quelle que soit la
       distribution. Ce sont celles-là qu'il faut corriger.
    2. Les valeurs *extrêmes* au sens de l'écart interquartile (IQR) : elles
       sont rares mais plausibles, et on choisit de les conserver.
    """
    rap.section("Valeurs aberrantes — hors bornes physiques")
    lignes = []
    for colonne in config.COLONNES_NUMERIQUES:
        serie = df[colonne]
        sous = int((serie < config.NOTE_MIN).sum())
        sur = int((serie > config.NOTE_MAX).sum())
        lignes.append({
            "colonne": colonne,
            "min": round(serie.min(), 2),
            "max": round(serie.max(), 2),
            "sous_0": sous,
            "sur_100": sur,
            "total": sous + sur,
        })

    serie = df[config.COLONNE_APTITUDE]
    hors = int(
        ((serie < config.APTITUDE_MIN) | (serie > config.APTITUDE_MAX)).sum()
    )
    lignes.append({
        "colonne": config.COLONNE_APTITUDE,
        "min": serie.min(),
        "max": serie.max(),
        "sous_0": "-",
        "sur_100": "-",
        "total": hors,
    })

    tableau = pd.DataFrame(lignes)
    rap.ecrire(tableau.to_string(index=False))

    masque = pd.Series(False, index=df.index)
    for colonne in config.COLONNES_NUMERIQUES:
        masque |= (df[colonne] < config.NOTE_MIN) | (df[colonne] > config.NOTE_MAX)
    rap.ecrire()
    rap.ecrire(f"Lignes concernées par au moins une note impossible : {int(masque.sum())}")
    if masque.any():
        rap.ecrire(
            df.loc[masque, [config.COLONNE_ID] + config.COLONNES_NOTES
                   + ["moyenne_generale", config.COLONNE_CIBLE]].head(20).to_string()
        )

    rap.section("Valeurs extrêmes — méthode de l'écart interquartile")
    lignes = []
    for colonne in config.COLONNES_NUMERIQUES + [config.COLONNE_APTITUDE]:
        serie = df[colonne].dropna()
        q1, q3 = serie.quantile(0.25), serie.quantile(0.75)
        iqr = q3 - q1
        bas, haut = q1 - 1.5 * iqr, q3 + 1.5 * iqr
        lignes.append({
            "colonne": colonne,
            "Q1": round(q1, 2),
            "Q3": round(q3, 2),
            "borne_basse": round(bas, 2),
            "borne_haute": round(haut, 2),
            "nb_extremes": int(((serie < bas) | (serie > haut)).sum()),
        })
    rap.ecrire(pd.DataFrame(lignes).to_string(index=False))
    return tableau


def decrire_coherence_moyenne(df: pd.DataFrame, rap: Rapporteur) -> None:
    """Vérifie la relation entre les notes et la moyenne générale.

    Ce contrôle révèle comment le jeu de données a été fabriqué et donne un
    moyen indépendant de repérer les notes corrompues.
    """
    rap.section("Cohérence de la moyenne générale")
    complet = df.dropna(subset=config.COLONNES_NOTES)
    recalcul = complet[config.COLONNES_NOTES].mean(axis=1)
    ecart = (recalcul - complet["moyenne_generale"]).abs()

    coherentes = int((ecart < 0.05).sum())
    rap.ecrire(f"Lignes sans note manquante : {len(complet)}")
    rap.ecrire(
        f"Moyenne recalculée identique à la colonne fournie : "
        f"{coherentes} / {len(complet)} ({coherentes / len(complet) * 100:.1f} %)"
    )
    rap.ecrire(f"Écart maximal observé : {ecart.max():.2f} point")
    rap.ecrire()
    rap.ecrire(
        "Lecture : moyenne_generale est la moyenne arithmétique des cinq notes,\n"
        "calculée AVANT l'injection des anomalies. Les seules lignes incohérentes\n"
        "sont précisément celles dont une note a été corrompue. La colonne n'a donc\n"
        "aucune valeur manquante et sert de témoin pour détecter les notes fausses."
    )

    incoherentes = complet.loc[ecart >= 0.05]
    if len(incoherentes):
        rap.ecrire()
        rap.ecrire(f"Lignes incohérentes : {len(incoherentes)}")


def decrire_cible(df: pd.DataFrame, rap: Rapporteur) -> None:
    rap.section("Répartition des quatre séries")
    effectifs = df[config.COLONNE_CIBLE].value_counts()
    proportions = df[config.COLONNE_CIBLE].value_counts(normalize=True) * 100
    tableau = pd.DataFrame({
        "effectif": effectifs,
        "pourcentage": proportions.round(2),
        "libellé": [config.LIBELLES_SERIES.get(s, "") for s in effectifs.index],
    })
    rap.ecrire(tableau.to_string())
    rap.ecrire()
    ratio = effectifs.max() / effectifs.min()
    rap.ecrire(
        f"Rapport effectif max / min = {ratio:.2f} → classes équilibrées."
        if ratio < 1.5 else
        f"Rapport effectif max / min = {ratio:.2f} → déséquilibre à surveiller."
    )
    rap.ecrire(
        f"Référence à battre (classe majoritaire) : {proportions.max():.1f} % d'exactitude."
    )


def decrire_liens(df: pd.DataFrame, rap: Rapporteur) -> None:
    rap.section("Note moyenne par série")
    profil = df.groupby(config.COLONNE_CIBLE)[
        config.COLONNES_NOTES + [config.COLONNE_APTITUDE, "moyenne_generale"]
    ].mean().round(2)
    rap.ecrire(profil.to_string())

    rap.section("Centre d'intérêt croisé avec la série")
    croisement = pd.crosstab(df[config.COLONNE_CATEGORIELLE], df[config.COLONNE_CIBLE])
    rap.ecrire(croisement.to_string())
    rap.ecrire()
    part_max = (croisement.T / croisement.sum(axis=1)).T.max(axis=1) * 100
    rap.ecrire("Part de la série dominante pour chaque centre d'intérêt (%) :")
    rap.ecrire(part_max.round(1).to_string())
    rap.ecrire()
    deterministes = (part_max >= 99.9).sum()
    rap.ecrire(
        f"{deterministes} modalités sur {len(part_max)} déterminent la série à elles\n"
        "seules. C'est l'observation la plus importante du jeu de données : le\n"
        "centre d'intérêt est de très loin le prédicteur le plus fort, et seule la\n"
        "modalité « Sciences et technologie » reste réellement ambiguë."
    )

    rap.section("Intérêt pour l'informatique croisé avec la série")
    rap.ecrire(pd.crosstab(df[config.COLONNE_BINAIRE], df[config.COLONNE_CIBLE]).to_string())

    rap.section("Niveau de motivation croisé avec la série")
    croisement = pd.crosstab(df[config.COLONNE_MOTIVATION], df[config.COLONNE_CIBLE])
    ordre = [m for m in config.ORDRE_MOTIVATION if m in croisement.index]
    rap.ecrire(croisement.loc[ordre].to_string())
    rap.ecrire()
    rap.ecrire(
        "La modalité « Faible » ne compte qu'une poignée d'élèves : la variable est\n"
        "très déséquilibrée et apportera peu d'information."
    )

    rap.section("Corrélations entre variables numériques")
    numeriques = config.COLONNES_NUMERIQUES + [config.COLONNE_APTITUDE]
    rap.ecrire(df[numeriques].corr().round(2).to_string())


# ==========================================================================
# Figures
# ==========================================================================
def tracer_figures(df: pd.DataFrame) -> list:
    config.creer_dossiers()
    fichiers = []

    # 1. Répartition des séries.
    fig, ax = plt.subplots(figsize=(7, 4.5))
    effectifs = df[config.COLONNE_CIBLE].value_counts().reindex(config.SERIES)
    ax.bar(effectifs.index, effectifs.values, color="#4C78A8")
    for x, valeur in enumerate(effectifs.values):
        ax.text(x, valeur + 8, str(int(valeur)), ha="center", fontsize=10)
    ax.set_title("Répartition des séries du Nouveau Secondaire")
    ax.set_ylabel("Nombre d'élèves")
    ax.set_ylim(0, effectifs.max() * 1.15)
    fig.tight_layout()
    chemin = config.DOSSIER_REPORTS / "01_repartition_series.png"
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    fichiers.append(chemin)

    # 2. Distribution des notes.
    fig, axes = plt.subplots(2, 3, figsize=(13, 7))
    for ax, colonne in zip(axes.ravel(), config.COLONNES_NUMERIQUES):
        serie = df[colonne].dropna()
        ax.hist(serie, bins=30, color="#72B7B2", edgecolor="white")
        ax.set_title(colonne)
        ax.set_xlabel("note")
    axes.ravel()[-1].set_title("moyenne_generale")
    fig.suptitle("Distribution des variables numériques (valeurs brutes)", fontsize=13)
    fig.tight_layout()
    chemin = config.DOSSIER_REPORTS / "02_distribution_notes.png"
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    fichiers.append(chemin)

    # 3. Boîtes à moustaches par série : lecture directe des profils.
    fig, axes = plt.subplots(1, 5, figsize=(16, 4.5), sharey=True)
    for ax, colonne in zip(axes, config.COLONNES_NOTES):
        donnees = [
            df.loc[df[config.COLONNE_CIBLE] == serie, colonne].dropna()
            for serie in config.SERIES
        ]
        ax.boxplot(donnees, tick_labels=config.SERIES)
        ax.set_title(colonne)
        ax.grid(axis="y", alpha=0.3)
    axes[0].set_ylabel("note sur 100")
    fig.suptitle("Notes par série — chaque série a un profil distinct", fontsize=13)
    fig.tight_layout()
    chemin = config.DOSSIER_REPORTS / "03_notes_par_serie.png"
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    fichiers.append(chemin)

    # 4. Valeurs manquantes.
    fig, ax = plt.subplots(figsize=(8, 4.5))
    manquants = df.isna().sum()
    manquants = manquants[manquants > 0].sort_values()
    ax.barh(manquants.index, manquants.values, color="#E45756")
    for y, valeur in enumerate(manquants.values):
        ax.text(valeur + 1, y, f"{valeur / len(df) * 100:.1f} %", va="center", fontsize=9)
    ax.set_title("Valeurs manquantes par colonne")
    ax.set_xlabel("nombre de valeurs manquantes")
    ax.set_xlim(0, manquants.max() * 1.25)
    fig.tight_layout()
    chemin = config.DOSSIER_REPORTS / "04_valeurs_manquantes.png"
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    fichiers.append(chemin)

    # 5. Centre d'intérêt x série.
    croisement = pd.crosstab(df[config.COLONNE_CATEGORIELLE], df[config.COLONNE_CIBLE])
    croisement = croisement.reindex(columns=config.SERIES, fill_value=0)
    fig, ax = plt.subplots(figsize=(9, 5))
    bas = [0] * len(croisement)
    couleurs = ["#4C78A8", "#54A24B", "#EECA3B", "#B279A2"]
    for serie, couleur in zip(config.SERIES, couleurs):
        ax.barh(croisement.index, croisement[serie], left=bas, label=serie, color=couleur)
        bas = [b + v for b, v in zip(bas, croisement[serie])]
    ax.set_title("Centre d'intérêt et série cible")
    ax.set_xlabel("Nombre d'élèves")
    ax.legend(title="Série")
    fig.tight_layout()
    chemin = config.DOSSIER_REPORTS / "05_centre_interet_serie.png"
    fig.savefig(chemin, dpi=130)
    plt.close(fig)
    fichiers.append(chemin)

    return fichiers


# ==========================================================================
def main() -> int:
    config.creer_dossiers()
    df = charger_donnees()

    rap = Rapporteur(config.DOSSIER_REPORTS / "exploration.txt")
    rap.titre("Partie A — Analyse exploratoire")

    decrire_structure(df, rap)
    decrire_statistiques(df, rap)
    decrire_valeurs_manquantes(df, rap)
    decrire_valeurs_aberrantes(df, rap)
    decrire_coherence_moyenne(df, rap)
    decrire_cible(df, rap)
    decrire_liens(df, rap)

    rap.titre("Observations principales")
    for numero, observation in enumerate(OBSERVATIONS, start=1):
        rap.ecrire(f"{numero}. {observation}")
        rap.ecrire()

    fichiers = tracer_figures(df)
    rap.section("Figures produites")
    for chemin in fichiers:
        rap.ecrire(f"  {chemin.relative_to(config.RACINE)}")

    rap.sauvegarder()
    rap.ecrire()
    rap.ecrire(f"Rapport texte : {rap.chemin.relative_to(config.RACINE)}")
    return 0


OBSERVATIONS = [
    "Le jeu de données compte 3 000 élèves et 12 colonnes, sans doublon, avec un\n"
    "   identifiant unique par élève. Cet identifiant est écarté de la modélisation.",

    "Les quatre séries sont presque équilibrées (710 à 786 élèves). Un modèle qui\n"
    "   prédirait toujours la classe majoritaire n'atteindrait que ~26 % : c'est la\n"
    "   référence minimale à dépasser.",

    "Neuf colonnes sur douze contiennent des valeurs manquantes : 900 cellules sur\n"
    "   30 000, soit 3 %. Mais elles sont dispersées, si bien que seules 2 222 lignes\n"
    "   sur 3 000 sont complètes : supprimer les lignes incomplètes coûterait 25,9 %\n"
    "   de l'échantillon. D'où le choix de l'imputation.",

    "Dix-sept notes sont physiquement impossibles (-10 ou 115). Elles sont converties\n"
    "   en valeurs manquantes puis imputées, ce qui évite de jeter le reste de la ligne.",

    "moyenne_generale est la moyenne exacte des cinq notes et ne comporte aucun trou :\n"
    "   elle a été calculée avant l'injection des anomalies. Elle est donc légèrement\n"
    "   plus informative que les notes elles-mêmes et sert de contrôle de cohérence.",

    "Chaque série possède un profil de notes net : SMP domine en maths et physique,\n"
    "   SVT en SVT, LLA en français et histoire, SES en histoire avec un français\n"
    "   solide. L'aptitude logique croît de LLA (2,5) à SMP (4,2).",

    "Le centre d'intérêt est un prédicteur presque déterministe : cinq de ses six\n"
    "   modalités correspondent à une seule série. Seule « Sciences et technologie »\n"
    "   se répartit entre SMP, SES et SVT. Une exactitude élevée est donc attendue,\n"
    "   mais elle mesure surtout la construction du jeu de données simulé.",

    "Le niveau de motivation est très déséquilibré (14 élèves seulement en « Faible »)\n"
    "   et se répartit de façon comparable entre les séries : son apport sera faible.",
]


if __name__ == "__main__":
    sys.exit(main())
