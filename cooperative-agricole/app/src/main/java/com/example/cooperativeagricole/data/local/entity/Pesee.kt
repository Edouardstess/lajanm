package com.example.cooperativeagricole.data.local.entity

import androidx.room.Entity
import androidx.room.ForeignKey
import androidx.room.Index
import androidx.room.PrimaryKey

/**
 * Entité Pesee : une livraison pesée par un planteur à la coopérative.
 *
 * La relation avec [Planteur] est une association « un planteur possède
 * plusieurs pesées ». Elle est traduite par une clé étrangère sur
 * `planteurCode` :
 *  - `onDelete = CASCADE` : supprimer un planteur supprime ses pesées, ce qui
 *    interdit les pesées orphelines ;
 *  - `onUpdate = CASCADE` : si le code d'un planteur change, ses pesées
 *    suivent.
 *
 * L'index sur `planteurCode` est nécessaire : sans lui Room avertit à la
 * compilation que les requêtes filtrant sur la clé étrangère seront lentes.
 */
@Entity(
    tableName = "pesees",
    foreignKeys = [
        ForeignKey(
            entity = Planteur::class,
            parentColumns = ["code"],
            childColumns = ["planteurCode"],
            onDelete = ForeignKey.CASCADE,
            onUpdate = ForeignKey.CASCADE,
        )
    ],
    indices = [Index(value = ["planteurCode"]), Index(value = ["datePesee"])]
)
data class Pesee(
    @PrimaryKey(autoGenerate = true)
    val id: Long = 0L,
    /** Code du planteur auquel la pesée est obligatoirement rattachée. */
    val planteurCode: String,
    /** Date de la pesée en millisecondes depuis l'epoch (UTC). */
    val datePesee: Long,
    /** Poids pesé, en kilogrammes. */
    val poidsKg: Double,
    /** Observation libre (état du produit, remarque du peseur...). */
    val observation: String? = null,
)
