package com.example.cooperativeagricole.data.sync

import android.util.Log
import androidx.room.withTransaction
import com.example.cooperativeagricole.data.local.database.CooperativeDatabase
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.remote.SourceDistante
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.launch

/**
 * Recopie en continu la base distante dans la base locale.
 *
 * Le sens de circulation est volontairement asymétrique :
 *
 * - **Distant → local** : ici. Chaque changement chez Firestore est appliqué à
 *   Room, qui reste la seule source lue par l'interface. Les écrans n'ont donc
 *   rien à savoir du réseau : ils observent Room, comme avant.
 * - **Local → distant** : dans les Repository, au moment de l'écriture.
 *
 * Les deux collections sont écoutées **ensemble** (`combine`) et appliquées
 * dans une même transaction : une pesée ne peut être insérée avant le planteur
 * qu'elle référence, sinon la clé étrangère la rejetterait.
 */
class SynchronisationCooperative(
    private val base: CooperativeDatabase,
    private val source: SourceDistante,
    private val portee: CoroutineScope,
) {

    fun demarrer() {
        portee.launch {
            var premiereReception = true
            combine(
                source.observerPlanteurs(),
                source.observerPesees(),
            ) { planteurs, pesees -> planteurs to pesees }
                .catch { erreur -> Log.e(ETIQUETTE, "Synchronisation interrompue.", erreur) }
                .collect { (planteurs, pesees) ->
                    val amorcage = premiereReception && planteurs.isEmpty() && pesees.isEmpty()
                    premiereReception = false

                    if (amorcage) {
                        televerserExistant()
                    } else {
                        appliquer(planteurs, pesees)
                    }
                }
        }
    }

    /**
     * Premier lancement sur un dépôt distant vide : plutôt que d'effacer le
     * contenu local — ce que ferait une recopie littérale de « rien » —, on
     * envoie l'existant vers Firestore. C'est ce qui permet d'installer
     * l'application sur un premier téléphone sans perdre ses données.
     */
    private suspend fun televerserExistant() {
        val planteurs = base.planteurDao().listerToutMaintenant()
        val pesees = base.peseeDao().listerToutMaintenant()
        if (planteurs.isEmpty() && pesees.isEmpty()) return

        Log.i(ETIQUETTE, "Dépôt distant vide : envoi de ${planteurs.size} planteurs et ${pesees.size} pesées.")
        planteurs.forEach(source::enregistrer)
        pesees.forEach(source::enregistrer)
    }

    /**
     * Aligne la base locale sur l'état distant, en une transaction :
     * l'interface ne voit jamais un état intermédiaire où les planteurs
     * seraient à jour mais pas les pesées.
     */
    private suspend fun appliquer(planteurs: List<Planteur>, pesees: List<Pesee>) {
        val planteurDao = base.planteurDao()
        val peseeDao = base.peseeDao()

        // Une pesée dont le planteur n'existe pas dans le même instantané est
        // orpheline côté distant : l'insérer violerait la clé étrangère.
        val codesConnus = planteurs.mapTo(HashSet()) { it.code }
        val peseesValides = pesees.filter { it.planteurCode in codesConnus }

        base.withTransaction {
            if (planteurs.isEmpty()) {
                planteurDao.supprimerTout()
            } else {
                planteurDao.supprimerHors(planteurs.map { it.code })
                planteurDao.insererOuRemplacer(planteurs)
            }

            if (peseesValides.isEmpty()) {
                peseeDao.supprimerTout()
            } else {
                peseeDao.supprimerHors(peseesValides.map { it.cleDistante })
                // L'`id` local est conservé quand la pesée est déjà connue :
                // le remplacer casserait les listes affichées à l'écran.
                val aEcrire = peseesValides.map { pesee ->
                    pesee.copy(id = peseeDao.idLocalPour(pesee.cleDistante) ?: 0L)
                }
                peseeDao.insererOuRemplacer(aEcrire)
            }
        }
    }

    private companion object {
        const val ETIQUETTE = "Synchronisation"
    }
}
