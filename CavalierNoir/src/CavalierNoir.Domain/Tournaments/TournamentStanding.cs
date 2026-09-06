using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>
/// Ligne de classement recalculée après chaque ronde (modèle de lecture projeté).
/// </summary>
public class TournamentStanding : Entity
{
    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public decimal Score { get; set; }

    /// <summary>Départage Buchholz : somme des scores des adversaires.</summary>
    public decimal Buchholz { get; set; }

    /// <summary>Départage Buchholz tronqué (moins le plus faible adversaire).</summary>
    public decimal BuchholzCut1 { get; set; }

    /// <summary>Départage Sonneborn-Berger.</summary>
    public decimal SonnebornBerger { get; set; }

    public int Wins { get; set; }

    public int Draws { get; set; }

    public int Losses { get; set; }

    public int GamesPlayed => Wins + Draws + Losses;

    public int Rank { get; set; }

    public int EloBefore { get; set; }

    public int EloAfter { get; set; }

    public int EloDelta => EloAfter - EloBefore;

    public string? Prize { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
