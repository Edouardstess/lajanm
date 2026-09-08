"""Moteur conversationnel de l'assistant d'orientation.

Ce module transforme le classifieur en assistant qui pose ses questions une
par une, en langage naturel, plutôt que d'imposer un formulaire.

**Ce que c'est.** Un automate à états doublé d'une analyse de texte à base de
règles. Il sait extraire un nombre d'une phrase, reconnaître une modalité
écrite librement, encaisser « je ne sais pas », corriger une réponse
précédente et répondre à quelques intentions (pourquoi, et si, recommencer).

**Ce que ce n'est pas.** Un modèle de langage. Il ne comprend que ce pour quoi
il a été programmé, et ne sait parler que d'orientation scolaire. Ce choix est
délibéré : l'énoncé du projet impose un fonctionnement local, sans API ni
connexion Internet. Aucun appel réseau n'est effectué.

Le moteur est **sans état** : la conversation est passée à chaque appel et
renvoyée mise à jour. Le service web n'a donc aucune session à conserver.
"""

from __future__ import annotations

import re
import sys
import unicodedata
from pathlib import Path

RACINE_PROJET = Path(__file__).resolve().parent.parent
if str(RACINE_PROJET) not in sys.path:
    sys.path.insert(0, str(RACINE_PROJET))

import config  # noqa: E402
import explication  # noqa: E402
from preprocessing import calculer_moyenne, construire_dataframe_eleve  # noqa: E402


# ==========================================================================
# Normalisation du texte
# ==========================================================================
def normaliser(texte: str) -> str:
    """Minuscules, sans accents, espaces réduits — pour comparer des mots."""
    texte = unicodedata.normalize("NFD", str(texte or ""))
    texte = "".join(c for c in texte if unicodedata.category(c) != "Mn")
    return re.sub(r"\s+", " ", texte.lower()).strip()


# ==========================================================================
# Description des champs à collecter
# ==========================================================================
SYNONYMES_MATIERES = {
    "math": ["math", "maths", "mathematique", "mathematiques", "algebre"],
    "physique": ["physique", "phys", "physiques"],
    "svt": ["svt", "science de la vie", "sciences de la vie", "biologie", "bio"],
    "francais": ["francais", "francai", "fr", "lettres"],
    "histoire": ["histoire", "histo", "hist", "geographie", "histoire geo"],
}

MOTS_CENTRE_INTERET = [
    # L'ordre compte : les libellés les plus spécifiques sont testés en premier,
    # sans quoi « sciences de la vie » serait capté par « sciences ».
    ("Santé et environnement", ["sante", "environnement", "medecine", "medical",
                                "infirmier", "ecologie", "sciences de la vie",
                                "science de la vie", "biologie", "bio"]),
    ("Économie et société", ["economie", "eco", "societe", "social", "commerce",
                             "gestion", "finance", "comptabilite", "affaires"]),
    ("Lettres et langues", ["lettres", "langue", "langues", "litterature",
                            "traduction", "anglais", "espagnol"]),
    ("Arts et culture", ["art", "arts", "culture", "musique", "dessin",
                         "peinture", "theatre", "danse", "artistique"]),
    ("Informatique", ["informatique", "info", "ordinateur", "programmation",
                      "code", "coder", "developpeur", "logiciel", "reseau"]),
    ("Sciences et technologie", ["science", "sciences", "technologie", "techno",
                                 "ingenierie", "ingenieur", "technique"]),
]

MOTS_MOTIVATION = [
    ("Très élevée", ["tres elevee", "tres eleve", "tres haute", "tres motive",
                     "tres motivee", "enorme", "maximale", "a fond", "tres fort"]),
    ("Élevée", ["elevee", "eleve", "haute", "haut", "motive", "motivee", "fort",
                "bonne", "bien", "grande"]),
    ("Moyenne", ["moyenne", "moyen", "normale", "normal", "correcte", "moyennement"]),
    ("Faible", ["faible", "basse", "bas", "peu", "pas trop", "pas beaucoup",
                "demotive", "demotivee"]),
]

MOTS_OUI = ["oui", "ouais", "ouep", "yep", "yes", "si", "bien sur", "carrement",
            "tout a fait", "absolument", "o", "j'aime", "beaucoup"]
MOTS_NON = ["non", "nan", "no", "nope", "pas vraiment", "pas du tout", "n",
            "jamais", "pas trop"]

