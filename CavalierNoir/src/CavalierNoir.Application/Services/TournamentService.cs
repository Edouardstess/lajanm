using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Services;
using CavalierNoir.Domain.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Gestion des compétitions : inscriptions et liste d'attente, appariements
/// (système suisse ou toutes rondes), saisie des résultats, classement avec
/// départages et mise à jour du classement ELO interne.
/// </summary>
public sealed class TournamentService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    INotificationService notifications,
    ILogger<TournamentService> logger)
{
    public async Task<IReadOnlyList<TournamentCard>> GetUpcomingAsync(int take = 5, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        return await context.Tournaments
            .AsNoTracking()
            .Where(t => !t.IsDeleted
                        && t.Status != TournamentStatus.Brouillon
                        && t.Status != TournamentStatus.Annule
                        && t.EndDate >= now)
            .OrderBy(t => t.StartDate)
            .Take(take)
            .Select(t => new TournamentCard(
                t.Id,
                t.Title,
                t.Slug,
                t.Type,
                t.Status,
                t.StartDate,
                t.EndDate,
                t.Location,
                t.Registrations.Count(r => r.Status == RegistrationStatus.Confirmee),
                t.MaxPlayers,
                t.EntryFee.Amount,
                t.EntryFee.Currency,
                t.ImageUrl))
            .ToListAsync(ct);
    }

    public async Task<PagedList<TournamentCard>> SearchAsync(
        TournamentStatus? status,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        var query = context.Tournaments
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Status != TournamentStatus.Brouillon);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var projected = query
            .OrderByDescending(t => t.StartDate)
            .Select(t => new TournamentCard(
                t.Id,
                t.Title,
                t.Slug,
                t.Type,
                t.Status,
                t.StartDate,
                t.EndDate,
                t.Location,
                t.Registrations.Count(r => r.Status == RegistrationStatus.Confirmee),
                t.MaxPlayers,
                t.EntryFee.Amount,
                t.EntryFee.Currency,
                t.ImageUrl));

        return await PagedList<TournamentCard>.CreateAsync(projected, page, pageSize, ct);
    }

    public Task<Tournament?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        context.Tournaments
            .Include(t => t.Rounds)
            .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted, ct);

    public Task<Tournament?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Tournaments
            .Include(t => t.Rounds)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    /// <summary>
    /// Inscrit un joueur. Au-delà de la capacité, l'inscription est placée en
    /// liste d'attente dans l'ordre d'arrivée.
    /// </summary>
    public async Task<Result<string>> RegisterAsync(int tournamentId, int userId, CancellationToken ct = default)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Registrations)
            .FirstOrDefaultAsync(t => t.Id == tournamentId && !t.IsDeleted, ct);

        if (tournament is null)
        {
            return Result<string>.Failure("Tournoi introuvable.", "NOTFOUND_005");
        }

        var now = clock.UtcNow;
        if (!tournament.IsRegistrationOpen(now))
        {
            return Result<string>.Failure("Les inscriptions ne sont pas ouvertes.", "BR-13");
        }

        if (tournament.Registrations.Any(r => r.UserId == userId && r.Status != RegistrationStatus.Annulee))
        {
            return Result<string>.Failure("Vous êtes déjà inscrit à ce tournoi.", "CONFLICT_004");
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<string>.Failure("Utilisateur introuvable.", "NOTFOUND_001");
        }

        if (tournament.MinimumElo is { } minElo && user.Elo < minElo)
        {
            return Result<string>.Failure($"Ce tournoi est réservé aux joueurs classés {minElo} ELO ou plus.");
        }

        if (tournament.MaximumElo is { } maxElo && user.Elo > maxElo)
        {
            return Result<string>.Failure($"Ce tournoi est réservé aux joueurs classés jusqu'à {maxElo} ELO.");
        }

        var registration = new TournamentRegistration
        {
            TournamentId = tournament.Id,
            UserId = userId,
            EloAtRegistration = user.Elo,
            RegisteredAt = now,
            CreatedAt = now,
            CreatedById = userId
        };

        string message;
        if (tournament.IsFull)
        {
            var position = tournament.Registrations.Count(r => r.Status == RegistrationStatus.ListeAttente) + 1;
            registration.PutOnWaitingList(position, now);
            message = $"Le tournoi est complet : vous êtes en liste d'attente (position {position}).";
        }
        else
        {
            registration.Confirm(now);
            message = "Votre inscription est confirmée.";
        }

        context.TournamentRegistrations.Add(registration);
        await context.SaveChangesAsync(ct);

        return Result<string>.Success(message);
    }

    /// <summary>Annule une inscription et promeut le premier de la liste d'attente.</summary>
    public async Task<Result> CancelRegistrationAsync(
        int tournamentId,
        int userId,
        string? reason,
        CancellationToken ct = default)
    {
        var registration = await context.TournamentRegistrations
            .FirstOrDefaultAsync(r => r.TournamentId == tournamentId && r.UserId == userId, ct);

        if (registration is null)
        {
            return Result.Failure("Inscription introuvable.", "NOTFOUND_006");
        }

        var tournament = await context.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, ct);
        if (tournament is not null && tournament.Status is TournamentStatus.EnCours or TournamentStatus.Termine)
        {
            return Result.Failure("Le tournoi a commencé : contactez l'arbitre.", "BR-13");
        }

        var now = clock.UtcNow;
        var wasConfirmed = registration.Status == RegistrationStatus.Confirmee;
        registration.Cancel(reason, now);

        if (wasConfirmed)
        {
            var next = await context.TournamentRegistrations
                .Where(r => r.TournamentId == tournamentId && r.Status == RegistrationStatus.ListeAttente)
                .OrderBy(r => r.WaitingListPosition)
                .FirstOrDefaultAsync(ct);

            if (next is not null)
            {
                next.Confirm(now);
                await notifications.NotifyAsync(
                    next.UserId,
                    "Une place s'est libérée",
                    "Votre inscription au tournoi est confirmée.",
                    $"/Tournois/{tournamentId}",
                    "trophy",
                    1,
                    NotificationChannel.InApp,
                    ct);
            }
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Génère les appariements de la ronde suivante. La ronde précédente doit être
    /// entièrement saisie ; les revanches sont interdites (BR-06).
    /// </summary>
    public async Task<Result<int>> GenerateNextRoundAsync(
        int tournamentId,
        int arbiterId,
        CancellationToken ct = default)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Rounds)
            .FirstOrDefaultAsync(t => t.Id == tournamentId && !t.IsDeleted, ct);

        if (tournament is null)
        {
            return Result<int>.Failure("Tournoi introuvable.", "NOTFOUND_005");
        }

        if (tournament.Status == TournamentStatus.InscriptionsCloturees)
        {
            tournament.Start(clock.UtcNow);
        }

        if (tournament.Status != TournamentStatus.EnCours)
        {
            return Result<int>.Failure(
                "Le tournoi doit être en cours pour générer une ronde.",
                "BR-13");
        }

        var previousRounds = tournament.Rounds.OrderBy(r => r.Number).ToList();
        var lastRound = previousRounds.LastOrDefault();

        if (lastRound is not null && lastRound.Status != RoundStatus.Terminee)
        {
            var pending = await context.TournamentGames
                .CountAsync(g => g.RoundId == lastRound.Id && g.Result == GameResult.NonJouee, ct);

            if (pending > 0)
            {
                return Result<int>.Failure(
                    $"{pending} partie(s) de la ronde {lastRound.Number} n'ont pas de résultat.");
            }

            lastRound.Close(clock.UtcNow);
        }

        if (previousRounds.Count >= tournament.PlannedRounds)
        {
            return Result<int>.Failure("Toutes les rondes prévues ont déjà été jouées.");
        }

        var candidates = await BuildPairingCandidatesAsync(tournament, ct);
        if (candidates.Count < 2)
        {
            return Result<int>.Failure("Pas assez de joueurs en lice pour apparier une ronde.");
        }

        IReadOnlyList<PairingResult> pairings;
        try
        {
            pairings = tournament.Type == TournamentType.ToutesRondes
                ? SwissPairingService.PairRoundRobin(candidates)[previousRounds.Count]
                : SwissPairingService.PairSwissRound(candidates);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result<int>.Failure("Le calendrier du toutes rondes est terminé.");
        }
        catch (DomainException ex)
        {
            return Result<int>.Failure(ex.Message, ex.Code);
        }

        var now = clock.UtcNow;
        var round = new TournamentRound
        {
            TournamentId = tournament.Id,
            Number = previousRounds.Count + 1,
            Status = RoundStatus.Apparee,
            StartTime = now,
            PairedAt = now,
            PairedById = arbiterId,
            CreatedAt = now,
            CreatedById = arbiterId
        };

        context.TournamentRounds.Add(round);
        await context.SaveChangesAsync(ct);

        var eloByUser = candidates.ToDictionary(c => c.UserId, c => c.Elo);

        foreach (var pairing in pairings)
        {
            var game = new TournamentGame
            {
                TournamentId = tournament.Id,
                RoundId = round.Id,
                BoardNumber = pairing.BoardNumber,
                WhitePlayerId = pairing.WhitePlayerId,
                BlackPlayerId = pairing.BlackPlayerId,
                IsBye = pairing.IsBye,
                IsRated = tournament.IsRated && !pairing.IsBye,
                Result = pairing.IsBye ? GameResult.Bye : GameResult.NonJouee,
                WhiteEloBefore = eloByUser.TryGetValue(pairing.WhitePlayerId, out var we) ? we : 0,
                BlackEloBefore = pairing.BlackPlayerId is { } bid && eloByUser.TryGetValue(bid, out var be) ? be : 0,
                CreatedAt = now,
                CreatedById = arbiterId
            };

            if (pairing.IsBye)
            {
                game.ResultEnteredById = arbiterId;
                game.ResultEnteredAt = now;
                game.PlayedAt = now;
            }

            context.TournamentGames.Add(game);
        }

        await context.SaveChangesAsync(ct);
        await RecomputeStandingsAsync(tournament.Id, ct);

        foreach (var pairing in pairings.Where(p => !p.IsBye))
        {
            await notifications.NotifyAsync(
                pairing.WhitePlayerId,
                $"Ronde {round.Number} : appariements publiés",
                $"Échiquier {pairing.BoardNumber}, vous jouez avec les blancs.",
                $"/Tournois/{tournament.Id}/Rondes/{round.Number}",
                "grid-3x3",
                2,
                NotificationChannel.InApp,
                ct);

            if (pairing.BlackPlayerId is { } blackId)
            {
                await notifications.NotifyAsync(
                    blackId,
                    $"Ronde {round.Number} : appariements publiés",
                    $"Échiquier {pairing.BoardNumber}, vous jouez avec les noirs.",
                    $"/Tournois/{tournament.Id}/Rondes/{round.Number}",
                    "grid-3x3",
                    2,
                    NotificationChannel.InApp,
                    ct);
            }
        }

        logger.LogInformation(
            "Ronde {RoundNumber} du tournoi {TournamentId} appariée ({Games} parties).",
            round.Number,
            tournament.Id,
            pairings.Count);

        return Result<int>.Success(round.Id);
    }

    /// <summary>Saisit ou corrige un résultat, applique l'ELO et recalcule le classement.</summary>
    public async Task<Result> EnterResultAsync(
        int gameId,
        GameResult result,
        int arbiterId,
        string? correctionReason = null,
        CancellationToken ct = default)
    {
        var game = await context.TournamentGames
            .Include(g => g.Tournament)
            .Include(g => g.WhitePlayer)
            .Include(g => g.BlackPlayer)
            .FirstOrDefaultAsync(g => g.Id == gameId, ct);

        if (game is null)
        {
            return Result.Failure("Partie introuvable.", "NOTFOUND_007");
        }

        var alreadyRated = await context.EloHistory.AnyAsync(h => h.GameId == gameId, ct);
        var now = clock.UtcNow;

        try
        {
            game.SetResult(result, arbiterId, now, correctionReason);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, ex.Code);
        }

        await context.SaveChangesAsync(ct);

        if (!alreadyRated && game.CountsForElo)
        {
            await ApplyEloAsync(game, ct);
        }

        await RecomputeStandingsAsync(game.TournamentId, ct);
        return Result.Success();
    }

    /// <summary>Applique la formule ELO aux deux joueurs et historise la variation.</summary>
    private async Task ApplyEloAsync(TournamentGame game, CancellationToken ct)
    {
        if (game.BlackPlayer is null || game.WhitePlayer is null)
        {
            return;
        }

        var timeControl = game.Tournament?.TimeControl ?? TimeControl.Classique;
        var whiteK = EloCalculator.KFactorFor(game.WhitePlayer.RatedGamesPlayed, game.WhitePlayer.Elo, timeControl);
        var blackK = EloCalculator.KFactorFor(game.BlackPlayer.RatedGamesPlayed, game.BlackPlayer.Elo, timeControl);

        var (white, black) = EloCalculator.ComputeForGame(
            game.WhitePlayer.Elo,
            game.BlackPlayer.Elo,
            game.ScoreForWhite,
            whiteK,
            blackK);

        var now = clock.UtcNow;

        context.EloHistory.Add(new EloHistory
        {
            UserId = game.WhitePlayerId,
            OldElo = white.OldElo,
            NewElo = white.NewElo,
            KFactor = white.KFactor,
            ExpectedScore = white.ExpectedScore,
            ActualScore = white.ActualScore,
            GameId = game.Id,
            TournamentId = game.TournamentId,
            OpponentId = game.BlackPlayerId,
            OpponentElo = black.OldElo,
            RecordedAt = now
        });

        context.EloHistory.Add(new EloHistory
        {
            UserId = game.BlackPlayerId!.Value,
            OldElo = black.OldElo,
            NewElo = black.NewElo,
            KFactor = black.KFactor,
            ExpectedScore = black.ExpectedScore,
            ActualScore = black.ActualScore,
            GameId = game.Id,
            TournamentId = game.TournamentId,
            OpponentId = game.WhitePlayerId,
            OpponentElo = white.OldElo,
            RecordedAt = now
        });

        game.WhitePlayer.Elo = white.NewElo;
        game.WhitePlayer.RatedGamesPlayed++;
        game.BlackPlayer.Elo = black.NewElo;
        game.BlackPlayer.RatedGamesPlayed++;

        await context.SaveChangesAsync(ct);
    }

    /// <summary>Recalcule le classement du tournoi (score, Buchholz, Sonneborn-Berger).</summary>
    public async Task RecomputeStandingsAsync(int tournamentId, CancellationToken ct = default)
    {
        var participants = await context.TournamentRegistrations
            .Where(r => r.TournamentId == tournamentId && r.Status != RegistrationStatus.Annulee)
            .Select(r => r.UserId)
            .ToListAsync(ct);

        if (participants.Count == 0)
        {
            return;
        }

        var games = await context.TournamentGames
            .Where(g => g.TournamentId == tournamentId)
            .Select(g => new
            {
                g.WhitePlayerId,
                g.BlackPlayerId,
                g.Result,
                g.IsBye
            })
            .ToListAsync(ct);

        var records = games
            .Select(g =>
            {
                var white = g.Result switch
                {
                    GameResult.VictoireBlancs or GameResult.ForfaitNoirs or GameResult.Bye => 1m,
                    GameResult.Nulle => 0.5m,
                    _ => 0m
                };

                var black = g.Result switch
                {
                    GameResult.VictoireNoirs or GameResult.ForfaitBlancs => 1m,
                    GameResult.Nulle => 0.5m,
                    _ => 0m
                };

                return new GameRecord(
                    g.WhitePlayerId,
                    g.BlackPlayerId,
                    white,
                    black,
                    g.IsBye,
                    g.Result != GameResult.NonJouee);
            })
            .ToList();

        var elos = await context.Users
            .Where(u => participants.Contains(u.Id))
            .Select(u => new { u.Id, u.Elo })
            .ToDictionaryAsync(u => u.Id, u => u.Elo, ct);

        var rows = StandingsCalculator.Compute(participants, records, elos);

        var existing = await context.TournamentStandings
            .Where(s => s.TournamentId == tournamentId)
            .ToListAsync(ct);

        var now = clock.UtcNow;

        foreach (var row in rows)
        {
            var standing = existing.FirstOrDefault(s => s.UserId == row.UserId);
            if (standing is null)
            {
                standing = new TournamentStanding
                {
                    TournamentId = tournamentId,
                    UserId = row.UserId,
                    EloBefore = elos.TryGetValue(row.UserId, out var elo) ? elo : 0
                };
                context.TournamentStandings.Add(standing);
            }

            standing.Score = row.Score;
            standing.Buchholz = row.Buchholz;
            standing.BuchholzCut1 = row.BuchholzCut1;
            standing.SonnebornBerger = row.SonnebornBerger;
            standing.Wins = row.Wins;
            standing.Draws = row.Draws;
            standing.Losses = row.Losses;
            standing.Rank = row.Rank;
            standing.EloAfter = elos.TryGetValue(row.UserId, out var current) ? current : standing.EloAfter;
            standing.UpdatedAt = now;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<StandingLine>> GetStandingsAsync(int tournamentId, CancellationToken ct = default) =>
        await context.TournamentStandings
            .AsNoTracking()
            .Where(s => s.TournamentId == tournamentId)
            .OrderBy(s => s.Rank)
            .Select(s => new StandingLine(
                s.Rank,
                s.UserId,
                s.User.Pseudonym ?? (s.User.FirstName + " " + s.User.LastName),
                s.User.Elo,
                s.Score,
                s.Buchholz,
                s.SonnebornBerger,
                s.Wins,
                s.Draws,
                s.Losses,
                s.EloAfter - s.EloBefore,
                s.Prize))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PairingLine>> GetPairingsAsync(
        int tournamentId,
        int roundNumber,
        CancellationToken ct = default) =>
        await context.TournamentGames
            .AsNoTracking()
            .Where(g => g.TournamentId == tournamentId && g.Round.Number == roundNumber)
            .OrderBy(g => g.BoardNumber)
            .Select(g => new PairingLine(
                g.Id,
                g.BoardNumber,
                g.WhitePlayerId,
                g.WhitePlayer.Pseudonym ?? (g.WhitePlayer.FirstName + " " + g.WhitePlayer.LastName),
                g.WhiteEloBefore,
                g.BlackPlayerId,
                g.BlackPlayer != null
                    ? (g.BlackPlayer.Pseudonym ?? (g.BlackPlayer.FirstName + " " + g.BlackPlayer.LastName))
                    : null,
                g.BlackPlayerId != null ? g.BlackEloBefore : null,
                g.Result,
                g.IsBye))
            .ToListAsync(ct);

    /// <summary>Clôture le tournoi : dernière ronde fermée, classement figé, podium notifié.</summary>
    public async Task<Result> FinishAsync(int tournamentId, int actorId, CancellationToken ct = default)
    {
        var tournament = await context.Tournaments
            .Include(t => t.Rounds)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

        if (tournament is null)
        {
            return Result.Failure("Tournoi introuvable.", "NOTFOUND_005");
        }

        var unfinished = await context.TournamentGames
            .CountAsync(g => g.TournamentId == tournamentId && g.Result == GameResult.NonJouee, ct);

        if (unfinished > 0)
        {
            return Result.Failure($"{unfinished} partie(s) sans résultat.");
        }

        var now = clock.UtcNow;
        foreach (var round in tournament.Rounds.Where(r => r.Status != RoundStatus.Terminee))
        {
            round.Status = RoundStatus.Terminee;
            round.EndTime ??= now;
        }

        try
        {
            tournament.Finish(now);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message, ex.Code);
        }

        tournament.UpdatedById = actorId;
        await context.SaveChangesAsync(ct);
        await RecomputeStandingsAsync(tournamentId, ct);

        var podium = await context.TournamentStandings
            .Where(s => s.TournamentId == tournamentId && s.Rank <= 3)
            .OrderBy(s => s.Rank)
            .ToListAsync(ct);

        foreach (var place in podium)
        {
            await notifications.NotifyAsync(
                place.UserId,
                $"Podium : {place.Rank}ᵉ place",
                $"Félicitations ! Vous terminez {place.Rank}ᵉ du tournoi « {tournament.Title} » "
                + $"avec {place.Score:0.#} point(s).",
                $"/Tournois/{tournament.Slug}",
                "trophy-fill",
                1,
                NotificationChannel.InApp,
                ct);
        }

        return Result.Success();
    }

    /// <summary>Construit l'état des joueurs encore en lice avant l'appariement.</summary>
    private async Task<List<PairingCandidate>> BuildPairingCandidatesAsync(
        Tournament tournament,
        CancellationToken ct)
    {
        var registrations = await context.TournamentRegistrations
            .Where(r => r.TournamentId == tournament.Id
                        && r.Status == RegistrationStatus.Confirmee
                        && !r.IsForfeited)
            .Select(r => new { r.UserId, r.EloAtRegistration })
            .ToListAsync(ct);

        var userIds = registrations.Select(r => r.UserId).ToList();

        var users = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Elo,
                u.FirstName,
                u.LastName,
                u.Pseudonym
            })
            .ToListAsync(ct);

        var games = await context.TournamentGames
            .Where(g => g.TournamentId == tournament.Id)
            .Select(g => new
            {
                g.WhitePlayerId,
                g.BlackPlayerId,
                g.IsBye,
                g.Result,
                RoundNumber = g.Round.Number
            })
            .ToListAsync(ct);

        var candidates = new List<PairingCandidate>(users.Count);

        foreach (var user in users)
        {
            var played = games
                .Where(g => g.WhitePlayerId == user.Id || g.BlackPlayerId == user.Id)
                .ToList();

            var opponents = new HashSet<int>();
            var whiteCount = 0;
            var blackCount = 0;
            var hasBye = false;
            bool? lastWasWhite = null;
            var score = 0m;

            foreach (var game in played.OrderBy(g => g.RoundNumber))
            {
                if (game.IsBye)
                {
                    hasBye = true;
                    score += 1m;
                    continue;
                }

                if (game.WhitePlayerId == user.Id)
                {
                    whiteCount++;
                    lastWasWhite = true;
                    if (game.BlackPlayerId is { } opponentId)
                    {
                        opponents.Add(opponentId);
                    }

                    score += game.Result switch
                    {
                        GameResult.VictoireBlancs or GameResult.ForfaitNoirs => 1m,
                        GameResult.Nulle => 0.5m,
                        _ => 0m
                    };
                }
                else
                {
                    blackCount++;
                    lastWasWhite = false;
                    opponents.Add(game.WhitePlayerId);

                    score += game.Result switch
                    {
                        GameResult.VictoireNoirs or GameResult.ForfaitBlancs => 1m,
                        GameResult.Nulle => 0.5m,
                        _ => 0m
                    };
                }
            }

            var displayName = !string.IsNullOrWhiteSpace(user.Pseudonym)
                ? user.Pseudonym!
                : $"{user.FirstName} {user.LastName}".Trim();

            candidates.Add(new PairingCandidate(
                user.Id,
                displayName,
                score,
                user.Elo,
                opponents,
                whiteCount,
                blackCount,
                hasBye,
                lastWasWhite));
        }

        return candidates;
    }
}
