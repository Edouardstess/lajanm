using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Learning;

/// <summary>Tentative de résolution d'un exercice par un membre.</summary>
public class ExerciseAttempt : Entity
{
    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string? SubmittedMoves { get; set; }

    public bool IsCorrect { get; set; }

    public int TimeSpentSeconds { get; set; }

    public int HintsUsed { get; set; }

    /// <summary>Nombre de demi-coups justes avant la première erreur.</summary>
    public int CorrectPrefixLength { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Vrai lorsque la tentative provient de l'e-mail quotidien.</summary>
    public bool FromDailyEmail { get; set; }

    public int? Rating { get; set; }
}
