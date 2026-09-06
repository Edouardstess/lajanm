using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.ValueObjects;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>Invariants des objets-valeurs du domaine.</summary>
public class ValueObjectTests
{
    [Fact]
    public void Money_ArrondiADeuxDecimales()
    {
        Assert.Equal(10.13m, new Money(10.125m, "HTG").Amount);
    }

    [Fact]
    public void Money_NormaliseLaDevise()
    {
        Assert.Equal("HTG", new Money(10m, " htg ").Currency);
    }

    [Fact]
    public void Money_AdditionDeDevisesDifferentes_EstRefusee()
    {
        var gourdes = Money.Htg(100m);
        var dollars = Money.Usd(100m);

        var exception = Assert.Throws<DomainException>(() => gourdes.Add(dollars));

        Assert.Equal("BR-14", exception.Code);
    }

    [Fact]
    public void Money_AdditionEtSoustraction()
    {
        var somme = Money.Htg(1500m) + Money.Htg(750m);
        var reste = somme - Money.Htg(250m);

        Assert.Equal(2250m, somme.Amount);
        Assert.Equal(2000m, reste.Amount);
    }

    [Fact]
    public void Money_EgaliteStructurelle()
    {
        Assert.Equal(Money.Htg(1500m), new Money(1500m, "HTG"));
        Assert.NotEqual(Money.Htg(1500m), Money.Usd(1500m));
    }

    [Fact]
    public void DateRange_FinAvantDebut_EstRefusee()
    {
        Assert.Throws<DomainException>(
            () => new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public void DateRange_ContientLesBornes()
    {
        var periode = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.True(periode.Contains(new DateOnly(2026, 1, 1)));
        Assert.True(periode.Contains(new DateOnly(2026, 12, 31)));
        Assert.False(periode.Contains(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void DateRange_OneYearFrom_Donne365Jours()
    {
        var periode = DateRange.OneYearFrom(new DateOnly(2026, 1, 1));

        Assert.Equal(365, periode.DurationInDays);
    }

    [Fact]
    public void DateRange_DetecteLeChevauchement()
    {
        var premier = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        var second = new DateRange(new DateOnly(2026, 6, 30), new DateOnly(2026, 12, 31));
        var troisieme = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31));

        Assert.True(premier.Overlaps(second));
        Assert.False(premier.Overlaps(troisieme));
    }

    [Theory]
    [InlineData(199)]
    [InlineData(3001)]
    public void EloRating_HorsBornes_EstRefuse(int valeur)
    {
        var exception = Assert.Throws<DomainException>(() => new EloRating(valeur));

        Assert.Equal("BR-07", exception.Code);
    }

    [Fact]
    public void EloRating_ShiftResteDansLesBornes()
    {
        Assert.Equal(200, new EloRating(210).Shift(-500).Value);
        Assert.Equal(3000, new EloRating(2990).Shift(500).Value);
    }

    [Theory]
    [InlineData("Tournoi d'été 2026", "tournoi-d-ete-2026")]
    [InlineData("Le Cavalier Noir — présentation", "le-cavalier-noir-presentation")]
    [InlineData("  Espaces   multiples  ", "espaces-multiples")]
    [InlineData("Ààéèêç", "aaeeec")]
    [InlineData("!!!", "element")]
    [InlineData("", "element")]
    [InlineData(null, "element")]
    public void Slug_ProduitUnIdentifiantLisible(string? entree, string attendu)
    {
        Assert.Equal(attendu, Slug.From(entree));
    }

    [Fact]
    public void Slug_WithSuffix_LeveLesCollisions()
    {
        Assert.Equal("mon-article-2", Slug.WithSuffix("mon-article", 2));
    }
}