MOTS_PASSER = ["je ne sais pas", "je sais pas", "sais pas", "aucune idee",
               "aucune idée", "passer", "passe", "skip", "suivant", "sans avis",
               "je prefere ne pas", "ne sais pas", "aucun", "je sais pa", "nsp"]

DESCRIPTION_APTITUDE_MOTS = [
    (5, ["tres bonne", "tres bon", "excellente", "excellent", "tres forte"]),
    (4, ["bonne", "bon", "forte", "fort", "bien"]),
    (3, ["moyenne", "moyen", "normale", "normal", "correcte"]),
    (2, ["faible", "basse", "bas", "pas terrible"]),
    (1, ["tres faible", "tres basse", "nulle", "tres mauvaise"]),
]


CHAMPS = [
    {
        "cle": "math",
        "type": "note",
        "question": "Commençons par les notes. Quelle est ta note en **mathématiques**, sur 100 ?",
        "relance": "Donne-moi un nombre entre 0 et 100 pour les mathématiques (par exemple « 78 »).",
        "nom": "mathématiques",
    },
    {
        "cle": "physique",
        "type": "note",
        "question": "Et en **physique** ?",
        "relance": "Un nombre entre 0 et 100 pour la physique, s'il te plaît.",
        "nom": "physique",
    },
    {
        "cle": "svt",
        "type": "note",
        "question": "Ta note en **SVT** ?",
        "relance": "Un nombre entre 0 et 100 pour la SVT.",
        "nom": "SVT",
    },
    {
        "cle": "francais",
        "type": "note",
        "question": "Ta note en **français** ?",
        "relance": "Un nombre entre 0 et 100 pour le français.",
        "nom": "français",
    },
    {
        "cle": "histoire",
        "type": "note",
        "question": "Dernière note : **histoire** ?",
        "relance": "Un nombre entre 0 et 100 pour l'histoire.",
        "nom": "histoire",
    },
    {
        "cle": "aptitude_logique",
        "type": "aptitude",
        "question": (
            "Merci. Maintenant, comment évaluerais-tu ton **aptitude au "
            "raisonnement logique**, de 1 à 5 ?"
        ),
        "relance": "Réponds par un chiffre de 1 à 5, ou par « faible », « moyenne », « bonne »…",
        "nom": "aptitude logique",
        "suggestions": ["1", "2", "3", "4", "5"],
        # Auto-évaluation : un élève peut légitimement ne pas savoir se situer.
        # Le pipeline impute alors par la médiane.
        "facultatif": True,
    },
    {
        "cle": "niveau_motivation",
        "type": "motivation",
        "question": "Quel est ton **niveau de motivation** pour les études ?",
        "relance": "Choisis : faible, moyenne, élevée ou très élevée.",
        "nom": "niveau de motivation",
        "suggestions": ["Faible", "Moyenne", "Élevée", "Très élevée"],
        "facultatif": True,
    },
    {
        "cle": "centre_interet",
        "type": "centre_interet",
        "question": (
            "Quel domaine t'intéresse le plus ?\n\n"
            "C'est la question la plus importante : c'est de loin ce qui pèse le "
            "plus dans la recommandation."
        ),
        "relance": (
            "Dis-moi un domaine parmi : arts et culture, lettres et langues, "
            "économie et société, santé et environnement, informatique, "
            "sciences et technologie."
        ),
        "nom": "centre d'intérêt",
        "suggestions": config.MODALITES_CENTRE_INTERET,
        "facultatif": True,
    },
    {
        "cle": "interesse_par_informatique",
        "type": "oui_non",
        "question": "Dernière question : **l'informatique t'intéresse-t-elle** ?",
        "relance": "Réponds simplement par oui ou non.",
        "nom": "intérêt pour l'informatique",
        "suggestions": ["Oui", "Non"],
        "facultatif": True,
    },
]

CHAMPS_PAR_CLE = {champ["cle"]: champ for champ in CHAMPS}


MESSAGE_ACCUEIL = (
    "Bonjour 👋 Je suis l'assistant d'orientation du Nouveau Secondaire.\n\n"
    "Je vais te poser quelques questions sur tes notes et tes centres d'intérêt, "
    "puis je te dirai quelle série — **SMP**, **SVT**, **SES** ou **LLA** — "
    "correspond le mieux à ton profil.\n\n"
    "Tu peux répondre librement (« j'ai eu 85 en maths »), dire « je ne sais pas » "
    "pour passer une question, ou revenir sur une réponse à tout moment."
)

