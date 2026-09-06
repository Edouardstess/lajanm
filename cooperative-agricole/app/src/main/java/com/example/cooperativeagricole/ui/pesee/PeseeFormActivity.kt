package com.example.cooperativeagricole.ui.pesee

import android.app.DatePickerDialog
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.widget.ArrayAdapter
import android.widget.Toast
import androidx.activity.OnBackPressedCallback
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.core.widget.doOnTextChanged
import androidx.lifecycle.ViewModelProvider
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.conteneur
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.databinding.ActivityPeseeFormBinding
import com.example.cooperativeagricole.domain.validation.ChampPesee
import com.example.cooperativeagricole.domain.validation.MotifInvalidite
import com.example.cooperativeagricole.domain.validation.SaisiePesee
import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.Formats
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.messageErreur
import com.example.cooperativeagricole.util.observerPendantAffichage
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

/**
 * Formulaire de saisie et de modification d'une pesée.
 *
 * Le planteur se choisit dans une liste déroulante alimentée par la base :
 * l'association obligatoire d'une pesée à un planteur est ainsi impossible à
 * enfreindre depuis l'interface, en plus d'être garantie par la clé étrangère.
 */
class PeseeFormActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityPeseeFormBinding

    private val viewModel: PeseeViewModel by lazy {
        ViewModelProvider(
            this,
            PeseeViewModel.Fabrique(conteneur.peseeRepository, conteneur.planteurRepository),
        )[PeseeViewModel::class.java]
    }

    /** -1 signifie « création » : aucune pesée existante n'a cet identifiant. */
    private val identifiant: Long? by lazy {
        intent.getLongExtra(EXTRA_ID, AUCUN_IDENTIFIANT).takeIf { it != AUCUN_IDENTIFIANT }
    }

    private val modification: Boolean get() = identifiant != null

    private var planteurs: List<Planteur> = emptyList()
    private var planteurChoisi: String? = null
    private var date: Long = Dates.aujourdHui()
    private var prerempli = false
    private var saisieTouchee = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        liaison = ActivityPeseeFormBinding.inflate(layoutInflater)
        setContentView(liaison.root)
        liaison.racine.appliquerInsetsSysteme()

        liaison.barre.setTitle(if (modification) R.string.pesee_modifier else R.string.pesee_nouvelle)
        liaison.barre.setNavigationOnClickListener { quitter() }
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() = quitter()
        })

        planteurChoisi = intent.getStringExtra(EXTRA_PLANTEUR)
        liaison.champDate.setText(Dates.formater(date))

        if (modification) {
            viewModel.preparerModification(requireNotNull(identifiant))
        }

        configurerSelecteurDeDate()
        liaison.champPoids.doOnTextChanged { _, _, _, _ ->
            saisieTouchee = true
            viewModel.effacerErreur(ChampPesee.POIDS)
        }
        liaison.champObservation.doOnTextChanged { _, _, _, _ -> saisieTouchee = true }

        liaison.boutonEnregistrer.setOnClickListener { enregistrer() }
        liaison.boutonAnnuler.setOnClickListener { quitter() }

        observerPendantAffichage {
            launch {
                viewModel.planteurs.collect { liste ->
                    planteurs = liste
                    remplirSelecteurDePlanteurs(liste)
                }
            }
            launch {
                viewModel.peseeAModifier.collect { pesee ->
                    if (pesee != null && !prerempli) {
                        prerempli = true
                        planteurChoisi = pesee.planteurCode
                        date = pesee.datePesee
                        liaison.champDate.setText(Dates.formater(date))
                        liaison.champPoids.setText(Formats.poids(pesee.poidsKg))
                        liaison.champObservation.setText(pesee.observation.orEmpty())
                        remplirSelecteurDePlanteurs(planteurs)
                        saisieTouchee = false
                    }
                }
            }
            launch {
                viewModel.etatFormulaire.collect { etat ->
                    liaison.boutonEnregistrer.isEnabled = !etat.enCours
                    liaison.blocPlanteur.error = etat.erreurs[ChampPesee.PLANTEUR]?.let {
                        // « Ce champ est obligatoire » conviendrait mal à une
                        // liste déroulante : le message y est plus direct.
                        if (it == MotifInvalidite.OBLIGATOIRE) {
                            getString(R.string.erreur_planteur_obligatoire)
                        } else {
                            messageErreur(it)
                        }
                    }
                    liaison.blocDate.error = etat.erreurs[ChampPesee.DATE]?.let { messageErreur(it) }
                    liaison.blocPoids.error = etat.erreurs[ChampPesee.POIDS]?.let { messageErreur(it) }
                }
            }
            launch {
                viewModel.evenements.collect { evenement ->
                    when (evenement) {
                        EvenementPesee.Enregistree -> terminer(R.string.message_pesee_enregistree)
                        EvenementPesee.Modifiee -> terminer(R.string.message_pesee_modifiee)
                        EvenementPesee.Echec -> signaler(R.string.message_echec)
                        else -> Unit
                    }
                }
            }
        }
    }

    /**
     * Remplit la liste déroulante. Les libellés portent le code du planteur :
     * deux planteurs peuvent être homonymes, leur code non.
     */
    private fun remplirSelecteurDePlanteurs(liste: List<Planteur>) {
        val libelles = liste.map { "${it.nomComplet} (${it.code})" }
        liaison.champPlanteur.setAdapter(
            ArrayAdapter(this, android.R.layout.simple_list_item_1, libelles)
        )
        liaison.champPlanteur.setOnItemClickListener { _, _, position, _ ->
            planteurChoisi = liste[position].code
            saisieTouchee = true
            viewModel.effacerErreur(ChampPesee.PLANTEUR)
        }
        // Réaffiche la sélection courante (pré-remplissage ou rotation).
        val position = liste.indexOfFirst { it.code == planteurChoisi }
        if (position >= 0) {
            liaison.champPlanteur.setText(libelles[position], false)
        }
    }

    private fun configurerSelecteurDeDate() {
        liaison.champDate.setOnClickListener {
            val (annee, mois, jour) = Dates.composantes(date)
            DatePickerDialog(this, { _, a, m, j ->
                date = Dates.de(a, m + 1, j)
                liaison.champDate.setText(Dates.formater(date))
                saisieTouchee = true
                viewModel.effacerErreur(ChampPesee.DATE)
            }, annee, mois, jour).apply {
                // Une pesée future n'a pas de sens : elle n'a pas eu lieu.
                datePicker.maxDate = System.currentTimeMillis()
            }.show()
        }
    }

    private fun enregistrer() {
        val saisie = SaisiePesee(
            planteurCode = planteurChoisi,
            date = date,
            poidsSaisi = liaison.champPoids.text?.toString().orEmpty(),
            observation = liaison.champObservation.text?.toString(),
        )
        viewModel.enregistrer(saisie, identifiant)
    }

    private fun quitter() {
        if (!saisieTouchee) {
            finish()
            return
        }
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.confirmation_quitter_titre)
            .setMessage(R.string.confirmation_quitter_message)
            .setNegativeButton(R.string.action_annuler, null)
            .setPositiveButton(R.string.confirmation_quitter_action) { _, _ -> finish() }
            .show()
    }

    private fun terminer(message: Int) {
        signaler(message)
        finish()
    }

    private fun signaler(message: Int) {
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
    }

    companion object {
        private const val EXTRA_ID = "identifiant_pesee"
        private const val EXTRA_PLANTEUR = "code_planteur"
        private const val AUCUN_IDENTIFIANT = -1L

        /** Création, éventuellement avec un planteur déjà choisi. */
        fun intentionCreation(contexte: Context, codePlanteur: String? = null): Intent =
            Intent(contexte, PeseeFormActivity::class.java).apply {
                if (codePlanteur != null) putExtra(EXTRA_PLANTEUR, codePlanteur)
            }

        fun intentionModification(contexte: Context, identifiant: Long): Intent =
            Intent(contexte, PeseeFormActivity::class.java)
                .putExtra(EXTRA_ID, identifiant)
    }
}
