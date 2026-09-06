package com.example.cooperativeagricole.ui.pesee

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.PeseeAvecPlanteur
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.repository.PeseeRepository
import com.example.cooperativeagricole.data.repository.PlanteurRepository
import com.example.cooperativeagricole.domain.validation.ChampPesee
import com.example.cooperativeagricole.domain.validation.MotifInvalidite
import com.example.cooperativeagricole.domain.validation.SaisiePesee
import com.example.cooperativeagricole.domain.validation.ValidationPesee
import com.example.cooperativeagricole.util.Formats
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

data class EtatFormulairePesee(
    val erreurs: Map<ChampPesee, MotifInvalidite> = emptyMap(),
    val enCours: Boolean = false,
)

sealed interface EvenementPesee {
    data object Enregistree : EvenementPesee
    data object Modifiee : EvenementPesee
    data object Supprimee : EvenementPesee
    data object AucunPlanteur : EvenementPesee
    data object OuvrirFormulaire : EvenementPesee
    data object Echec : EvenementPesee
}

/**
 * ViewModel des pesées.
 *
 * Il dépend des deux repositories : celui des pesées pour le CRUD, et celui
 * des planteurs pour alimenter la liste déroulante du formulaire — une pesée
 * ne pouvant exister sans planteur.
 */
class PeseeViewModel(
    private val peseeRepository: PeseeRepository,
    private val planteurRepository: PlanteurRepository,
) : ViewModel() {

    val pesees: StateFlow<List<PeseeAvecPlanteur>> = peseeRepository.toutesAvecPlanteur
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    /** Alimente le sélecteur de planteur du formulaire. */
    val planteurs: StateFlow<List<Planteur>> = planteurRepository.tous
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    val nombrePesees: StateFlow<Int> = peseeRepository.nombre
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), 0)

    val poidsTotal: StateFlow<Double> = peseeRepository.poidsTotal
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), 0.0)

    private val _etatFormulaire = MutableStateFlow(EtatFormulairePesee())
    val etatFormulaire: StateFlow<EtatFormulairePesee> = _etatFormulaire.asStateFlow()

    private val _peseeAModifier = MutableStateFlow<Pesee?>(null)
    val peseeAModifier: StateFlow<Pesee?> = _peseeAModifier.asStateFlow()

    /**
     * Événements à usage unique. Le tampon d'un élément évite qu'une émission
     * reste bloquée quand aucun écran n'écoute (une suppression déclenchée
     * depuis un écran qui se ferme dans la foulée, par exemple) ; un événement
     * sans destinataire est simplement abandonné, ce qui est le comportement
     * voulu — il ne doit pas resurgir plus tard.
     */
    private val _evenements = MutableSharedFlow<EvenementPesee>(
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    val evenements: SharedFlow<EvenementPesee> = _evenements.asSharedFlow()

    fun preparerModification(id: Long) {
        viewModelScope.launch {
            _peseeAModifier.value = peseeRepository.trouver(id)
        }
    }

    /**
     * Demande l'ouverture du formulaire de saisie.
     *
     * Une pesée doit être rattachée à un planteur : s'il n'en existe aucun,
     * l'écran est prévenu au lieu d'ouvrir un formulaire impossible à
     * valider. La vérification interroge la base plutôt que la liste déjà
     * chargée, qui peut ne pas encore être arrivée.
     */
    fun demanderNouvelleSaisie() {
        viewModelScope.launch {
            val evenement = if (planteurRepository.compterMaintenant() == 0) {
                EvenementPesee.AucunPlanteur
            } else {
                EvenementPesee.OuvrirFormulaire
            }
            _evenements.emit(evenement)
        }
    }

    fun enregistrer(saisie: SaisiePesee, identifiant: Long?) {
        viewModelScope.launch {
            val erreurs = ValidationPesee.valider(saisie)
            if (erreurs.isNotEmpty()) {
                _etatFormulaire.value = EtatFormulairePesee(erreurs = erreurs)
                return@launch
            }

            _etatFormulaire.value = EtatFormulairePesee(enCours = true)

            // En modification, la pesée existante est recopiée : `copy` conserve
            // sa `cleDistante`, l'identité de la pesée dans la base distante.
            // La reconstruire de zéro en tirerait une nouvelle et laisserait un
            // document orphelin chez Firestore.
            val existante = _peseeAModifier.value
            val pesee = (existante ?: Pesee(planteurCode = "", datePesee = 0L, poidsKg = 0.0)).copy(
                id = identifiant ?: 0L,
                planteurCode = requireNotNull(saisie.planteurCode),
                datePesee = requireNotNull(saisie.date),
                poidsKg = requireNotNull(Formats.versNombre(saisie.poidsSaisi)),
                observation = saisie.observation?.trim()?.takeIf { it.isNotEmpty() },
            )

            runCatching {
                if (identifiant == null) peseeRepository.inserer(pesee)
                else peseeRepository.modifier(pesee)
            }.onSuccess {
                _etatFormulaire.value = EtatFormulairePesee()
                _evenements.emit(
                    if (identifiant == null) EvenementPesee.Enregistree else EvenementPesee.Modifiee
                )
            }.onFailure {
                _etatFormulaire.value = EtatFormulairePesee()
                _evenements.emit(EvenementPesee.Echec)
            }
        }
    }

    fun supprimer(pesee: Pesee) {
        viewModelScope.launch {
            runCatching { peseeRepository.supprimer(pesee) }
                .onSuccess { _evenements.emit(EvenementPesee.Supprimee) }
                .onFailure { _evenements.emit(EvenementPesee.Echec) }
        }
    }

    fun effacerErreur(champ: ChampPesee) {
        val erreurs = _etatFormulaire.value.erreurs
        if (erreurs.containsKey(champ)) {
            _etatFormulaire.value = _etatFormulaire.value.copy(erreurs = erreurs - champ)
        }
    }

    class Fabrique(
        private val peseeRepository: PeseeRepository,
        private val planteurRepository: PlanteurRepository,
    ) : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            require(modelClass.isAssignableFrom(PeseeViewModel::class.java)) {
                "ViewModel inconnu : ${modelClass.name}"
            }
            return PeseeViewModel(peseeRepository, planteurRepository) as T
        }
    }
}
