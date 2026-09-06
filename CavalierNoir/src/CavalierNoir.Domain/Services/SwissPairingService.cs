using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Services;

/// <summary>
/// Joueur candidat à l'appariement d'une ronde, dans l'état où il se trouve
/// avant celle-ci.
/// </summary>
/// <param name="UserId">Identifiant du joueur.</param>
/// <param name="DisplayName">Nom affiché sur la feuille d'appariement.</param>
/// <param name="Score">Score cumulé avant la ronde.</param>
/// <param name="Elo">Classement au moment de l'appariement.</param>
/// <param name="OpponentIds">Adversaires déjà rencontrés dans ce tournoi (BR-06).</param>
/// <param name="WhiteCount">Nombre de parties déjà jouées avec les blancs.</param>
/// <param name="BlackCount">Nombre de parties déjà jouées avec les noirs.</param>
/// <param name="HasReceivedBye">Le joueur a déjà bénéficié d'un bye.</param>
/// <param name="LastColorWasWhite">Couleur de la ronde précédente, si elle existe.</param>
public sealed record PairingCandidate(
    int UserId,
    string DisplayName,
    decimal Score,
    int Elo,
    IReadOnlySet<int> OpponentIds,
    int WhiteCount,
    int BlackCount,
    bool HasReceivedBye,
    bool? LastColorWasWhite)
{
    /// <summary>Déséquilibre de couleurs : positif si le joueur a trop souvent eu les blancs.</summary>
    public int ColorBalance => WhiteCount - BlackCount;
}

/// <summary>Appariement produit pour une ronde.</summary>
/// <param name="BoardNumber">Numéro d'échiquier, à partir de 1.</param>
/// <param name="WhitePlayerId">Joueur ayant les blancs (ou le joueur exempt en cas de bye).</param>
/// <param name="BlackPlayerId">Joueur ayant les noirs, nul en cas de bye.</param>
/// <param name="IsBye">Vrai lorsque le joueur est exempt et marque un point d'office.</param>
public sealed record PairingResult(int BoardNumber, int WhitePlayerId, int? BlackPlayerId, bool IsBye);

/// <summary>
/// Générateur d'appariements. Implémente un système suisse simplifié
/// (« Dutch » monrad) et le toutes rondes par la méthode du cercle de Berger.
/// </summary>
public static class SwissPairingService
{
    /// <summary>
    /// Apparie une ronde du système suisse : regroupement par score, interdiction
    /// de rejouer un même adversaire (BR-06), alternance des couleurs et bye
    /// attribué au joueur le moins bien classé n'en ayant pas encore bénéficié.
    /// </summary>
    public static IReadOnlyList<PairingResult> PairSwissRound(IEnumerable<PairingCandidate> candidates)
    {
        var players = candidates
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.Elo)
            .ThenBy(p => p.UserId)
            .ToList();

        if (players.Count < 2)
        {
            throw new DomainException("Un appariement requiert au moins deux joueurs.");
        }

        var results = new List<PairingResult>();
        PairingCandidate? byePlayer = null;

        if (players.Count % 2 == 1)
        {
            byePlayer = SelectByePlayer(players);
            players.Remove(byePlayer);
        }

        if (!TryPairRecursively(players, [], out var pairs))
        {
            // Aucune combinaison sans revanche : on relâche la contrainte BR-06.
            pairs = PairGreedyAllowingRematch(players);
        }

        var board = 1;
        foreach (var (first, second) in pairs)
        {
            var (white, black) = ChooseColors(first, second);
            results.Add(new PairingResult(board++, white.UserId, black.UserId, false));
        }

        if (byePlayer is not null)
        {
            results.Add(new PairingResult(board, byePlayer.UserId, null, true));
        }

