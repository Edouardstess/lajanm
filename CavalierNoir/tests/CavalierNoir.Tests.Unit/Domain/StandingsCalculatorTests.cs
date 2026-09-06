using CavalierNoir.Domain.Services;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>Vérifie le calcul des scores et des départages d'un tournoi.</summary>
public class StandingsCalculatorTests
{
    private static GameRecord Partie(int blancs, int noirs, decimal scoreBlancs) =>
        new(blancs, noirs, scoreBlancs, 1m - scoreBlancs, IsBye: false, IsPlayed: true);

    [Fact]
    public void Compute_AdditionneLesPointsDesDeuxCouleurs()
    {
        var lignes = StandingsCalculator.Compute(
            [1, 2, 3, 4],
            [
                Partie(1, 2, 1m),     // 1 bat 2
                Partie(3, 4, 0.5m),   // 3 et 4 font nulle
                Partie(1, 3, 1m),     // 1 bat 3
                Partie(4, 2, 1m)      // 4 bat 2
            ]);

        var premier = lignes.First(l => l.UserId == 1);
        var quatre = lignes.First(l => l.UserId == 4);
        var deux = lignes.First(l => l.UserId == 2);

        Assert.Equal(2m, premier.Score);
        Assert.Equal(1.5m, quatre.Score);
        Assert.Equal(0m, deux.Score);
    }

    [Fact]
    public void Compute_CompteVictoiresNullesEtDefaites()
    {
        var lignes = StandingsCalculator.Compute(
            [1, 2, 3],
            [
                Partie(1, 2, 1m),
                Partie(1, 3, 0.5m),
                Partie(2, 3, 0m)
            ]);

        var un = lignes.First(l => l.UserId == 1);

        Assert.Equal(1, un.Wins);
        Assert.Equal(1, un.Draws);
        Assert.Equal(0, un.Losses);
        Assert.Equal(2, un.GamesPlayed());
    }

    [Fact]
    public void Compute_Buchholz_EstLaSommeDesScoresDesAdversaires()
    {
        var lignes = StandingsCalculator.Compute(
            [1, 2, 3],
            [
                Partie(1, 2, 1m),   // 1 : 1 pt · 2 : 0 pt
                Partie(1, 3, 1m),   // 1 : 2 pts · 3 : 0 pt
                Partie(2, 3, 1m)    // 2 : 1 pt · 3 : 0 pt
            ]);

        // Le joueur 1 a rencontré 2 (1 pt) et 3 (0 pt) : Buchholz = 1.
        Assert.Equal(1m, lignes.First(l => l.UserId == 1).Buchholz);
    }

    [Fact]
    public void Compute_ClasseParScorePuisDepartage()
    {
        var lignes = StandingsCalculator.Compute(
            [1, 2, 3, 4],
            [
                Partie(1, 4, 1m),
                Partie(2, 3, 1m),
                Partie(1, 2, 0.5m),
                Partie(3, 4, 1m)
            ]);

        Assert.Equal(1, lignes[0].Rank);
        Assert.True(lignes[0].Score >= lignes[1].Score);
        Assert.True(lignes[1].Score >= lignes[2].Score);
    }

    [Fact]
    public void Compute_UnByeRapporteUnPointSansCompterCommeVictoire()
    {
        var lignes = StandingsCalculator.Compute(
            [1, 2, 3],
            [
                Partie(2, 3, 1m),
                new GameRecord(1, null, 1m, 0m, IsBye: true, IsPlayed: true)
            ]);

        var exempt = lignes.First(l => l.UserId == 1);

        Assert.Equal(1m, exempt.Score);
        Assert.Equal(0, exempt.Wins);
        Assert.Equal(0m, exempt.Buchholz);
    }

    [Fact]
    public void Compute_IgnoreLesPartiesNonJouees()
    {
        var lignes = StandingsCalculator.Compute(
            [1, 2],
            [new GameRecord(1, 2, 0m, 0m, IsBye: false, IsPlayed: false)]);

        Assert.All(lignes, l => Assert.Equal(0m, l.Score));
        Assert.All(lignes, l => Assert.Equal(0, l.GamesPlayed()));
    }

    [Fact]
    public void Compute_RangsPartagesEnCasDEgaliteComplete()
    {
        var lignes = StandingsCalculator.Compute([1, 2], []);

        Assert.Equal(1, lignes[0].Rank);
        Assert.Equal(1, lignes[1].Rank);
    }
}

/// <summary>Petit utilitaire de lisibilité pour les tests ci-dessus.</summary>
internal static class StandingRowExtensions
{
    public static int GamesPlayed(this StandingRow ligne) => ligne.Wins + ligne.Draws + ligne.Losses;
}
