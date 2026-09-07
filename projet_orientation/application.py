"""Application PyQt d'aide à l'orientation scolaire.

Exécution :

    python application.py

L'application charge le modèle sérialisé par ``train.py`` et n'effectue
jamais de nouvel entraînement. Comme le fichier ``.joblib`` contient le
pipeline complet (nettoyage, imputations, encodages, normalisation,
classifieur), le prétraitement appliqué à un profil saisi est par
construction identique à celui de l'entraînement.

Avertissement : cet outil est une aide à la décision. Il ne constitue pas un
dispositif officiel d'orientation scolaire.
"""

from __future__ import annotations

import json
import random
import sys
from pathlib import Path

import joblib
import numpy as np

import config
from database import HistoriqueOrientations
from preprocessing import calculer_moyenne, construire_dataframe_eleve
from qt_compat import QAction, Qt, QtCore, QtGui, QtWidgets, VERSION_QT, executer


NON_RENSEIGNE = "— non renseigné —"

COULEURS_SERIES = {
    "SMP": "#4C78A8",
    "SVT": "#54A24B",
    "SES": "#E1A93B",
    "LLA": "#B279A2",
}

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

FEUILLE_DE_STYLE = """
QWidget            { font-size: 13px; }
QGroupBox          { font-weight: 600; border: 1px solid #C8CDD4; border-radius: 6px;
                     margin-top: 14px; padding-top: 10px; }
QGroupBox::title   { subcontrol-origin: margin; left: 10px; padding: 0 4px; color: #2C3E50; }
QPushButton        { padding: 7px 14px; border-radius: 5px; border: 1px solid #B9C0C8;
                     background: #F4F6F8; }
QPushButton:hover  { background: #E8ECF1; }
QPushButton#principal { background: #2D6CA2; color: white; border: 1px solid #255A88;
                        font-weight: 600; padding: 9px 18px; }
QPushButton#principal:hover    { background: #357BB8; }
QPushButton#principal:disabled { background: #A9B6C2; border-color: #A9B6C2; }
QLabel#titreResultat { font-size: 21px; font-weight: 700; color: #1B3A57; }
QLabel#sousTitre     { color: #5A6672; }
QPlainTextEdit, QTextEdit { font-family: "DejaVu Sans Mono", Consolas, monospace;
                            font-size: 12px; }
"""


# ==========================================================================
# Widget d'affichage graphique des probabilités (bonus)
# ==========================================================================
class BarresProbabilites(QtWidgets.QWidget):
    """Diagramme en barres horizontales dessiné directement avec QPainter.

    Écrit à la main plutôt qu'avec Matplotlib : l'application reste ainsi
    légère et ne dépend que de PyQt pour son affichage.
    """

    def __init__(self, parent=None):
        super().__init__(parent)
        self.probabilites: dict[str, float] = {}
        self.serie_recommandee: str | None = None
        self.setMinimumHeight(150)
        self.setSizePolicy(
            QtWidgets.QSizePolicy.Policy.Expanding,
            QtWidgets.QSizePolicy.Policy.Expanding,
        )

    def definir_probabilites(self, probabilites: dict, recommandee: str | None = None):
        self.probabilites = probabilites or {}
        self.serie_recommandee = recommandee
        self.update()

    def effacer(self):
        self.definir_probabilites({}, None)

    def paintEvent(self, evenement):  # noqa: N802 (nom imposé par Qt)
        peintre = QtGui.QPainter(self)
        peintre.setRenderHint(QtGui.QPainter.RenderHint.Antialiasing)

        if not self.probabilites:
            peintre.setPen(QtGui.QColor("#8A96A3"))
            peintre.drawText(
                self.rect(),
                Qt.AlignmentFlag.AlignCenter,
                "Les probabilités s'afficheront ici\naprès la prédiction.",
            )
            return

        series = sorted(self.probabilites, key=self.probabilites.get, reverse=True)
        marge_gauche, marge_droite = 52, 62
        hauteur_ligne = self.height() / max(len(series), 1)
        hauteur_barre = min(26, hauteur_ligne * 0.55)
        largeur_max = max(self.width() - marge_gauche - marge_droite, 10)

        police_serie = peintre.font()
        police_serie.setBold(True)

        for index, serie in enumerate(series):
            probabilite = float(self.probabilites[serie])
            y = index * hauteur_ligne + (hauteur_ligne - hauteur_barre) / 2
            est_recommandee = serie == self.serie_recommandee

            # Nom de la série.
            police_serie.setPointSizeF(max(9.0, police_serie.pointSizeF()))
            peintre.setFont(police_serie)
            peintre.setPen(QtGui.QColor("#1B3A57" if est_recommandee else "#5A6672"))
            peintre.drawText(
                QtCore.QRectF(0, y, marge_gauche - 8, hauteur_barre),
                Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter,
                serie,
            )

            # Fond de la barre.
            fond = QtCore.QRectF(marge_gauche, y, largeur_max, hauteur_barre)
            peintre.setPen(Qt.PenStyle.NoPen)
            peintre.setBrush(QtGui.QColor("#ECEFF3"))
            peintre.drawRoundedRect(fond, 4, 4)

            # Barre remplie.
            couleur = QtGui.QColor(COULEURS_SERIES.get(serie, "#4C78A8"))
            if not est_recommandee:
                couleur.setAlpha(150)
            peintre.setBrush(couleur)
            largeur = max(largeur_max * probabilite, 2.0)
            peintre.drawRoundedRect(QtCore.QRectF(marge_gauche, y, largeur, hauteur_barre), 4, 4)

            # Pourcentage.
            police_valeur = peintre.font()
            police_valeur.setBold(est_recommandee)
            peintre.setFont(police_valeur)
            peintre.setPen(QtGui.QColor("#1B3A57" if est_recommandee else "#5A6672"))
            peintre.drawText(
                QtCore.QRectF(marge_gauche + largeur_max + 8, y, marge_droite - 12, hauteur_barre),
                Qt.AlignmentFlag.AlignLeft | Qt.AlignmentFlag.AlignVCenter,
                f"{probabilite * 100:.1f} %",
            )

        peintre.end()


