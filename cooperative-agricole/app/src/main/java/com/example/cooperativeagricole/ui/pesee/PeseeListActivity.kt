package com.example.cooperativeagricole.ui.pesee

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.PopupMenu
import androidx.lifecycle.ViewModelProvider
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.conteneur
import com.example.cooperativeagricole.data.local.entity.PeseeAvecPlanteur
import com.example.cooperativeagricole.databinding.ActivityPeseeListBinding
import com.example.cooperativeagricole.ui.planteur.PlanteurFormActivity
import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.Formats
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.observerPendantAffichage
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

/**
 * Liste de toutes les pesées, la plus récente en tête, avec le total du jour
 * et le poids cumulé de la coopérative.
 */
class PeseeListActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityPeseeListBinding

    private val viewModel: PeseeViewModel by lazy {
        ViewModelProvider(
            this,
            PeseeViewModel.Fabrique(conteneur.peseeRepository, conteneur.planteurRepository),
        )[PeseeViewModel::class.java]
    }

    private val adapter = PeseeAdapter(
        surClic = { element ->
            startActivity(PeseeFormActivity.intentionModification(this, element.pesee.id))
        },
        surOptions = { element, ancre -> ouvrirOptions(element, ancre) },
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        liaison = ActivityPeseeListBinding.inflate(layoutInflater)
        setContentView(liaison.root)
        liaison.racine.appliquerInsetsSysteme()

        liaison.barre.setNavigationOnClickListener { finish() }

        liaison.liste.layoutManager = LinearLayoutManager(this)
        liaison.liste.adapter = adapter

        liaison.boutonAjouter.setOnClickListener { viewModel.demanderNouvelleSaisie() }

        observerPendantAffichage {
            launch {
                viewModel.pesees.collect { pesees ->
                    adapter.submitList(pesees)
                    val vide = pesees.isEmpty()
                    liaison.etatVide.visibility = if (vide) View.VISIBLE else View.GONE
                    liaison.liste.visibility = if (vide) View.GONE else View.VISIBLE
                }
            }
            launch {
                viewModel.nombrePesees.collect { liaison.valeurNombre.text = it.toString() }
            }
            launch {
                viewModel.poidsTotal.collect { poids ->
                    liaison.valeurPoids.text =
                        getString(R.string.format_poids, Formats.poids(poids))
                }
            }
            launch {
                viewModel.evenements.collect { evenement ->
                    when (evenement) {
                        EvenementPesee.OuvrirFormulaire ->
                            startActivity(PeseeFormActivity.intentionCreation(this@PeseeListActivity))
                        EvenementPesee.AucunPlanteur -> proposerCreationPlanteur()
                        EvenementPesee.Supprimee ->
                            signaler(R.string.message_pesee_supprimee)
                        EvenementPesee.Echec -> signaler(R.string.message_echec)
                        EvenementPesee.Enregistree, EvenementPesee.Modifiee -> Unit
                    }
                }
            }
        }
    }

    private fun ouvrirOptions(element: PeseeAvecPlanteur, ancre: View) {
        PopupMenu(this, ancre).apply {
            menuInflater.inflate(R.menu.menu_element, menu)
            setOnMenuItemClickListener { entree ->
                when (entree.itemId) {
                    R.id.action_modifier -> {
                        startActivity(
                            PeseeFormActivity.intentionModification(
                                this@PeseeListActivity, element.pesee.id
                            )
                        )
                        true
                    }
                    R.id.action_supprimer -> {
                        confirmerSuppression(element)
                        true
                    }
                    else -> false
                }
            }
        }.show()
    }

    private fun confirmerSuppression(element: PeseeAvecPlanteur) {
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.confirmation_titre_suppression)
            .setMessage(
                getString(
                    R.string.confirmation_supprimer_pesee,
                    Dates.formater(element.pesee.datePesee),
                    getString(R.string.format_poids, Formats.poids(element.pesee.poidsKg)),
                )
            )
            .setNegativeButton(R.string.action_annuler, null)
            .setPositiveButton(R.string.action_supprimer) { _, _ ->
                viewModel.supprimer(element.pesee)
            }
            .show()
    }

    /** Sans planteur, aucune pesée ne peut être rattachée : on le dit, et on
     *  propose directement d'en créer un. */
    private fun proposerCreationPlanteur() {
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.pesee_aucun_planteur_titre)
            .setMessage(R.string.pesee_aucun_planteur_message)
            .setNegativeButton(R.string.action_annuler, null)
            .setPositiveButton(R.string.pesee_aucun_planteur_action) { _, _ ->
                startActivity(PlanteurFormActivity.intention(this))
            }
            .show()
    }

    private fun signaler(message: Int) {
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
    }
}
