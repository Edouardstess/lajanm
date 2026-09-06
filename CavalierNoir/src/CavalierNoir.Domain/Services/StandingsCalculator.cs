namespace CavalierNoir.Domain.Services;

/// <summary>Partie jouée, réduite à ce dont le classement a besoin.</summary>
/// <param name="WhiteId">Joueur des blancs.</param>
/// <param name="BlackId">Joueur des noirs, nul en cas de bye.</param>
/// <param name="WhiteScore">Points marqués par les blancs (1 / 0,5 / 0).</param>
/// <param name="BlackScore">Points marqués par les noirs.</param>
/// <param name="IsBye">Vrai pour un point d'office.</param>
/// <param name="IsPlayed">Faux tant que le résultat n'est pas saisi.</param>
public sealed record GameRecord(
    int WhiteId,
    int? BlackId,
    decimal WhiteScore,
    decimal BlackScore,
    bool IsBye,
    bool IsPlayed);

/// <summary>Ligne de classement calculée.</summary>
public sealed record StandingRow
{
    public required int UserId { get; init; }

    public decimal Score { get; init; }

    public decimal Buchholz { get; init; }

    public decimal BuchholzCut1 { get; init; }

    public decimal SonnebornBerger { get; init; }

    public int Wins { get; init; }

    public int Draws { get; init; }

    public int Losses { get; init; }

    public int Rank { get; set; }
}

/// <summary>
/// Calcule le classement d'un tournoi et ses départages conformément aux
/// recommandations FIDE : Buchholz, Buchholz tronqué, Sonneborn-Berger.
/// </summary>
public static class StandingsCalculator
{
    public static IReadOnlyList<StandingRow> Compute(
        IEnumerable<int> participantIds,
        IEnumerable<GameRecord> games,
        IReadOnlyDictionary<int, int>? eloByUser = null)
    {
        var players = participantIds.Distinct().ToList();
        var playedGames = games.Where(g => g.IsPlayed).ToList();

        var scores = players.ToDictionary(id => id, _ => 0m);
        var wins = players.ToDictionary(id => id, _ => 0);
        var draws = players.ToDictionary(id => id, _ => 0);
        var losses = players.ToDictionary(id => id, _ => 0);
        var opponents = players.ToDictionary(id => id, _ => new List<(int OpponentId, decimal Score)>());

        foreach (var game in playedGames)
        {
            if (scores.ContainsKey(game.WhiteId))
            {
                scores[game.WhiteId] += game.WhiteScore;
                Tally(game.WhiteScore, game.IsBye, wins, draws, losses, game.WhiteId);
            }

            if (game.BlackId is null || game.IsBye)
            {
                continue;
            }

            var blackId = game.BlackId.Value;
            if (scores.ContainsKey(blackId))
            {
                scores[blackId] += game.BlackScore;
                Tally(game.BlackScore, false, wins, draws, losses, blackId);
            }

            if (opponents.ContainsKey(game.WhiteId))
            {
                opponents[game.WhiteId].Add((blackId, game.WhiteScore));
            }

            if (opponents.ContainsKey(blackId))
            {
                opponents[blackId].Add((game.WhiteId, game.BlackScore));
            }
        }

        var rows = new List<StandingRow>(players.Count);

        foreach (var id in players)
        {
            var faced = opponents[id];
            var opponentScores = faced
                .Select(o => scores.TryGetValue(o.OpponentId, out var s) ? s : 0m)
                .ToList();

            var buchholz = opponentScores.Sum();
            var buchholzCut1 = opponentScores.Count > 0 ? buchholz - opponentScores.Min() : 0m;

            // Sonneborn-Berger : somme des scores des adversaires battus,
            // plus la moitié de ceux contre lesquels la partie a été nulle.
            var sonneborn = faced.Sum(o =>
            {
                var opponentScore = scores.TryGetValue(o.OpponentId, out var s) ? s : 0m;
                return o.Score * opponentScore;
            });

            rows.Add(new StandingRow
            {
                UserId = id,
                Score = scores[id],
                Buchholz = decimal.Round(buchholz, 2),
                BuchholzCut1 = decimal.Round(buchholzCut1, 2),
                SonnebornBerger = decimal.Round(sonneborn, 2),
                Wins = wins[id],
                Draws = draws[id],
                Losses = losses[id]
            });
        }

        var ordered = rows
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.BuchholzCut1)
            .ThenByDescending(r => r.Buchholz)
            .ThenByDescending(r => r.SonnebornBerger)
            .ThenByDescending(r => r.Wins)
            .ThenByDescending(r => eloByUser is not null && eloByUser.TryGetValue(r.UserId, out var elo) ? elo : 0)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            // Rang partagé lorsque tous les départages sont égaux.
            if (i > 0 && SameRanking(ordered[i - 1], ordered[i]))
            {
                ordered[i].Rank = ordered[i - 1].Rank;
            }
            else
            {
                ordered[i].Rank = i + 1;
            }
        }

        return ordered;
    }

    private static bool SameRanking(StandingRow a, StandingRow b) =>
        a.Score == b.Score
        && a.BuchholzCut1 == b.BuchholzCut1
        && a.Buchholz == b.Buchholz
        && a.SonnebornBerger == b.SonnebornBerger;

    private static void Tally(
        decimal score,
        bool isBye,
        IDictionary<int, int> wins,
        IDictionary<int, int> draws,
        IDictionary<int, int> losses,
        int userId)
    {
        if (isBye)
        {
            // Un bye rapporte un point mais ne compte pas comme une victoire jouée.
            return;
        }

        if (score >= 1m)
        {
            wins[userId]++;
        }
        else if (score == 0.5m)
        {
            draws[userId]++;
        }
        else
        {
            losses[userId]++;
        }
    }
}
