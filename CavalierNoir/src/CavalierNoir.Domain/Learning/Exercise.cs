using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Learning;

/// <summary>
/// Exercice tactique : une position FEN, une solution attendue, trois indices
/// progressifs et une explication. Racine de l'agrégat Exercice.
/// </summary>
public class Exercise : AuditableEntity, IAggregateRoot, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Position de départ au format FEN, validée par <see cref="Fen"/>.</summary>
    public string Fen { get; set; } = ValueObjects.Fen.StartingPosition;

    /// <summary>Suite de coups attendue, en notation SAN.</summary>
    public string Solution { get; set; } = string.Empty;

    public ExerciseTheme Theme { get; set; } = ExerciseTheme.MatEnUn;

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Moyen;

    public int? EstimatedTimeSeconds { get; set; }

    public string? Hint1 { get; set; }

    public string? Hint2 { get; set; }

    public string? Hint3 { get; set; }

    public string? Explanation { get; set; }

    /// <summary>Source de la position : nom d'une partie, d'un ouvrage, d'un auteur…</summary>
    public string? Source { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Visible des visiteurs non authentifiés (« exercice de la semaine »).</summary>
    public bool IsPublic { get; set; }

    public int? CollectionId { get; set; }

    public ExerciseCollection? Collection { get; set; }

    /// <summary>Dernière date d'utilisation comme exercice du jour (BR-08 : pas de répétition sous 30 jours).</summary>
    public DateOnly? LastUsedAsDailyOn { get; set; }

    public int AttemptCount { get; set; }

    public int SuccessCount { get; set; }

    public int TotalTimeSpentSeconds { get; set; }

    public decimal RatingSum { get; set; }

    public int RatingCount { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }

    public ICollection<ExerciseAttempt> Attempts { get; set; } = new List<ExerciseAttempt>();

    // --- Comportement métier ---

    /// <summary>Taux de réussite en pourcentage, recalculé à chaque tentative.</summary>
    public int SuccessRate => AttemptCount == 0 ? 0 : (int)Math.Round(SuccessCount * 100.0 / AttemptCount);

    public int AverageTimeSeconds => AttemptCount == 0 ? 0 : TotalTimeSpentSeconds / AttemptCount;

    public decimal AverageRating => RatingCount == 0 ? 0m : decimal.Round(RatingSum / RatingCount, 1);

    public bool WhiteToMove => ValueObjects.Fen.TryParse(Fen, out var parsed, out _) && parsed!.WhiteToMove;

    public string SideToMoveLabel => WhiteToMove ? "Les blancs jouent et gagnent" : "Les noirs jouent et gagnent";

    public int HintCount => new[] { Hint1, Hint2, Hint3 }.Count(h => !string.IsNullOrWhiteSpace(h));

    public string? HintAt(int index) => index switch
    {
        1 => Hint1,
        2 => Hint2,
        3 => Hint3,
        _ => null
    };

    /// <summary>Vérifie la réponse d'un membre contre la solution enregistrée.</summary>
    public bool IsCorrectAnswer(string? submitted)
    {
        var expected = MoveSequence.Parse(Solution);
        var answer = MoveSequence.Parse(submitted);
        return answer.Matches(expected);
    }

    public void RegisterAttempt(bool isCorrect, int timeSpentSeconds)
    {
        AttemptCount++;
        if (isCorrect)
        {
            SuccessCount++;
        }

        TotalTimeSpentSeconds += Math.Clamp(timeSpentSeconds, 0, 3600);
    }

    public void AddRating(int stars)
    {
        if (stars is < 1 or > 5)
        {
            throw new DomainException("Une note doit être comprise entre 1 et 5.");
        }

        RatingSum += stars;
        RatingCount++;
    }

    public void Publish(DateTime when)
    {
        if (!ValueObjects.Fen.IsValid(Fen))
        {
            throw new DomainException("La position FEN doit être valide avant publication.");
        }

        if (MoveSequence.Parse(Solution).IsEmpty)
        {
            throw new DomainException("La solution doit contenir au moins un coup.");
        }

        IsPublished = true;
        UpdatedAt = when;
    }

    /// <summary>BR-08 : un exercice ne peut pas être renvoyé avant 30 jours.</summary>
    public bool IsEligibleForDaily(DateOnly today, int cooldownDays = 30) =>
        IsPublished && (LastUsedAsDailyOn is null || LastUsedAsDailyOn.Value.AddDays(cooldownDays) <= today);
}