MESSAGE_AIDE = (
    "Voici ce que je comprends :\n\n"
    "• **Une note** — « 85 », « 85/100 », « j'ai eu 15/20 » (je convertis sur 100)\n"
    "• **Plusieurs d'un coup** — « maths 85, physique 70 »\n"
    "• **Une correction** — « en fait maths c'est 90 »\n"
    "• **Passer** — « je ne sais pas », « passer »\n"
    "• **pourquoi** — le détail de ma recommandation\n"
    "• **et si…** — « et si j'avais 90 en maths ? »\n"
    "• **recommencer** — on repart de zéro\n\n"
    "Je ne sais parler que d'orientation scolaire : je ne suis pas un assistant "
    "généraliste."
)

MESSAGE_INCOMPRIS = (
    "Je n'ai pas compris. Je suis un assistant spécialisé dans l'orientation "
    "scolaire, avec un vocabulaire limité — tape **aide** pour voir ce que je "
    "sais faire."
)


# ==========================================================================
# Extraction d'information
# ==========================================================================
def extraire_nombre(texte: str) -> float | None:
    """Extrait une note d'une phrase libre.

    Gère « 85 », « 85/100 », « 15/20 » (converti sur 100), « 85 % »,
    « j'ai eu 85 », « 85,5 ».
    """
    normalise = normaliser(texte).replace(",", ".")

    # Forme « x sur y » ou « x/y » : on convertit si le barème n'est pas 100.
    fraction = re.search(r"(\d+(?:\.\d+)?)\s*(?:/|sur)\s*(\d+(?:\.\d+)?)", normalise)
    if fraction:
        valeur, bareme = float(fraction.group(1)), float(fraction.group(2))
        if bareme > 0:
            return round(valeur / bareme * 100, 2)

    nombres = re.findall(r"\d+(?:\.\d+)?", normalise)
    if not nombres:
        return None
    return float(nombres[0])


def est_passer(texte: str) -> bool:
    normalise = normaliser(texte)
    return any(mot in normalise for mot in MOTS_PASSER)


def apparier(texte: str, table: list[tuple]) -> object | None:
    """Cherche la première modalité dont un mot-clé apparaît dans le texte."""
    normalise = normaliser(texte)
    for valeur, mots in table:
        for mot in mots:
            if re.search(rf"\b{re.escape(mot)}\b", normalise):
                return valeur
    return None


def interpreter_oui_non(texte: str) -> str | None:
    normalise = normaliser(texte)
    for mot in MOTS_NON:
        if re.search(rf"\b{re.escape(mot)}\b", normalise):
            return "Non"
    for mot in MOTS_OUI:
        if re.search(rf"\b{re.escape(mot)}\b", normalise):
            return "Oui"
    return None


def interpreter_champ(champ: dict, texte: str):
    """Convertit une réponse libre en valeur exploitable.

    Renvoie ``(valeur, erreur)``. ``valeur`` vaut ``None`` avec une erreur
    quand la réponse est inutilisable, et ``None`` sans erreur quand
    l'élève a explicitement choisi de passer.
    """
    type_champ = champ["type"]

    if type_champ == "note":
        valeur = extraire_nombre(texte)
        if valeur is None:
            return None, champ["relance"]
        if not (config.NOTE_MIN <= valeur <= config.NOTE_MAX):
            return None, (
                f"« {valeur:g} » ne peut pas être une note sur 100. "
                f"Donne-moi un nombre entre 0 et 100."
            )
        return valeur, None

    if type_champ == "aptitude":
        mot = apparier(texte, DESCRIPTION_APTITUDE_MOTS)
        if mot is not None:
            return float(mot), None
        valeur = extraire_nombre(texte)
        if valeur is None:
            return None, champ["relance"]
        if not (config.APTITUDE_MIN <= valeur <= config.APTITUDE_MAX):
            return None, "L'aptitude logique se note de 1 à 5."
        return float(round(valeur)), None

    if type_champ == "motivation":
        valeur = apparier(texte, MOTS_MOTIVATION)
        if valeur is not None:
            return valeur, None
        nombre = extraire_nombre(texte)
        if nombre is not None and 1 <= nombre <= 4:
            return config.ORDRE_MOTIVATION[int(nombre) - 1], None
        return None, champ["relance"]

    if type_champ == "centre_interet":
        valeur = apparier(texte, MOTS_CENTRE_INTERET)
        if valeur is not None:
            return valeur, None
        nombre = extraire_nombre(texte)
        if nombre is not None and 1 <= nombre <= len(config.MODALITES_CENTRE_INTERET):
            return config.MODALITES_CENTRE_INTERET[int(nombre) - 1], None
        return None, champ["relance"]

    if type_champ == "oui_non":
        valeur = interpreter_oui_non(texte)
        if valeur is not None:
            return valeur, None
        return None, champ["relance"]

    return None, champ["relance"]


