package com.example.cooperativeagricole.data.local.database

import androidx.room.TypeConverter
import com.example.cooperativeagricole.data.local.entity.Sexe

/**
 * SQLite ne connaît que quelques types primitifs. Les convertisseurs
 * indiquent à Room comment ranger un type métier dans une colonne et comment
 * le reconstruire à la lecture.
 *
 * Les dates, elles, sont déjà des `Long` (millisecondes depuis l'epoch) dans
 * les entités : elles se stockent telles quelles, se trient correctement en
 * SQL et n'ont donc besoin d'aucun convertisseur.
 */
class Convertisseurs {

    @TypeConverter
    fun sexeVersTexte(sexe: Sexe): String = sexe.code

    @TypeConverter
    fun texteVersSexe(code: String): Sexe = Sexe.depuisCode(code)
}