# ==========================================================================
# Fenêtre principale
# ==========================================================================
class FenetreOrientation(QtWidgets.QMainWindow):

    def __init__(self):
        super().__init__()
        self.setWindowTitle("Orientation scolaire — Nouveau Secondaire (aide à la décision)")
        self.resize(1120, 760)

        self.modele = None
        self.metadonnees = {}
        self.historique = HistoriqueOrientations()
        self.derniere_prediction = None

        self._charger_modele()
        self._construire_interface()
        self._construire_menu()
        self.reinitialiser()

    # ------------------------------------------------------------------
    # Modèle
    # ------------------------------------------------------------------
    def _charger_modele(self) -> None:
        if not config.CHEMIN_MODELE.exists():
            QtWidgets.QMessageBox.critical(
                self,
                "Modèle introuvable",
                f"Le fichier {config.CHEMIN_MODELE.name} est absent du dossier "
                f"« {config.DOSSIER_MODEL.name} ».\n\n"
                "Entraînez d'abord le modèle :\n\n    python train.py",
            )
            return

        try:
            self.modele = joblib.load(config.CHEMIN_MODELE)
        except Exception as erreur:  # pragma: no cover - dépend de l'environnement
            QtWidgets.QMessageBox.critical(
                self, "Chargement impossible",
                f"Le modèle n'a pas pu être chargé :\n\n{erreur}",
            )
            return

        if config.CHEMIN_METADONNEES.exists():
            try:
                self.metadonnees = json.loads(
                    config.CHEMIN_METADONNEES.read_text(encoding="utf-8")
                )
            except json.JSONDecodeError:
                self.metadonnees = {}

    @property
    def nom_modele(self) -> str:
        return self.metadonnees.get("modele_retenu", type(
            self.modele.named_steps["classifieur"]
        ).__name__ if self.modele else "—")

    # ------------------------------------------------------------------
    # Construction de l'interface
    # ------------------------------------------------------------------
    def _construire_interface(self) -> None:
        self.onglets = QtWidgets.QTabWidget()
        self.onglets.addTab(self._onglet_orientation(), "Orientation")
        self.onglets.addTab(self._onglet_historique(), "Historique")
        self.onglets.addTab(self._onglet_modele(), "Modèle")
        self.setCentralWidget(self.onglets)

        self._actualiser_barre_etat()

    def _actualiser_barre_etat(self) -> None:
        """Message permanent de la barre d'état, reflétant l'état courant."""
        self.statusBar().showMessage(
            f"Modèle : {self.nom_modele}   |   {VERSION_QT}   |   "
            f"{self.historique.compter()} orientation(s) enregistrée(s)"
        )

    # -- Onglet 1 : saisie et résultat ----------------------------------
    def _onglet_orientation(self) -> QtWidgets.QWidget:
        page = QtWidgets.QWidget()
        disposition = QtWidgets.QHBoxLayout(page)
        disposition.setContentsMargins(14, 14, 14, 14)
        disposition.setSpacing(14)

        disposition.addWidget(self._panneau_saisie(), 5)
        disposition.addWidget(self._panneau_resultat(), 6)
        return page

    def _panneau_saisie(self) -> QtWidgets.QWidget:
        panneau = QtWidgets.QWidget()
        disposition = QtWidgets.QVBoxLayout(panneau)
        disposition.setContentsMargins(0, 0, 0, 0)

        # --- Élève ---
        groupe_eleve = QtWidgets.QGroupBox("Élève (facultatif)")
        formulaire = QtWidgets.QFormLayout(groupe_eleve)
        self.champ_nom = QtWidgets.QLineEdit()
        self.champ_nom.setPlaceholderText("Nom de l'élève, pour l'historique")
        self.champ_nom.setMaxLength(80)
        formulaire.addRow("Nom :", self.champ_nom)
        disposition.addWidget(groupe_eleve)

        # --- Notes ---
        groupe_notes = QtWidgets.QGroupBox("Notes scolaires (sur 100)")
        formulaire = QtWidgets.QFormLayout(groupe_notes)
        formulaire.setLabelAlignment(Qt.AlignmentFlag.AlignRight)

        self.champs_notes = {}
        for matiere in config.COLONNES_NOTES:
            champ = QtWidgets.QDoubleSpinBox()
            champ.setRange(config.NOTE_MIN, config.NOTE_MAX)  # saisie hors bornes impossible
            champ.setDecimals(1)
            champ.setSingleStep(1.0)
            champ.setSuffix(" / 100")
            champ.valueChanged.connect(self._actualiser_moyenne)
            self.champs_notes[matiere] = champ
            formulaire.addRow(f"{LIBELLES_MATIERES[matiere]} :", champ)

        self.etiquette_moyenne = QtWidgets.QLabel("—")
        police = self.etiquette_moyenne.font()
        police.setBold(True)
        police.setPointSize(police.pointSize() + 1)
        self.etiquette_moyenne.setFont(police)
        formulaire.addRow("Moyenne générale :", self.etiquette_moyenne)

        indication = QtWidgets.QLabel(
            "La moyenne générale est calculée automatiquement à partir des cinq notes."
        )
        indication.setObjectName("sousTitre")
        indication.setWordWrap(True)
        formulaire.addRow("", indication)
        disposition.addWidget(groupe_notes)

        # --- Profil ---
        groupe_profil = QtWidgets.QGroupBox("Profil et centres d'intérêt")
        formulaire = QtWidgets.QFormLayout(groupe_profil)
        formulaire.setLabelAlignment(Qt.AlignmentFlag.AlignRight)

        self.champ_aptitude = QtWidgets.QSpinBox()
        self.champ_aptitude.setRange(int(config.APTITUDE_MIN), int(config.APTITUDE_MAX))
        self.champ_aptitude.valueChanged.connect(self._actualiser_aptitude)
        self.etiquette_aptitude = QtWidgets.QLabel()
        self.etiquette_aptitude.setObjectName("sousTitre")

        ligne_aptitude = QtWidgets.QHBoxLayout()
        ligne_aptitude.addWidget(self.champ_aptitude)
        ligne_aptitude.addWidget(self.etiquette_aptitude, 1)
        conteneur = QtWidgets.QWidget()
        conteneur.setLayout(ligne_aptitude)
        formulaire.addRow("Aptitude logique (1-5) :", conteneur)

        self.champ_motivation = QtWidgets.QComboBox()
        self.champ_motivation.addItems(config.ORDRE_MOTIVATION + [NON_RENSEIGNE])
        formulaire.addRow("Niveau de motivation :", self.champ_motivation)

        self.champ_centre_interet = QtWidgets.QComboBox()
        self.champ_centre_interet.addItems(
            config.MODALITES_CENTRE_INTERET + [NON_RENSEIGNE]
        )
        formulaire.addRow("Centre d'intérêt :", self.champ_centre_interet)

        self.champ_informatique = QtWidgets.QComboBox()
        self.champ_informatique.addItems(config.MODALITES_BINAIRE + [NON_RENSEIGNE])
        formulaire.addRow("Intérêt pour l'informatique :", self.champ_informatique)
        disposition.addWidget(groupe_profil)

        # --- Boutons ---
        boutons = QtWidgets.QHBoxLayout()
        self.bouton_predire = QtWidgets.QPushButton("Prédire l'orientation")
        self.bouton_predire.setObjectName("principal")
        self.bouton_predire.setEnabled(self.modele is not None)
        self.bouton_predire.clicked.connect(self.predire)

        bouton_reinitialiser = QtWidgets.QPushButton("Réinitialiser")
        bouton_reinitialiser.clicked.connect(self.reinitialiser)

        bouton_exemple = QtWidgets.QPushButton("Profil d'exemple")
        bouton_exemple.setToolTip("Remplit le formulaire avec un profil tiré au hasard")
        bouton_exemple.clicked.connect(self.remplir_exemple)

        boutons.addWidget(self.bouton_predire, 2)
        boutons.addWidget(bouton_reinitialiser, 1)
        boutons.addWidget(bouton_exemple, 1)
        disposition.addLayout(boutons)
        disposition.addStretch()
        return panneau

    def _panneau_resultat(self) -> QtWidgets.QWidget:
        panneau = QtWidgets.QWidget()
        disposition = QtWidgets.QVBoxLayout(panneau)
        disposition.setContentsMargins(0, 0, 0, 0)

        groupe = QtWidgets.QGroupBox("Résultat de l'orientation")
        interne = QtWidgets.QVBoxLayout(groupe)

        self.etiquette_serie = QtWidgets.QLabel("En attente d'une prédiction")
        self.etiquette_serie.setObjectName("titreResultat")
        self.etiquette_serie.setWordWrap(True)
        interne.addWidget(self.etiquette_serie)

        self.etiquette_libelle_serie = QtWidgets.QLabel("")
        self.etiquette_libelle_serie.setObjectName("sousTitre")
        self.etiquette_libelle_serie.setWordWrap(True)
        interne.addWidget(self.etiquette_libelle_serie)

        separateur = QtWidgets.QFrame()
        separateur.setFrameShape(QtWidgets.QFrame.Shape.HLine)
        separateur.setFrameShadow(QtWidgets.QFrame.Shadow.Sunken)
        interne.addWidget(separateur)

        self.graphique = BarresProbabilites()
        interne.addWidget(self.graphique, 1)
        disposition.addWidget(groupe, 3)

        groupe_explication = QtWidgets.QGroupBox("Pourquoi cette recommandation ?")
        interne = QtWidgets.QVBoxLayout(groupe_explication)
        self.zone_explication = QtWidgets.QTextEdit()
        self.zone_explication.setReadOnly(True)
        interne.addWidget(self.zone_explication)
        disposition.addWidget(groupe_explication, 2)

        groupe_console = QtWidgets.QGroupBox("Sortie détaillée")
        interne = QtWidgets.QVBoxLayout(groupe_console)
        self.zone_console = QtWidgets.QPlainTextEdit()
        self.zone_console.setReadOnly(True)
        interne.addWidget(self.zone_console)
        disposition.addWidget(groupe_console, 2)

        avertissement = QtWidgets.QLabel(
            "Outil d'aide à la décision. Ce résultat ne constitue pas une "
            "orientation officielle et doit être discuté avec l'élève et l'équipe "
            "pédagogique."
        )
        avertissement.setObjectName("sousTitre")
        avertissement.setWordWrap(True)
        disposition.addWidget(avertissement)
        return panneau

    # -- Onglet 2 : historique ------------------------------------------
    def _onglet_historique(self) -> QtWidgets.QWidget:
        page = QtWidgets.QWidget()
        disposition = QtWidgets.QVBoxLayout(page)
        disposition.setContentsMargins(14, 14, 14, 14)

        self.etiquette_resume_historique = QtWidgets.QLabel()
        disposition.addWidget(self.etiquette_resume_historique)

        self.table_historique = QtWidgets.QTableWidget()
        self.table_historique.setEditTriggers(
            QtWidgets.QAbstractItemView.EditTrigger.NoEditTriggers
        )
        self.table_historique.setSelectionBehavior(
            QtWidgets.QAbstractItemView.SelectionBehavior.SelectRows
        )
        self.table_historique.setAlternatingRowColors(True)
        disposition.addWidget(self.table_historique, 1)

        boutons = QtWidgets.QHBoxLayout()
        for libelle, action in (
            ("Actualiser", self.actualiser_historique),
            ("Supprimer la ligne", self.supprimer_ligne_historique),
            ("Exporter en CSV", self.exporter_historique),
            ("Tout effacer", self.vider_historique),
        ):
            bouton = QtWidgets.QPushButton(libelle)
            bouton.clicked.connect(action)
            boutons.addWidget(bouton)
        boutons.addStretch()
        disposition.addLayout(boutons)
        return page

    # -- Onglet 3 : modèle ----------------------------------------------
    def _onglet_modele(self) -> QtWidgets.QWidget:
        page = QtWidgets.QWidget()
        disposition = QtWidgets.QVBoxLayout(page)
        disposition.setContentsMargins(14, 14, 14, 14)

        haut = QtWidgets.QHBoxLayout()

        groupe_infos = QtWidgets.QGroupBox("Modèle chargé")
        interne = QtWidgets.QVBoxLayout(groupe_infos)
        self.zone_infos_modele = QtWidgets.QPlainTextEdit()
        self.zone_infos_modele.setReadOnly(True)
        self.zone_infos_modele.setPlainText(self._texte_infos_modele())
        interne.addWidget(self.zone_infos_modele)
        haut.addWidget(groupe_infos, 1)

        groupe_comparaison = QtWidgets.QGroupBox("Comparaison des modèles (jeu de test)")
        interne = QtWidgets.QVBoxLayout(groupe_comparaison)
        self.table_comparaison = QtWidgets.QTableWidget()
        self.table_comparaison.setEditTriggers(
            QtWidgets.QAbstractItemView.EditTrigger.NoEditTriggers
        )
        interne.addWidget(self.table_comparaison)
        haut.addWidget(groupe_comparaison, 1)
        disposition.addLayout(haut, 1)

        groupe_figures = QtWidgets.QGroupBox("Figures produites par l'analyse")
        interne = QtWidgets.QVBoxLayout(groupe_figures)
        self.selecteur_figure = QtWidgets.QComboBox()
        self.selecteur_figure.currentTextChanged.connect(self._afficher_figure)
        interne.addWidget(self.selecteur_figure)

        zone = QtWidgets.QScrollArea()
        zone.setWidgetResizable(True)
        self.etiquette_figure = QtWidgets.QLabel("Aucune figure disponible.")
        self.etiquette_figure.setAlignment(Qt.AlignmentFlag.AlignCenter)
        zone.setWidget(self.etiquette_figure)
        interne.addWidget(zone, 1)
        disposition.addWidget(groupe_figures, 2)

        self._remplir_comparaison()
        self._remplir_selecteur_figures()
        return page

    def _construire_menu(self) -> None:
        menu_fichier = self.menuBar().addMenu("&Fichier")

        action_exporter = QAction("Exporter l'historique en CSV…", self)
        action_exporter.triggered.connect(self.exporter_historique)
        menu_fichier.addAction(action_exporter)
        menu_fichier.addSeparator()

        action_quitter = QAction("Quitter", self)
        action_quitter.setShortcut("Ctrl+Q")
        action_quitter.triggered.connect(self.close)
        menu_fichier.addAction(action_quitter)

        menu_aide = self.menuBar().addMenu("&Aide")
        action_propos = QAction("À propos", self)
        action_propos.triggered.connect(self.afficher_a_propos)
        menu_aide.addAction(action_propos)

    # ------------------------------------------------------------------
    # Formulaire
    # ------------------------------------------------------------------
    def _actualiser_moyenne(self) -> None:
        moyenne = calculer_moyenne(
            {matiere: champ.value() for matiere, champ in self.champs_notes.items()}
        )
        self.etiquette_moyenne.setText(f"{moyenne:.2f} / 100")

    def _actualiser_aptitude(self, valeur: int) -> None:
        self.etiquette_aptitude.setText(f"({DESCRIPTION_APTITUDE.get(valeur, '')})")

    def reinitialiser(self) -> None:
        """Remet le formulaire et l'affichage dans leur état initial."""
        self.champ_nom.clear()
        for champ in self.champs_notes.values():
            champ.setValue(60.0)
        self.champ_aptitude.setValue(3)
        self.champ_motivation.setCurrentText("Élevée")
        self.champ_centre_interet.setCurrentIndex(0)
        self.champ_informatique.setCurrentText("Non")

        self._actualiser_moyenne()
        self._actualiser_aptitude(self.champ_aptitude.value())

        self.etiquette_serie.setText("En attente d'une prédiction")
        self.etiquette_libelle_serie.setText("")
        self.zone_explication.clear()
        self.zone_console.clear()
        self.graphique.effacer()
        self.derniere_prediction = None

    def remplir_exemple(self) -> None:
        """Remplit le formulaire avec un profil cohérent tiré au hasard."""
        exemples = [
            ({"math": 88, "physique": 84, "svt": 66, "francais": 62, "histoire": 58},
             5, "Élevée", "Informatique", "Oui"),
            ({"math": 68, "physique": 66, "svt": 88, "francais": 70, "histoire": 64},
             4, "Très élevée", "Santé et environnement", "Non"),
            ({"math": 66, "physique": 57, "svt": 61, "francais": 76, "histoire": 84},
             3, "Élevée", "Économie et société", "Non"),
            ({"math": 55, "physique": 48, "svt": 57, "francais": 89, "histoire": 80},
             2, "Très élevée", "Arts et culture", "Non"),
            ({"math": 79, "physique": 74, "svt": 72, "francais": 68, "histoire": 63},
             4, "Élevée", "Sciences et technologie", "Oui"),
        ]
        notes, aptitude, motivation, interet, informatique = random.choice(exemples)

        for matiere, valeur in notes.items():
            self.champs_notes[matiere].setValue(float(valeur))
        self.champ_aptitude.setValue(aptitude)
        self.champ_motivation.setCurrentText(motivation)
        self.champ_centre_interet.setCurrentText(interet)
        self.champ_informatique.setCurrentText(informatique)

    def lire_profil(self) -> dict:
        """Construit le dictionnaire de profil à partir du formulaire."""

        def valeur_combo(combo):
            texte = combo.currentText()
            return None if texte == NON_RENSEIGNE else texte

        profil = {
            matiere: float(champ.value())
            for matiere, champ in self.champs_notes.items()
        }
        profil[config.COLONNE_APTITUDE] = float(self.champ_aptitude.value())
        profil[config.COLONNE_MOTIVATION] = valeur_combo(self.champ_motivation)
        profil[config.COLONNE_CATEGORIELLE] = valeur_combo(self.champ_centre_interet)
        profil[config.COLONNE_BINAIRE] = valeur_combo(self.champ_informatique)
        profil["moyenne_generale"] = calculer_moyenne(profil)
        return profil

    def valider_profil(self, profil: dict) -> tuple[list[str], list[str]]:
        """Contrôle la saisie.

        Renvoie (erreurs bloquantes, avertissements). Les champs numériques
        sont déjà bornés par les widgets ; ces contrôles couvrent les cas que
        les widgets ne peuvent pas détecter.
        """
        erreurs, avertissements = [], []

        for matiere in config.COLONNES_NOTES:
            valeur = profil.get(matiere)
            if valeur is None or not np.isfinite(valeur):
                erreurs.append(f"La note de {LIBELLES_MATIERES[matiere].lower()} est absente.")
            elif not (config.NOTE_MIN <= valeur <= config.NOTE_MAX):
                erreurs.append(
                    f"La note de {LIBELLES_MATIERES[matiere].lower()} doit être "
                    f"comprise entre {config.NOTE_MIN:.0f} et {config.NOTE_MAX:.0f}."
                )

        notes = [profil[matiere] for matiere in config.COLONNES_NOTES]
        if all(note == 0 for note in notes):
            erreurs.append("Toutes les notes sont à 0 : le profil est vide.")
        elif len(set(notes)) == 1:
            avertissements.append(
                "Les cinq notes sont identiques : le modèle ne dispose d'aucun "
                "contraste entre les matières."
            )

        if profil.get(config.COLONNE_CATEGORIELLE) is None:
            avertissements.append(
                "Le centre d'intérêt n'est pas renseigné. C'est la variable la plus "
                "influente du modèle : la prédiction sera nettement moins fiable."
            )
        if profil.get(config.COLONNE_MOTIVATION) is None:
            avertissements.append("Le niveau de motivation n'est pas renseigné.")
        if profil.get(config.COLONNE_BINAIRE) is None:
            avertissements.append("L'intérêt pour l'informatique n'est pas renseigné.")

        return erreurs, avertissements

    # ------------------------------------------------------------------
    # Prédiction
    # ------------------------------------------------------------------
    def predire(self) -> None:
        if self.modele is None:
            QtWidgets.QMessageBox.warning(
                self, "Modèle absent",
                "Aucun modèle n'est chargé. Lancez d'abord :  python train.py",
            )
            return

        profil = self.lire_profil()
        erreurs, avertissements = self.valider_profil(profil)

        if erreurs:
            QtWidgets.QMessageBox.warning(
                self, "Saisie incomplète", "\n".join(f"• {e}" for e in erreurs)
            )
            return

        if avertissements:
            reponse = QtWidgets.QMessageBox.question(
                self, "Confirmer la prédiction",
                "\n".join(f"• {a}" for a in avertissements) + "\n\nContinuer ?",
                QtWidgets.QMessageBox.StandardButton.Yes
                | QtWidgets.QMessageBox.StandardButton.No,
                QtWidgets.QMessageBox.StandardButton.Yes,
            )
            if reponse != QtWidgets.QMessageBox.StandardButton.Yes:
                return

        # Le DataFrame reprend exactement les colonnes attendues par le
        # pipeline ; celui-ci applique ensuite le prétraitement d'entraînement.
        donnees = construire_dataframe_eleve(profil)

        try:
            serie = str(self.modele.predict(donnees)[0])
            probabilites = self._probabilites(donnees)
        except Exception as erreur:  # pragma: no cover
            QtWidgets.QMessageBox.critical(
                self, "Erreur de prédiction", f"La prédiction a échoué :\n\n{erreur}"
            )
            return

        self._afficher_resultat(profil, serie, probabilites)

        identifiant = self.historique.enregistrer(
            profil, serie, probabilites,
            nom_eleve=self.champ_nom.text().strip() or None,
            modele=self.nom_modele,
        )
        self.derniere_prediction = (identifiant, serie, probabilites)
        self.actualiser_historique()
        self.statusBar().showMessage(
            f"Orientation n°{identifiant} enregistrée — série recommandée : {serie}",
            6000,
        )

    def _probabilites(self, donnees) -> dict:
        """Probabilités par série, si le modèle sait les fournir."""
        if not hasattr(self.modele, "predict_proba"):
            return {}
        valeurs = self.modele.predict_proba(donnees)[0]
        classes = [str(classe) for classe in self.modele.classes_]
        return dict(zip(classes, (float(v) for v in valeurs)))

    def _afficher_resultat(self, profil: dict, serie: str, probabilites: dict) -> None:
        self.etiquette_serie.setText(f"Série recommandée : {serie}")
        self.etiquette_libelle_serie.setText(config.LIBELLES_SERIES.get(serie, ""))

        couleur = COULEURS_SERIES.get(serie, "#1B3A57")
        self.etiquette_serie.setStyleSheet(f"color: {couleur};")

        self.graphique.definir_probabilites(probabilites, serie)
        self.zone_explication.setHtml(self._construire_explication(profil, serie, probabilites))
        self.zone_console.setPlainText(self._sortie_texte(profil, serie, probabilites))

    def _sortie_texte(self, profil: dict, serie: str, probabilites: dict) -> str:
        """Sortie au format demandé dans l'énoncé du projet."""
        lignes = [
            "=" * 40,
            "RÉSULTAT DE L'ORIENTATION".center(40),
            "=" * 40,
            "",
        ]
        if probabilites:
            for nom in sorted(probabilites, key=probabilites.get, reverse=True):
                lignes.append(f"{nom} : {probabilites[nom] * 100:.1f} %")
        else:
            lignes.append("(le modèle retenu ne fournit pas de probabilités)")

        lignes += [
            "",
            f"Série recommandée : {serie}",
            "",
            "-" * 40,
            "Profil analysé",
            "-" * 40,
        ]
        for matiere in config.COLONNES_NOTES:
            lignes.append(f"{LIBELLES_MATIERES[matiere]:<16}: {profil[matiere]:.1f}")
        lignes += [
            f"{'Moyenne':<16}: {profil['moyenne_generale']:.2f}",
            f"{'Aptitude logique':<16}: {profil[config.COLONNE_APTITUDE]:.0f} / 5",
            f"{'Motivation':<16}: {profil[config.COLONNE_MOTIVATION] or 'non renseignée'}",
            f"{'Centre intérêt':<16}: {profil[config.COLONNE_CATEGORIELLE] or 'non renseigné'}",
            f"{'Informatique':<16}: {profil[config.COLONNE_BINAIRE] or 'non renseigné'}",
            "",
            f"Modèle : {self.nom_modele}",
        ]
        return "\n".join(lignes)

    # ------------------------------------------------------------------
    # Explication (bonus)
    # ------------------------------------------------------------------
    def _construire_explication(self, profil: dict, serie: str, probabilites: dict) -> str:
        parties = []

        if probabilites:
            classement = sorted(probabilites.items(), key=lambda x: x[1], reverse=True)
            premiere, seconde = classement[0], classement[1]
            ecart = premiere[1] - seconde[1]
            if ecart > 0.35:
                confiance = "La recommandation est <b>nette</b>"
            elif ecart > 0.15:
                confiance = "La recommandation est <b>assez nette</b>"
            else:
                confiance = "La recommandation est <b>peu tranchée</b>"
            parties.append(
                f"{confiance} : {premiere[0]} obtient {premiere[1] * 100:.1f} %, "
                f"devant {seconde[0]} à {seconde[1] * 100:.1f} % "
                f"(écart de {ecart * 100:.1f} points)."
            )
            if ecart <= 0.15:
                parties.append(
                    "Les deux séries méritent d'être examinées avec l'élève."
                )

        contributions = self._contributions_locales(profil, serie)
        if contributions:
            elements = "".join(
                f"<li>{libelle} <span style='color:#5A6672'>"
                f"(poids {poids:+.2f})</span></li>"
                for libelle, poids in contributions
            )
            parties.append(
                "Les éléments du profil qui poussent le plus vers "
                f"<b>{serie}</b> :<ul>{elements}</ul>"
            )
        else:
            parties.append(self._explication_de_repli(profil, serie))

        parties.append(
            "<i>Le modèle a appris sur un jeu de données simulé de 3 000 élèves. "
            "Il reproduit des régularités statistiques, pas une décision "
            "pédagogique.</i>"
        )
        return "<br><br>".join(parties)

    def _contributions_locales(self, profil: dict, serie: str) -> list[tuple[str, float]]:
        """Décompose la décision d'un modèle linéaire pour ce profil précis.

        Pour une régression logistique, le score d'une série est une somme
        ``coefficient × valeur encodée``. On peut donc dire, pour cet élève,
        quels éléments ont réellement pesé — et pas seulement quelles
        variables comptent en moyenne.
        """
        try:
            classifieur = self.modele.named_steps["classifieur"]
            if not hasattr(classifieur, "coef_"):
                return []

            preparation = self.modele.named_steps["preparation"]
            nettoyage = self.modele.named_steps["nettoyage"]

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
                    self._libelle_variable(noms[index], profil, encode[index]),
                    float(contributions[index]),
                ))
                if len(resultat) == 5:
                    break
            return resultat
        except Exception:  # pragma: no cover - l'explication reste facultative
            return []

    @staticmethod
    def _libelle_variable(nom: str, profil: dict, valeur_encodee: float = 0.0) -> str:
        """Traduit un nom de colonne encodée en phrase lisible.

        ``valeur_encodee`` est la valeur standardisée : son signe indique si
        l'élève est au-dessus ou en dessous de la moyenne des 3 000 élèves.
        Sans cette nuance, « note de français 58/100 » pousserait vers SMP
        sans qu'on comprenne que c'est justement parce qu'elle est basse.
        """
        def situer(seuil=0.35):
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

    def _explication_de_repli(self, profil: dict, serie: str) -> str:
        """Explication descriptive quand le modèle n'est pas linéaire."""
        notes = {m: profil[m] for m in config.COLONNES_NOTES}
        meilleures = sorted(notes, key=notes.get, reverse=True)[:2]
        elements = [
            "points forts : "
            + " et ".join(
                f"{LIBELLES_MATIERES[m].lower()} ({notes[m]:.0f})" for m in meilleures
            )
        ]
        if profil.get(config.COLONNE_CATEGORIELLE):
            elements.append(
                f"centre d'intérêt « {profil[config.COLONNE_CATEGORIELLE]} »"
            )
        valeur = int(profil[config.COLONNE_APTITUDE])
        elements.append(
            f"aptitude logique {valeur}/5 ({DESCRIPTION_APTITUDE.get(valeur, '')})"
        )
        return (
            f"Profil rapproché de la série <b>{serie}</b> — "
            + " ; ".join(elements) + "."
        )

    # ------------------------------------------------------------------
    # Historique
    # ------------------------------------------------------------------
    COLONNES_HISTORIQUE = [
        ("id", "N°"),
        ("horodatage", "Date"),
        ("nom_eleve", "Élève"),
        ("moyenne_generale", "Moyenne"),
        ("centre_interet", "Centre d'intérêt"),
        ("aptitude_logique", "Aptitude"),
        ("serie_predite", "Série"),
        ("modele", "Modèle"),
    ]

    def actualiser_historique(self) -> None:
        lignes = self.historique.lister()
        cles = [cle for cle, _ in self.COLONNES_HISTORIQUE]
        entetes = [entete for _, entete in self.COLONNES_HISTORIQUE]

        self.table_historique.setColumnCount(len(cles))
        self.table_historique.setHorizontalHeaderLabels(entetes)
        self.table_historique.setRowCount(len(lignes))

        for index_ligne, ligne in enumerate(lignes):
            for index_colonne, cle in enumerate(cles):
                valeur = ligne.get(cle)
                if isinstance(valeur, float):
                    texte = f"{valeur:.2f}" if cle == "moyenne_generale" else f"{valeur:.0f}"
                else:
                    texte = "" if valeur is None else str(valeur)
                element = QtWidgets.QTableWidgetItem(texte)
                if cle == "serie_predite" and valeur in COULEURS_SERIES:
                    element.setForeground(QtGui.QColor(COULEURS_SERIES[valeur]))
                    police = element.font()
                    police.setBold(True)
                    element.setFont(police)
                self.table_historique.setItem(index_ligne, index_colonne, element)

        self.table_historique.resizeColumnsToContents()
        entete = self.table_historique.horizontalHeader()
        entete.setStretchLastSection(True)
        self._actualiser_barre_etat()

        repartition = self.historique.repartition_series()
        resume = "  ·  ".join(f"{serie} : {n}" for serie, n in repartition.items())
        self.etiquette_resume_historique.setText(
            f"<b>{self.historique.compter()}</b> orientation(s) enregistrée(s)"
            + (f"   —   {resume}" if resume else "")
            + f"<br><span style='color:#5A6672'>Base locale : "
            f"{self.historique.chemin.name}</span>"
        )

    def supprimer_ligne_historique(self) -> None:
        ligne = self.table_historique.currentRow()
        if ligne < 0:
            QtWidgets.QMessageBox.information(
                self, "Aucune sélection", "Sélectionnez d'abord une ligne."
            )
            return
        identifiant = int(self.table_historique.item(ligne, 0).text())
        reponse = QtWidgets.QMessageBox.question(
            self, "Confirmer", f"Supprimer l'orientation n°{identifiant} ?"
        )
        if reponse == QtWidgets.QMessageBox.StandardButton.Yes:
            self.historique.supprimer(identifiant)
            self.actualiser_historique()

    def vider_historique(self) -> None:
        if self.historique.compter() == 0:
            return
        reponse = QtWidgets.QMessageBox.question(
            self, "Confirmer",
            "Effacer définitivement tout l'historique des orientations ?",
        )
        if reponse == QtWidgets.QMessageBox.StandardButton.Yes:
            self.historique.vider()
            self.actualiser_historique()

    def exporter_historique(self) -> None:
        if self.historique.compter() == 0:
            QtWidgets.QMessageBox.information(
                self, "Historique vide", "Il n'y a rien à exporter."
            )
            return
        chemin, _ = QtWidgets.QFileDialog.getSaveFileName(
            self, "Exporter l'historique",
            str(Path.home() / "historique_orientations.csv"),
            "Fichiers CSV (*.csv)",
        )
        if chemin:
            self.historique.exporter_csv(chemin)
            self.statusBar().showMessage(f"Historique exporté vers {chemin}", 6000)

    # ------------------------------------------------------------------
    # Onglet Modèle
    # ------------------------------------------------------------------
    def _texte_infos_modele(self) -> str:
        if self.modele is None:
            return "Aucun modèle chargé.\n\nLancez :  python train.py"

        lignes = [
            f"Algorithme retenu : {self.nom_modele}",
            f"Fichier           : {config.CHEMIN_MODELE.name}",
            f"Entraîné le       : {self.metadonnees.get('date_entrainement', '—')}",
            f"Séries prédites   : {', '.join(str(c) for c in self.modele.classes_)}",
        ]

        metriques = self.metadonnees.get("metriques_test", {})
        if metriques:
            lignes += ["", "Performances sur le jeu de test :"]
            libelles = {
                "accuracy": "Exactitude",
                "precision_macro": "Précision (macro)",
                "recall_macro": "Rappel (macro)",
                "f1_macro": "F1-score (macro)",
            }
            for cle, libelle in libelles.items():
                if cle in metriques:
                    lignes.append(f"  {libelle:<20} {metriques[cle]:.4f}")

        validation = self.metadonnees.get("validation_croisee_f1_macro", {})
        if validation:
            lignes.append(
                f"  {'F1 validation croisée':<20} {validation.get('moyenne', 0):.4f} "
                f"± {validation.get('ecart_type', 0):.4f} "
                f"({validation.get('nb_plis', '?')} plis)"
            )

        importances = self.metadonnees.get("importances_principales")
        if importances:
            lignes += ["", "Variables les plus influentes :"]
            for nom, valeur in list(importances.items())[:10]:
                lignes.append(f"  {nom:<42} {valeur:.4f}")

        versions = self.metadonnees.get("versions", {})
        if versions:
            lignes += ["", "Versions utilisées à l'entraînement :"]
            for nom, valeur in versions.items():
                lignes.append(f"  {nom:<15} {valeur}")

        return "\n".join(lignes)

    def _remplir_comparaison(self) -> None:
        comparaison = self.metadonnees.get("comparaison") or {}
        if not comparaison:
            self.table_comparaison.setRowCount(0)
            return

        colonnes = ["accuracy", "precision_macro", "recall_macro", "f1_macro"]
        entetes = ["Modèle", "Accuracy", "Precision", "Recall", "F1-score"]
        ordre = sorted(comparaison, key=lambda n: comparaison[n]["f1_macro"], reverse=True)

        self.table_comparaison.setColumnCount(len(entetes))
        self.table_comparaison.setHorizontalHeaderLabels(entetes)
        self.table_comparaison.setRowCount(len(ordre))

        for index_ligne, nom in enumerate(ordre):
            element = QtWidgets.QTableWidgetItem(nom)
            if nom == self.nom_modele:
                police = element.font()
                police.setBold(True)
                element.setFont(police)
            self.table_comparaison.setItem(index_ligne, 0, element)

            for index_colonne, cle in enumerate(colonnes, start=1):
                valeur = comparaison[nom].get(cle, float("nan"))
                cellule = QtWidgets.QTableWidgetItem(f"{valeur:.4f}")
                cellule.setTextAlignment(Qt.AlignmentFlag.AlignCenter)
                self.table_comparaison.setItem(index_ligne, index_colonne, cellule)

        self.table_comparaison.resizeColumnsToContents()
        self.table_comparaison.horizontalHeader().setStretchLastSection(True)

    def _remplir_selecteur_figures(self) -> None:
        if not config.DOSSIER_REPORTS.exists():
            return
        figures = sorted(config.DOSSIER_REPORTS.glob("*.png"))
        self.selecteur_figure.addItems([chemin.name for chemin in figures])
        if figures:
            self._afficher_figure(figures[0].name)

    def _afficher_figure(self, nom_fichier: str) -> None:
        if not nom_fichier:
            return
        chemin = config.DOSSIER_REPORTS / nom_fichier
        if not chemin.exists():
            self.etiquette_figure.setText("Figure introuvable.")
            return
        image = QtGui.QPixmap(str(chemin))
        if image.isNull():
            self.etiquette_figure.setText("Figure illisible.")
            return
        self.etiquette_figure.setPixmap(
            image.scaledToWidth(940, Qt.TransformationMode.SmoothTransformation)
        )

    # ------------------------------------------------------------------
    def afficher_a_propos(self) -> None:
        QtWidgets.QMessageBox.about(
            self, "À propos",
            "<b>Système d'aide à l'orientation scolaire</b><br>"
            "Nouveau Secondaire — séries SMP, SVT, SES, LLA<br><br>"
            f"Modèle : {self.nom_modele}<br>"
            f"Interface : {VERSION_QT}<br><br>"
            "Projet IA2 — UNITECH, niveaux 3 et 4 Sciences Informatiques.<br><br>"
            "<i>Outil d'aide à la décision. Le jeu de données d'entraînement est "
            "simulé à des fins pédagogiques : les résultats ne valident aucune "
            "méthode réelle d'orientation.</i>",
        )


# ==========================================================================
def main() -> int:
    application = QtWidgets.QApplication(sys.argv)
    application.setApplicationName("Orientation Nouveau Secondaire")
    application.setStyleSheet(FEUILLE_DE_STYLE)

    fenetre = FenetreOrientation()
    fenetre.actualiser_historique()
    fenetre.show()
    return executer(application)


if __name__ == "__main__":
    sys.exit(main())