# Marques qui séparent deux couples « matière + note » dans une même phrase.
SEPARATEUR_DE_GROUPE = re.compile(r"\bet\b|\bpuis\b|[,;.]")

# Une note, éventuellement accompagnée de son barème : « 85 », « 15/20 »,
# « 15 sur 20 ». Le barème est capturé pour pouvoir ramener la note sur 100.
MOTIF_NOTE = r"(?P<valeur>\d+(?:\.\d+)?)(?:\s*(?:/|sur)\s*(?P<bareme>\d+(?:\.\d+)?))?"


def _valeur_sur_cent(correspondance) -> float:
    """Convertit une note sur 100 en tenant compte du barème indiqué."""
    valeur = float(correspondance.group("valeur"))
    bareme = correspondance.groupdict().get("bareme")
    if bareme and float(bareme) > 0 and float(bareme) != 100:
        return round(valeur / float(bareme) * 100, 2)
    return valeur


def extraire_paires(texte: str) -> dict:
    """Repère les couples « matière + note » présents dans une même phrase.

    Permet de répondre « maths 85, physique 70 et svt 64 » d'un seul coup,
    ou de corriger « en fait maths c'est 90 ».
    """
    # Seule la virgule décimale est convertie : les autres virgules séparent
    # deux couples et doivent rester visibles comme frontières.
    normalise = re.sub(r"(\d),(\d)", r"\1.\2", normaliser(texte))
    trouvees = {}

    for cle, synonymes in SYNONYMES_MATIERES.items():
        for synonyme in sorted(synonymes, key=len, reverse=True):
            mot = re.escape(synonyme)
            # Les deux ordres se rencontrent aussi naturellement l'un que
            # l'autre : « maths 85 » et « j'ai eu 85 en maths ».
            motifs = (
                rf"\b{mot}\b(?P<sep>\D{{0,18}}?){MOTIF_NOTE}",
                rf"{MOTIF_NOTE}(?P<sep>\D{{0,18}}?)\b{mot}\b",
            )

            valeur = None
            for motif in motifs:
                correspondance = re.search(motif, normalise)
                if correspondance is None:
                    continue
                # Une conjonction ou une ponctuation entre la matière et le
                # nombre signale qu'ils appartiennent à deux groupes
                # différents : dans « 40 en maths et 95 en français », le 95
                # est la note de français, pas celle de maths.
                if SEPARATEUR_DE_GROUPE.search(correspondance.group("sep")):
                    continue
                valeur = _valeur_sur_cent(correspondance)
                break

            if valeur is not None:
                if config.NOTE_MIN <= valeur <= config.NOTE_MAX:
                    trouvees[cle] = valeur
                break

    return trouvees


def detecter_commande(texte: str) -> str | None:
    normalise = normaliser(texte)

    if re.search(r"\b(recommencer|recommence|reset|nouveau|nouvelle|redemarrer|"
                 r"efface|remise a zero)\b", normalise):
        return "recommencer"
    if re.search(r"\b(aide|help|commandes|que sais tu|que peux tu)\b", normalise):
        return "aide"
    if re.search(r"\b(pourquoi|explique|explication|comment ca|justifie|"
                 r"sur quoi|detail)\b", normalise):
        return "pourquoi"
    if re.search(r"\b(resume|recapitulatif|recap|mon profil|profil)\b", normalise):
        return "profil"
    if re.search(r"^\s*(et si|si j|si je|imagine|suppose)", normalise):
        return "hypothese"
    return None


