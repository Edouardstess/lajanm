package com.example.cooperativeagricole.data.local.entity

import androidx.room.Embedded

/**
 * Résultat d'une jointure entre `pesees` et `planteurs`.
 *
 * La liste générale des pesées doit afficher le nom du planteur : plutôt que
 * de faire une requête par ligne depuis l'interface, la jointure est faite
 * une seule fois par la base. `@Embedded` demande à Room de reconstruire
 * l'objet [Pesee] à partir des colonnes de `pesees` présentes dans le
 * résultat, les autres colonnes alimentant les champs restants.
 */
data class PeseeAvecPlanteur(
    @Embedded val pesee: Pesee,
    val planteurNom: String,
    val planteurPrenom: String,
    val planteurLocalite: String,
) {
    val nomCompletPlanteur: String
        get() = "$planteurPrenom $planteurNom"
}
