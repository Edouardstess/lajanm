"""Historique des orientations (bonus : persistance SQLite).

Chaque prédiction faite depuis l'interface est enregistrée localement dans un
fichier SQLite. Aucune connexion réseau n'est utilisée, conformément à la
contrainte « l'application devra fonctionner localement, sans API ».
"""

from __future__ import annotations

import json
import sqlite3
from datetime import datetime
from pathlib import Path

import config


SCHEMA = """
CREATE TABLE IF NOT EXISTS orientations (
    id                          INTEGER PRIMARY KEY AUTOINCREMENT,
    horodatage                  TEXT    NOT NULL,
    nom_eleve                   TEXT,
    math                        REAL,
    physique                    REAL,
    svt                         REAL,
    francais                    REAL,
    histoire                    REAL,
    moyenne_generale            REAL,
    aptitude_logique            REAL,
    niveau_motivation           TEXT,
    centre_interet              TEXT,
    interesse_par_informatique  TEXT,
    serie_predite               TEXT    NOT NULL,
    probabilites                TEXT,
    modele                      TEXT
);
"""

COLONNES_PROFIL = [
    "math", "physique", "svt", "francais", "histoire", "moyenne_generale",
    "aptitude_logique", "niveau_motivation", "centre_interet",
    "interesse_par_informatique",
]


class HistoriqueOrientations:
    """Petit dépôt de données au-dessus de SQLite."""

    def __init__(self, chemin: Path | str | None = None):
        self.chemin = Path(chemin) if chemin else config.CHEMIN_BASE_HISTORIQUE
        self.chemin.parent.mkdir(parents=True, exist_ok=True)
        self._creer_schema()

    def _connexion(self) -> sqlite3.Connection:
        connexion = sqlite3.connect(self.chemin)
        connexion.row_factory = sqlite3.Row
        return connexion

    def _creer_schema(self) -> None:
        with self._connexion() as connexion:
            connexion.executescript(SCHEMA)

    # ------------------------------------------------------------------
    def enregistrer(
        self,
        profil: dict,
        serie_predite: str,
        probabilites: dict | None = None,
        nom_eleve: str | None = None,
        modele: str | None = None,
    ) -> int:
        """Ajoute une orientation et renvoie son identifiant."""
        valeurs = {colonne: profil.get(colonne) for colonne in COLONNES_PROFIL}
        valeurs.update({
            "horodatage": datetime.now().isoformat(timespec="seconds"),
            "nom_eleve": nom_eleve or None,
            "serie_predite": serie_predite,
            "probabilites": json.dumps(probabilites or {}, ensure_ascii=False),
            "modele": modele,
        })

        colonnes = ", ".join(valeurs)
        marqueurs = ", ".join(f":{colonne}" for colonne in valeurs)
        with self._connexion() as connexion:
            curseur = connexion.execute(
                f"INSERT INTO orientations ({colonnes}) VALUES ({marqueurs})", valeurs
            )
            return int(curseur.lastrowid)

    def lister(self, limite: int = 200) -> list[dict]:
        """Renvoie les orientations, de la plus récente à la plus ancienne."""
        with self._connexion() as connexion:
            lignes = connexion.execute(
                "SELECT * FROM orientations ORDER BY id DESC LIMIT ?", (limite,)
            ).fetchall()
        return [dict(ligne) for ligne in lignes]

    def compter(self) -> int:
        with self._connexion() as connexion:
            return int(
                connexion.execute("SELECT COUNT(*) FROM orientations").fetchone()[0]
            )

    def repartition_series(self) -> dict:
        """Nombre d'orientations enregistrées par série."""
        with self._connexion() as connexion:
            lignes = connexion.execute(
                "SELECT serie_predite, COUNT(*) AS n FROM orientations "
                "GROUP BY serie_predite ORDER BY n DESC"
            ).fetchall()
        return {ligne["serie_predite"]: ligne["n"] for ligne in lignes}

    def supprimer(self, identifiant: int) -> None:
        with self._connexion() as connexion:
            connexion.execute("DELETE FROM orientations WHERE id = ?", (identifiant,))

    def vider(self) -> None:
        with self._connexion() as connexion:
            connexion.execute("DELETE FROM orientations")

    def exporter_csv(self, chemin: Path | str) -> Path:
        """Exporte tout l'historique dans un fichier CSV."""
        import csv

        chemin = Path(chemin)
        lignes = self.lister(limite=10_000)
        with chemin.open("w", newline="", encoding="utf-8") as fichier:
            if not lignes:
                fichier.write("")
                return chemin
            redacteur = csv.DictWriter(fichier, fieldnames=list(lignes[0]))
            redacteur.writeheader()
            redacteur.writerows(lignes)
        return chemin


if __name__ == "__main__":
    historique = HistoriqueOrientations()
    print(f"Base : {historique.chemin}")
    print(f"Orientations enregistrées : {historique.compter()}")
    repartition = historique.repartition_series()
    if repartition:
        for serie, nombre in repartition.items():
            print(f"  {serie} : {nombre}")
