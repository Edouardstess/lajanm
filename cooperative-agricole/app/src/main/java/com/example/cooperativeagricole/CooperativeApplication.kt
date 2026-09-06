package com.example.cooperativeagricole

import android.app.Application
import android.content.Context
import com.example.cooperativeagricole.data.local.database.CooperativeDatabase
import com.example.cooperativeagricole.data.remote.ConnexionFirebase
import com.example.cooperativeagricole.data.remote.EtatSynchronisation
import com.example.cooperativeagricole.data.remote.SourceDistante
import com.example.cooperativeagricole.data.repository.PeseeRepository
import com.example.cooperativeagricole.data.repository.PlanteurRepository
import com.example.cooperativeagricole.data.sync.SynchronisationCooperative
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

/**
 * Conteneur de dépendances de l'application.
 *
 * L'application construit ici, une fois pour toutes, la base locale, la source
 * distante et les repositories, puis les fournit aux ViewModels via leurs
 * fabriques. C'est de l'injection de dépendances « à la main » : suffisante à
 * cette échelle, elle rend explicite ce que chaque couche reçoit, sans ajouter
 * Hilt ou Koin.
 *
 * `lazy` retarde l'ouverture de la base et l'initialisation de Firebase
 * jusqu'au premier accès réel, pour ne pas ralentir le démarrage.
 */
class ConteneurApplication(
    private val contexte: Context,
    private val portee: CoroutineScope,
) {

    private val base: CooperativeDatabase by lazy { CooperativeDatabase.obtenir(contexte) }

    /** `null` quand aucun `google-services.json` n'est fourni : local seul. */
    private val sourceDistante: SourceDistante? by lazy {
        ConnexionFirebase.creerSourceDistante(contexte)
    }

    val planteurRepository: PlanteurRepository by lazy {
        PlanteurRepository(base.planteurDao(), sourceDistante)
    }

    val peseeRepository: PeseeRepository by lazy {
        PeseeRepository(base.peseeDao(), sourceDistante)
    }

    /** État affiché par l'écran d'accueil. Constant si Firebase est absent. */
    val etatSynchronisation: StateFlow<EtatSynchronisation> by lazy {
        sourceDistante?.etat ?: MutableStateFlow(EtatSynchronisation.DESACTIVEE)
    }

    /**
     * Ouvre l'écoute de la base distante. Sans configuration Firebase, ne fait
     * rien — et l'application reste pleinement fonctionnelle.
     */
    fun demarrerSynchronisation() {
        val source = sourceDistante ?: return
        SynchronisationCooperative(base, source, portee).demarrer()
    }
}

class CooperativeApplication : Application() {

    /**
     * Portée de vie de l'application : la synchronisation doit continuer quand
     * l'utilisateur passe d'un écran à l'autre, elle ne peut donc pas dépendre
     * du cycle de vie d'une Activity ni d'un ViewModel.
     */
    private val portee = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    lateinit var conteneur: ConteneurApplication
        private set

    override fun onCreate() {
        super.onCreate()
        conteneur = ConteneurApplication(this, portee)
        conteneur.demarrerSynchronisation()
    }
}

/** Raccourci utilisé par les activités pour atteindre le conteneur. */
val Context.conteneur: ConteneurApplication
    get() = (applicationContext as CooperativeApplication).conteneur
