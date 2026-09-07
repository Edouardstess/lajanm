package com.example.cooperativeagricole.data.repository

import com.example.cooperativeagricole.data.local.dao.PlanteurDao
import com.example.cooperativeagricole.data.local.entity.Planteur
import kotlinx.coroutines.flow.Flow

/**
 * Repository des planteurs.
 *
 * Rôle de cette couche :
 *  - centraliser l'accès aux données des planteurs ;
 *  - parler au DAO, et à lui seul ;
 *  - fournir aux ViewModels une interface qui ne dépend pas de Room.
 *
 * Concrètement, le ViewModel ignore d'où viennent les données. Si demain la
 * coopérative ajoutait un serveur, seul ce fichier changerait : ni les
 * ViewModels ni les écrans n'auraient à être touchés.
 */
class PlanteurRepository(private val planteurDao: PlanteurDao) {

    val tous: Flow<List<Planteur>> = planteurDao.listerTous()

    val nombre: Flow<Int> = planteurDao.compter()

    fun rechercher(recherche: String): Flow<List<Planteur>> =
        planteurDao.rechercher(recherche.trim())

    fun observer(code: String): Flow<Planteur?> = planteurDao.observerParCode(code)

    suspend fun trouver(code: String): Planteur? = planteurDao.trouverParCode(code)

    suspend fun existe(code: String): Boolean = planteurDao.existe(code)

    suspend fun compterMaintenant(): Int = planteurDao.compterMaintenant()

    suspend fun inserer(planteur: Planteur) = planteurDao.inserer(planteur)

    suspend fun modifier(planteur: Planteur) = planteurDao.modifier(planteur)

    /** Les pesées du planteur suivent, par cascade de la clé étrangère. */
    suspend fun supprimer(planteur: Planteur) = planteurDao.supprimer(planteur)
}
