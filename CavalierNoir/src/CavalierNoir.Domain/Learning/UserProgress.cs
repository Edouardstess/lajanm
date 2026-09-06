using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Learning;

/// <summary>
/// Projection de la progression d'un membre, mise à jour à chaque tentative.
/// Évite d'agréger la table des tentatives à chaque affichage du tableau de bord.
/// </summary>
public class UserProgress : Entity
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int TotalAttempts { get; set; }

    public int CorrectAttempts { get; set; }

    public int TotalTimeSpentSeconds { get; set; }

    public int TotalHintsUsed { get; set; }

    /// <summary>Série de jours consécutifs avec au moins un exercice résolu.</summary>
    public int CurrentStreak { get; set; }

    public int LongestStreak { get; set; }

    public DateOnly? LastSolvedOn { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int SuccessRate => TotalAttempts == 0 ? 0 : (int)Math.Round(CorrectAttempts * 100.0 / TotalAttempts);

    public int AverageTimeSeconds => TotalAttempts == 0 ? 0 : TotalTimeSpentSeconds / TotalAttempts;

    /// <summary>Enregistre une tentative et met à jour la série quotidienne.</summary>
    public void Register(bool isCorrect, int timeSpentSeconds, int hintsUsed, DateOnly today, DateTime now)
    {
        TotalAttempts++;
        TotalTimeSpentSeconds += Math.Clamp(timeSpentSeconds, 0, 3600);
        TotalHintsUsed += hintsUsed;

        if (isCorrect)
        {
            CorrectAttempts++;

            if (LastSolvedOn is null)
            {
                CurrentStreak = 1;
            }
            else if (LastSolvedOn.Value == today)
            {
                // Déjà compté aujourd'hui : la série ne bouge pas.
            }
            else if (LastSolvedOn.Value.AddDays(1) == today)
            {
                CurrentStreak++;
            }
            else
            {
                CurrentStreak = 1;
            }

            LastSolvedOn = today;
            LongestStreak = Math.Max(LongestStreak, CurrentStreak);
        }

        UpdatedAt = now;
    }

    /// <summary>Rompt la série si aucun exercice n'a été résolu hier ni aujourd'hui.</summary>
    public void BreakStreakIfStale(DateOnly today)
    {
        if (LastSolvedOn is null)
        {
            CurrentStreak = 0;
            return;
        }

        if (LastSolvedOn.Value.AddDays(1) < today)
        {
            CurrentStreak = 0;
        }
    }
}
