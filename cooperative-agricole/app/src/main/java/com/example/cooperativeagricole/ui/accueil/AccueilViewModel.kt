package com.example.cooperativeagricole.ui.accueil

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.example.cooperativeagricole.data.repository.PeseeRepository
import com.example.cooperativeagricole.data.repository.PlanteurRepository
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn

/** Ce que l'écran d'accueil affiche, en un seul objet. */
data class EtatAccueil(
    val nombrePlanteurs: Int = 0,
    val nombrePesees: Int = 0,
    val poidsTotal: Double = 0.0,
)

/**
 * ViewModel de l'écran d'accueil.
 *
 * Il assemble les trois compteurs en un seul état. Les émettre séparément
 * ferait clignoter l'écran, chaque valeur arrivant à son rythme ; ici,
 * l'accueil se redessine une fois par changement réel.
 */
class AccueilViewModel(
    planteurRepository: PlanteurRepository,
    peseeRepository: PeseeRepository,
) : ViewModel() {

    val etat: StateFlow<EtatAccueil> = combine(
        planteurRepository.nombre,
        peseeRepository.nombre,
        peseeRepository.poidsTotal,
    ) { planteurs, pesees, poids ->
        EtatAccueil(planteurs, pesees, poids)
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), EtatAccueil())

    class Fabrique(
        private val planteurRepository: PlanteurRepository,
        private val peseeRepository: PeseeRepository,
    ) : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            require(modelClass.isAssignableFrom(AccueilViewModel::class.java)) {
                "ViewModel inconnu : ${modelClass.name}"
            }
            return AccueilViewModel(planteurRepository, peseeRepository) as T
        }
    }
}
