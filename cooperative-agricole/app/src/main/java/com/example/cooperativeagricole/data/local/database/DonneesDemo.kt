package com.example.cooperativeagricole.data.local.database

import com.example.cooperativeagricole.data.local.dao.PeseeDao
import com.example.cooperativeagricole.data.local.dao.PlanteurDao
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.local.entity.Sexe
import com.example.cooperativeagricole.util.Dates

/**
 * Jeu de données de démonstration inséré à la première ouverture de
 * l'application : 10 planteurs et une trentaine de pesées, en quantité
 * suffisante pour montrer les listes, la recherche, les agrégats (nombre de
 * pesées, poids total) et le cas d'un planteur sans aucune pesée.
 */
object DonneesDemo {

    private val PLANTEURS = listOf(
        Planteur("PL-001", "Jean-Baptiste", "Marie Carmelle", Sexe.FEMININ, Dates.de(1985, 3, 12), "Kenscoff"),
        Planteur("PL-002", "Dorvilus", "Jean Robert", Sexe.MASCULIN, Dates.de(1978, 11, 2), "Furcy"),
        Planteur("PL-003", "Pierre-Louis", "Anténor", Sexe.MASCULIN, Dates.de(1969, 6, 25), "Seguin"),
        Planteur("PL-004", "Célestin", "Rose Micheline", Sexe.FEMININ, Dates.de(1990, 1, 30), "Kenscoff"),
        Planteur("PL-005", "Augustin", "Wilner", Sexe.MASCULIN, Dates.de(1982, 9, 8), "Marbial"),
        Planteur("PL-006", "Saint-Fleur", "Guerdine", Sexe.FEMININ, Dates.de(1995, 4, 17), "Furcy"),
        Planteur("PL-007", "Charles", "Fritznel", Sexe.MASCULIN, Dates.de(1973, 12, 5), "Seguin"),
        Planteur("PL-008", "Noël", "Yvrose", Sexe.FEMININ, Dates.de(1988, 7, 21), "Belle-Fontaine"),
        Planteur("PL-009", "Étienne", "Dieuseul", Sexe.MASCULIN, Dates.de(1965, 2, 14), "Marbial"),
        // Volontairement sans pesée : montre l'état vide de l'écran de détail.
        Planteur("PL-010", "Lafleur", "Nadège", Sexe.FEMININ, Dates.de(1998, 10, 3), "Belle-Fontaine"),
    )

    private val PESEES = listOf(
        Pesee(planteurCode = "PL-001", datePesee = Dates.ilYAJours(2), poidsKg = 42.5, observation = "Café en parche, bien séché"),
        Pesee(planteurCode = "PL-001", datePesee = Dates.ilYAJours(9), poidsKg = 38.0, observation = null),
        Pesee(planteurCode = "PL-001", datePesee = Dates.ilYAJours(23), poidsKg = 51.25, observation = "Deuxième récolte"),
        Pesee(planteurCode = "PL-002", datePesee = Dates.ilYAJours(1), poidsKg = 27.75, observation = null),
        Pesee(planteurCode = "PL-002", datePesee = Dates.ilYAJours(14), poidsKg = 33.5, observation = "Humidité un peu élevée"),
        Pesee(planteurCode = "PL-003", datePesee = Dates.ilYAJours(3), poidsKg = 64.0, observation = "Cacao fermenté"),
        Pesee(planteurCode = "PL-003", datePesee = Dates.ilYAJours(11), poidsKg = 58.5, observation = null),
        Pesee(planteurCode = "PL-003", datePesee = Dates.ilYAJours(28), poidsKg = 70.25, observation = "Grosse livraison"),
        Pesee(planteurCode = "PL-004", datePesee = Dates.ilYAJours(4), poidsKg = 19.5, observation = null),
        Pesee(planteurCode = "PL-004", datePesee = Dates.ilYAJours(18), poidsKg = 22.0, observation = null),
        Pesee(planteurCode = "PL-005", datePesee = Dates.ilYAJours(0), poidsKg = 45.0, observation = "Pesée du jour"),
        Pesee(planteurCode = "PL-005", datePesee = Dates.ilYAJours(7), poidsKg = 40.75, observation = null),
        Pesee(planteurCode = "PL-005", datePesee = Dates.ilYAJours(21), poidsKg = 47.5, observation = "Sacs de 25 kg"),
        Pesee(planteurCode = "PL-006", datePesee = Dates.ilYAJours(5), poidsKg = 31.0, observation = null),
        Pesee(planteurCode = "PL-006", datePesee = Dates.ilYAJours(16), poidsKg = 29.25, observation = "Tri à refaire"),
        Pesee(planteurCode = "PL-007", datePesee = Dates.ilYAJours(2), poidsKg = 55.5, observation = null),
        Pesee(planteurCode = "PL-007", datePesee = Dates.ilYAJours(13), poidsKg = 61.0, observation = "Qualité export"),
        Pesee(planteurCode = "PL-007", datePesee = Dates.ilYAJours(30), poidsKg = 49.75, observation = null),
        Pesee(planteurCode = "PL-008", datePesee = Dates.ilYAJours(6), poidsKg = 24.0, observation = null),
        Pesee(planteurCode = "PL-008", datePesee = Dates.ilYAJours(19), poidsKg = 26.5, observation = "Livraison partielle"),
        Pesee(planteurCode = "PL-008", datePesee = Dates.ilYAJours(35), poidsKg = 30.0, observation = null),
        Pesee(planteurCode = "PL-009", datePesee = Dates.ilYAJours(8), poidsKg = 72.5, observation = "Meilleur rendement du mois"),
        Pesee(planteurCode = "PL-009", datePesee = Dates.ilYAJours(24), poidsKg = 68.0, observation = null),
    )

    suspend fun remplir(planteurDao: PlanteurDao, peseeDao: PeseeDao) {
        PLANTEURS.forEach { planteurDao.inserer(it) }
        // Les pesées sont insérées après les planteurs : la clé étrangère
        // exige que le planteur référencé existe déjà.
        PESEES.forEach { peseeDao.inserer(it) }
    }
}
