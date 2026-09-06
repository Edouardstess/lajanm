using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Services;

/// <summary>
/// Calcul du classement ELO interne selon la formule FIDE :
/// <c>Rn = Ra + K × (Sr − Se)</c> où <c>Se = 1 / (1 + 10^((Rb − Ra) / 400))</c>.
/// </summary>
public static class EloCalculator
{
    /// <summary>Facteur K des 20 premières parties classées (BR-07).</summary>
    public const int KFactorNewPlayer = 32;

    /// <summary>Facteur K entre la 21ᵉ et la 50ᵉ partie.</summary>
    public const int KFactorDeveloping = 24;

    /// <summary>Facteur K des joueurs confirmés.</summary>
    public const int KFactorEstablished = 16;

    /// <summary>Facteur K réduit des parties rapides et blitz.</summary>
    public const int KFactorFast = 10;

    /// <summary>
    /// Facteur K applicable à un joueur, fonction de son expérience, de son niveau
    /// et de la cadence de jeu.
    /// </summary>
    public static int KFactorFor(int ratedGamesPlayed, int elo, TimeControl timeControl = TimeControl.Classique)
    {
        if (timeControl is TimeControl.Blitz or TimeControl.Bullet)
        {
            return KFactorFast;
        }

        if (ratedGamesPlayed < 20)
        {
            return KFactorNewPlayer;
        }

        if (elo >= 2400)
        {
            return KFactorEstablished;
        }

        return ratedGamesPlayed < 50 ? KFactorDeveloping : KFactorEstablished;
    }

    /// <summary>
    /// Espérance de gain d'un joueur classé <paramref name="playerElo"/> face à un
    /// adversaire classé <paramref name="opponentElo"/>. Valeur dans ]0 ; 1[.
    /// </summary>
    public static decimal ExpectedScore(int playerElo, int opponentElo)
    {
        var exponent = (opponentElo - playerElo) / 400.0;
        var expected = 1.0 / (1.0 + Math.Pow(10, exponent));
        return decimal.Round((decimal)expected, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Nouveau classement après une partie. <paramref name="actualScore"/> vaut
    /// 1 (gain), 0,5 (nulle) ou 0 (perte).
    /// </summary>
    public static EloUpdate Compute(int playerElo, int opponentElo, decimal actualScore, int kFactor)
    {
        if (actualScore is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualScore),
                actualScore,
                "Le score réel doit valoir 0, 0,5 ou 1.");
        }

        var expected = ExpectedScore(playerElo, opponentElo);
        var rawDelta = kFactor * (actualScore - expected);
        var delta = (int)Math.Round(rawDelta, MidpointRounding.AwayFromZero);

        // Une victoire ne doit jamais faire perdre de points, ni une défaite en faire gagner.
        if (actualScore > 0.5m && delta < 1)
        {
            delta = 1;
        }
        else if (actualScore < 0.5m && delta > -1)
        {
            delta = -1;
        }

        var newElo = Math.Clamp(playerElo + delta, EloRating.Minimum, EloRating.Maximum);

        return new EloUpdate(playerElo, newElo, kFactor, expected, actualScore);
    }

    /// <summary>Applique la formule aux deux joueurs d'une même partie.</summary>
    public static (EloUpdate White, EloUpdate Black) ComputeForGame(
        int whiteElo,
        int blackElo,
        decimal whiteScore,
        int whiteKFactor,
        int blackKFactor)
    {
        var white = Compute(whiteElo, blackElo, whiteScore, whiteKFactor);
        var black = Compute(blackElo, whiteElo, 1m - whiteScore, blackKFactor);
        return (white, black);
    }

    /// <summary>
    /// Classement de départ conseillé selon le niveau déclaré à l'inscription
    /// (BR-07 : plafonné à 1600 sans justificatif).
    /// </summary>
    public static int InitialRating(PlayerLevel level, bool hasProof = false) => level switch
    {
        PlayerLevel.Debutant => 1200,
        PlayerLevel.Intermediaire => 1400,
        PlayerLevel.Avance => 1600,
        PlayerLevel.Expert => hasProof ? 1800 : 1600,
        _ => EloRating.DefaultRating
    };
}

/// <summary>Résultat d'un calcul ELO, prêt à être historisé.</summary>
public readonly record struct EloUpdate(
    int OldElo,
    int NewElo,
    int KFactor,
    decimal ExpectedScore,
    decimal ActualScore)
{
    public int Delta => NewElo - OldElo;
}
