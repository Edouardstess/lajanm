package com.example.cooperativeagricole.data.repository

import com.example.cooperativeagricole.data.local.dao.PlanteurDao
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.remote.SourceDistante
import kotlinx.coroutines.flow.Flow

/**
 * Repository des planteurs.
 *
 * Rôle de cette couche :
 *  - centraliser l'accès aux données des planteurs ;
 *  - parler au DAO, et à lui seul ;
 *  - fournir aux ViewModels une interface qui ne dépend pas de Room.
 *
 * Concrètement, le ViewModel ignore d'où viennent les données — c'est ce qui a
 * permis d'ajouter la base distante Firestore sans toucher une seule ligne des
 * ViewModels ni des écrans : seules les écritures de ce fichier ont changé.
 *
 * Sens de circulation :
 *  - **lectures** : toujours depuis Room, y compris quand Firebase est actif.
 *    L'application reste ainsi utilisable hors réseau, et l'interface n'attend
 *    jamais le serveur ;
 *  - **écritures** : d'abord Room, puis la base distante. Si [sourceDistante]
 *    est `null` (aucune configuration Firebase), l'application travaille en
 *    local seul, exactement comme avant.
 */
class PlanteurRepository(
    private val planteurDao: PlanteurDao,
    private val sourceDistante: SourceDistante? = null,
) {

    val tous: Flow<List<Planteur>> = planteurDao.listerTous()

    val nombre: Flow<Int> = planteurDao.compter()

    fun rechercher(recherche: String): Flow<List<Planteur>> =
        planteurDao.rechercher(recherche.trim())

    fun observer(code: String): Flow<Planteur?> = planteurDao.observerParCode(code)

    suspend fun trouver(code: String): Planteur? = planteurDao.trouverParCode(code)

    suspend fun existe(code: String): Boolean = planteurDao.existe(code)

    suspend fun compterMaintenant(): Int = planteurDao.compterMaintenant()

    suspend fun inserer(planteur: Planteur) {
        planteurDao.inserer(planteur)
        sourceDistante?.enregistrer(planteur)
    }

    suspend fun modifier(planteur: Planteur) {
        planteurDao.modifier(planteur)
        sourceDistante?.enregistrer(planteur)
    }

    suspend fun supprimer(planteur: Planteur) {
        planteurDao.supprimer(planteur)
        // Localement, SQLite supprime les pesées par cascade ; côté distant,
        // SourceDistante s'en charge explicitement.
        sourceDistante?.supprimerPlanteur(planteur.code)
    }
}