def detecter_modification(texte: str) -> dict:
    """Champs modifiés par une phrase du type « et si j'avais 90 en maths ».

    Couvre les notes, l'aptitude logique et le centre d'intérêt.
    """
    modifications = dict(extraire_paires(texte))
    normalise = normaliser(texte)

    if re.search(r"\b(aptitude|logique|raisonnement)\b", normalise):
        nombre = extraire_nombre(texte)
        if nombre is not None and config.APTITUDE_MIN <= nombre <= config.APTITUDE_MAX:
            modifications["aptitude_logique"] = float(round(nombre))

    centre = apparier(texte, MOTS_CENTRE_INTERET)
    if centre is not None:
        modifications["centre_interet"] = centre

    motivation = apparier(texte, MOTS_MOTIVATION)
    if motivation is not None and re.search(r"\bmotiv", normalise):
        modifications["niveau_motivation"] = motivation

    return modifications


# ==========================================================================
# État de la conversation
# ==========================================================================
def etat_initial() -> dict:
    return {"profil": {}, "etape": "accueil", "resultat": None}


def champ_suivant(profil: dict) -> dict | None:
    """Premier champ encore non renseigné, dans l'ordre défini."""
    for champ in CHAMPS:
        if champ["cle"] not in profil:
            return champ
    return None


def profil_complet(profil: dict) -> bool:
    return champ_suivant(profil) is None


def profil_pour_modele(profil: dict) -> dict:
    """Complète le profil avec les valeurs nulles et la moyenne calculée."""
    complet = {cle: profil.get(cle) for cle in config.COLONNES_FEATURES}
    complet["moyenne_generale"] = calculer_moyenne(profil)
    return complet


# ==========================================================================
# Prédiction et mise en forme
# ==========================================================================
def predire(modele, profil: dict) -> dict:
    donnees_profil = profil_pour_modele(profil)
    donnees = construire_dataframe_eleve(donnees_profil)

    serie = str(modele.predict(donnees)[0])
    probabilites = {}
    if hasattr(modele, "predict_proba"):
        valeurs = modele.predict_proba(donnees)[0]
        probabilites = {
            str(classe): round(float(valeur), 4)
            for classe, valeur in zip(modele.classes_, valeurs)
        }

    contributions = explication.contributions_locales(modele, donnees_profil, serie)
    confiance = explication.analyser_confiance(probabilites)

    avertissements = []
    if profil.get(config.COLONNE_CATEGORIELLE) is None:
        avertissements.append(
            "Tu n'as pas indiqué de centre d'intérêt. C'est la variable la plus "
            "influente : ma recommandation est nettement moins fiable."
        )
    if profil.get(config.COLONNE_MOTIVATION) is None:
        avertissements.append("Le niveau de motivation n'a pas été renseigné.")
    if profil.get(config.COLONNE_BINAIRE) is None:
        avertissements.append("L'intérêt pour l'informatique n'a pas été renseigné.")

    notes = [profil[matiere] for matiere in config.COLONNES_NOTES]
    if len(set(notes)) == 1:
        avertissements.append(
            "Tes cinq notes sont identiques : je n'ai aucun contraste entre les "
            "matières pour te départager."
        )

    return {
        "serie": serie,
        "libelle": config.LIBELLES_SERIES.get(serie, ""),
        "probabilites": probabilites,
        "moyenne_generale": donnees_profil["moyenne_generale"],
        "contributions": [
            {"libelle": libelle, "poids": round(poids, 3)}
            for libelle, poids in contributions
        ],
        "confiance": confiance,
        "avertissements": avertissements,
    }


def texte_resume_profil(profil: dict) -> str:
    lignes = ["Voici ce que j'ai retenu :\n"]
    for matiere in config.COLONNES_NOTES:
        valeur = profil.get(matiere)
        nom = explication.LIBELLES_MATIERES[matiere]
        lignes.append(f"• {nom} : **{valeur:g}/100**" if valeur is not None
                      else f"• {nom} : non renseignée")

    moyenne = calculer_moyenne(profil)
    lignes.append(f"• Moyenne générale : **{moyenne:.2f}/100**")

    aptitude = profil.get(config.COLONNE_APTITUDE)
    lignes.append(
        f"• Aptitude logique : **{aptitude:g}/5** "
        f"({explication.DESCRIPTION_APTITUDE.get(int(aptitude), '')})"
        if aptitude is not None else "• Aptitude logique : non renseignée"
    )
    for cle, nom, absent in (
        (config.COLONNE_MOTIVATION, "Motivation", "non renseignée"),
        (config.COLONNE_CATEGORIELLE, "Centre d'intérêt", "non renseigné"),
        (config.COLONNE_BINAIRE, "Intérêt pour l'informatique", "non renseigné"),
    ):
        valeur = profil.get(cle)
        lignes.append(f"• {nom} : **{valeur}**" if valeur else f"• {nom} : {absent}")
    return "\n".join(lignes)


