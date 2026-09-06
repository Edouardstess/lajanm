using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Learning;

/// <summary>
/// Planification de l'exercice du jour. Une ligne par date : la contrainte
/// d'unicité sur <see cref="ScheduledOn"/> garantit qu'un seul exercice est envoyé.
/// </summary>
public class DailyExercise : AuditableEntity
{
    public DateOnly ScheduledOn { get; set; }

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    /// <summary>Choisi manuellement par le responsable pédagogique plutôt que par l'algorithme.</summary>
    public bool IsManualSelection { get; set; }

    public DateTime? DispatchedAt { get; set; }

    public int RecipientCount { get; set; }

    public int OpenedCount { get; set; }

    public int SolvedCount { get; set; }

    public string? DispatchError { get; set; }

    public bool IsDispatched => DispatchedAt.HasValue;
}
