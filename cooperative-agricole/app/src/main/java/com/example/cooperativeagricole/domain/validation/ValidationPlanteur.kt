package com.example.cooperativeagricole.domain.validation

import com.example.cooperativeagricole.util.Dates

/** Valeurs brutes saisies dans le formulaire planteur. */
data class SaisiePlanteur(
    val code: String,
    val nom: String,
    val prenom: String,
    val localite: String,
    val dateNaissance: Long?,
)

/**
 * Règles de validation du formulaire planteur.
 *
 * Volontairement sans dépendance à Android : ces règles sont couvertes par
 * des tests unitaires JVM (`ValidationPlanteurTest`).
 */
object ValidationPlanteur {

    const val LONGUEUR_MIN_CODE = 3
    const val LONGUEUR_MIN_NOM = 2
    const val AGE_MINIMUM = 15
    const val AGE_MAXIMUM = 110

    /** Lettres, chiffres, tiret et souligné : ni espace ni accent. */
    private val FORMAT_CODE = Regex("^[A-Za-z0-9_-]+$")

    /**
     * @return les champs en erreur ; une map vide signifie « saisie valide ».
     */
    fun valider(saisie: SaisiePlanteur): Map<ChampPlanteur, MotifInvalidite> {
        val erreurs = mutableMapOf<ChampPlanteur, MotifInvalidite>()

        val code = saisie.code.trim()
        when {
            code.isEmpty() -> erreurs[ChampPlanteur.CODE] = MotifInvalidite.OBLIGATOIRE
            code.length < LONGUEUR_MIN_CODE -> erreurs[ChampPlanteur.CODE] = MotifInvalidite.TROP_COURT
            !FORMAT_CODE.matches(code) -> erreurs[ChampPlanteur.CODE] = MotifInvalidite.FORMAT_INVALIDE
        }

        val nom = saisie.nom.trim()
        when {
            nom.isEmpty() -> erreurs[ChampPlanteur.NOM] = MotifInvalidite.OBLIGATOIRE
            nom.length < LONGUEUR_MIN_NOM -> erreurs[ChampPlanteur.NOM] = MotifInvalidite.TROP_COURT
        }

        val prenom = saisie.prenom.trim()
        when {
            prenom.isEmpty() -> erreurs[ChampPlanteur.PRENOM] = MotifInvalidite.OBLIGATOIRE
            prenom.length < LONGUEUR_MIN_NOM -> erreurs[ChampPlanteur.PRENOM] = MotifInvalidite.TROP_COURT
        }

        if (saisie.localite.trim().isEmpty()) {
            erreurs[ChampPlanteur.LOCALITE] = MotifInvalidite.OBLIGATOIRE
        }

        val naissance = saisie.dateNaissance
        if (naissance == null) {
            erreurs[ChampPlanteur.DATE_NAISSANCE] = MotifInvalidite.OBLIGATOIRE
        } else if (naissance > Dates.aujourdHui()) {
            erreurs[ChampPlanteur.DATE_NAISSANCE] = MotifInvalidite.DATE_FUTURE
        } else {
            val age = Dates.anneesDepuis(naissance)
            if (age < AGE_MINIMUM || age > AGE_MAXIMUM) {
                erreurs[ChampPlanteur.DATE_NAISSANCE] = MotifInvalidite.AGE_HORS_LIMITES
            }
        }

        return erreurs
    }
}
