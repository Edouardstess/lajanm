package com.example.cooperativeagricole.util

import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/**
 * Utilitaires de dates.
 *
 * `java.time` n'est disponible qu'à partir d'Android 8 (API 26) alors que
 * l'application vise API 24 : on s'en tient donc à `Calendar` et
 * `SimpleDateFormat`, disponibles sur toutes les versions supportées.
 */
object Dates {

    private val FORMAT_COURT = SimpleDateFormat("dd/MM/yyyy", Locale.FRANCE)
    private val FORMAT_LONG = SimpleDateFormat("d MMMM yyyy", Locale.FRANCE)

    /** Millisecondes correspondant à une date civile, à minuit, mois de 1 à 12. */
    fun de(annee: Int, mois: Int, jour: Int): Long = calendrier().apply {
        set(Calendar.YEAR, annee)
        set(Calendar.MONTH, mois - 1)
        set(Calendar.DAY_OF_MONTH, jour)
    }.timeInMillis

    /** Aujourd'hui à minuit : deux pesées du même jour ont ainsi la même date. */
    fun aujourdHui(): Long = calendrier().timeInMillis

    fun ilYAJours(jours: Int): Long = calendrier().apply {
        add(Calendar.DAY_OF_MONTH, -jours)
    }.timeInMillis

    fun formater(millis: Long): String = FORMAT_COURT.format(Date(millis))

    fun formaterLong(millis: Long): String = FORMAT_LONG.format(Date(millis))

    /** Décompose une date pour pré-remplir un sélecteur de date. */
    fun composantes(millis: Long): Triple<Int, Int, Int> {
        val calendrier = Calendar.getInstance().apply { timeInMillis = millis }
        return Triple(
            calendrier.get(Calendar.YEAR),
            calendrier.get(Calendar.MONTH),
            calendrier.get(Calendar.DAY_OF_MONTH),
        )
    }

    fun anneesDepuis(millis: Long): Int {
        val naissance = Calendar.getInstance().apply { timeInMillis = millis }
        val maintenant = Calendar.getInstance()
        var age = maintenant.get(Calendar.YEAR) - naissance.get(Calendar.YEAR)
        if (maintenant.get(Calendar.DAY_OF_YEAR) < naissance.get(Calendar.DAY_OF_YEAR)) age--
        return age
    }

    /** Calendrier positionné sur aujourd'hui à 00:00:00.000. */
    private fun calendrier(): Calendar = Calendar.getInstance().apply {
        set(Calendar.HOUR_OF_DAY, 0)
        set(Calendar.MINUTE, 0)
        set(Calendar.SECOND, 0)
        set(Calendar.MILLISECOND, 0)
    }
}