def texte_explication(resultat: dict) -> str:
    parties = []
    confiance = resultat.get("confiance")
    if confiance:
        parties.append(
            f"Ma recommandation est **{confiance['adjectif']}** : "
            f"{confiance['premiere']} obtient "
            f"{confiance['probabilite_premiere'] * 100:.1f} %, devant "
            f"{confiance['seconde']} à "
            f"{confiance['probabilite_seconde'] * 100:.1f} % "
            f"(écart de {confiance['ecart'] * 100:.1f} points)."
        )
        if confiance["hesitation"]:
            parties.append(
                "L'écart est faible : les deux séries méritent d'être examinées "
                "avec ton conseiller."
            )

    contributions = resultat.get("contributions") or []
    if contributions:
        details = "\n".join(
            f"• {c['libelle']} _(poids {c['poids']:+.2f})_" for c in contributions
        )
        parties.append(
            f"Ce qui pousse le plus vers **{resultat['serie']}** dans ton profil :\n\n"
            + details
        )

    parties.append(
        "_Ces poids viennent des coefficients du modèle appliqués à tes propres "
        "valeurs. Ils décrivent un calcul statistique, pas un jugement sur toi._"
    )
    return "\n\n".join(parties)


# ==========================================================================
# Moteur
# ==========================================================================
def _question(champ: dict, prefixe: str = "") -> dict:
    contenu = champ["question"]
    if prefixe:
        contenu = f"{prefixe}\n\n{contenu}"
    return {"type": "texte", "contenu": contenu}


def _suggestions_pour(champ: dict | None) -> list[str]:
    if champ is None:
        return ["Pourquoi ?", "Et si j'avais 90 en maths ?", "Recommencer"]
    if champ.get("suggestions"):
        suggestions = list(champ["suggestions"])
        if champ.get("facultatif"):
            suggestions.append("Je ne sais pas")
        return suggestions
    return []


def _messages_resultat(resultat: dict) -> list[dict]:
    """Message principal + carte de résultat + avertissements éventuels."""
    messages = [{
        "type": "resultat",
        "contenu": (
            f"D'après ton profil, la série qui te correspond le mieux est "
            f"**{resultat['serie']}** — {resultat['libelle']}."
        ),
        "serie": resultat["serie"],
        "libelle": resultat["libelle"],
        "probabilites": resultat["probabilites"],
        "moyenne_generale": resultat["moyenne_generale"],
    }]

    if resultat["avertissements"]:
        messages.append({
            "type": "avertissement",
            "contenu": "\n".join(f"• {a}" for a in resultat["avertissements"]),
        })

    messages.append({
        "type": "texte",
        "contenu": (
            "Tu peux me demander **pourquoi**, tester une hypothèse "
            "(« et si j'avais 90 en maths ? ») ou **recommencer**.\n\n"
            "_Je suis un outil d'aide à la décision, entraîné sur des données "
            "simulées. Parles-en avec ton conseiller d'orientation._"
        ),
    })
    return messages


