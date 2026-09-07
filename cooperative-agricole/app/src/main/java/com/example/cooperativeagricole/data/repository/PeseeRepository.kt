package com.example.cooperativeagricole.data.repository

import com.example.cooperativeagricole.data.local.dao.PeseeDao
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.PeseeAvecPlanteur
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

/**
 * Repository des pesées.
 *
 * Il rend aussi les agrégats directement exploitables par l'interface : les
 * `SUM` de SQLite valent NULL quand il n'y a aucune ligne, ce qui n'a pas de
 * sens à afficher. La conversion en 0.0 est faite ici, une seule fois, plutôt
 * que répétée dans chaque écran.
 */
class PeseeRepository(private val peseeDao: PeseeDao) {

    val toutesAvecPlanteur: Flow<List<PeseeAvecPlanteur>> = peseeDao.listerToutesAvecPlanteur()

    val nombre: Flow<Int> = peseeDao.compter()

    val poidsTotal: Flow<Double> = peseeDao.poidsTotal().map { it ?: 0.0 }

    fun listerParPlanteur(code: String): Flow<List<Pesee>> = peseeDao.listerParPlanteur(code)

    fun nombreParPlanteur(code: String): Flow<Int> = peseeDao.compterParPlanteur(code)

    fun poidsTotalParPlanteur(code: String): Flow<Double> =
        peseeDao.poidsTotalParPlanteur(code).map { it ?: 0.0 }

    fun dateDernierePesee(code: String): Flow<Long?> = peseeDao.dateDernierePeseeParPlanteur(code)

    suspend fun trouver(id: Long): Pesee? = peseeDao.trouverParId(id)

    suspend fun inserer(pesee: Pesee): Long = peseeDao.inserer(pesee)

    suspend fun modifier(pesee: Pesee) = peseeDao.modifier(pesee)

    suspend fun supprimer(pesee: Pesee) = peseeDao.supprimer(pesee)
}
