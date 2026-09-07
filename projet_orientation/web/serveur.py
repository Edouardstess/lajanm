"""Service web de prédiction — déploiement du modèle d'orientation.

Ce module expose le modèle entraîné par ``train.py`` sous forme d'API HTTP,
accompagnée d'une page web de saisie.

    uvicorn web.serveur:app --reload        # depuis projet_orientation/
    python web/serveur.py                   # équivalent, port 8000

Précision importante : l'énoncé du projet impose que **l'application PyQt**
fonctionne localement, sans API ni connexion Internet. Cette contrainte est
respectée — `application.py` est inchangée et n'appelle jamais ce service.
Le présent module est un ajout : il rend le même modèle accessible depuis un
navigateur, sur un poste distant ou un téléphone.

Le modèle est chargé **une seule fois** au démarrage : chaque requête ne fait
qu'appeler ``predict`` sur le pipeline déjà en mémoire.
"""

from __future__ import annotations

import json
import os
import sys
from pathlib import Path
from typing import Literal

import joblib
from fastapi import FastAPI, HTTPException
from fastapi.responses import FileResponse, JSONResponse
from pydantic import BaseModel, Field, field_validator

# Le service vit dans web/ mais réutilise les modules du projet.
RACINE_PROJET = Path(__file__).resolve().parent.parent
if str(RACINE_PROJET) not in sys.path:
    sys.path.insert(0, str(RACINE_PROJET))

import config  # noqa: E402
from preprocessing import calculer_moyenne, construire_dataframe_eleve  # noqa: E402


DOSSIER_WEB = Path(__file__).resolve().parent

# ==========================================================================
# Chargement du modèle
# ==========================================================================
MODELE = None
METADONNEES: dict = {}
ERREUR_CHARGEMENT: str | None = None


def charger_modele() -> None:
    """Charge le pipeline sérialisé et ses métadonnées."""
    global MODELE, METADONNEES, ERREUR_CHARGEMENT

    if not config.CHEMIN_MODELE.exists():
        ERREUR_CHARGEMENT = (
            f"Modèle introuvable : {config.CHEMIN_MODELE}. "
            "Lancez « python train.py » avant de démarrer le service."
        )
        return

    try:
        MODELE = joblib.load(config.CHEMIN_MODELE)
        ERREUR_CHARGEMENT = None
    except Exception as erreur:  # pragma: no cover
        ERREUR_CHARGEMENT = f"Chargement impossible : {erreur}"
        return

    if config.CHEMIN_METADONNEES.exists():
        try:
            METADONNEES = json.loads(
                config.CHEMIN_METADONNEES.read_text(encoding="utf-8")
            )
        except json.JSONDecodeError:
            METADONNEES = {}


charger_modele()


# ==========================================================================
# Schémas d'entrée et de sortie
# ==========================================================================
NiveauMotivation = Literal["Faible", "Moyenne", "Élevée", "Très élevée"]
CentreInteret = Literal[
    "Arts et culture",
    "Informatique",
    "Lettres et langues",
    "Santé et environnement",
    "Sciences et technologie",
    "Économie et société",
]
OuiNon = Literal["Oui", "Non"]


class ProfilEleve(BaseModel):
    """Profil soumis pour prédiction.

    Les bornes sont déclarées ici : une note hors de [0, 100] est rejetée
    avec un code 422 avant même d'atteindre le modèle. Le nettoyage interne
    du pipeline reste en place comme seconde barrière.
    """

    math: float = Field(..., ge=0, le=100, description="Note de mathématiques sur 100")
    physique: float = Field(..., ge=0, le=100, description="Note de physique sur 100")
    svt: float = Field(..., ge=0, le=100, description="Note de SVT sur 100")
    francais: float = Field(..., ge=0, le=100, description="Note de français sur 100")
    histoire: float = Field(..., ge=0, le=100, description="Note d'histoire sur 100")

    aptitude_logique: float = Field(..., ge=1, le=5, description="Aptitude logique de 1 à 5")

    # Les trois champs suivants acceptent null : le modèle sait traiter
    # l'information manquante grâce à la modalité « Inconnu ».
    niveau_motivation: NiveauMotivation | None = None
    centre_interet: CentreInteret | None = None
    interesse_par_informatique: OuiNon | None = None

    @field_validator("math", "physique", "svt", "francais", "histoire")
    @classmethod
    def refuser_notes_non_finies(cls, valeur: float) -> float:
        if valeur != valeur:  # NaN
            raise ValueError("La note doit être un nombre.")
        return valeur

    model_config = {
        "json_schema_extra": {
            "example": {
                "math": 88,
                "physique": 84,
                "svt": 66,
                "francais": 62,
                "histoire": 58,
                "aptitude_logique": 5,
                "niveau_motivation": "Élevée",
                "centre_interet": "Informatique",
                "interesse_par_informatique": "Oui",
            }
        }
    }


