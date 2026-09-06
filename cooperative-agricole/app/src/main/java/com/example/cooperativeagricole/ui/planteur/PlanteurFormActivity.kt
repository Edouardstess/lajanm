package com.example.cooperativeagricole.ui.planteur

import android.app.DatePickerDialog
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.activity.OnBackPressedCallback
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.core.widget.doOnTextChanged
import androidx.lifecycle.ViewModelProvider
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.conteneur
import com.example.cooperativeagricole.data.local.entity.Sexe
import com.example.cooperativeagricole.databinding.ActivityPlanteurFormBinding
import com.example.cooperativeagricole.domain.validation.ChampPlanteur
import com.example.cooperativeagricole.domain.validation.MotifInvalidite
import com.example.cooperativeagricole.domain.validation.SaisiePlanteur
import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.appliquerInsetsSysteme
import com.example.cooperativeagricole.util.messageErreur
import com.example.cooperativeagricole.util.observerPendantAffichage
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch

/**
 * Formulaire de création et de modification d'un planteur.
 *
 * Un seul écran pour les deux usages : la présence du code du planteur dans
 * l'intention décide du mode. Cela évite de dupliquer un formulaire entier
 * pour une différence de comportement tenant en quelques lignes.
 */
class PlanteurFormActivity : AppCompatActivity() {

    private lateinit var liaison: ActivityPlanteurFormBinding

    private val viewModel: PlanteurViewModel by lazy {
        ViewModelProvider(
            this,
            PlanteurViewModel.Fabrique(conteneur.planteurRepository, conteneur.peseeRepository),
        )[PlanteurViewModel::class.java]
    }

    private val codeExistant: String? by lazy { intent.getStringExtra(EXTRA_CODE) }
    private val modification: Boolean get() = codeExistant != null

    private var dateNaissance: Long? = null
    private var prerempli = false
    private var saisieTouchee = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        liaison = ActivityPlanteurFormBinding.inflate(layoutInflater)
        setContentView(liaison.root)
        liaison.racine.appliquerInsetsSysteme()

