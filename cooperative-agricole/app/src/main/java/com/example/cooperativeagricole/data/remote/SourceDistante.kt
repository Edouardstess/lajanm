package com.example.cooperativeagricole.data.remote

import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.local.entity.Sexe
import com.google.firebase.firestore.DocumentSnapshot
import com.google.firebase.firestore.FirebaseFirestore
import kotlinx.coroutines.channels.awaitClose
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.callbackFlow
import kotlinx.coroutines.flow.conflate

/**
 * Base distante : le miroir Firestore des tables locales.
 *
 * Deux collections, une par entité, avec un document par ligne :
 *
 * ```
 * planteurs/{code}          nom, prenom, sexe, dateNaissance, localite
 * pesees/{cleDistante}      planteurCode, datePesee, poidsKg, observation
 * ```
 *
 * Les écritures ne sont pas attendues : le SDK Firestore les met en file
 * d'attente, les rejoue à la reconnexion et met à jour son cache local
 * immédiatement. Attendre le serveur ne rendrait donc pas l'écriture plus
 * sûre — cela ne ferait qu'immobiliser l'interface hors réseau.
 */
class SourceDistante(private val firestore: FirebaseFirestore) {

    private val _etat = MutableStateFlow(EtatSynchronisation.CONNEXION)
    val etat: StateFlow<EtatSynchronisation> = _etat.asStateFlow()

    // --- Lectures observables ---

    fun observerPlanteurs(): Flow<List<Planteur>> =
        observerCollection(COLLECTION_PLANTEURS, ::versPlanteur)

    fun observerPesees(): Flow<List<Pesee>> =
        observerCollection(COLLECTION_PESEES, ::versPesee)

    /**
     * Transforme l'écoute d'une collection Firestore, qui fonctionne par
     * rappels, en `Flow`. `awaitClose` détache l'écoute quand plus personne ne
     * collecte : sans cela, l'écoute survivrait à l'écran qui l'a demandée.
     *
     * `conflate` parce que chaque instantané décrit la collection entière et
     * non une différence : si plusieurs arrivent pendant qu'on traite le
     * précédent, seul le dernier a un sens, les intermédiaires peuvent tomber.
     */
    private fun <T> observerCollection(
        collection: String,
        conversion: (DocumentSnapshot) -> T?,
    ): Flow<List<T>> = callbackFlow {
        val ecoute = firestore.collection(collection)
            .addSnapshotListener { instantane, erreur ->
                if (erreur != null) {
                    _etat.value = EtatSynchronisation.ERREUR
                    return@addSnapshotListener
                }
                if (instantane == null) return@addSnapshotListener

                // Les métadonnées de l'instantané disent d'où il vient : c'est
                // la façon la plus fiable de connaître l'état réel de la
                // liaison, plus que d'interroger la connectivité du système.
                // `hasPendingWrites()` s'écrit avec ses parenthèses : côté Java
                // ce n'est pas un accesseur `getX()`, Kotlin n'en fait donc pas
                // une propriété — contrairement à `isFromCache`.
                _etat.value = when {
                    instantane.metadata.hasPendingWrites() -> EtatSynchronisation.ENVOI_EN_ATTENTE
                    instantane.metadata.isFromCache -> EtatSynchronisation.HORS_LIGNE
                    else -> EtatSynchronisation.SYNCHRONISEE
                }
                trySend(instantane.documents.mapNotNull(conversion))
            }
        awaitClose { ecoute.remove() }
    }.conflate()

    // --- Écritures ---

    fun enregistrer(planteur: Planteur) {
        firestore.collection(COLLECTION_PLANTEURS)
            .document(planteur.code)
            .set(versDocument(planteur))
    }

    fun enregistrer(pesee: Pesee) {
        firestore.collection(COLLECTION_PESEES)
            .document(pesee.cleDistante)
            .set(versDocument(pesee))
    }

    /**
     * Firestore ne connaît pas les clés étrangères : la cascade que SQLite
     * applique toute seule doit être reproduite ici, sans quoi les pesées du
     * planteur supprimé resteraient dans la base distante et reviendraient à
     * la prochaine synchronisation.
     */
    fun supprimerPlanteur(code: String) {
        firestore.collection(COLLECTION_PLANTEURS).document(code).delete()
        firestore.collection(COLLECTION_PESEES)
            .whereEqualTo(CHAMP_PLANTEUR, code)
            .get()
            .addOnSuccessListener { resultat ->
                resultat.documents.forEach { it.reference.delete() }
            }
    }

    fun supprimerPesee(cleDistante: String) {
        firestore.collection(COLLECTION_PESEES).document(cleDistante).delete()
    }

    // --- Conversions ---

    private fun versDocument(planteur: Planteur): Map<String, Any> = mapOf(
        "code" to planteur.code,
        "nom" to planteur.nom,
        "prenom" to planteur.prenom,
        "sexe" to planteur.sexe.code,
        "dateNaissance" to planteur.dateNaissance,
        "localite" to planteur.localite,
    )

    private fun versDocument(pesee: Pesee): Map<String, Any?> = mapOf(
        "cleDistante" to pesee.cleDistante,
        CHAMP_PLANTEUR to pesee.planteurCode,
        "datePesee" to pesee.datePesee,
        "poidsKg" to pesee.poidsKg,
        "observation" to pesee.observation,
    )

    /**
     * Un document incomplet est ignoré plutôt que de faire échouer toute la
     * synchronisation : la base distante est modifiable depuis la console
     * Firebase, elle n'est donc pas garantie bien formée.
     */
    private fun versPlanteur(document: DocumentSnapshot): Planteur? {
        val nom = document.getString("nom") ?: return null
        val prenom = document.getString("prenom") ?: return null
        val localite = document.getString("localite") ?: return null
        val naissance = document.getLong("dateNaissance") ?: return null
        return Planteur(
            code = document.id,
            nom = nom,
            prenom = prenom,
            sexe = Sexe.depuisCode(document.getString("sexe") ?: Sexe.MASCULIN.code),
            dateNaissance = naissance,
            localite = localite,
        )
    }

    private fun versPesee(document: DocumentSnapshot): Pesee? {
        val planteurCode = document.getString(CHAMP_PLANTEUR) ?: return null
        val date = document.getLong("datePesee") ?: return null
        val poids = document.getDouble("poidsKg") ?: return null
        return Pesee(
            // `id` est local : il sera remplacé par celui de cet appareil au
            // moment d'écrire dans Room (voir SynchronisationCooperative).
            id = 0L,
            planteurCode = planteurCode,
            datePesee = date,
            poidsKg = poids,
            observation = document.getString("observation"),
            cleDistante = document.id,
        )
    }

    private companion object {
        const val COLLECTION_PLANTEURS = "planteurs"
        const val COLLECTION_PESEES = "pesees"
        const val CHAMP_PLANTEUR = "planteurCode"
    }
}
