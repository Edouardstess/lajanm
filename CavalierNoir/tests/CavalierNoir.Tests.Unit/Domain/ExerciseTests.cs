using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Learning;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>Comportement des exercices : correction, statistiques et règle BR-08.</summary>
public class ExerciseTests
{
    private static Exercise MatDuCouloir() => new()
    {
        Title = "Le mat du couloir",
        Fen = "6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1",
        Solution = "a1a8",
        Theme = ExerciseTheme.MatEnUn,
        Difficulty = DifficultyLevel.TresFacile,
        IsPublished = true
    };

    [Theory]
    [InlineData("a1a8")]
    [InlineData("a1a8#")]
    [InlineData("1. a1a8#")]
    [InlineData("  a1a8 ")]
    public void IsCorrectAnswer_AccepteLesFormesEquivalentes(string reponse)
    {
        Assert.True(MatDuCouloir().IsCorrectAnswer(reponse));
    }

    [Theory]
    [InlineData("a1a7")]
    [InlineData("a1a8 g1g2")]
    [InlineData("")]
    [InlineData(null)]
    public void IsCorrectAnswer_RefuseLesMauvaisesReponses(string? reponse)
    {
        Assert.False(MatDuCouloir().IsCorrectAnswer(reponse));
    }

    [Fact]
    public void RegisterAttempt_MetAJourLeTauxDeReussite()
    {
        var exercice = MatDuCouloir();

        exercice.RegisterAttempt(isCorrect: true, timeSpentSeconds: 30);
        exercice.RegisterAttempt(isCorrect: false, timeSpentSeconds: 90);
        exercice.RegisterAttempt(isCorrect: true, timeSpentSeconds: 60);

        Assert.Equal(3, exercice.AttemptCount);
        Assert.Equal(2, exercice.SuccessCount);
        Assert.Equal(67, exercice.SuccessRate);
        Assert.Equal(60, exercice.AverageTimeSeconds);
    }

    [Fact]
    public void RegisterAttempt_PlafonneLeTempsPasse()
    {
        var exercice = MatDuCouloir();

        exercice.RegisterAttempt(isCorrect: true, timeSpentSeconds: 99_999);

        Assert.Equal(3600, exercice.TotalTimeSpentSeconds);
    }

    [Fact]
    public void SuccessRate_SansTentative_VautZero()
    {
        Assert.Equal(0, MatDuCouloir().SuccessRate);
    }

    [Fact]
    public void WhiteToMove_LitLaPosition()
    {
        Assert.True(MatDuCouloir().WhiteToMove);
        Assert.Equal("Les blancs jouent et gagnent", MatDuCouloir().SideToMoveLabel);
    }

    [Fact]
    public void Publish_RefuseUnePositionInvalide()
    {
        var exercice = MatDuCouloir();
        exercice.Fen = "position bancale";

        Assert.Throws<DomainException>(() => exercice.Publish(DateTime.UtcNow));
    }

    [Fact]
    public void Publish_RefuseUneSolutionVide()
    {
        var exercice = MatDuCouloir();
        exercice.Solution = "   ";

        Assert.Throws<DomainException>(() => exercice.Publish(DateTime.UtcNow));
    }

    [Fact]
    public void IsEligibleForDaily_RespecteLeDelaiDeTrenteJours()
    {
        var exercice = MatDuCouloir();
        var aujourdhui = new DateOnly(2026, 6, 1);

        Assert.True(exercice.IsEligibleForDaily(aujourdhui));

        exercice.LastUsedAsDailyOn = aujourdhui.AddDays(-10);
        Assert.False(exercice.IsEligibleForDaily(aujourdhui));

        exercice.LastUsedAsDailyOn = aujourdhui.AddDays(-30);
        Assert.True(exercice.IsEligibleForDaily(aujourdhui));
    }

    [Fact]
    public void IsEligibleForDaily_ExclutLesExercicesNonPublies()
    {
        var exercice = MatDuCouloir();
        exercice.IsPublished = false;

        Assert.False(exercice.IsEligibleForDaily(new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void AddRating_RefuseUneNoteHorsBornes()
    {
        var exercice = MatDuCouloir();

        Assert.Throws<DomainException>(() => exercice.AddRating(0));
        Assert.Throws<DomainException>(() => exercice.AddRating(6));
    }

    [Fact]
    public void AverageRating_MoyenneLesNotes()
    {
        var exercice = MatDuCouloir();

        exercice.AddRating(5);
        exercice.AddRating(4);

        Assert.Equal(4.5m, exercice.AverageRating);
    }

    [Fact]
    public void HintAt_RenvoieLIndiceDemande()
    {
        var exercice = MatDuCouloir();
        exercice.Hint1 = "Premier";
        exercice.Hint2 = "Deuxième";

        Assert.Equal("Premier", exercice.HintAt(1));
        Assert.Equal("Deuxième", exercice.HintAt(2));
        Assert.Null(exercice.HintAt(3));
        Assert.Null(exercice.HintAt(9));
        Assert.Equal(2, exercice.HintCount);
    }
}