        return results;
    }

    /// <summary>
    /// Apparie l'intégralité d'un toutes rondes (méthode de Berger) :
    /// renvoie une liste de rondes, chacune contenant ses appariements.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<PairingResult>> PairRoundRobin(
        IReadOnlyList<PairingCandidate> candidates)
    {
        if (candidates.Count < 2)
        {
            throw new DomainException("Un toutes rondes requiert au moins deux joueurs.");
        }

        var ids = candidates
            .OrderByDescending(c => c.Elo)
            .ThenBy(c => c.UserId)
            .Select(c => (int?)c.UserId)
            .ToList();

        // Joueur fictif pour un effectif impair : son adversaire est exempt.
        if (ids.Count % 2 == 1)
        {
            ids.Add(null);
        }

        var count = ids.Count;
        var rounds = new List<IReadOnlyList<PairingResult>>();

        for (var round = 0; round < count - 1; round++)
        {
            var pairings = new List<PairingResult>();
            var board = 1;

            for (var i = 0; i < count / 2; i++)
            {
                var first = ids[i];
                var second = ids[count - 1 - i];

                if (first is null || second is null)
                {
                    var exempt = first ?? second;
                    if (exempt is not null)
                    {
                        pairings.Add(new PairingResult(board++, exempt.Value, null, true));
                    }

                    continue;
                }

                // Alternance des couleurs d'une ronde à l'autre.
                var (white, black) = round % 2 == 0 ? (first.Value, second.Value) : (second.Value, first.Value);
                pairings.Add(new PairingResult(board++, white, black, false));
            }

            rounds.Add(pairings);

            // Rotation : le premier joueur reste fixe, les autres tournent.
            var last = ids[^1];
            ids.RemoveAt(ids.Count - 1);
            ids.Insert(1, last);
        }

        return rounds;
    }

    /// <summary>
    /// Bye : le joueur le moins bien classé au score qui n'en a pas encore
    /// bénéficié ; à défaut, le dernier du classement.
    /// </summary>
    private static PairingCandidate SelectByePlayer(IReadOnlyList<PairingCandidate> orderedPlayers)
    {
        for (var i = orderedPlayers.Count - 1; i >= 0; i--)
        {
            if (!orderedPlayers[i].HasReceivedBye)
            {
                return orderedPlayers[i];
            }
        }

        return orderedPlayers[^1];
    }

    /// <summary>
    /// Recherche par retour arrière d'un appariement complet sans revanche.
    /// L'espace de recherche reste modeste (tournois de club), la profondeur est
    /// bornée par le nombre de joueurs.
    /// </summary>
    private static bool TryPairRecursively(
        List<PairingCandidate> remaining,
        List<(PairingCandidate, PairingCandidate)> accumulated,
        out List<(PairingCandidate, PairingCandidate)> pairs)
    {
        if (remaining.Count == 0)
        {
            pairs = [.. accumulated];
            return true;
        }

        var first = remaining[0];

        for (var i = 1; i < remaining.Count; i++)
        {
            var candidate = remaining[i];
            if (first.OpponentIds.Contains(candidate.UserId))
            {
                continue;
            }

            var next = new List<PairingCandidate>(remaining);
            next.RemoveAt(i);
            next.RemoveAt(0);

            accumulated.Add((first, candidate));
            if (TryPairRecursively(next, accumulated, out pairs))
            {
                return true;
            }

            accumulated.RemoveAt(accumulated.Count - 1);
        }

        pairs = [];
        return false;
    }

    /// <summary>Repli : apparier de proche en proche, revanches autorisées.</summary>
    private static List<(PairingCandidate, PairingCandidate)> PairGreedyAllowingRematch(
        List<PairingCandidate> players)
    {
        var pairs = new List<(PairingCandidate, PairingCandidate)>();
        var pool = new List<PairingCandidate>(players);

        while (pool.Count >= 2)
        {
            var first = pool[0];
            pool.RemoveAt(0);

            var index = pool.FindIndex(p => !first.OpponentIds.Contains(p.UserId));
            if (index < 0)
            {
                index = 0;
            }

            var second = pool[index];
            pool.RemoveAt(index);
            pairs.Add((first, second));
        }

        return pairs;
    }

    /// <summary>
    /// Attribue les couleurs : priorité à l'équilibre blancs/noirs, puis à
    /// l'alternance par rapport à la ronde précédente, enfin au mieux classé.
    /// </summary>
    private static (PairingCandidate White, PairingCandidate Black) ChooseColors(
        PairingCandidate first,
        PairingCandidate second)
    {
        if (first.ColorBalance != second.ColorBalance)
        {
            return first.ColorBalance < second.ColorBalance ? (first, second) : (second, first);
        }

        if (first.LastColorWasWhite != second.LastColorWasWhite)
        {
            if (first.LastColorWasWhite == true)
            {
                return (second, first);
            }

            if (second.LastColorWasWhite == true)
            {
                return (first, second);
            }
        }

        return first.Elo >= second.Elo ? (first, second) : (second, first);
    }
}
