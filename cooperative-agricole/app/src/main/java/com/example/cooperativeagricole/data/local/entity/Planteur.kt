package com.example.cooperativeagricole.data.local.entity

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

/**
 * Sexe d'un planteur. Stocké en base sous forme de texte ("M" / "F") par le
 * convertisseur [com.example.cooperativeagricole.data.local.database.Convertisseurs] :
 * un type énuméré évite les valeurs incohérentes que laisserait passer une
 * simple chaîne de caractères.
 */
enum class Sexe(val code: String) {
    MASCULIN("M"),
    FEMININ("F");

    companion object {
        fun depuisCode(code: String): Sexe =
            entries.firstOrNull { it.code == code } ?: MASCULIN
    }
}

/**
 * Entité Planteur : un membre de la coopérative qui apporte sa récolte.
 *
 * Le `code` est l'identifiant fourni par le modèle conceptuel de données ;
 * il sert directement de clé primaire, ce qui garantit son unicité au niveau
 * de la base (contrainte fonctionnelle « unicité de l'identifiant du
 * planteur ») et non seulement dans le code de l'application.
 */
@Entity(
    tableName = "planteurs",
    indices = [Index(value = ["nom", "prenom"]), Index(value = ["localite"])]
)
data class Planteur(
    @PrimaryKey
    val code: String,
    val nom: String,
    val prenom: String,
    val sexe: Sexe,
    /** Date de naissance en millisecondes depuis l'epoch (UTC). */
    val dateNaissance: Long,
    val localite: String,
) {
    /**
     * Propriété calculée : elle n'a pas de champ associé, Room ne la stocke
     * donc pas en colonne. Elle évite de répéter la concaténation dans
     * chaque écran.
     */
    val nomComplet: String
        get() = "$prenom $nom"
}
