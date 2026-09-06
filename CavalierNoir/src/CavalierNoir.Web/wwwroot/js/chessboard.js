/**
 * Échiquier interactif — Cavalier Noir
 * ---------------------------------------------------------------------------
 * Rend une position FEN et enregistre les coups saisis en notation par
 * coordonnées (« e2e4 »), la notation attendue par la correction serveur.
 *
 * Volontairement sans moteur d'échecs : le composant n'a pas à connaître les
 * règles du jeu. Il déplace ce qu'on lui demande de déplacer, et c'est le
 * serveur qui juge la réponse. Cela évite d'embarquer une bibliothèque tierce
 * (que la politique de sécurité de contenu du site interdirait de toute façon)
 * et garde la vérité côté serveur, où elle n'est pas manipulable.
 */
(function () {
  'use strict';

  var GLYPHES = {
    K: '♔', Q: '♕', R: '♖', B: '♗', N: '♘', P: '♙',
    k: '♚', q: '♛', r: '♜', b: '♝', n: '♞', p: '♟'
  };

  var COLONNES = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

  /** Développe le champ de placement d'une FEN en grille 8×8 (rangée 8 en tête). */
  function fenVersGrille(fen) {
    var grille = [];
    var placement = String(fen || '').split(' ')[0];
    var rangees = placement.split('/');

    for (var r = 0; r < 8; r++) {
      var ligne = [];
      var contenu = rangees[r] || '8';

      for (var i = 0; i < contenu.length; i++) {
        var c = contenu.charAt(i);
        if (c >= '1' && c <= '8') {
          var vides = parseInt(c, 10);
          for (var v = 0; v < vides; v++) { ligne.push(''); }
        } else {
          ligne.push(c);
        }
      }

      while (ligne.length < 8) { ligne.push(''); }
      grille.push(ligne.slice(0, 8));
    }

    while (grille.length < 8) { grille.push(['', '', '', '', '', '', '', '']); }
    return grille;
  }

  function nomCase(rangee, colonne, retourne) {
    var r = retourne ? rangee : rangee;
    return COLONNES[colonne] + String(8 - r);
  }

  function Echiquier(element) {
    this.element = element;
    this.fen = element.getAttribute('data-fen') || '';
    this.interactif = element.getAttribute('data-interactif') === 'true';
    this.retourne = element.getAttribute('data-retourne') === 'true';
    this.grille = fenVersGrille(this.fen);
    this.selection = null;
    this.coups = [];

    var cibleId = element.getAttribute('data-champ-coups');
    this.champ = cibleId ? document.getElementById(cibleId) : null;

    var affichageId = element.getAttribute('data-affichage-coups');
    this.affichage = affichageId ? document.getElementById(affichageId) : null;

    this.dessiner();

    if (this.interactif) {
      this.element.classList.add('echiquier--interactif');
      this.element.addEventListener('click', this.surClic.bind(this));
      this.element.addEventListener('keydown', this.surTouche.bind(this));
    }
  }

  Echiquier.prototype.dessiner = function () {
    var fragment = document.createDocumentFragment();

    for (var r = 0; r < 8; r++) {
      for (var c = 0; c < 8; c++) {
        var rangee = this.retourne ? 7 - r : r;
        var colonne = this.retourne ? 7 - c : c;
        var piece = this.grille[rangee][colonne];
        var nom = nomCase(rangee, colonne, this.retourne);

        var cellule = document.createElement(this.interactif ? 'button' : 'div');
        cellule.className = 'case' + ((rangee + colonne) % 2 === 1 ? ' case--sombre' : '');
        cellule.setAttribute('data-case', nom);

        if (this.interactif) {
          cellule.type = 'button';
          cellule.setAttribute('aria-label',
            nom + (piece ? ', ' + this.decrirePiece(piece) : ', case vide'));
        } else {
          cellule.setAttribute('role', 'gridcell');
        }

        if (piece) {
          var span = document.createElement('span');
          span.className = piece === piece.toUpperCase() ? 'piece--blanche' : 'piece--noire';
          span.setAttribute('aria-hidden', 'true');
          span.textContent = GLYPHES[piece] || '';
          cellule.appendChild(span);
        }

        // Coordonnées discrètes sur la première colonne et la dernière rangée.
        if (c === 0 || r === 7) {
          var coord = document.createElement('span');
          coord.className = 'case__coord';
          coord.setAttribute('aria-hidden', 'true');
          coord.textContent = c === 0 ? String(8 - rangee) : COLONNES[colonne];
          cellule.appendChild(coord);
        }

        fragment.appendChild(cellule);
      }
    }

    this.element.textContent = '';
    this.element.appendChild(fragment);
  };

  Echiquier.prototype.decrirePiece = function (lettre) {
    var noms = {
      k: 'roi', q: 'dame', r: 'tour', b: 'fou', n: 'cavalier', p: 'pion'
    };
    var nom = noms[lettre.toLowerCase()] || 'pièce';
    return (lettre === lettre.toUpperCase() ? 'blanc ' : 'noir ') + nom;
  };

  Echiquier.prototype.surTouche = function (evenement) {
    if (evenement.key === 'Escape') {
      this.deselectionner();
    }
  };

  Echiquier.prototype.surClic = function (evenement) {
    var cellule = evenement.target.closest('[data-case]');
    if (!cellule) { return; }

    var nom = cellule.getAttribute('data-case');
    var position = this.indexDeCase(nom);

    if (!this.selection) {
      if (!this.grille[position.rangee][position.colonne]) { return; }
      this.selectionner(nom);
      return;
    }

    if (this.selection === nom) {
      this.deselectionner();
      return;
    }

    this.jouer(this.selection, nom);
    this.deselectionner();
  };

  Echiquier.prototype.indexDeCase = function (nom) {
    return {
      colonne: COLONNES.indexOf(nom.charAt(0)),
      rangee: 8 - parseInt(nom.charAt(1), 10)
    };
  };

  Echiquier.prototype.selectionner = function (nom) {
    this.selection = nom;
    var cellule = this.element.querySelector('[data-case="' + nom + '"]');
    if (cellule) { cellule.classList.add('case--selectionnee'); }
  };

  Echiquier.prototype.deselectionner = function () {
    var selectionnee = this.element.querySelector('.case--selectionnee');
    if (selectionnee) { selectionnee.classList.remove('case--selectionnee'); }
    this.selection = null;
  };

  /** Applique un déplacement, sans vérifier sa légalité (c'est le rôle du serveur). */
  Echiquier.prototype.jouer = function (depuis, vers) {
    var d = this.indexDeCase(depuis);
    var a = this.indexDeCase(vers);
    var piece = this.grille[d.rangee][d.colonne];

    if (!piece) { return; }

    var coup = depuis + vers;

    // Promotion : un pion qui atteint la dernière rangée devient dame par défaut.
    if (piece.toLowerCase() === 'p' && (a.rangee === 0 || a.rangee === 7)) {
      coup += 'q';
      piece = piece === 'P' ? 'Q' : 'q';
    }

    this.grille[d.rangee][d.colonne] = '';
    this.grille[a.rangee][a.colonne] = piece;
    this.coups.push(coup);

    this.dessiner();
    this.synchroniser();
  };

  Echiquier.prototype.annuler = function () {
    if (this.coups.length === 0) { return; }
    this.coups.pop();
    this.grille = fenVersGrille(this.fen);

    // Rejoue la séquence restante depuis la position initiale.
    var restants = this.coups.slice();
    this.coups = [];
    for (var i = 0; i < restants.length; i++) {
      this.jouer(restants[i].substring(0, 2), restants[i].substring(2, 4));
    }

    if (restants.length === 0) {
      this.dessiner();
      this.synchroniser();
    }
  };

  Echiquier.prototype.reinitialiser = function () {
    this.grille = fenVersGrille(this.fen);
    this.coups = [];
    this.deselectionner();
    this.dessiner();
    this.synchroniser();
  };

  Echiquier.prototype.synchroniser = function () {
    var texte = this.coups.join(' ');
    if (this.champ) { this.champ.value = texte; }
    if (this.affichage) {
      this.affichage.textContent = texte || 'Aucun coup joué pour l’instant.';
    }
  };

  function initialiser() {
    var echiquiers = document.querySelectorAll('[data-echiquier]');
    var instances = [];

    for (var i = 0; i < echiquiers.length; i++) {
      instances.push(new Echiquier(echiquiers[i]));
    }

    // Boutons « annuler » et « recommencer », reliés par l'identifiant de l'échiquier.
    document.addEventListener('click', function (evenement) {
      var bouton = evenement.target.closest('[data-echiquier-action]');
      if (!bouton) { return; }

      var cible = document.getElementById(bouton.getAttribute('data-echiquier-cible'));
      if (!cible) { return; }

      for (var i = 0; i < instances.length; i++) {
        if (instances[i].element === cible) {
          if (bouton.getAttribute('data-echiquier-action') === 'annuler') {
            instances[i].annuler();
          } else {
            instances[i].reinitialiser();
          }
          break;
        }
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initialiser);
  } else {
    initialiser();
  }
})();
