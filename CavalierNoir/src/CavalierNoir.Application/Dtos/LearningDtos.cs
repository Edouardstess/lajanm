using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Application.Dtos;

/// <summary>Critères de recherche de la bibliothèque d'exercices.</summary>
public sealed class ExerciseFilter
{
    public string? Search { get; init; }

    public ExerciseTheme? Theme { get; init; }

    public DifficultyLevel? Difficulty { get; init; }

    public int? CollectionId { get; init; }

    /// <summary>Restreint aux exercices visibles des visiteurs non authentifiés.</summary>
    public bool PublicOnly { get; init; }

    /// <summary>Exclut les exercices déjà réussis par ce membre.</summary>
    public int? UnsolvedByUserId { get; init; }

    public bool IncludeUnpublished { get; init; }

    /// <summary>« recent », « difficulte », « populaire », « reussite ».</summary>
    public string Sort { get; init; } = "recent";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 12;
}

/// <summary>Carte d'exercice affichée dans la bibliothèque.</summary>
public sealed record ExerciseCard(
    int Id,
    string Title,
    string Fen,
    ExerciseTheme Theme,
    DifficultyLevel Difficulty,
    bool WhiteToMove,
    int SuccessRate,
    int AttemptCount,
    bool IsPublic,
    bool IsPublished,
    bool SolvedByCurrentUser);

/// <summary>Réponse soumise par un membre.</summary>
public sealed class ExerciseSubmission
{
    public int ExerciseId { get; init; }

    public int UserId { get; init; }

    public string? Moves { get; init; }

    public int TimeSpentSeconds { get; init; }

    public int HintsUsed { get; init; }

    public bool FromDailyEmail { get; init; }
}

/// <summary>Correction renvoyée après une tentative.</summary>
public sealed record ExerciseCorrection(
    bool IsCorrect,
    int CorrectPrefixLength,
    string ExpectedSolution,
    string? Explanation,
    int NewStreak,
    IReadOnlyList<string> BadgesEarned);

/// <summary>Statistiques personnelles du tableau de bord.</summary>
public sealed record ProgressSummary(
    int TotalAttempts,
    int CorrectAttempts,
    int SuccessRate,
    int CurrentStreak,
    int LongestStreak,
    int AverageTimeSeconds,
    DateOnly? LastSolvedOn);
