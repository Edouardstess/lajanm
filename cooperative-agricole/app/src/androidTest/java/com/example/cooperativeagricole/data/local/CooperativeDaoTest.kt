package com.example.cooperativeagricole.data.local

import android.content.Context
import android.database.sqlite.SQLiteConstraintException
import androidx.room.Room
import androidx.test.core.app.ApplicationProvider
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.cooperativeagricole.data.local.dao.PeseeDao
import com.example.cooperativeagricole.data.local.dao.PlanteurDao
import com.example.cooperativeagricole.data.local.database.CooperativeDatabase
import com.example.cooperativeagricole.data.local.entity.Pesee
import com.example.cooperativeagricole.data.local.entity.Planteur
import com.example.cooperativeagricole.data.local.entity.Sexe
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.test.runTest
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertThrows
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

/**
 * Tests instrumentés des DAO, sur une base Room en mémoire.
 *
 * Ils vérifient les garanties que l'on attend de la couche de persistance —
 * unicité du code, cascade de suppression, agrégats — plutôt que le simple
 * fait que Room compile.
 */
@RunWith(AndroidJUnit4::class)
class CooperativeDaoTest {

    private lateinit var base: CooperativeDatabase
    private lateinit var planteurDao: PlanteurDao
    private lateinit var peseeDao: PeseeDao

    private val planteur = Planteur(
        code = "PL-001",
        nom = "Dorvilus",
        prenom = "Jean Robert",
        sexe = Sexe.MASCULIN,
        dateNaissance = 0L,
        localite = "Furcy",
    )

    @Before
    fun ouvrir() {
        val contexte = ApplicationProvider.getApplicationContext<Context>()
        // Base en mémoire : chaque test repart d'un état vierge et rien n'est
        // écrit sur le disque de l'appareil.
        base = Room.inMemoryDatabaseBuilder(contexte, CooperativeDatabase::class.java)
            .allowMainThreadQueries()
            .build()
        planteurDao = base.planteurDao()
        peseeDao = base.peseeDao()
    }

    @After
    fun fermer() {
        base.close()
    }

    @Test
    fun unPlanteurInsereEstRelu() = runTest {
        planteurDao.inserer(planteur)

        val relu = planteurDao.trouverParCode("PL-001")

        assertEquals(planteur, relu)
        assertEquals(1, planteurDao.compterMaintenant())
    }

    @Test
    fun deuxPlanteursNePeuventPasPartagerLeMemeCode() = runTest {
        planteurDao.inserer(planteur)

        assertThrows(SQLiteConstraintException::class.java) {
            kotlinx.coroutines.runBlocking {
                planteurDao.inserer(planteur.copy(nom = "Autre"))
            }
        }
    }

    @Test
    fun unePeseeNePeutPasReferencerUnPlanteurInexistant() = runTest {
        assertThrows(SQLiteConstraintException::class.java) {
            kotlinx.coroutines.runBlocking {
                peseeDao.inserer(Pesee(planteurCode = "INCONNU", datePesee = 0L, poidsKg = 10.0))
            }
        }
    }

    @Test
    fun lesAgregatsSuiventLesPeseesDuPlanteur() = runTest {
        planteurDao.inserer(planteur)
        peseeDao.inserer(Pesee(planteurCode = "PL-001", datePesee = 1_000L, poidsKg = 10.5))
        peseeDao.inserer(Pesee(planteurCode = "PL-001", datePesee = 2_000L, poidsKg = 4.5))

        assertEquals(2, peseeDao.compterParPlanteur("PL-001").first())
        assertEquals(15.0, peseeDao.poidsTotalParPlanteur("PL-001").first()!!, 0.001)
        assertEquals(2_000L, peseeDao.dateDernierePeseeParPlanteur("PL-001").first())
    }

    @Test
    fun leTotalEstNulQuandLePlanteurNAAucunePesee() = runTest {
        planteurDao.inserer(planteur)

        assertEquals(0, peseeDao.compterParPlanteur("PL-001").first())
        assertNull(peseeDao.poidsTotalParPlanteur("PL-001").first())
    }

    @Test
    fun supprimerUnPlanteurSupprimeSesPesees() = runTest {
        planteurDao.inserer(planteur)
        peseeDao.inserer(Pesee(planteurCode = "PL-001", datePesee = 1_000L, poidsKg = 10.0))

        planteurDao.supprimer(planteur)

        assertEquals(0, peseeDao.compter().first())
    }

    @Test
    fun laRechercheTrouveParNomEtParLocalite() = runTest {
        planteurDao.inserer(planteur)
        planteurDao.inserer(
            planteur.copy(code = "PL-002", nom = "Célestin", prenom = "Rose", localite = "Kenscoff")
        )

        assertEquals(1, planteurDao.rechercher("Dorvilus").first().size)
        assertEquals(1, planteurDao.rechercher("Kenscoff").first().size)
        assertEquals(2, planteurDao.rechercher("PL-").first().size)
    }

    @Test
    fun laJointureRamenneLeNomDuPlanteur() = runTest {
        planteurDao.inserer(planteur)
        peseeDao.inserer(Pesee(planteurCode = "PL-001", datePesee = 1_000L, poidsKg = 10.0))

        val ligne = peseeDao.listerToutesAvecPlanteur().first().single()

        assertEquals("Jean Robert Dorvilus", ligne.nomCompletPlanteur)
        assertEquals("Furcy", ligne.planteurLocalite)
    }
}
