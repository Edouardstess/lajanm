"""Chargement et prétraitement des données (Partie B du projet).

Le prétraitement est encapsulé dans un ``ColumnTransformer`` scikit-learn,
lui-même placé dans un ``Pipeline`` avec le classifieur. C'est ce choix qui
garantit l'exigence du cahier des charges : « appliquer exactement le même
prétraitement que celui utilisé lors de l'entraînement ». L'application PyQt
ne réimplémente rien, elle appelle ``predict`` sur le pipeline sérialisé.

Types de variables distingués :

===========================  ===========================  ========================
Type                         Colonnes                     Traitement
===========================  ===========================  ========================
Numériques continues         math, physique, svt,         médiane + StandardScaler
                             francais, histoire,
                             moyenne_generale
Ordinale numérique           aptitude_logique (1-5)       médiane + StandardScaler
Ordinale textuelle           niveau_motivation            OrdinalEncoder ordonné
Binaire                      interesse_par_informatique   OneHotEncoder
Catégorielle nominale        centre_interet               OneHotEncoder
Identifiant (exclu)          id_eleve                     jamais utilisé
===========================  ===========================  ========================
"""

from __future__ import annotations

import numpy as np
import pandas as pd
from sklearn.base import BaseEstimator, TransformerMixin
from sklearn.compose import ColumnTransformer
from sklearn.impute import SimpleImputer
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, OrdinalEncoder, StandardScaler

import config


# ==========================================================================
# Chargement
# ==========================================================================
def charger_donnees(chemin=None) -> pd.DataFrame:
    """Charge le jeu de données brut (CSV de préférence, sinon Excel)."""
    if chemin is not None:
        chemin = str(chemin)
        if chemin.endswith((".xlsx", ".xls")):
            return pd.read_excel(chemin)
        return pd.read_csv(chemin)

    if config.CHEMIN_DATASET_CSV.exists():
        return pd.read_csv(config.CHEMIN_DATASET_CSV)
    if config.CHEMIN_DATASET_XLSX.exists():
        return pd.read_excel(config.CHEMIN_DATASET_XLSX)

    raise FileNotFoundError(
        f"Aucun jeu de données trouvé dans {config.DOSSIER_DATA}. "
        "Placez-y dataset_orientation_NS_3000.csv."
    )


def separer_x_y(df: pd.DataFrame):
    """Sépare les variables explicatives de la variable cible.

    L'identifiant ``id_eleve`` est écarté : c'est une clé technique, sans
    pouvoir prédictif, et l'inclure reviendrait à laisser le modèle
    apprendre par cœur des numéros d'élèves.
    """
    lignes_valides = df[config.COLONNE_CIBLE].notna()
    df = df.loc[lignes_valides]

    X = df[config.COLONNES_FEATURES].copy()
    y = df[config.COLONNE_CIBLE].copy()
    return X, y


# ==========================================================================
# Nettoyage : première étape du pipeline
# ==========================================================================
class NettoyeurValeurs(BaseEstimator, TransformerMixin):
    """Remplace les valeurs hors domaine par ``NaN``.

    Les valeurs aberrantes du jeu de données sont des notes impossibles
    (``-10``, ``115``) et d'éventuelles modalités inconnues. Plutôt que de
    supprimer ces lignes — ce qui jetterait les autres colonnes, correctes —
    on les convertit en valeurs manquantes, prises en charge juste après par
    les imputeurs. Les anomalies sont ainsi traitées par le même mécanisme
    que les trous d'origine.

    Placer ce nettoyage *dans* le pipeline le rend automatiquement actif au
    moment de la prédiction, y compris depuis l'interface PyQt.
    """

    def fit(self, X, y=None):
        self.colonnes_ = list(X.columns)
        return self

    def transform(self, X):
        X = pd.DataFrame(X, columns=getattr(self, "colonnes_", None)).copy()

        # Notes et moyenne : bornées à [0, 100].
        for colonne in config.COLONNES_NUMERIQUES:
            if colonne in X.columns:
                valeurs = pd.to_numeric(X[colonne], errors="coerce")
                hors_bornes = (valeurs < config.NOTE_MIN) | (valeurs > config.NOTE_MAX)
                X[colonne] = valeurs.mask(hors_bornes)

        # Aptitude logique : échelle 1 à 5.
        if config.COLONNE_APTITUDE in X.columns:
            valeurs = pd.to_numeric(X[config.COLONNE_APTITUDE], errors="coerce")
            hors_bornes = (
                (valeurs < config.APTITUDE_MIN) | (valeurs > config.APTITUDE_MAX)
            )
            X[config.COLONNE_APTITUDE] = valeurs.mask(hors_bornes)

        # Variables textuelles : toute modalité non prévue devient manquante.
        modalites_attendues = {
            config.COLONNE_MOTIVATION: config.ORDRE_MOTIVATION,
            config.COLONNE_BINAIRE: config.MODALITES_BINAIRE,
            config.COLONNE_CATEGORIELLE: config.MODALITES_CENTRE_INTERET,
        }
        for colonne, modalites in modalites_attendues.items():
            if colonne in X.columns:
                serie = X[colonne].astype("object")
                serie = serie.where(serie.isin(modalites), other=np.nan)
                X[colonne] = serie

        return X

    def get_feature_names_out(self, input_features=None):
        return np.asarray(
            input_features
            if input_features is not None
            else getattr(self, "colonnes_", [])
        )


