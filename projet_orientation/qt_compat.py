"""Couche de compatibilité PyQt6 / PyQt5.

Le cahier des charges impose « PyQt » sans préciser la version. Ce module
importe PyQt6 s'il est disponible, sinon PyQt5, et expose les mêmes noms dans
les deux cas. L'application n'a donc pas à savoir laquelle est installée.

Les rares différences réellement gênantes sont traitées ici :

* ``QAction`` a été déplacé de ``QtWidgets`` (PyQt5) vers ``QtGui`` (PyQt6) ;
* ``exec_()`` est devenu ``exec()``.

Les énumérations sont utilisées partout sous leur forme complète
(``Qt.AlignmentFlag.AlignCenter`` plutôt que ``Qt.AlignCenter``), forme
acceptée par les deux versions.
"""

from __future__ import annotations

VERSION_QT = None

try:  # PyQt6 en priorité
    from PyQt6 import QtCore, QtGui, QtWidgets
    from PyQt6.QtCore import Qt
    from PyQt6.QtGui import QAction

    VERSION_QT = "PyQt6"
except ImportError:  # pragma: no cover - dépend de l'environnement
    try:
        from PyQt5 import QtCore, QtGui, QtWidgets
        from PyQt5.QtCore import Qt
        from PyQt5.QtWidgets import QAction

        VERSION_QT = "PyQt5"
    except ImportError as erreur:  # pragma: no cover
        raise ImportError(
            "Aucune version de PyQt n'est installée.\n"
            "Installez-en une :  pip install PyQt5   (ou PyQt6)"
        ) from erreur


def executer(application) -> int:
    """Lance la boucle d'évènements, quelle que soit la version de PyQt."""
    if hasattr(application, "exec"):
        return application.exec()
    return application.exec_()  # pragma: no cover - PyQt5 très ancien


__all__ = ["QtCore", "QtGui", "QtWidgets", "Qt", "QAction", "VERSION_QT", "executer"]