        liaison.barre.setTitle(
            if (modification) R.string.planteur_modifier else R.string.planteur_nouveau
        )
        liaison.barre.setNavigationOnClickListener { quitter() }
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() = quitter()
        })

        if (modification) {
            // Le code est la clé primaire de la table et la cible des clés
            // étrangères des pesées : le laisser modifier casserait le lien
            // entre un planteur et son historique.
            liaison.champCode.isEnabled = false
            liaison.blocCode.helperText = getString(R.string.champ_code_verrouille)
            viewModel.preparerModification(requireNotNull(codeExistant))
        }

        configurerSuiviDeSaisie()
        configurerSelecteurDeDate()

        liaison.boutonEnregistrer.setOnClickListener { enregistrer() }
        liaison.boutonAnnuler.setOnClickListener { quitter() }

        observerPendantAffichage {
            launch {
                viewModel.planteurAModifier.collect { planteur ->
                    if (planteur != null && !prerempli) {
                        prerempli = true
                        liaison.champCode.setText(planteur.code)
                        liaison.champPrenom.setText(planteur.prenom)
                        liaison.champNom.setText(planteur.nom)
                        liaison.champLocalite.setText(planteur.localite)
                        dateNaissance = planteur.dateNaissance
                        liaison.champDateNaissance.setText(Dates.formater(planteur.dateNaissance))
                        when (planteur.sexe) {
                            Sexe.MASCULIN -> liaison.sexeMasculin.isChecked = true
                            Sexe.FEMININ -> liaison.sexeFeminin.isChecked = true
                        }
                        saisieTouchee = false
                    }
                }
            }
            launch {
                viewModel.etatFormulaire.collect { etat ->
                    liaison.boutonEnregistrer.isEnabled = !etat.enCours
                    liaison.blocCode.error = messageOuNull(etat.erreurs[ChampPlanteur.CODE])
                    liaison.blocNom.error = messageOuNull(etat.erreurs[ChampPlanteur.NOM])
                    liaison.blocPrenom.error = messageOuNull(etat.erreurs[ChampPlanteur.PRENOM])
                    liaison.blocLocalite.error = messageOuNull(etat.erreurs[ChampPlanteur.LOCALITE])
                    liaison.blocDateNaissance.error =
                        messageOuNull(etat.erreurs[ChampPlanteur.DATE_NAISSANCE])
                }
            }
            launch {
                viewModel.evenements.collect { evenement ->
                    when (evenement) {
                        EvenementPlanteur.Enregistre -> terminer(R.string.message_planteur_enregistre)
                        EvenementPlanteur.Modifie -> terminer(R.string.message_planteur_modifie)
                        EvenementPlanteur.Echec -> signaler(R.string.message_echec)
                        EvenementPlanteur.Supprime -> Unit
                    }
                }
            }
        }
    }

    /** Efface l'erreur d'un champ dès que l'utilisateur le corrige. */
    private fun configurerSuiviDeSaisie() {
        liaison.champCode.doOnTextChanged { _, _, _, _ ->
            saisieTouchee = true
            viewModel.effacerErreur(ChampPlanteur.CODE)
        }
        liaison.champNom.doOnTextChanged { _, _, _, _ ->
            saisieTouchee = true
            viewModel.effacerErreur(ChampPlanteur.NOM)
        }
        liaison.champPrenom.doOnTextChanged { _, _, _, _ ->
            saisieTouchee = true
            viewModel.effacerErreur(ChampPlanteur.PRENOM)
        }
        liaison.champLocalite.doOnTextChanged { _, _, _, _ ->
            saisieTouchee = true
            viewModel.effacerErreur(ChampPlanteur.LOCALITE)
        }
    }

    /**
     * La date se choisit dans un calendrier plutôt qu'au clavier : aucune
     * date malformée ne peut alors entrer dans le formulaire, et le sélecteur
     * refuse lui-même les dates futures.
     */
    private fun configurerSelecteurDeDate() {
        liaison.champDateNaissance.setOnClickListener {
            val (annee, mois, jour) = Dates.composantes(dateNaissance ?: DEFAUT_NAISSANCE)
            DatePickerDialog(this, { _, a, m, j ->
                dateNaissance = Dates.de(a, m + 1, j)
                liaison.champDateNaissance.setText(Dates.formater(requireNotNull(dateNaissance)))
                saisieTouchee = true
                viewModel.effacerErreur(ChampPlanteur.DATE_NAISSANCE)
            }, annee, mois, jour).apply {
                datePicker.maxDate = System.currentTimeMillis()
            }.show()
        }
    }

    private fun enregistrer() {
        val saisie = SaisiePlanteur(
            code = liaison.champCode.text?.toString().orEmpty(),
            nom = liaison.champNom.text?.toString().orEmpty(),
            prenom = liaison.champPrenom.text?.toString().orEmpty(),
            localite = liaison.champLocalite.text?.toString().orEmpty(),
            dateNaissance = dateNaissance,
        )
        val sexe = if (liaison.sexeFeminin.isChecked) Sexe.FEMININ else Sexe.MASCULIN
        viewModel.enregistrer(saisie, sexe, modification)
    }

    /** Confirme l'abandon si quelque chose a été saisi. */
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

    private fun messageOuNull(motif: MotifInvalidite?) =
        motif?.let { messageErreur(it) }

    private fun terminer(message: Int) {
        signaler(message)
        finish()
    }

    private fun signaler(message: Int) {
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
    }

    companion object {
        private const val EXTRA_CODE = "code_planteur"

        /** 1990 par défaut : plus proche de l'âge courant qu'aujourd'hui. */
        private val DEFAUT_NAISSANCE = Dates.de(1990, 1, 1)

        /** Intention de création, ou de modification si un code est fourni. */
        fun intention(contexte: Context, code: String? = null): Intent =
            Intent(contexte, PlanteurFormActivity::class.java).apply {
                if (code != null) putExtra(EXTRA_CODE, code)
            }
    }
}