class ResultatOrientation(BaseModel):
    serie_recommandee: str
    libelle: str
    probabilites: dict[str, float]
    moyenne_generale: float
    avertissements: list[str]
    modele: str


# ==========================================================================
# Application
# ==========================================================================
app = FastAPI(
    title="API d'aide à l'orientation scolaire",
    description=(
        "Prédiction de la série du Nouveau Secondaire (SMP, SVT, SES, LLA) "
        "à partir du profil académique d'un élève.\n\n"
        "**Outil d'aide à la décision.** Le modèle est entraîné sur un jeu de "
        "données simulé à des fins pédagogiques : ses résultats ne valident "
        "aucune méthode réelle d'orientation."
    ),
    version="1.0.0",
)


@app.get("/health", include_in_schema=False)
def sante():
    """Sonde de disponibilité — utilisée par Render (`healthCheckPath`)."""
    if MODELE is None:
        return JSONResponse(
            status_code=503,
            content={"statut": "indisponible", "detail": ERREUR_CHARGEMENT},
        )
    return {"statut": "ok", "modele": nom_modele()}


@app.get("/", include_in_schema=False)
def page_accueil():
    """Sert la page de saisie."""
    return FileResponse(DOSSIER_WEB / "page.html")


def nom_modele() -> str:
    if METADONNEES.get("modele_retenu"):
        return METADONNEES["modele_retenu"]
    if MODELE is not None:
        return type(MODELE.named_steps["classifieur"]).__name__
    return "—"


@app.get("/api/modele", summary="Informations sur le modèle déployé")
def informations_modele():
    if MODELE is None:
        raise HTTPException(status_code=503, detail=ERREUR_CHARGEMENT)

    return {
        "modele": nom_modele(),
        "date_entrainement": METADONNEES.get("date_entrainement"),
        "series": [str(classe) for classe in MODELE.classes_],
        "metriques_test": METADONNEES.get("metriques_test", {}),
        "validation_croisee": METADONNEES.get("validation_croisee_f1_macro", {}),
        "comparaison": METADONNEES.get("comparaison", {}),
        "importances_principales": METADONNEES.get("importances_principales", {}),
        "variables_attendues": config.COLONNES_FEATURES,
        "avertissement": (
            "Outil d'aide à la décision. Jeu de données d'entraînement simulé "
            "à des fins pédagogiques."
        ),
    }


@app.post(
    "/api/predire",
    response_model=ResultatOrientation,
    summary="Prédit la série la plus compatible avec un profil",
)
def predire(profil: ProfilEleve):
    if MODELE is None:
        raise HTTPException(status_code=503, detail=ERREUR_CHARGEMENT)

    donnees_profil = profil.model_dump()
    donnees_profil["moyenne_generale"] = calculer_moyenne(donnees_profil)

    # Le DataFrame reprend les colonnes attendues par le pipeline, qui
    # applique ensuite exactement le prétraitement de l'entraînement.
    donnees = construire_dataframe_eleve(donnees_profil)

    try:
        serie = str(MODELE.predict(donnees)[0])
        probabilites = {}
        if hasattr(MODELE, "predict_proba"):
            valeurs = MODELE.predict_proba(donnees)[0]
            probabilites = {
                str(classe): round(float(valeur), 4)
                for classe, valeur in zip(MODELE.classes_, valeurs)
            }
    except Exception as erreur:  # pragma: no cover
        raise HTTPException(status_code=500, detail=f"Prédiction impossible : {erreur}")

    return ResultatOrientation(
        serie_recommandee=serie,
        libelle=config.LIBELLES_SERIES.get(serie, ""),
        probabilites=probabilites,
        moyenne_generale=donnees_profil["moyenne_generale"],
        avertissements=construire_avertissements(donnees_profil),
        modele=nom_modele(),
    )


def construire_avertissements(profil: dict) -> list[str]:
    """Signale ce qui affaiblit la fiabilité de la prédiction."""
    avertissements = []

    if profil.get(config.COLONNE_CATEGORIELLE) is None:
        avertissements.append(
            "Le centre d'intérêt n'est pas renseigné. C'est la variable la plus "
            "influente du modèle : la prédiction est nettement moins fiable."
        )
    if profil.get(config.COLONNE_MOTIVATION) is None:
        avertissements.append("Le niveau de motivation n'est pas renseigné.")
    if profil.get(config.COLONNE_BINAIRE) is None:
        avertissements.append("L'intérêt pour l'informatique n'est pas renseigné.")

    notes = [profil[matiere] for matiere in config.COLONNES_NOTES]
    if len(set(notes)) == 1:
        avertissements.append(
            "Les cinq notes sont identiques : le modèle ne dispose d'aucun "
            "contraste entre les matières."
        )

    return avertissements


# ==========================================================================
if __name__ == "__main__":
    import uvicorn

    # Render fournit le port à écouter via la variable PORT.
    port = int(os.environ.get("PORT", "8000"))
    uvicorn.run(app, host="0.0.0.0", port=port)