# ==========================================================================
# Construction du prétraitement
# ==========================================================================
def construire_preprocesseur() -> ColumnTransformer:
    """Assemble le ``ColumnTransformer`` appliqué à toutes les colonnes."""

    # -- Numériques continues -------------------------------------------
    # Médiane plutôt que moyenne : insensible aux valeurs extrêmes.
    # Standardisation indispensable pour la régression logistique, le KNN
    # et le SVM, qui raisonnent sur des distances ou des coefficients.
    pipeline_numerique = Pipeline([
        ("imputation", SimpleImputer(strategy="median")),
        ("normalisation", StandardScaler()),
    ])

    # -- Ordinale numérique (aptitude logique 1-5) -----------------------
    pipeline_aptitude = Pipeline([
        ("imputation", SimpleImputer(strategy="median")),
        ("normalisation", StandardScaler()),
    ])

    # -- Ordinale textuelle (niveau de motivation) -----------------------
    # L'ordre Faible < Moyenne < Élevée < Très élevée porte une information
    # que le one-hot détruirait : on encode donc en entiers ordonnés.
    pipeline_motivation = Pipeline([
        ("imputation", SimpleImputer(strategy="most_frequent")),
        ("encodage", OrdinalEncoder(
            categories=[config.ORDRE_MOTIVATION],
            handle_unknown="use_encoded_value",
            unknown_value=np.nan,
        )),
        ("imputation_post", SimpleImputer(strategy="most_frequent")),
        ("normalisation", StandardScaler()),
    ])

    # -- Binaire (intérêt pour l'informatique) ---------------------------
    # Les valeurs manquantes deviennent une modalité « Inconnu » à part
    # entière : imputer par le mode inventerait une réponse que l'élève
    # n'a pas donnée.
    pipeline_binaire = Pipeline([
        ("imputation", SimpleImputer(
            strategy="constant", fill_value=config.VALEUR_INCONNUE
        )),
        ("encodage", OneHotEncoder(
            categories=[config.MODALITES_BINAIRE + [config.VALEUR_INCONNUE]],
            handle_unknown="ignore",
            sparse_output=False,
        )),
    ])

    # -- Catégorielle nominale (centre d'intérêt) ------------------------
    pipeline_categoriel = Pipeline([
        ("imputation", SimpleImputer(
            strategy="constant", fill_value=config.VALEUR_INCONNUE
        )),
        ("encodage", OneHotEncoder(
            categories=[config.MODALITES_CENTRE_INTERET + [config.VALEUR_INCONNUE]],
            handle_unknown="ignore",
            sparse_output=False,
        )),
    ])

    return ColumnTransformer(
        transformers=[
            ("numeriques", pipeline_numerique, config.COLONNES_NUMERIQUES),
            ("aptitude", pipeline_aptitude, [config.COLONNE_APTITUDE]),
            ("motivation", pipeline_motivation, [config.COLONNE_MOTIVATION]),
            ("binaire", pipeline_binaire, [config.COLONNE_BINAIRE]),
            ("categorielle", pipeline_categoriel, [config.COLONNE_CATEGORIELLE]),
        ],
        remainder="drop",  # toute colonne non listée (dont id_eleve) est ignorée
        verbose_feature_names_out=False,
    )


def construire_pipeline(classifieur) -> Pipeline:
    """Chaîne nettoyage → prétraitement → classifieur.

    C'est cet objet complet qui est sauvegardé avec Joblib, si bien que le
    fichier ``.joblib`` contient à la fois le modèle et sa préparation de
    données.
    """
    return Pipeline([
        ("nettoyage", NettoyeurValeurs()),
        ("preparation", construire_preprocesseur()),
        ("classifieur", classifieur),
    ])


# ==========================================================================
# Utilitaires partagés avec l'application PyQt
# ==========================================================================
def calculer_moyenne(notes: dict) -> float:
    """Moyenne générale à partir des cinq notes disciplinaires.

    Reproduit la règle observée dans le jeu de données : la moyenne
    générale y est la moyenne arithmétique non pondérée des cinq notes.
    """
    valeurs = [
        float(notes[matiere])
        for matiere in config.COLONNES_NOTES
        if notes.get(matiere) is not None
    ]
    if not valeurs:
        return float("nan")
    return round(sum(valeurs) / len(valeurs), 2)


def construire_dataframe_eleve(profil: dict) -> pd.DataFrame:
    """Transforme un profil saisi en DataFrame d'une ligne.

    Les colonnes sont produites dans l'ordre exact attendu par le pipeline.
    Si ``moyenne_generale`` est absente du profil, elle est calculée à
    partir des notes.
    """
    profil = dict(profil)
    if profil.get("moyenne_generale") is None:
        profil["moyenne_generale"] = calculer_moyenne(profil)

    ligne = {colonne: profil.get(colonne) for colonne in config.COLONNES_FEATURES}
    return pd.DataFrame([ligne], columns=config.COLONNES_FEATURES)
