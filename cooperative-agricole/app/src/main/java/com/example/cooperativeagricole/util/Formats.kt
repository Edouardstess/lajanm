package com.example.cooperativeagricole.util

import java.text.NumberFormat
import java.util.Locale

/** Mise en forme des nombres affichés (poids en kilogrammes). */
object Formats {

    private val POIDS: NumberFormat = NumberFormat.getNumberInstance(Locale.FRANCE).apply {
        minimumFractionDigits = 1
        maximumFractionDigits = 2
    }

    /** Ex. : 1 234,5 — l'unité est ajoutée par la ressource de texte. */
    fun poids(kilogrammes: Double): String = POIDS.format(kilogrammes)

    /** Convertit une saisie utilisateur ("12,5" ou "12.5") en nombre. */
    fun versNombre(saisie: String): Double? =
        saisie.trim().replace(',', '.').toDoubleOrNull()
}
