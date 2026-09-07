package com.example.cooperativeagricole

import android.content.Intent
import android.os.Bundle
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.ViewModelProvider
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
 * L'écran n'observe qu'un seul état ([EtatAccueil]), alimenté par la base. Les
 * compteurs se mettent à jour d'eux-mêmes dès qu'un planteur ou une pesée est
 * ajouté ou supprimé — aucun autre écran n'a à prévenir celui-ci.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityMainBinding

    private val viewModel: AccueilViewModel by lazy {
        ViewModelProvider(
            this,
            AccueilViewModel.Fabrique(conteneur.planteurRepository, conteneur.peseeRepository),
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
            launch { viewModel.etat.collect { etat -> afficher(etat) } }
        }
    }

    private fun afficher(etat: EtatAccueil) {
        liaison.valeurPlanteurs.text = etat.nombrePlanteurs.toString()
        liaison.valeurPesees.text = etat.nombrePesees.toString()
        liaison.valeurPoidsTotal.text =
            getString(R.string.format_poids, Formats.poids(etat.poidsTotal))
    }
}
