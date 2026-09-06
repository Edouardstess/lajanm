package com.example.cooperativeagricole.util

import android.content.Context
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.domain.validation.MotifInvalidite
import com.example.cooperativeagricole.domain.validation.ValidationPesee
import com.example.cooperativeagricole.domain.validation.ValidationPlanteur
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch

/**
 * L'application est en « edge-to-edge » (obligatoire à partir d'Android 15) :
 * le contenu passe sous les barres système. Cette extension rend au contenu
 * la marge nécessaire pour ne pas être masqué.
 */
fun View.appliquerInsetsSysteme() {
    ViewCompat.setOnApplyWindowInsetsListener(this) { vue, insets ->
        val barres = insets.getInsets(WindowInsetsCompat.Type.systemBars())
        vue.setPadding(barres.left, barres.top, barres.right, barres.bottom)
        insets
    }
}

/**
 * Observe des flux en respectant le cycle de vie : la collecte démarre quand
 * l'écran devient visible et s'arrête quand il ne l'est plus. Sans cela, un
 * écran en arrière-plan continuerait de consommer les mises à jour de la base.
 */
fun AppCompatActivity.observerPendantAffichage(bloc: suspend CoroutineScope.() -> Unit) {
    lifecycleScope.launch {
        repeatOnLifecycle(Lifecycle.State.STARTED) { bloc() }
    }
}

/**
 * Traduit un motif d'invalidité en message affichable.
 *
 * La règle métier ignore tout d'Android ; c'est ici, et seulement ici, que
 * l'on rejoint les ressources de texte.
 */
fun Context.messageErreur(motif: MotifInvalidite): String = when (motif) {
    MotifInvalidite.OBLIGATOIRE -> getString(R.string.erreur_obligatoire)
    MotifInvalidite.TROP_COURT -> getString(R.string.erreur_trop_court)
    MotifInvalidite.FORMAT_INVALIDE -> getString(R.string.erreur_format_code)
    MotifInvalidite.DEJA_UTILISE -> getString(R.string.erreur_code_deja_utilise)
    MotifInvalidite.DATE_FUTURE -> getString(R.string.erreur_date_future)
    MotifInvalidite.AGE_HORS_LIMITES -> getString(
        R.string.erreur_age,
        ValidationPlanteur.AGE_MINIMUM,
        ValidationPlanteur.AGE_MAXIMUM,
    )
    MotifInvalidite.NOMBRE_INVALIDE -> getString(R.string.erreur_nombre)
    MotifInvalidite.VALEUR_NON_POSITIVE -> getString(R.string.erreur_poids_positif)
    MotifInvalidite.VALEUR_TROP_GRANDE -> getString(
        R.string.erreur_poids_maximum,
        Formats.poids(ValidationPesee.POIDS_MAXIMUM_KG),
    )
}

/** Initiales affichées dans la pastille ronde d'un planteur. */
fun initialesDe(prenom: String, nom: String): String {
    val premiere = prenom.trim().firstOrNull()?.uppercaseChar()
    val seconde = nom.trim().firstOrNull()?.uppercaseChar()
    return listOfNotNull(premiere, seconde).joinToString("")
}
