package com.example.cooperativeagricole.domain.validation

import com.example.cooperativeagricole.util.Dates
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Tests unitaires des règles de saisie d'un planteur.
 *
 * Ils tournent sur la JVM, sans émulateur : c'est précisément l'intérêt
 * d'avoir sorti ces règles de l'Activity.
 */
class ValidationPlanteurTest {

    private fun saisie(
        code: String = "PL-001",
        nom: String = "Dorvilus",
        prenom: String = "Jean Robert",
        localite: String = "Furcy",
        dateNaissance: Long? = Dates.de(1990, 5, 20),
    ) = SaisiePlanteur(code, nom, prenom, localite, dateNaissance)

    @Test
    fun `une saisie complete est valide`() {
        assertTrue(ValidationPlanteur.valider(saisie()).isEmpty())
    }

    @Test
    fun `le code est obligatoire`() {
        val erreurs = ValidationPlanteur.valider(saisie(code = "   "))
        assertEquals(MotifInvalidite.OBLIGATOIRE, erreurs[ChampPlanteur.CODE])
    }

    @Test
    fun `le code trop court est refuse`() {
        val erreurs = ValidationPlanteur.valider(saisie(code = "PL"))
        assertEquals(MotifInvalidite.TROP_COURT, erreurs[ChampPlanteur.CODE])
    }

    @Test
    fun `le code n accepte ni espace ni accent`() {
        assertEquals(
            MotifInvalidite.FORMAT_INVALIDE,
            ValidationPlanteur.valider(saisie(code = "PL 001"))[ChampPlanteur.CODE],
        )
        assertEquals(
            MotifInvalidite.FORMAT_INVALIDE,
            ValidationPlanteur.valider(saisie(code = "PLÉ01"))[ChampPlanteur.CODE],
        )
    }

    @Test
    fun `les espaces autour du nom ne le rendent pas valide`() {
        val erreurs = ValidationPlanteur.valider(saisie(nom = "   "))
        assertEquals(MotifInvalidite.OBLIGATOIRE, erreurs[ChampPlanteur.NOM])
    }

    @Test
    fun `la localite est obligatoire`() {
        val erreurs = ValidationPlanteur.valider(saisie(localite = ""))
        assertEquals(MotifInvalidite.OBLIGATOIRE, erreurs[ChampPlanteur.LOCALITE])
    }

    @Test
    fun `une date de naissance absente est refusee`() {
        val erreurs = ValidationPlanteur.valider(saisie(dateNaissance = null))
        assertEquals(MotifInvalidite.OBLIGATOIRE, erreurs[ChampPlanteur.DATE_NAISSANCE])
    }

    @Test
    fun `une date de naissance future est refusee`() {
        val demain = Dates.aujourdHui() + 24 * 60 * 60 * 1000L
        val erreurs = ValidationPlanteur.valider(saisie(dateNaissance = demain))
        assertEquals(MotifInvalidite.DATE_FUTURE, erreurs[ChampPlanteur.DATE_NAISSANCE])
    }

    @Test
    fun `un age trop faible est refuse`() {
        val recent = Dates.ilYAJours(365 * 5)
        val erreurs = ValidationPlanteur.valider(saisie(dateNaissance = recent))
        assertEquals(MotifInvalidite.AGE_HORS_LIMITES, erreurs[ChampPlanteur.DATE_NAISSANCE])
    }

    @Test
    fun `plusieurs champs invalides sont tous signales`() {
        val erreurs = ValidationPlanteur.valider(saisie(code = "", nom = "", localite = ""))
        assertEquals(3, erreurs.size)
    }
}
