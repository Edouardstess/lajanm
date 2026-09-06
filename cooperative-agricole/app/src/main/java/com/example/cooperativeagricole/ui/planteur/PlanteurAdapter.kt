package com.example.cooperativeagricole.ui.planteur

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.local.entity.Sexe
import com.example.cooperativeagricole.databinding.ItemPlanteurBinding
import com.example.cooperativeagricole.util.initialesDe

/**
 * Adapter du RecyclerView des planteurs.
 *
 * L'Adapter fait le lien entre les données et les vues : il crée les vues
 * réutilisables (`onCreateViewHolder`) et y place les valeurs d'un élément
 * (`onBindViewHolder`). Le RecyclerView, lui, ne garde en mémoire que les
 * quelques vues visibles, quelle que soit la taille de la liste.
 *
 * `ListAdapter` ajoute `DiffUtil` : il compare l'ancienne et la nouvelle
 * liste et n'anime que les lignes réellement modifiées, au lieu de tout
 * redessiner à chaque émission du Flow.
 */
class PlanteurAdapter(
    private val surClic: (Planteur) -> Unit,
) : ListAdapter<Planteur, PlanteurAdapter.VuePlanteur>(Difference) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VuePlanteur {
        val liaison = ItemPlanteurBinding.inflate(
            LayoutInflater.from(parent.context), parent, false
        )
        return VuePlanteur(liaison)
    }

    override fun onBindViewHolder(holder: VuePlanteur, position: Int) {
        holder.afficher(getItem(position))
    }

    inner class VuePlanteur(private val liaison: ItemPlanteurBinding) :
        RecyclerView.ViewHolder(liaison.root) {

        fun afficher(planteur: Planteur) = with(liaison) {
            val contexte = root.context
            initiales.text = initialesDe(planteur.prenom, planteur.nom)
            nomComplet.text = planteur.nomComplet
            sexeLocalite.text = contexte.getString(
                R.string.format_deux_valeurs,
                contexte.getString(libelleSexe(planteur.sexe)),
                planteur.localite,
            )
            code.text = contexte.getString(R.string.format_code_planteur, planteur.code)
            root.setOnClickListener { surClic(planteur) }
        }
    }

    private companion object Difference : DiffUtil.ItemCallback<Planteur>() {
        /** Deux lignes désignent le même planteur si le code est identique. */
        override fun areItemsTheSame(ancien: Planteur, nouveau: Planteur) =
            ancien.code == nouveau.code

        /** Le contenu affiché a-t-il changé ? `data class` fournit l'égalité. */
        override fun areContentsTheSame(ancien: Planteur, nouveau: Planteur) =
            ancien == nouveau
    }
}

/** Libellé d'un sexe, partagé par la liste et la fiche détaillée. */
fun libelleSexe(sexe: Sexe): Int = when (sexe) {
    Sexe.MASCULIN -> R.string.champ_sexe_masculin
    Sexe.FEMININ -> R.string.champ_sexe_feminin
}
