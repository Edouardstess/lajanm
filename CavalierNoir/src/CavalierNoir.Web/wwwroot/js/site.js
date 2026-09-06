/**
 * Comportements généraux du site — Cavalier Noir
 * ---------------------------------------------------------------------------
 * Aucun cadriciel : quelques dizaines de lignes suffisent, et le site reste
 * entièrement utilisable si le script ne se charge pas (amélioration
 * progressive).
 */
(function () {
  'use strict';

  /* --- Menu principal sur mobile ---------------------------------------- */
  function menu() {
    var bascule = document.querySelector('[data-nav-bascule]');
    var nav = document.querySelector('[data-nav]');
    if (!bascule || !nav) { return; }

    bascule.addEventListener('click', function () {
      var ouvert = nav.getAttribute('data-ouvert') === 'true';
      nav.setAttribute('data-ouvert', ouvert ? 'false' : 'true');
      bascule.setAttribute('aria-expanded', ouvert ? 'false' : 'true');
    });
  }

  /* --- Thème clair / sombre --------------------------------------------- */
  function theme() {
    var bouton = document.querySelector('[data-theme-bascule]');
    if (!bouton) { return; }

    var stocke = null;
    try { stocke = localStorage.getItem('cn-theme'); } catch (e) { /* stockage indisponible */ }

    if (stocke) { document.documentElement.setAttribute('data-theme', stocke); }

    bouton.addEventListener('click', function () {
      var actuel = document.documentElement.getAttribute('data-theme');
      var suivant = actuel === 'sombre' ? 'clair' : 'sombre';
      document.documentElement.setAttribute('data-theme', suivant);
      try { localStorage.setItem('cn-theme', suivant); } catch (e) { /* ignoré */ }
      bouton.setAttribute('aria-pressed', suivant === 'sombre' ? 'true' : 'false');
    });
  }

  /* --- Fermeture des alertes -------------------------------------------- */
  function alertes() {
    document.addEventListener('click', function (evenement) {
      var bouton = evenement.target.closest('[data-fermer-alerte]');
      if (!bouton) { return; }
      var alerte = bouton.closest('.alerte');
      if (alerte) { alerte.remove(); }
    });
  }

  /* --- Confirmation des actions destructrices --------------------------- */
  function confirmations() {
    document.addEventListener('submit', function (evenement) {
      var formulaire = evenement.target;
      var message = formulaire.getAttribute('data-confirmer');
      if (message && !window.confirm(message)) {
        evenement.preventDefault();
      }
    });
  }

  /* --- Chronomètre de résolution ---------------------------------------- */
  function chronometre() {
    var champ = document.querySelector('[data-chrono]');
    if (!champ) { return; }

    var debut = Date.now();
    var affichage = document.querySelector('[data-chrono-affichage]');

    setInterval(function () {
      var secondes = Math.floor((Date.now() - debut) / 1000);
      champ.value = String(secondes);

      if (affichage) {
        var m = Math.floor(secondes / 60);
        var s = secondes % 60;
        affichage.textContent = (m < 10 ? '0' : '') + m + ':' + (s < 10 ? '0' : '') + s;
      }
    }, 1000);
  }

  /* --- Indices progressifs ---------------------------------------------- */
  function indices() {
    var bouton = document.querySelector('[data-indice-bouton]');
    if (!bouton) { return; }

    var zone = document.querySelector('[data-indice-zone]');
    var compteur = document.querySelector('[data-indice-compteur]');
    var niveau = 0;

    bouton.addEventListener('click', function () {
      niveau += 1;
      if (niveau > 3) { return; }

      var url = bouton.getAttribute('data-indice-url') + '/' + niveau;

      fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function (reponse) { return reponse.ok ? reponse.json() : null; })
        .then(function (donnees) {
          if (!donnees) { return; }

          var bloc = document.createElement('div');
          bloc.className = 'indice';
          bloc.innerHTML = '';
          var titre = document.createElement('strong');
          titre.textContent = 'Indice ' + niveau + ' — ';
          var texte = document.createTextNode(donnees.texte);
          bloc.appendChild(titre);
          bloc.appendChild(texte);

          if (zone) { zone.appendChild(bloc); }
          if (compteur) { compteur.value = String(niveau); }
          if (niveau >= 3 || !donnees.disponible) { bouton.disabled = true; }
        })
        .catch(function () {
          bouton.disabled = true;
        });
    });
  }

  /* --- Onglets ----------------------------------------------------------- */
  function onglets() {
    var groupes = document.querySelectorAll('[data-onglets]');

    Array.prototype.forEach.call(groupes, function (groupe) {
      var boutons = groupe.querySelectorAll('[data-onglet]');

      Array.prototype.forEach.call(boutons, function (bouton) {
        bouton.addEventListener('click', function () {
          var cible = bouton.getAttribute('data-onglet');

          Array.prototype.forEach.call(boutons, function (autre) {
            autre.setAttribute('aria-selected', autre === bouton ? 'true' : 'false');
          });

          var panneaux = document.querySelectorAll('[data-panneau-onglet]');
          Array.prototype.forEach.call(panneaux, function (panneau) {
            panneau.hidden = panneau.getAttribute('data-panneau-onglet') !== cible;
          });
        });
      });
    });
  }

  function demarrer() {
    menu();
    theme();
    alertes();
    confirmations();
    chronometre();
    indices();
    onglets();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', demarrer);
  } else {
    demarrer();
  }
})();