def repondre(message: str | None, etat: dict | None, modele) -> dict:
    """Traite un tour de conversation.

    Renvoie ``{"messages": [...], "etat": {...}, "suggestions": [...]}``.
    """
    etat = dict(etat) if etat else etat_initial()
    etat.setdefault("profil", {})
    etat.setdefault("resultat", None)
    profil = dict(etat["profil"])

    # --- Ouverture de la conversation -----------------------------------
    if message is None or not str(message).strip():
        if not profil:
            champ = CHAMPS[0]
            etat["profil"] = profil
            return {
                "messages": [
                    {"type": "texte", "contenu": MESSAGE_ACCUEIL},
                    _question(champ),
                ],
                "etat": etat,
                "suggestions": _suggestions_pour(champ),
            }
        return _relancer(etat, profil)

    texte = str(message).strip()
    commande = detecter_commande(texte)

    # --- Commandes ------------------------------------------------------
    if commande == "recommencer":
        nouvel_etat = etat_initial()
        champ = CHAMPS[0]
        return {
            "messages": [
                {"type": "texte", "contenu": "D'accord, on repart de zéro."},
                _question(champ),
            ],
            "etat": nouvel_etat,
            "suggestions": _suggestions_pour(champ),
        }

    if commande == "aide":
        return _avec_contexte(etat, profil, [{"type": "texte", "contenu": MESSAGE_AIDE}])

    if commande == "profil":
        return _avec_contexte(
            etat, profil,
            [{"type": "texte", "contenu": texte_resume_profil(profil)}]
            if profil else
            [{"type": "texte", "contenu": "Tu ne m'as encore rien dit sur ton profil."}],
        )

    if commande == "pourquoi":
        if etat.get("resultat"):
            return _avec_contexte(
                etat, profil,
                [{"type": "texte", "contenu": texte_explication(etat["resultat"])}],
            )
        return _avec_contexte(
            etat, profil,
            [{"type": "texte", "contenu":
              "Je n'ai pas encore fait de recommandation — finissons d'abord "
              "les questions."}],
        )

    # --- Hypothèse « et si… » -------------------------------------------
    if commande == "hypothese":
        return _traiter_hypothese(texte, etat, profil, modele)

    champ = champ_suivant(profil)

    # --- Corrections et réponses multiples ------------------------------
    paires = extraire_paires(texte)
    if paires:
        # Une phrase contenant explicitement des couples « matière + note »
        # est traitée comme telle, même si elle répond aussi à la question
        # en cours : « en fait maths c'est 90 » corrige, sans rien casser.
        anciennes = {cle: profil.get(cle) for cle in paires}
        profil.update(paires)
        etat["profil"] = profil

        corrigees = [cle for cle, valeur in anciennes.items() if valeur is not None]
        nouvelles = [cle for cle, valeur in anciennes.items() if valeur is None]

        fragments = []
        if nouvelles:
            fragments.append(
                "J'ai noté " + ", ".join(
                    f"**{explication.LIBELLES_MATIERES[c].lower()} {paires[c]:g}**"
                    for c in nouvelles
                )
            )
        if corrigees:
            fragments.append(
                "J'ai corrigé " + ", ".join(
                    f"**{explication.LIBELLES_MATIERES[c].lower()} : "
                    f"{anciennes[c]:g} → {paires[c]:g}**" for c in corrigees
                )
            )
        accuse = ". ".join(fragments) + "."

        if profil_complet(profil):
            return _conclure(etat, profil, modele, prefixe=accuse)

        suivant = champ_suivant(profil)
        return {
            "messages": [_question(suivant, prefixe=accuse)],
            "etat": etat,
            "suggestions": _suggestions_pour(suivant),
        }

    # --- Réponse à la question en cours ---------------------------------
    if champ is None:
        # Le profil est complet : on reste sur le résultat.
        return _avec_contexte(
            etat, profil,
            [{"type": "texte", "contenu": MESSAGE_INCOMPRIS}],
        )

    if est_passer(texte):
        if not champ.get("facultatif"):
            return {
                "messages": [{
                    "type": "texte",
                    "contenu": (
                        f"J'ai besoin de ta note en {champ['nom']} pour continuer — "
                        "c'est la base du calcul. Une estimation suffit."
                    ),
                }],
                "etat": etat,
                "suggestions": _suggestions_pour(champ),
            }
        profil[champ["cle"]] = None
        etat["profil"] = profil
        accuse = f"Très bien, je laisse **{champ['nom']}** de côté."
        if profil_complet(profil):
            return _conclure(etat, profil, modele, prefixe=accuse)
        suivant = champ_suivant(profil)
        return {
            "messages": [_question(suivant, prefixe=accuse)],
            "etat": etat,
            "suggestions": _suggestions_pour(suivant),
        }

    valeur, erreur = interpreter_champ(champ, texte)
    if erreur:
        return {
            "messages": [{"type": "texte", "contenu": erreur}],
            "etat": etat,
            "suggestions": _suggestions_pour(champ),
        }

    profil[champ["cle"]] = valeur
    etat["profil"] = profil

    if profil_complet(profil):
        return _conclure(etat, profil, modele)

    suivant = champ_suivant(profil)
    return {
        "messages": [_question(suivant)],
        "etat": etat,
        "suggestions": _suggestions_pour(suivant),
    }


