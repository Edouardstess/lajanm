package com.example.cooperativeagricole.data.local.entity

import androidx.room.Entity
import androidx.room.ForeignKey
import androidx.room.Index
import androidx.room.PrimaryKey
import java.util.UUID

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
    indices = [
        Index(value = ["planteurCode"]),
        Index(value = ["datePesee"]),
        Index(value = ["cleDistante"], unique = true),
    ]
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
    /**
     * Identifiant stable de la pesée dans la base distante.
     *
     * `id` est auto-incrémenté par SQLite : deux téléphones attribueraient le
     * même `id` à deux pesées différentes. Cette clé, tirée au hasard à la
     * création, identifie donc la pesée d'un appareil à l'autre ; `id` reste
     * l'identifiant local, celui du MCD.
     */
    val cleDistante: String = UUID.randomUUID().toString(),
)
