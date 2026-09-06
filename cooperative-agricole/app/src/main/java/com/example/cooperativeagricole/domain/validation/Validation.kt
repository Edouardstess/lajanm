package com.example.cooperativeagricole.domain.validation

/**
 * Motif d'invalidité d'un champ saisi.
 *
 * La validation ne renvoie pas de texte : elle renvoie une *cause*, que
 * l'écran traduit ensuite en message via les ressources `strings.xml`. C'est
 * ce qui permet de tester ces règles en JUnit, sans émulateur ni contexte
 * Android.
 */
enum class MotifInvalidite {
    OBLIGATOIRE,
    TROP_COURT,
    FORMAT_INVALIDE,
    DEJA_UTILISE,
    DATE_FUTURE,
    AGE_HORS_LIMITES,
    NOMBRE_INVALIDE,
    VALEUR_NON_POSITIVE,
    VALEUR_TROP_GRANDE,
}

/** Champs du formulaire planteur pouvant porter une erreur. */
enum class ChampPlanteur { CODE, NOM, PRENOM, LOCALITE, DATE_NAISSANCE }

/** Champs du formulaire pesée pouvant porter une erreur. */
enum class ChampPesee { PLANTEUR, DATE, POIDS }
