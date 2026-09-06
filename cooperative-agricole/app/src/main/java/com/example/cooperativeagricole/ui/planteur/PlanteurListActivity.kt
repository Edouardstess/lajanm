package com.example.cooperativeagricole.ui.planteur

import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.View
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.conteneur
import com.example.cooperativeagricole.databinding.ActivityPlanteurListBinding
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.observerPendantAffichage
import kotlinx.coroutines.launch

/**
 * Liste des planteurs, avec recherche.
 *
 * L'écran ne conserve aucun état : il affiche ce que le ViewModel émet, et
 * lui transmet les actions de l'utilisateur. C'est cette séparation qui rend
 * la rotation de l'écran indolore — la liste et la recherche survivent.
 */
class PlanteurListActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityPlanteurListBinding

    private val viewModel: PlanteurViewModel by lazy {
        ViewModelProvider(
            this,
            PlanteurViewModel.Fabrique(conteneur.planteurRepository, conteneur.peseeRepository),
        )[PlanteurViewModel::class.java]
    }

    private val adapter = PlanteurAdapter { planteur ->
        startActivity(PlanteurDetailActivity.intention(this, planteur.code))
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        liaison = ActivityPlanteurListBinding.inflate(layoutInflater)
        setContentView(liaison.root)
        liaison.racine.appliquerInsetsSysteme()

        liaison.barre.setNavigationOnClickListener { finish() }

        liaison.liste.layoutManager = LinearLayoutManager(this)
        liaison.liste.adapter = adapter

        liaison.boutonAjouter.setOnClickListener {
            startActivity(PlanteurFormActivity.intention(this))
        }

        liaison.champRecherche.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: Editable?) {
                viewModel.mettreAJourRecherche(s?.toString().orEmpty())
            }
        })

        observerPendantAffichage {
            launch {
                viewModel.planteurs.collect { planteurs ->
                    adapter.submitList(planteurs)
                    afficherEtatVide(planteurs.isEmpty())
                }
            }
        }
    }

    /**
     * Deux listes vides très différentes : « aucun planteur enregistré » et
     * « aucun résultat pour cette recherche ». Les confondre laisserait
     * croire que les données ont disparu.
     */
    private fun afficherEtatVide(vide: Boolean) {
        liaison.etatVide.visibility = if (vide) View.VISIBLE else View.GONE
        liaison.liste.visibility = if (vide) View.GONE else View.VISIBLE
        if (!vide) return

        val recherche = viewModel.recherche.value.isNotBlank()
        liaison.etatVideTitre.setText(
            if (recherche) R.string.planteurs_recherche_vide_titre else R.string.planteurs_vide_titre
        )
        liaison.etatVideMessage.setText(
            if (recherche) R.string.planteurs_recherche_vide_message else R.string.planteurs_vide_message
        )
    }
}
