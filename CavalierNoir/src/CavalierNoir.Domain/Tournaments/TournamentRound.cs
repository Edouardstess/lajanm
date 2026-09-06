using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>Ronde d'un tournoi : un lot d'appariements joués simultanément.</summary>
public class TournamentRound : AuditableEntity
{
    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public int Number { get; set; }

    public RoundStatus Status { get; set; } = RoundStatus.Planifiee;

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateTime? PairedAt { get; set; }

    public int? PairedById { get; set; }

    public ICollection<TournamentGame> Games { get; set; } = new List<TournamentGame>();

    public bool AllGamesPlayed => Games.Count > 0 && Games.All(g => g.Result != GameResult.NonJouee);

    public void MarkPaired(int arbiterId, DateTime when)
    {
        Status = RoundStatus.Apparee;
        PairedById = arbiterId;
        PairedAt = when;
        UpdatedAt = when;
    }

    public void Close(DateTime when)
    {
        if (!AllGamesPlayed)
        {
            throw new DomainException("Toutes les parties de la ronde doivent avoir un résultat.");
        }

        Status = RoundStatus.Terminee;
        EndTime = when;
        UpdatedAt = when;
    }
}
