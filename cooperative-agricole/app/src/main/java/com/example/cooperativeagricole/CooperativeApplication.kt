package com.example.cooperativeagricole

import android.app.Application
import android.content.Context
import com.example.cooperativeagricole.data.local.database.CooperativeDatabase
import com.example.cooperativeagricole.data.repository.PeseeRepository
import com.example.cooperativeagricole.data.repository.PlanteurRepository

/**
 * Conteneur de dépendances de l'application.
 *
 * L'application construit ici, une fois pour toutes, la base et les
 * repositories, puis les fournit aux ViewModels via leurs fabriques. C'est de
 * l'injection de dépendances « à la main » : suffisante à cette échelle,
 * elle rend explicite ce que chaque couche reçoit, sans ajouter Hilt ou Koin.
 *
 * `lazy` retarde l'ouverture de la base jusqu'au premier accès réel, pour ne
 * pas ralentir le démarrage de l'application.
 */
class ConteneurApplication(contexte: Context) {

    private val base: CooperativeDatabase by lazy { CooperativeDatabase.obtenir(contexte) }

    val planteurRepository: PlanteurRepository by lazy { PlanteurRepository(base.planteurDao()) }

    val peseeRepository: PeseeRepository by lazy { PeseeRepository(base.peseeDao()) }
}

class CooperativeApplication : Application() {

    lateinit var conteneur: ConteneurApplication
        private set

    override fun onCreate() {
        super.onCreate()
        conteneur = ConteneurApplication(this)
    }
}

/** Raccourci utilisé par les activités pour atteindre le conteneur. */
val Context.conteneur: ConteneurApplication
    get() = (applicationContext as CooperativeApplication).conteneur
