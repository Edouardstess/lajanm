using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>
/// Partie d'une ronde. Porte aussi l'appariement (numéro d'échiquier, couleurs) :
/// un appariement sans résultat est une partie « non jouée ».
/// </summary>
public class TournamentGame : AuditableEntity
{
    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public int RoundId { get; set; }

    public TournamentRound Round { get; set; } = null!;

    public int BoardNumber { get; set; }

    public int WhitePlayerId { get; set; }

    public ApplicationUser WhitePlayer { get; set; } = null!;

    /// <summary>Nul en cas de bye (nombre impair de joueurs).</summary>
    public int? BlackPlayerId { get; set; }

    public ApplicationUser? BlackPlayer { get; set; }

    public GameResult Result { get; set; } = GameResult.NonJouee;

    public string? Pgn { get; set; }

    public string? OpeningName { get; set; }

    public int? MoveCount { get; set; }

    public bool IsBye { get; set; }

    public bool IsRated { get; set; } = true;

    public DateTime? PlayedAt { get; set; }

    public int? ResultEnteredById { get; set; }

    public DateTime? ResultEnteredAt { get; set; }

    /// <summary>Justification obligatoire lors de la correction d'un résultat déjà saisi.</summary>
    public string? ResultCorrectionReason { get; set; }

    public int WhiteEloBefore { get; set; }

    public int BlackEloBefore { get; set; }

    // --- Comportement métier ---

    /// <summary>Points marqués par les blancs : 1, 0,5 ou 0.</summary>
    public decimal ScoreForWhite => Result switch
    {
        GameResult.VictoireBlancs or GameResult.ForfaitNoirs or GameResult.Bye => 1m,
        GameResult.Nulle => 0.5m,
        _ => 0m
    };

    public decimal ScoreForBlack => Result switch
    {
        GameResult.VictoireNoirs or GameResult.ForfaitBlancs => 1m,
        GameResult.Nulle => 0.5m,
        _ => 0m
    };

    public decimal ScoreFor(int userId)
    {
        if (userId == WhitePlayerId)
        {
            return ScoreForWhite;
        }

        if (BlackPlayerId.HasValue && userId == BlackPlayerId.Value)
        {
            return ScoreForBlack;
        }

        return 0m;
    }

    public int? OpponentOf(int userId)
    {
        if (userId == WhitePlayerId)
        {
            return BlackPlayerId;
        }

        return BlackPlayerId.HasValue && userId == BlackPlayerId.Value ? WhitePlayerId : null;
    }

    /// <summary>Le résultat compte-t-il pour le classement ELO ? Un bye et un double forfait ne comptent pas.</summary>
    public bool CountsForElo =>
        IsRated && !IsBye && BlackPlayerId.HasValue
        && Result is GameResult.VictoireBlancs or GameResult.VictoireNoirs or GameResult.Nulle;

    public void SetResult(GameResult result, int arbiterId, DateTime when, string? correctionReason = null)
    {
        if (Result != GameResult.NonJouee && string.IsNullOrWhiteSpace(correctionReason))
        {
            throw new DomainException(
                "La correction d'un résultat déjà saisi exige une justification.");
        }

        if (result == GameResult.Bye && !IsBye)
        {
            throw new DomainException("Seul un appariement de type « bye » peut recevoir ce résultat.");
        }

        Result = result;
        ResultEnteredById = arbiterId;
        ResultEnteredAt = when;
        PlayedAt ??= when;
        ResultCorrectionReason = correctionReason;
        UpdatedAt = when;
    }
}
