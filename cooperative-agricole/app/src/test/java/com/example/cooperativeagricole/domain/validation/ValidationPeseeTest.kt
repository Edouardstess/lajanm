package com.example.cooperativeagricole.domain.validation

import com.example.cooperativeagricole.util.Dates
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/** Tests unitaires des règles de saisie d'une pesée. */
class ValidationPeseeTest {

    private fun saisie(
        planteurCode: String? = "PL-001",
        date: Long? = Dates.aujourdHui(),
        poidsSaisi: String = "42,5",
        observation: String? = null,
    ) = SaisiePesee(planteurCode, date, poidsSaisi, observation)

    @Test
    fun `une saisie complete est valide`() {
        assertTrue(ValidationPesee.valider(saisie()).isEmpty())
    }

    @Test
    fun `la virgule decimale est acceptee comme le point`() {
        assertTrue(ValidationPesee.valider(saisie(poidsSaisi = "42.5")).isEmpty())
        assertTrue(ValidationPesee.valider(saisie(poidsSaisi = "42,5")).isEmpty())
    }

    @Test
    fun `une pesee sans planteur est refusee`() {
        val erreurs = ValidationPesee.valider(saisie(planteurCode = null))
        assertEquals(MotifInvalidite.OBLIGATOIRE, erreurs[ChampPesee.PLANTEUR])
    }

    @Test
    fun `un poids non numerique est refuse`() {
        val erreurs = ValidationPesee.valider(saisie(poidsSaisi = "quarante"))
        assertEquals(MotifInvalidite.NOMBRE_INVALIDE, erreurs[ChampPesee.POIDS])
    }

    @Test
    fun `un poids nul ou negatif est refuse`() {
        assertEquals(
            MotifInvalidite.VALEUR_NON_POSITIVE,
            ValidationPesee.valider(saisie(poidsSaisi = "0"))[ChampPesee.POIDS],
        )
        assertEquals(
            MotifInvalidite.VALEUR_NON_POSITIVE,
            ValidationPesee.valider(saisie(poidsSaisi = "-3"))[ChampPesee.POIDS],
        )
    }

    @Test
    fun `un poids demesure est refuse`() {
        val erreurs = ValidationPesee.valider(saisie(poidsSaisi = "99999"))
        assertEquals(MotifInvalidite.VALEUR_TROP_GRANDE, erreurs[ChampPesee.POIDS])
    }

    @Test
    fun `une pesee datee dans le futur est refusee`() {
        val demain = Dates.aujourdHui() + 24 * 60 * 60 * 1000L
        val erreurs = ValidationPesee.valider(saisie(date = demain))
        assertEquals(MotifInvalidite.DATE_FUTURE, erreurs[ChampPesee.DATE])
    }
}
