package com.example.cooperativeagricole.ui.planteur

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.local.entity.Sexe
import com.example.cooperativeagricole.data.repository.PeseeRepository
import com.example.cooperativeagricole.data.repository.PlanteurRepository
import com.example.cooperativeagricole.domain.validation.ChampPlanteur
import com.example.cooperativeagricole.domain.validation.MotifInvalidite
import com.example.cooperativeagricole.domain.validation.SaisiePlanteur
import com.example.cooperativeagricole.domain.validation.ValidationPlanteur
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

/** Détail d'un planteur : sa fiche et la synthèse de ses pesées. */
data class DetailPlanteur(
    val planteur: Planteur,
    val pesees: List<Pesee>,
    val nombrePesees: Int,
    val poidsTotal: Double,
    val dateDernierePesee: Long?,
)

/** État du formulaire : uniquement ce que l'écran doit afficher. */
data class EtatFormulairePlanteur(
    val erreurs: Map<ChampPlanteur, MotifInvalidite> = emptyMap(),
    val enCours: Boolean = false,
)

/** Événement ponctuel (message, fermeture d'écran), à consommer une fois. */
sealed interface EvenementPlanteur {
    data object Enregistre : EvenementPlanteur
    data object Modifie : EvenementPlanteur
    data object Supprime : EvenementPlanteur
    data object Echec : EvenementPlanteur
}

/**
 * ViewModel des planteurs.
 *
 * Il porte l'état de l'interface et la logique de présentation ; il survit
 * aux rotations d'écran, ce qui évite de relire la base à chaque changement
 * de configuration. Il ne connaît ni Activity, ni View, ni Context : il ne
 * parle qu'aux repositories.
 */
@OptIn(ExperimentalCoroutinesApi::class)
class PlanteurViewModel(
    private val planteurRepository: PlanteurRepository,
    private val peseeRepository: PeseeRepository,
) : ViewModel() {

    // --- Liste ---

    private val _recherche = MutableStateFlow("")
    val recherche: StateFlow<String> = _recherche.asStateFlow()

    /**
     * `flatMapLatest` : à chaque frappe, la requête précédente est annulée et
     * remplacée par la nouvelle. `stateIn` transforme le flux froid en état
     * observable partagé par l'écran.
     */
    val planteurs: StateFlow<List<Planteur>> = _recherche
        .flatMapLatest { texte ->
            if (texte.isBlank()) planteurRepository.tous else planteurRepository.rechercher(texte)
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    val nombrePlanteurs: StateFlow<Int> = planteurRepository.nombre
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), 0)

    // --- Détail ---

    private val _codeSelectionne = MutableStateFlow<String?>(null)

    val detail: StateFlow<DetailPlanteur?> = _codeSelectionne
        .flatMapLatest { code ->
            if (code == null) {
                flowOf(null)
            } else {
                combine(
                    planteurRepository.observer(code),
                    peseeRepository.listerParPlanteur(code),
                    peseeRepository.nombreParPlanteur(code),
                    peseeRepository.poidsTotalParPlanteur(code),
                    peseeRepository.dateDernierePesee(code),
                ) { planteur, pesees, nombre, poids, derniere ->
                    planteur?.let { DetailPlanteur(it, pesees, nombre, poids, derniere) }
                }
            }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), null)

    // --- Formulaire ---

    private val _etatFormulaire = MutableStateFlow(EtatFormulairePlanteur())
    val etatFormulaire: StateFlow<EtatFormulairePlanteur> = _etatFormulaire.asStateFlow()

    private val _planteurAModifier = MutableStateFlow<Planteur?>(null)
    val planteurAModifier: StateFlow<Planteur?> = _planteurAModifier.asStateFlow()

    /**
     * Événements à usage unique. Le tampon d'un élément évite qu'une émission
     * reste bloquée quand aucun écran n'écoute (une suppression déclenchée
     * depuis un écran qui se ferme dans la foulée, par exemple) ; un événement
     * sans destinataire est simplement abandonné, ce qui est le comportement
     * voulu — il ne doit pas resurgir plus tard.
     */
    private val _evenements = MutableSharedFlow<EvenementPlanteur>(
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    val evenements: SharedFlow<EvenementPlanteur> = _evenements.asSharedFlow()

    fun mettreAJourRecherche(texte: String) {
        _recherche.value = texte
    }

    fun selectionner(code: String) {
        _codeSelectionne.value = code
    }

    /** Charge la fiche à modifier pour pré-remplir le formulaire. */
    fun preparerModification(code: String) {
        viewModelScope.launch {
            _planteurAModifier.value = planteurRepository.trouver(code)
        }
    }

    /**
     * Enregistre une création ou une modification.
     *
     * En modification, le code n'est pas modifiable : il est la clé primaire
     * et la cible des clés étrangères des pesées. Le formulaire l'affiche donc
     * en lecture seule, ce qui rend l'opération sûre côté base.
     */
    fun enregistrer(saisie: SaisiePlanteur, sexe: Sexe, modification: Boolean) {
        viewModelScope.launch {
            val erreurs = ValidationPlanteur.valider(saisie).toMutableMap()

            val code = saisie.code.trim()
            if (!modification && !erreurs.containsKey(ChampPlanteur.CODE) &&
                planteurRepository.existe(code)
            ) {
                erreurs[ChampPlanteur.CODE] = MotifInvalidite.DEJA_UTILISE
            }

            if (erreurs.isNotEmpty()) {
                _etatFormulaire.value = EtatFormulairePlanteur(erreurs = erreurs)
                return@launch
            }

            _etatFormulaire.value = EtatFormulairePlanteur(enCours = true)
            val planteur = Planteur(
                code = code,
                nom = saisie.nom.trim(),
                prenom = saisie.prenom.trim(),
                sexe = sexe,
                dateNaissance = requireNotNull(saisie.dateNaissance),
                localite = saisie.localite.trim(),
            )

            runCatching {
                if (modification) planteurRepository.modifier(planteur)
                else planteurRepository.inserer(planteur)
            }.onSuccess {
                _etatFormulaire.value = EtatFormulairePlanteur()
                _evenements.emit(
                    if (modification) EvenementPlanteur.Modifie else EvenementPlanteur.Enregistre
                )
            }.onFailure {
                _etatFormulaire.value = EtatFormulairePlanteur()
                _evenements.emit(EvenementPlanteur.Echec)
            }
        }
    }

    /** Supprime un planteur ; ses pesées suivent, par cascade. */
    fun supprimer(planteur: Planteur) {
        viewModelScope.launch {
            runCatching { planteurRepository.supprimer(planteur) }
                .onSuccess { _evenements.emit(EvenementPlanteur.Supprime) }
                .onFailure { _evenements.emit(EvenementPlanteur.Echec) }
        }
    }

    /** Efface l'erreur d'un champ dès que l'utilisateur le corrige. */
    fun effacerErreur(champ: ChampPlanteur) {
        val erreurs = _etatFormulaire.value.erreurs
        if (erreurs.containsKey(champ)) {
            _etatFormulaire.value = _etatFormulaire.value.copy(erreurs = erreurs - champ)
        }
    }

    /**
     * Fabrique de ViewModel : le ViewModel a des dépendances (les
     * repositories), le système ne sait donc pas l'instancier seul.
     */
    class Fabrique(
        private val planteurRepository: PlanteurRepository,
        private val peseeRepository: PeseeRepository,
    ) : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            require(modelClass.isAssignableFrom(PlanteurViewModel::class.java)) {
                "ViewModel inconnu : ${modelClass.name}"
            }
            return PlanteurViewModel(planteurRepository, peseeRepository) as T
        }
    }
}
