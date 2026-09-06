package com.example.cooperativeagricole.data.remote

/**
 * État de la liaison avec la base distante, tel qu'il est montré à
 * l'utilisateur.
 *
 * L'application fonctionne dans tous les cas : ces états informent, ils ne
 * bloquent jamais la saisie.
 */
enum class EtatSynchronisation {
    /** Aucune configuration Firebase : l'application travaille en local seul. */
    DESACTIVEE,

    /** Liaison en cours d'établissement. */
    CONNEXION,

    /** Données locales et distantes alignées. */
    SYNCHRONISEE,

    /**
     * Des écritures faites ici n'ont pas encore été confirmées par le serveur.
     * Firestore les conserve et les rejouera : rien n'est perdu.
     */
    ENVOI_EN_ATTENTE,

    /** Pas de réseau : lecture et écriture continuent sur le cache local. */
    HORS_LIGNE,

    /** Le serveur a refusé la lecture (règles de sécurité, projet fermé…). */
    ERREUR,
}