def _conclure(etat: dict, profil: dict, modele, prefixe: str = "") -> dict:
    """Calcule et présente la recommandation."""
    if modele is None:
        return {
            "messages": [{
                "type": "texte",
                "contenu": "Le modèle n'est pas chargé, je ne peux pas conclure.",
            }],
            "etat": etat,
            "suggestions": [],
        }

    resultat = predire(modele, profil)
    etat["profil"] = profil
    etat["resultat"] = resultat
    etat["etape"] = "resultat"

    messages = []
    if prefixe:
        messages.append({"type": "texte", "contenu": prefixe})
    messages.append({"type": "texte", "contenu": texte_resume_profil(profil)})
    messages.extend(_messages_resultat(resultat))

    return {"messages": messages, "etat": etat, "suggestions": _suggestions_pour(None)}


def _traiter_hypothese(texte: str, etat: dict, profil: dict, modele) -> dict:
    """Rejoue la prédiction avec un ou plusieurs champs modifiés."""
    if not etat.get("resultat"):
        return _avec_contexte(
            etat, profil,
            [{"type": "texte", "contenu":
              "Finissons d'abord les questions : je pourrai ensuite tester des "
              "hypothèses avec toi."}],
        )

    modifications = detecter_modification(texte)
    if not modifications:
        return _avec_contexte(
            etat, profil,
            [{"type": "texte", "contenu":
              "Je n'ai pas saisi ce que tu voudrais changer. Essaie par exemple "
              "« et si j'avais 90 en maths ? » ou « et si je choisissais "
              "l'informatique ? »."}],
        )

    profil_hypothese = dict(profil)
    profil_hypothese.update(modifications)
    resultat = predire(modele, profil_hypothese)
    serie_actuelle = etat["resultat"]["serie"]

    changements = ", ".join(
        f"**{CHAMPS_PAR_CLE[cle]['nom'] if cle in CHAMPS_PAR_CLE else cle} → "
        f"{valeur:g}**" if isinstance(valeur, (int, float))
        else f"**{CHAMPS_PAR_CLE[cle]['nom'] if cle in CHAMPS_PAR_CLE else cle} → "
             f"{valeur}**"
        for cle, valeur in modifications.items()
    )

    if resultat["serie"] == serie_actuelle:
        verdict = (
            f"Avec {changements}, la recommandation **ne change pas** : "
            f"toujours {resultat['serie']}."
        )
        probabilite_avant = etat["resultat"]["probabilites"].get(serie_actuelle, 0)
        probabilite_apres = resultat["probabilites"].get(serie_actuelle, 0)
        ecart = (probabilite_apres - probabilite_avant) * 100
        if abs(ecart) >= 0.1:
            verdict += f" Sa probabilité passe de {probabilite_avant * 100:.1f} % à {probabilite_apres * 100:.1f} %."
    else:
        verdict = (
            f"Avec {changements}, la recommandation **change** : "
            f"{serie_actuelle} → **{resultat['serie']}** "
            f"({resultat['libelle']})."
        )

    messages = [
        {"type": "texte", "contenu": verdict},
        {
            "type": "resultat",
            "contenu": "Résultat de l'hypothèse :",
            "serie": resultat["serie"],
            "libelle": resultat["libelle"],
            "probabilites": resultat["probabilites"],
            "moyenne_generale": resultat["moyenne_generale"],
            "hypothese": True,
        },
        {"type": "texte", "contenu":
         "_C'est une simulation : ton profil enregistré n'a pas changé._"},
    ]
    # L'hypothèse ne modifie pas l'état : le profil réel est préservé.
    return {"messages": messages, "etat": etat, "suggestions": _suggestions_pour(None)}


def _relancer(etat: dict, profil: dict) -> dict:
    champ = champ_suivant(profil)
    if champ is None:
        return {"messages": [], "etat": etat, "suggestions": _suggestions_pour(None)}
    return {
        "messages": [_question(champ)],
        "etat": etat,
        "suggestions": _suggestions_pour(champ),
    }


def _avec_contexte(etat: dict, profil: dict, messages: list) -> dict:
    """Ajoute la relance de la question en cours après une réponse hors sujet."""
    champ = champ_suivant(profil)
    if champ is not None:
        messages = messages + [_question(champ)]
    return {"messages": messages, "etat": etat, "suggestions": _suggestions_pour(champ)}
