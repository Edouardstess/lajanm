"""Explication d'une recommandation, partagée par toutes les interfaces.

Ce module répond à la question « pourquoi cette série ? » pour un profil
donné. Il est utilisé à l'identique par l'application PyQt et par
l'assistant conversationnel du service web : la même prédiction reçoit
donc partout la même justification.

Le principe : pour un modèle linéaire, le score d'une série est la somme
``coefficient × valeur encodée``. On peut donc décomposer la décision
**pour cet élève précis**, et pas seulement dire quelles variables comptent
en moyenne. Si le modèle n'est pas linéaire, une explication descriptive de
repli prend le relais.
"""

from __future__ import annotations

import numpy as np

import config
from preprocessing import construire_dataframe_eleve


LIBELLES_MATIERES = {
    "math": "Mathématiques",
    "physique": "Physique",
    "svt": "SVT",
    "francais": "Français",
    "histoire": "Histoire",
}

DESCRIPTION_APTITUDE = {
    1: "très faible",
    2: "faible",
    3: "moyenne",
    4: "bonne",
    5: "très bonne",
}


# ==========================================================================
# Libellés
# ==========================================================================
def libelle_variable(nom: str, profil: dict, valeur_encodee: float = 0.0) -> str:
    """Traduit un nom de colonne encodée en phrase lisible.

    ``valeur_encodee`` est la valeur standardisée : son signe indique si
    l'élève est au-dessus ou en dessous de la moyenne des 3 000 élèves.
    Sans cette nuance, « note de français 58/100 » pousserait vers SMP sans
    qu'on comprenne que c'est justement parce qu'elle est basse.
    """

    def situer(seuil: float = 0.35) -> str:
        if valeur_encodee > seuil:
            return "élevée"
        if valeur_encodee < -seuil:
            return "basse"
        return "dans la moyenne"

    if nom in LIBELLES_MATIERES:
        matiere = LIBELLES_MATIERES[nom].lower()
        article = "d'" if matiere[0] in "aeiouyh" else "de "
        return f"note {article}{matiere} {situer()} ({profil[nom]:.0f}/100)"

    if nom == "moyenne_generale":
        return f"moyenne générale {situer()} ({profil['moyenne_generale']:.1f}/100)"

    if nom == config.COLONNE_APTITUDE:
        valeur = int(profil[config.COLONNE_APTITUDE])
        return f"aptitude logique {valeur}/5 ({DESCRIPTION_APTITUDE.get(valeur, '')})"

    if nom == config.COLONNE_MOTIVATION:
        return f"niveau de motivation « {profil[config.COLONNE_MOTIVATION]} »"

    if nom.startswith(config.COLONNE_CATEGORIELLE + "_"):
        return f"centre d'intérêt « {nom.split('_', 2)[-1]} »"

    if nom.startswith(config.COLONNE_BINAIRE + "_"):
        reponse = nom.split("_")[-1]
        return (
            "intérêt déclaré pour l'informatique" if reponse == "Oui"
            else f"intérêt pour l'informatique : {reponse.lower()}"
        )

    return nom


# ==========================================================================
# Contributions locales
# ==========================================================================
def contributions_locales(
    modele, profil: dict, serie: str, maximum: int = 5
) -> list[tuple[str, float]]:
    """Éléments du profil qui poussent le plus vers ``serie``.

    Renvoie une liste de couples (libellé lisible, poids), du plus
    influent au moins influent. Liste vide si le modèle n'expose pas de
    coefficients — l'explication reste facultative, jamais bloquante.
    """
    try:
        classifieur = modele.named_steps["classifieur"]
        if not hasattr(classifieur, "coef_"):
            return []

        preparation = modele.named_steps["preparation"]
        nettoyage = modele.named_steps["nettoyage"]

        donnees = construire_dataframe_eleve(profil)
        encode = preparation.transform(nettoyage.transform(donnees))[0]
        noms = list(preparation.get_feature_names_out())

        classes = [str(classe) for classe in classifieur.classes_]
        if serie not in classes:
            return []
        coefficients = classifieur.coef_[classes.index(serie)]

        contributions = coefficients * encode
        ordre = np.argsort(contributions)[::-1]

        resultat = []
        for index in ordre:
            if contributions[index] <= 0.05:
                break
            resultat.append((
                libelle_variable(noms[index], profil, encode[index]),
                float(contributions[index]),
            ))
            if len(resultat) == maximum:
                break
        return resultat
    except Exception:  # pragma: no cover - l'explication reste facultative
        return []


def explication_de_repli(profil: dict, serie: str) -> str:
    """Explication descriptive quand le modèle n'est pas linéaire."""
    notes = {matiere: profil[matiere] for matiere in config.COLONNES_NOTES}
    meilleures = sorted(notes, key=notes.get, reverse=True)[:2]

    elements = [
        "points forts : "
        + " et ".join(
            f"{LIBELLES_MATIERES[m].lower()} ({notes[m]:.0f})" for m in meilleures
        )
    ]
    if profil.get(config.COLONNE_CATEGORIELLE):
        elements.append(f"centre d'intérêt « {profil[config.COLONNE_CATEGORIELLE]} »")

    valeur = int(profil[config.COLONNE_APTITUDE])
    elements.append(
        f"aptitude logique {valeur}/5 ({DESCRIPTION_APTITUDE.get(valeur, '')})"
    )
    return f"Profil rapproché de la série {serie} — " + " ; ".join(elements) + "."


# ==========================================================================
# Confiance
# ==========================================================================
def analyser_confiance(probabilites: dict) -> dict | None:
    """Compare les deux séries les mieux classées.

    Renvoie un dictionnaire décrivant l'écart, ou ``None`` si le modèle ne
    fournit pas de probabilités.
    """
    if not probabilites or len(probabilites) < 2:
        return None

    classement = sorted(probabilites.items(), key=lambda couple: couple[1], reverse=True)
    (premiere, valeur_premiere), (seconde, valeur_seconde) = classement[0], classement[1]
    ecart = valeur_premiere - valeur_seconde

    if ecart > 0.35:
        niveau, adjectif = "nette", "nette"
    elif ecart > 0.15:
        niveau, adjectif = "assez_nette", "assez nette"
    else:
        niveau, adjectif = "peu_tranchee", "peu tranchée"

    return {
        "niveau": niveau,
        "adjectif": adjectif,
        "premiere": premiere,
        "probabilite_premiere": valeur_premiere,
        "seconde": seconde,
        "probabilite_seconde": valeur_seconde,
        "ecart": ecart,
        "hesitation": ecart <= 0.15,
    }
