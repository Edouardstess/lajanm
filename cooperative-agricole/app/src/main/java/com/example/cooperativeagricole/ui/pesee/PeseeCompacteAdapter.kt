package com.example.cooperativeagricole.ui.pesee

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.example.cooperativeagricole.R
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.databinding.ItemPeseeCompacteBinding
import com.example.cooperativeagricole.util.Dates
import com.example.cooperativeagricole.util.Formats

/**
 * Adapter de l'historique des pesées affiché dans la fiche d'un planteur.
 *
 * Il travaille sur des [Pesee] simples : dans ce contexte le planteur est
 * déjà connu, la jointure de la liste générale n'a pas lieu d'être.
 */
class PeseeCompacteAdapter(
    private val surClic: (Pesee) -> Unit,
    private val surOptions: (Pesee, View) -> Unit,
) : ListAdapter<Pesee, PeseeCompacteAdapter.VuePesee>(Difference) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VuePesee {
        val liaison = ItemPeseeCompacteBinding.inflate(
            LayoutInflater.from(parent.context), parent, false
        )
        return VuePesee(liaison)
    }

    override fun onBindViewHolder(holder: VuePesee, position: Int) {
        holder.afficher(getItem(position))
    }

    inner class VuePesee(private val liaison: ItemPeseeCompacteBinding) :
        RecyclerView.ViewHolder(liaison.root) {

        fun afficher(pesee: Pesee) = with(liaison) {
            val contexte = root.context
            date.text = Dates.formaterLong(pesee.datePesee)
            poids.text = contexte.getString(R.string.format_poids, Formats.poids(pesee.poidsKg))
            val note = pesee.observation
            observation.text = note.orEmpty()
            observation.visibility = if (note.isNullOrBlank()) View.GONE else View.VISIBLE
            root.setOnClickListener { surClic(pesee) }
            boutonOptions.setOnClickListener { vue -> surOptions(pesee, vue) }
        }
    }

    private companion object Difference : DiffUtil.ItemCallback<Pesee>() {
        override fun areItemsTheSame(ancien: Pesee, nouveau: Pesee) = ancien.id == nouveau.id

        override fun areContentsTheSame(ancien: Pesee, nouveau: Pesee) = ancien == nouveau
    }
}
