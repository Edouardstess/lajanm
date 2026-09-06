package com.example.cooperativeagricole

import android.content.Intent
import android.os.Bundle
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.ViewModelProvider
import com.example.cooperativeagricole.databinding.ActivityMainBinding
import com.example.cooperativeagricole.ui.pesee.PeseeListActivity
import com.example.cooperativeagricole.ui.pesee.PeseeViewModel
import com.example.cooperativeagricole.ui.planteur.PlanteurListActivity
import com.example.cooperativeagricole.ui.planteur.PlanteurViewModel
import com.example.cooperativeagricole.util.Formats
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.observerPendantAffichage
import kotlinx.coroutines.launch

/**
 * Écran d'accueil : les deux portes d'entrée de l'application (planteurs et
 * pesées) et trois compteurs.
 *
 * Les compteurs viennent des ViewModels, donc de la base : ils se mettent à
 * jour tout seuls dès qu'un planteur ou une pesée est ajouté ou supprimé, sans
 * qu'aucun écran ait à prévenir celui-ci.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityMainBinding

    private val planteurViewModel: PlanteurViewModel by lazy {
        ViewModelProvider(
            this,
            PlanteurViewModel.Fabrique(conteneur.planteurRepository, conteneur.peseeRepository),
        )[PlanteurViewModel::class.java]
    }

    private val peseeViewModel: PeseeViewModel by lazy {
        ViewModelProvider(
            this,
            PeseeViewModel.Fabrique(conteneur.peseeRepository, conteneur.planteurRepository),
        )[PeseeViewModel::class.java]
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
            launch {
                planteurViewModel.nombrePlanteurs.collect { nombre ->
                    liaison.valeurPlanteurs.text = nombre.toString()
                }
            }
            launch {
                peseeViewModel.nombrePesees.collect { nombre ->
                    liaison.valeurPesees.text = nombre.toString()
                }
            }
            launch {
                peseeViewModel.poidsTotal.collect { poids ->
                    liaison.valeurPoidsTotal.text =
                        getString(R.string.format_poids, Formats.poids(poids))
                }
            }
        }
    }
}
