using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>
/// Historique des variations de classement. Jamais supprimé : il constitue la
/// mémoire sportive du club (data lineage de l'ELO).
/// </summary>
public class EloHistory : Entity
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public int OldElo { get; set; }

    public int NewElo { get; set; }

    public int Delta => NewElo - OldElo;

    public int KFactor { get; set; }

    public decimal ExpectedScore { get; set; }

    public decimal ActualScore { get; set; }

    public int? GameId { get; set; }

    public TournamentGame? Game { get; set; }

    public int? TournamentId { get; set; }

    public int? OpponentId { get; set; }

    public int? OpponentElo { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Motif quand la variation est saisie manuellement par un arbitre.</summary>
    public string? Reason { get; set; }
}
