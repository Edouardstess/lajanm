"""Configuration centrale du projet d'orientation scolaire.

Toutes les constantes partagées entre l'exploration, l'entraînement,
l'évaluation et l'application PyQt sont définies ici, afin qu'une
modification (chemin, borne, liste de modalités) se propage partout.
"""

from pathlib import Path

# --------------------------------------------------------------------------
# Chemins
# --------------------------------------------------------------------------
RACINE = Path(__file__).resolve().parent

DOSSIER_DATA = RACINE / "data"
DOSSIER_MODEL = RACINE / "model"
DOSSIER_REPORTS = RACINE / "reports"

CHEMIN_DATASET_CSV = DOSSIER_DATA / "dataset_orientation_NS_3000.csv"
CHEMIN_DATASET_XLSX = DOSSIER_DATA / "dataset_orientation_NS_3000.xlsx"

CHEMIN_MODELE = DOSSIER_MODEL / "modele_orientation.joblib"
CHEMIN_METADONNEES = DOSSIER_MODEL / "metadonnees_modele.json"
CHEMIN_COMPARAISON = DOSSIER_MODEL / "comparaison_modeles.csv"

CHEMIN_BASE_HISTORIQUE = DOSSIER_MODEL / "historique_orientations.sqlite"

# --------------------------------------------------------------------------
# Colonnes
# --------------------------------------------------------------------------
COLONNE_ID = "id_eleve"          # identifiant : jamais utilisé comme prédicteur
COLONNE_CIBLE = "serie_cible"

# Notes scolaires sur 100.
COLONNES_NOTES = ["math", "physique", "svt", "francais", "histoire"]

# Variables numériques continues fournies au modèle.
COLONNES_NUMERIQUES = COLONNES_NOTES + ["moyenne_generale"]

# Variable ordinale numérique (échelle 1 à 5).
COLONNE_APTITUDE = "aptitude_logique"

# Variable ordinale textuelle : l'ordre ci-dessous est significatif.
COLONNE_MOTIVATION = "niveau_motivation"
ORDRE_MOTIVATION = ["Faible", "Moyenne", "Élevée", "Très élevée"]

# Variable binaire Oui/Non.
COLONNE_BINAIRE = "interesse_par_informatique"
MODALITES_BINAIRE = ["Non", "Oui"]

# Variable catégorielle nominale.
COLONNE_CATEGORIELLE = "centre_interet"
MODALITES_CENTRE_INTERET = [
    "Arts et culture",
    "Informatique",
    "Lettres et langues",
    "Santé et environnement",
    "Sciences et technologie",
    "Économie et société",
]

# Ordre des colonnes attendu en entrée du pipeline. L'application PyQt
# construit son DataFrame à une ligne en respectant exactement cet ordre.
COLONNES_FEATURES = (
    COLONNES_NUMERIQUES
    + [COLONNE_APTITUDE, COLONNE_MOTIVATION, COLONNE_BINAIRE, COLONNE_CATEGORIELLE]
)

# Valeur utilisée pour matérialiser une modalité manquante (plutôt que
# d'imputer par le mode, ce qui injecterait un signal fort et faux).
VALEUR_INCONNUE = "Inconnu"

# --------------------------------------------------------------------------
# Bornes de validité (utilisées pour le nettoyage et pour la saisie PyQt)
# --------------------------------------------------------------------------
NOTE_MIN, NOTE_MAX = 0.0, 100.0
APTITUDE_MIN, APTITUDE_MAX = 1.0, 5.0

# --------------------------------------------------------------------------
# Cible
# --------------------------------------------------------------------------
SERIES = ["SMP", "SVT", "SES", "LLA"]

LIBELLES_SERIES = {
    "SMP": "Sciences, Mathématiques et Physique",
    "SVT": "Sciences de la Vie et de la Terre",
    "SES": "Sciences Économiques et Sociales",
    "LLA": "Lettres, Langues et Arts",
}

# --------------------------------------------------------------------------
# Apprentissage
# --------------------------------------------------------------------------
GRAINE_ALEATOIRE = 42
PROPORTION_TEST = 0.2
NB_PLIS_VALIDATION_CROISEE = 5


def creer_dossiers() -> None:
    """Crée les dossiers de sortie s'ils n'existent pas encore."""
    for dossier in (DOSSIER_DATA, DOSSIER_MODEL, DOSSIER_REPORTS):
        dossier.mkdir(parents=True, exist_ok=True)
