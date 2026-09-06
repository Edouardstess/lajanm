package com.example.cooperativeagricole.data.local.database

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import androidx.room.TypeConverters
import androidx.sqlite.db.SupportSQLiteDatabase
import com.example.cooperativeagricole.data.local.dao.PeseeDao
import com.example.cooperativeagricole.data.local.dao.PlanteurDao
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.Planteur

/**
 * Base de données Room de l'application.
 *
 * C'est le point d'entrée de la persistance : elle déclare les tables
 * (`entities`), la version du schéma, les convertisseurs de types, et expose
 * un DAO par table. Room génère l'implémentation à la compilation.
 *
 * L'instance est unique pour toute l'application (patron singleton) : ouvrir
 * plusieurs fois le même fichier SQLite gaspillerait de la mémoire et
 * risquerait des accès concurrents non coordonnés.
 */
@Database(
    entities = [Planteur::class, Pesee::class],
    version = 1,
    exportSchema = true,
)
@TypeConverters(Convertisseurs::class)
abstract class CooperativeDatabase : RoomDatabase() {

    abstract fun planteurDao(): PlanteurDao

    abstract fun peseeDao(): PeseeDao

    companion object {
        private const val NOM_FICHIER = "cooperative.db"

        @Volatile
        private var instance: CooperativeDatabase? = null

        fun obtenir(context: Context): CooperativeDatabase =
            instance ?: synchronized(this) {
                instance ?: construire(context).also { instance = it }
            }

        private fun construire(context: Context): CooperativeDatabase =
            Room.databaseBuilder(
                context.applicationContext,
                CooperativeDatabase::class.java,
                NOM_FICHIER,
            )
                // Le jeu de démonstration n'est inséré qu'à la création du
                // fichier, donc une seule fois par installation.
                .addCallback(RappelDeCreation())
                .build()

        /**
         * Insère les données de démonstration au moment même où les tables
         * viennent d'être créées, dans la transaction de création.
         *
         * Le remplissage est volontairement **synchrone** : le confier à une
         * coroutine laisserait la base vide pendant quelques instants, et la
         * synchronisation distante, qui démarre au lancement, prendrait ce vide
         * pour l'état réel de l'appareil.
         */
        private class RappelDeCreation : RoomDatabase.Callback() {
            override fun onCreate(db: SupportSQLiteDatabase) {
                super.onCreate(db)
                DonneesDemo.remplir(db)
            }
        }
    }
}
