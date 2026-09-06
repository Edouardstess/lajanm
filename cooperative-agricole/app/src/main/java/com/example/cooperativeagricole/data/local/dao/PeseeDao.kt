package com.example.cooperativeagricole.data.local.dao

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update
import androidx.room.Upsert
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.PeseeAvecPlanteur
import kotlinx.coroutines.flow.Flow

/**
 * DAO des pesées.
 *
 * Outre le CRUD, il expose les agrégats demandés par le sujet (nombre de
 * pesées et poids total d'un planteur). Ces calculs sont faits par SQLite :
 * les remonter en Kotlin obligerait à charger toutes les lignes en mémoire
 * pour en faire la somme.
 */
@Dao
interface PeseeDao {

    @Insert(onConflict = OnConflictStrategy.ABORT)
    suspend fun inserer(pesee: Pesee): Long

    @Update
    suspend fun modifier(pesee: Pesee): Int

    @Delete
    suspend fun supprimer(pesee: Pesee): Int

    /**
     * Liste générale : chaque pesée est accompagnée de son planteur.
     * `INNER JOIN` suffit — une pesée sans planteur ne peut pas exister,
     * la clé étrangère l'interdit.
     */
    @Query(
        """
        SELECT pesee.*,
               planteur.nom AS planteurNom,
               planteur.prenom AS planteurPrenom,
               planteur.localite AS planteurLocalite
        FROM pesees AS pesee
        INNER JOIN planteurs AS planteur ON pesee.planteurCode = planteur.code
        ORDER BY pesee.datePesee DESC, pesee.id DESC
        """
    )
    fun listerToutesAvecPlanteur(): Flow<List<PeseeAvecPlanteur>>

    @Query("SELECT * FROM pesees WHERE planteurCode = :code ORDER BY datePesee DESC, id DESC")
    fun listerParPlanteur(code: String): Flow<List<Pesee>>

    @Query("SELECT * FROM pesees WHERE id = :id")
    suspend fun trouverParId(id: Long): Pesee?

    @Query("SELECT COUNT(*) FROM pesees WHERE planteurCode = :code")
    fun compterParPlanteur(code: String): Flow<Int>

    /**
     * `SUM` renvoie NULL quand le planteur n'a aucune pesée : le type de
     * retour est donc nullable, et la couche Repository le ramène à 0.
     */
    @Query("SELECT SUM(poidsKg) FROM pesees WHERE planteurCode = :code")
    fun poidsTotalParPlanteur(code: String): Flow<Double?>

    @Query("SELECT MAX(datePesee) FROM pesees WHERE planteurCode = :code")
    fun dateDernierePeseeParPlanteur(code: String): Flow<Long?>

    @Query("SELECT COUNT(*) FROM pesees")
    fun compter(): Flow<Int>

    @Query("SELECT SUM(poidsKg) FROM pesees")
    fun poidsTotal(): Flow<Double?>

    // --- Réservé à la synchronisation avec la base distante ---
    //
    // Les pesées venues du réseau sont reconnues par leur `cleDistante`, pas
    // par leur `id` : celui-ci est propre à l'appareil. `idLocalPour` donne
    // l'`id` déjà attribué ici à une pesée distante, afin de la mettre à jour
    // au lieu d'en créer un doublon.

    @Upsert
    suspend fun enregistrerDepuisDistant(pesees: List<Pesee>)

    @Query("SELECT id FROM pesees WHERE cleDistante = :cleDistante")
    suspend fun idLocalPour(cleDistante: String): Long?

    @Query("DELETE FROM pesees WHERE cleDistante NOT IN (:clesConservees)")
    suspend fun supprimerHors(clesConservees: List<String>)

    @Query("DELETE FROM pesees")
    suspend fun supprimerTout()

    @Query("SELECT * FROM pesees")
    suspend fun listerToutMaintenant(): List<Pesee>
}
