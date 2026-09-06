using CavalierNoir.Domain.ValueObjects;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>
/// Comparaison des suites de coups : la correction doit accepter les notations
/// équivalentes sans jamais valider une réponse fausse.
/// </summary>
public class MoveSequenceTests
{
    [Fact]
    public void Parse_ChaineVide_DonneUneSuiteVide()
    {
        Assert.True(MoveSequence.Parse(null).IsEmpty);
        Assert.True(MoveSequence.Parse("   ").IsEmpty);
    }

    [Fact]
    public void Parse_RetireLaNumerotationDesCoups()
    {
        var suite = MoveSequence.Parse("1. e2e4 e7e5 2. g1f3");

        Assert.Equal(3, suite.Count);
        Assert.Equal(["e2e4", "e7e5", "g1f3"], suite.Moves);
    }

    [Fact]
    public void Parse_IgnoreLesCommentairesEtLeResultat()
    {
        var suite = MoveSequence.Parse("1. e2e4 {une bonne case} e7e5 (variante) 1-0");

        Assert.Equal(["e2e4", "e7e5"], suite.Moves);
    }

    [Theory]
    [InlineData("a1a8", "a1a8")]
    [InlineData("a1a8#", "a1a8")]
    [InlineData("1. a1a8#", "a1a8")]
    [InlineData("a1xa8+", "a1a8")]
    [InlineData("  a1a8  ", "a1a8")]
    public void Matches_ToleranteAuxAnnotations(string saisie, string attendue)
    {
        var reponse = MoveSequence.Parse(saisie);
        var solution = MoveSequence.Parse(attendue);

        Assert.True(reponse.Matches(solution));
    }

    [Fact]
    public void Matches_RefuseUneSuiteDeLongueurDifferente()
    {
        var reponse = MoveSequence.Parse("e2e4");
        var solution = MoveSequence.Parse("e2e4 e7e5");

        Assert.False(reponse.Matches(solution));
    }

    [Fact]
    public void Matches_RefuseUnCoupDifferent()
    {
        var reponse = MoveSequence.Parse("a1a7");
        var solution = MoveSequence.Parse("a1a8");

        Assert.False(reponse.Matches(solution));
    }

    [Fact]
    public void Matches_UneSuiteVideNeValideJamais()
    {
        Assert.False(MoveSequence.Parse("").Matches(MoveSequence.Parse("a1a8")));
        Assert.False(MoveSequence.Parse("a1a8").Matches(MoveSequence.Parse("")));
    }

    [Fact]
    public void Matches_DistingueLaCasseDesLettresDePiece()
    {
        // En notation algébrique, « B » est le fou et « b » la colonne b :
        // confondre les deux validerait des réponses fausses.
        var reponse = MoveSequence.Parse("Bc4");
        var solution = MoveSequence.Parse("bc4");

        Assert.False(reponse.Matches(solution));
    }

    [Fact]
    public void CommonPrefixLength_CompteLesDemiCoupsJustes()
    {
        var reponse = MoveSequence.Parse("e2e4 e7e5 b1c3");
        var solution = MoveSequence.Parse("e2e4 e7e5 g1f3");

        Assert.Equal(2, reponse.CommonPrefixLength(solution));
    }

    [Fact]
    public void RoqueRamèneAUneFormeCanonique()
    {
        Assert.Equal(MoveSequence.Parse("O-O").Moves, MoveSequence.Parse("0-0").Moves);
        Assert.Equal(MoveSequence.Parse("O-O-O").Moves, MoveSequence.Parse("000").Moves);
    }

    [Fact]
    public void ToNumberedNotation_RestitueLaNumerotation()
    {
        var suite = MoveSequence.Parse("e2e4 e7e5 g1f3");

        Assert.Equal("1. e2e4 e7e5 2. g1f3", suite.ToNumberedNotation());
    }
}
