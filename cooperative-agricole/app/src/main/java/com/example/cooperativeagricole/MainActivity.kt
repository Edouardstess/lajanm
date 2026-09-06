package com.example.cooperativeagricole

import android.content.Intent
import android.graphics.PorterDuff
import android.os.Bundle
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.ViewModelProvider
import com.example.cooperativeagricole.data.remote.EtatSynchronisation
import com.example.cooperativeagricole.databinding.ActivityMainBinding
import com.example.cooperativeagricole.ui.accueil.AccueilViewModel
import com.example.cooperativeagricole.ui.accueil.EtatAccueil
import com.example.cooperativeagricole.ui.pesee.PeseeListActivity
import com.example.cooperativeagricole.ui.planteur.PlanteurListActivity
import com.example.cooperativeagricole.util.Formats
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.observerPendantAffichage
import kotlinx.coroutines.launch

/**
 * Écran d'accueil : l'identité de l'application, les chiffres de la
 * coopérative, et les deux portes d'entrée vers les planteurs et les pesées.
 *
 * L'écran n'observe qu'un seul état ([EtatAccueil]) : les compteurs viennent de
 * la base, l'état de synchronisation de la liaison Firestore. Ils se mettent à
 * jour d'eux-mêmes — aucun autre écran n'a à prévenir celui-ci.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityMainBinding

    private val viewModel: AccueilViewModel by lazy {
        ViewModelProvider(
            this,
            AccueilViewModel.Fabrique(
                conteneur.planteurRepository,
                conteneur.peseeRepository,
                conteneur.etatSynchronisation,
            ),
        )[AccueilViewModel::class.java]
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        liaison = ActivityMainBinding.inflate(layoutInflater)
        setContentView(liaison.root)
        liaison.racine.appliquerInsetsSysteme()

        liaison.cartePlanteurs.setOnClickListener {
            startActivity(Intent(this, PlanteurListActivity::class.java))
        }
        liaison.cartePesees.setOnClickListener {
            startActivity(Intent(this, PeseeListActivity::class.java))
        }

        observerPendantAffichage {
            launch { viewModel.etat.collect(::afficher) }
        }
    }

    private fun afficher(etat: EtatAccueil) {
        liaison.valeurPlanteurs.text = etat.nombrePlanteurs.toString()
        liaison.valeurPesees.text = etat.nombrePesees.toString()
        liaison.valeurPoidsTotal.text =
            getString(R.string.format_poids, Formats.poids(etat.poidsTotal))
        afficherSynchronisation(etat.synchronisation)
    }

    /**
     * L'étiquette d'état dit deux choses à la fois : le libellé nomme la
     * situation, la couleur du point la qualifie. Un utilisateur qui ne lit pas
     * l'étiquette voit quand même, d'un coup d'œil, si quelque chose cloche.
     */
    private fun afficherSynchronisation(etat: EtatSynchronisation) {
        val libelle = when (etat) {
            EtatSynchronisation.DESACTIVEE -> R.string.etat_locale
            EtatSynchronisation.CONNEXION -> R.string.etat_connexion
            EtatSynchronisation.SYNCHRONISEE -> R.string.etat_synchronise
            EtatSynchronisation.ENVOI_EN_ATTENTE -> R.string.etat_envoi
            EtatSynchronisation.HORS_LIGNE -> R.string.etat_hors_ligne
            EtatSynchronisation.ERREUR -> R.string.etat_erreur
        }
        val couleur = when (etat) {
            EtatSynchronisation.SYNCHRONISEE -> R.color.succes
            EtatSynchronisation.ENVOI_EN_ATTENTE, EtatSynchronisation.HORS_LIGNE -> R.color.attention
            EtatSynchronisation.ERREUR -> R.color.erreur
            EtatSynchronisation.DESACTIVEE, EtatSynchronisation.CONNEXION -> R.color.neutre
        }

        liaison.texteEtat.setText(libelle)
        // Le point est un drawable partagé entre les états : il est teinté à
        // chaque changement plutôt que remplacé par un dessin par état.
        liaison.pointEtat.background?.mutate()?.setColorFilter(
            ContextCompat.getColor(this, couleur),
            PorterDuff.Mode.SRC_IN,
        )
    }
}
