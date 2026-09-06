package com.example.cooperativeagricole.ui.pesee

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.data.local.entity.PeseeAvecPlanteur
import com.example.cooperativeagricole.databinding.ItemPeseeBinding
import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.Formats

/**
 * Adapter de la liste générale des pesées : chaque ligne montre le planteur,
 * la date, le poids, et propose un menu « modifier / supprimer ».
 */
class PeseeAdapter(
    private val surClic: (PeseeAvecPlanteur) -> Unit,
    private val surOptions: (PeseeAvecPlanteur, View) -> Unit,
) : ListAdapter<PeseeAvecPlanteur, PeseeAdapter.VuePesee>(Difference) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VuePesee {
        val liaison = ItemPeseeBinding.inflate(
            LayoutInflater.from(parent.context), parent, false
        )
        return VuePesee(liaison)
    }

    override fun onBindViewHolder(holder: VuePesee, position: Int) {
        holder.afficher(getItem(position))
    }

    inner class VuePesee(private val liaison: ItemPeseeBinding) :
        RecyclerView.ViewHolder(liaison.root) {

        fun afficher(element: PeseeAvecPlanteur) = with(liaison) {
            val contexte = root.context
            planteur.text = element.nomCompletPlanteur
            date.text = contexte.getString(
                R.string.format_deux_valeurs,
                Dates.formater(element.pesee.datePesee),
                element.planteurLocalite,
            )
            poids.text = contexte.getString(
                R.string.format_poids,
                Formats.poids(element.pesee.poidsKg),
            )
            val note = element.pesee.observation
            observation.text = note.orEmpty()
            observation.visibility = if (note.isNullOrBlank()) View.GONE else View.VISIBLE
            root.setOnClickListener { surClic(element) }
            boutonOptions.setOnClickListener { vue -> surOptions(element, vue) }
        }
    }

    private companion object Difference : DiffUtil.ItemCallback<PeseeAvecPlanteur>() {
        override fun areItemsTheSame(ancien: PeseeAvecPlanteur, nouveau: PeseeAvecPlanteur) =
            ancien.pesee.id == nouveau.pesee.id

        override fun areContentsTheSame(ancien: PeseeAvecPlanteur, nouveau: PeseeAvecPlanteur) =
            ancien == nouveau
    }
}
