using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Services;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>
/// Vérifie la formule ELO de la FIDE. Les valeurs de référence sont celles
/// publiées dans le manuel FIDE (table des écarts de classement).
/// </summary>
public class EloCalculatorTests
{
    [Fact]
    public void ExpectedScore_DeuxJoueursDeMemeNiveau_EstUneChanceSurDeux()
    {
        var attendu = EloCalculator.ExpectedScore(1500, 1500);

        Assert.Equal(0.5m, attendu);
    }

    [Theory]
    [InlineData(1600, 1500, 0.64)]   // +100 points d'écart ≈ 64 %
    [InlineData(1500, 1600, 0.36)]   // −100 points d'écart ≈ 36 %
    [InlineData(1900, 1500, 0.91)]   // +400 points d'écart ≈ 91 %
    public void ExpectedScore_SuitLaTableFide(int joueur, int adversaire, double attendu)
    {
        var resultat = (double)EloCalculator.ExpectedScore(joueur, adversaire);

        Assert.InRange(resultat, attendu - 0.01, attendu + 0.01);
    }

    [Fact]
    public void ExpectedScore_EstSymetrique()
    {
        var blancs = EloCalculator.ExpectedScore(1720, 1480);
        var noirs = EloCalculator.ExpectedScore(1480, 1720);

        Assert.Equal(1m, blancs + noirs);
    }

    [Fact]
    public void Compute_VictoireContrePlusFort_RapporteBeaucoupDePoints()
    {
        var resultat = EloCalculator.Compute(1400, 1800, actualScore: 1m, kFactor: 32);

        Assert.True(resultat.Delta > 25, $"Le gain attendu dépasse 25 points, obtenu : {resultat.Delta}.");
        Assert.Equal(1400, resultat.OldElo);
        Assert.Equal(1400 + resultat.Delta, resultat.NewElo);
    }

    [Fact]
    public void Compute_VictoireContrePlusFaible_RapportePeuDePoints()
    {
        var resultat = EloCalculator.Compute(1800, 1400, actualScore: 1m, kFactor: 32);

        Assert.InRange(resultat.Delta, 1, 6);
    }

    [Fact]
    public void Compute_UneVictoireNeFaitJamaisPerdreDePoints()
    {
        // Écart extrême : l'espérance de gain vaut pratiquement 1, l'arrondi
        // donnerait 0 sans le garde-fou.
        var resultat = EloCalculator.Compute(2900, 800, actualScore: 1m, kFactor: 16);

        Assert.True(resultat.Delta >= 1);
    }

    [Fact]
    public void Compute_UneDefaiteNeFaitJamaisGagnerDePoints()
    {
        var resultat = EloCalculator.Compute(800, 2900, actualScore: 0m, kFactor: 16);

        Assert.True(resultat.Delta <= -1);
    }

    [Fact]
    public void ComputeForGame_LesVariationsSeCompensentApprox()
    {
        var (blancs, noirs) = EloCalculator.ComputeForGame(1600, 1600, whiteScore: 1m, 32, 32);

        Assert.Equal(16, blancs.Delta);
        Assert.Equal(-16, noirs.Delta);
    }

    [Fact]
    public void Compute_NeSortJamaisDesBornes()
    {
        var basse = EloCalculator.Compute(205, 2800, actualScore: 0m, kFactor: 40);
        var haute = EloCalculator.Compute(2995, 400, actualScore: 1m, kFactor: 40);

        Assert.True(basse.NewElo >= 200);
        Assert.True(haute.NewElo <= 3000);
    }

    [Theory]
    [InlineData(0, 1200, TimeControl.Classique, EloCalculator.KFactorNewPlayer)]
    [InlineData(19, 1200, TimeControl.Classique, EloCalculator.KFactorNewPlayer)]
    [InlineData(25, 1200, TimeControl.Classique, EloCalculator.KFactorDeveloping)]
    [InlineData(120, 1200, TimeControl.Classique, EloCalculator.KFactorEstablished)]
    [InlineData(120, 2500, TimeControl.Classique, EloCalculator.KFactorEstablished)]
    [InlineData(5, 1200, TimeControl.Blitz, EloCalculator.KFactorFast)]
    public void KFactorFor_AppliqueLaRegleBr07(
        int partiesJouees,
        int elo,
        TimeControl cadence,
        int attendu)
    {
        Assert.Equal(attendu, EloCalculator.KFactorFor(partiesJouees, elo, cadence));
    }

    [Fact]
    public void Compute_ScoreInvalide_EstRejete()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => EloCalculator.Compute(1500, 1500, actualScore: 1.5m, kFactor: 32));
    }
}
