package com.example.cooperativeagricole.data.local.dao

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update
import androidx.room.Upsert
import com.example.cooperativeagricole.data.local.entity.Planteur
import kotlinx.coroutines.flow.Flow

/**
 * DAO (Data Access Object) des planteurs.
 *
 * Le DAO est la seule porte d'entrée vers la table `planteurs` : il décrit
 * *quelles* opérations sont possibles, Room en génère l'implémentation SQL à
 * la compilation. Aucune couche supérieure n'écrit de SQL.
 *
 * Deux styles de retour cohabitent volontairement :
 *  - `Flow<...>` pour les lectures observables : Room réémet la valeur à
 *    chaque modification de la table, ce qui rafraîchit l'écran tout seul ;
 *  - `suspend fun` pour les écritures et les lectures ponctuelles : Room les
 *    exécute hors du thread principal.
 */
@Dao
interface PlanteurDao {

    /**
     * `ABORT` fait échouer l'insertion si le code existe déjà, au lieu de
     * remplacer silencieusement le planteur existant : l'unicité du code est
     * une contrainte fonctionnelle, pas une préférence.
     */
    @Insert(onConflict = OnConflictStrategy.ABORT)
    suspend fun inserer(planteur: Planteur)

    @Update
    suspend fun modifier(planteur: Planteur): Int

    @Delete
    suspend fun supprimer(planteur: Planteur): Int

    @Query("SELECT * FROM planteurs ORDER BY nom COLLATE NOCASE, prenom COLLATE NOCASE")
    fun listerTous(): Flow<List<Planteur>>

    @Query(
        """
        SELECT * FROM planteurs
        WHERE code LIKE '%' || :recherche || '%'
           OR nom LIKE '%' || :recherche || '%'
           OR prenom LIKE '%' || :recherche || '%'
           OR localite LIKE '%' || :recherche || '%'
        ORDER BY nom COLLATE NOCASE, prenom COLLATE NOCASE
        """
    )
    fun rechercher(recherche: String): Flow<List<Planteur>>

    /** Observable : l'écran de détail se met à jour après une modification. */
    @Query("SELECT * FROM planteurs WHERE code = :code")
    fun observerParCode(code: String): Flow<Planteur?>

    /** Lecture ponctuelle, pour pré-remplir le formulaire de modification. */
    @Query("SELECT * FROM planteurs WHERE code = :code")
    suspend fun trouverParCode(code: String): Planteur?

    @Query("SELECT EXISTS(SELECT 1 FROM planteurs WHERE code = :code)")
    suspend fun existe(code: String): Boolean

    @Query("SELECT COUNT(*) FROM planteurs")
    fun compter(): Flow<Int>

    /** Compte ponctuel, quand un écran doit décider immédiatement. */
    @Query("SELECT COUNT(*) FROM planteurs")
    suspend fun compterMaintenant(): Int

    // --- Réservé à la synchronisation avec la base distante ---
    //
    // Ces opérations ne sont jamais appelées par l'interface : elles servent à
    // recopier localement l'état du dépôt distant. Contrairement à la saisie,
    // qui refuse un code déjà pris (`ABORT`), la synchronisation doit mettre à
    // jour ce qu'elle connaît déjà.
    //
    // `@Upsert` et non `@Insert(REPLACE)` : ce dernier SUPPRIME la ligne avant
    // de la réinsérer, ce qui déclencherait la cascade de la clé étrangère et
    // effacerait toutes les pesées du planteur à chaque synchronisation.
    // `@Upsert` met à jour la ligne existante, sans la détruire.

    @Upsert
    suspend fun enregistrerDepuisDistant(planteurs: List<Planteur>)

    @Query("DELETE FROM planteurs WHERE code NOT IN (:codesConserves)")
    suspend fun supprimerHors(codesConserves: List<String>)

    @Query("DELETE FROM planteurs")
    suspend fun supprimerTout()

    @Query("SELECT * FROM planteurs")
    suspend fun listerToutMaintenant(): List<Planteur>
}
