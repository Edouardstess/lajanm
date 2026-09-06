package com.example.cooperativeagricole.data.remote

import android.content.Context
import android.util.Log
import com.google.firebase.FirebaseApp
import com.google.firebase.firestore.FirebaseFirestore

/**
 * Point d'entrée vers Firebase.
 *
 * L'application doit rester utilisable sans compte Firebase : c'est ici que
 * cette garantie se joue. `FirebaseApp.initializeApp` renvoie `null` quand
 * aucun `google-services.json` n'a été fourni ; le reste du code reçoit alors
 * une source distante nulle et travaille en local seul, sans le savoir.
 */
object ConnexionFirebase {

    private const val ETIQUETTE = "Firebase"

    fun creerSourceDistante(contexte: Context): SourceDistante? {
        val application = try {
            FirebaseApp.initializeApp(contexte.applicationContext)
        } catch (erreur: IllegalStateException) {
            Log.w(ETIQUETTE, "Configuration Firebase invalide, mode local seul.", erreur)
            null
        }

        if (application == null) {
            Log.i(ETIQUETTE, "Aucun google-services.json : base distante désactivée.")
            return null
        }

        return SourceDistante(FirebaseFirestore.getInstance(application))
    }
}
