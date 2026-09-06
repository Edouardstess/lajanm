package com.example.cooperativeagricole.ui.planteur

import android.content.Context
import android.content.Intent
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
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.databinding.ActivityPlanteurDetailBinding
import com.example.cooperativeagricole.ui.pesee.PeseeCompacteAdapter
import com.example.cooperativeagricole.ui.pesee.PeseeFormActivity
import com.example.cooperativeagricole.ui.pesee.PeseeViewModel
import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.Formats
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.initialesDe
import com.example.cooperativeagricole.util.observerPendantAffichage
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

/**
 * Fiche détaillée d'un planteur : ses informations, la synthèse de ses pesées
 * (nombre, poids total, dernière pesée) et l'historique complet.
 *
 * L'écran observe un unique état [DetailPlanteur] assemblé par le ViewModel à
 * partir de cinq requêtes. Assembler côté ViewModel évite les affichages
 * incohérents où le nombre de pesées serait à jour mais pas le poids total.
 */
class PlanteurDetailActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityPlanteurDetailBinding

    private val code: String by lazy { requireNotNull(intent.getStringExtra(EXTRA_CODE)) }

    private val viewModel: PlanteurViewModel by lazy {
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

    private val adapter = PeseeCompacteAdapter(
        surClic = { pesee -> startActivity(PeseeFormActivity.intentionModification(this, pesee.id)) },
        surOptions = { pesee, ancre -> ouvrirOptions(pesee, ancre) },
    )

    private var nombreDePesees = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        liaison = ActivityPlanteurDetailBinding.inflate(layoutInflater)
        setContentView(liaison.root)
        liaison.racine.appliquerInsetsSysteme()

        liaison.barre.setNavigationOnClickListener { finish() }
        liaison.barre.setOnMenuItemClickListener { element ->
            when (element.itemId) {
                R.id.action_modifier -> {
                    startActivity(PlanteurFormActivity.intention(this, code))
                    true
                }
                R.id.action_supprimer -> {
                    confirmerSuppression()
                    true
                }
                else -> false
            }
        }

        liaison.listePesees.layoutManager = LinearLayoutManager(this)
        liaison.listePesees.adapter = adapter
        // La liste est dans un NestedScrollView : c'est la page entière qui
        // défile, la liste se déploie donc sur toute sa hauteur.
        liaison.listePesees.isNestedScrollingEnabled = false

        liaison.boutonAjouterPesee.setOnClickListener {
            startActivity(PeseeFormActivity.intentionCreation(this, code))
        }

        etiqueter()
        viewModel.selectionner(code)

        observerPendantAffichage {
            launch {
                viewModel.detail.collect { detail ->
                    if (detail == null) return@collect
                    afficher(detail)
                }
            }
        }
    }

    /** Les étiquettes du bloc « Informations » ne changent jamais. */
    private fun etiqueter() {
        liaison.ligneCode.etiquette.setText(R.string.detail_code)
        liaison.ligneNom.etiquette.setText(R.string.detail_nom_complet)
        liaison.ligneLocalite.etiquette.setText(R.string.detail_localite)
        liaison.ligneSexe.etiquette.setText(R.string.detail_sexe)
        liaison.ligneNaissance.etiquette.setText(R.string.detail_date_naissance)
    }

    private fun afficher(detail: DetailPlanteur) {
        val planteur = detail.planteur
        nombreDePesees = detail.nombrePesees

        liaison.initiales.text = initialesDe(planteur.prenom, planteur.nom)
        liaison.nomComplet.text = planteur.nomComplet
        liaison.sousTitre.text = getString(
            R.string.format_deux_valeurs,
            getString(libelleSexe(planteur.sexe)),
            planteur.localite,
        )

        liaison.ligneCode.valeur.text = planteur.code
        liaison.ligneNom.valeur.text = planteur.nomComplet
        liaison.ligneLocalite.valeur.text = planteur.localite
        liaison.ligneSexe.valeur.text = getString(libelleSexe(planteur.sexe))
        liaison.ligneNaissance.valeur.text = getString(
            R.string.format_deux_valeurs,
            Dates.formater(planteur.dateNaissance),
            getString(R.string.format_age, Dates.anneesDepuis(planteur.dateNaissance)),
        )

        liaison.valeurNombrePesees.text = detail.nombrePesees.toString()
        liaison.valeurPoidsTotal.text =
            getString(R.string.format_poids, Formats.poids(detail.poidsTotal))
        liaison.valeurDernierePesee.text = detail.dateDernierePesee
            ?.let { Dates.formater(it) }
            ?: getString(R.string.detail_aucune_valeur)

        adapter.submitList(detail.pesees)
        val vide = detail.pesees.isEmpty()
        liaison.messageAucunePesee.visibility = if (vide) View.VISIBLE else View.GONE
        liaison.listePesees.visibility = if (vide) View.GONE else View.VISIBLE
    }

    private fun ouvrirOptions(pesee: Pesee, ancre: View) {
        PopupMenu(this, ancre).apply {
            menuInflater.inflate(R.menu.menu_element, menu)
            setOnMenuItemClickListener { element ->
                when (element.itemId) {
                    R.id.action_modifier -> {
                        startActivity(PeseeFormActivity.intentionModification(this@PlanteurDetailActivity, pesee.id))
                        true
                    }
                    R.id.action_supprimer -> {
                        confirmerSuppressionPesee(pesee)
                        true
                    }
                    else -> false
                }
            }
        }.show()
    }

    /**
     * Supprimer un planteur supprime aussi ses pesées (cascade) : la
     * confirmation le dit explicitement et annonce le nombre concerné.
     */
    private fun confirmerSuppression() {
        val planteur = viewModel.detail.value?.planteur ?: return
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.confirmation_titre_suppression)
            .setMessage(
                getString(
                    R.string.confirmation_supprimer_planteur,
                    planteur.nomComplet,
                    nombreDePesees,
                )
            )
            .setNegativeButton(R.string.action_annuler, null)
            .setPositiveButton(R.string.action_supprimer) { _, _ ->
                viewModel.supprimer(planteur)
                Toast.makeText(this, R.string.message_planteur_supprime, Toast.LENGTH_SHORT).show()
                finish()
            }
            .show()
    }

    private fun confirmerSuppressionPesee(pesee: Pesee) {
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.confirmation_titre_suppression)
            .setMessage(
                getString(
                    R.string.confirmation_supprimer_pesee,
                    Dates.formater(pesee.datePesee),
                    getString(R.string.format_poids, Formats.poids(pesee.poidsKg)),
                )
            )
            .setNegativeButton(R.string.action_annuler, null)
            .setPositiveButton(R.string.action_supprimer) { _, _ ->
                peseeViewModel.supprimer(pesee)
                Toast.makeText(this, R.string.message_pesee_supprimee, Toast.LENGTH_SHORT).show()
            }
            .show()
    }

    companion object {
        private const val EXTRA_CODE = "code_planteur"

        fun intention(contexte: Context, code: String): Intent =
            Intent(contexte, PlanteurDetailActivity::class.java)
                .putExtra(EXTRA_CODE, code)
    }
}
