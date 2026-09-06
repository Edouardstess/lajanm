package com.example.cooperativeagricole.domain.validation

import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.Formats

/** Valeurs brutes saisies dans le formulaire pesée. */
data class SaisiePesee(
    val planteurCode: String?,
    val date: Long?,
    val poidsSaisi: String,
    val observation: String?,
)

/**
 * Règles de validation du formulaire pesée.
 *
 * La règle la plus importante est l'association obligatoire à un planteur :
 * elle est vérifiée ici, mais aussi garantie par la clé étrangère de la table
 * `pesees`. Une contrainte fonctionnelle de ce niveau mérite d'être tenue
 * autant par l'interface que par la base.
 */
object ValidationPesee {

    const val POIDS_MAXIMUM_KG = 10_000.0

    fun valider(saisie: SaisiePesee): Map<ChampPesee, MotifInvalidite> {
        val erreurs = mutableMapOf<ChampPesee, MotifInvalidite>()

        if (saisie.planteurCode.isNullOrBlank()) {
            erreurs[ChampPesee.PLANTEUR] = MotifInvalidite.OBLIGATOIRE
        }

        val date = saisie.date
        if (date == null) {
            erreurs[ChampPesee.DATE] = MotifInvalidite.OBLIGATOIRE
        } else if (date > Dates.aujourdHui()) {
            erreurs[ChampPesee.DATE] = MotifInvalidite.DATE_FUTURE
        }

        val poidsSaisi = saisie.poidsSaisi.trim()
        if (poidsSaisi.isEmpty()) {
            erreurs[ChampPesee.POIDS] = MotifInvalidite.OBLIGATOIRE
        } else {
            val poids = Formats.versNombre(poidsSaisi)
            when {
                poids == null || poids.isNaN() -> erreurs[ChampPesee.POIDS] = MotifInvalidite.NOMBRE_INVALIDE
                poids <= 0.0 -> erreurs[ChampPesee.POIDS] = MotifInvalidite.VALEUR_NON_POSITIVE
                poids > POIDS_MAXIMUM_KG -> erreurs[ChampPesee.POIDS] = MotifInvalidite.VALEUR_TROP_GRANDE
            }
        }

        return erreurs
    }
}
