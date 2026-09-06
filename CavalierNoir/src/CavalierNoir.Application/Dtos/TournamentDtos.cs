using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Application.Dtos;

/// <summary>Carte de tournoi pour le calendrier public.</summary>
public sealed record TournamentCard(
    int Id,
    string Title,
    string Slug,
    TournamentType Type,
    TournamentStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    string? Location,
    int ConfirmedPlayers,
    int? MaxPlayers,
    decimal EntryFee,
    string Currency,
    string? ImageUrl);

/// <summary>Ligne d'appariement affichée sur la feuille de ronde.</summary>
public sealed record PairingLine(
    int GameId,
    int BoardNumber,
    int WhiteId,
    string WhiteName,
    int WhiteElo,
    int? BlackId,
    string? BlackName,
    int? BlackElo,
    GameResult Result,
    bool IsBye);

/// <summary>Ligne de classement affichée au public.</summary>
public sealed record StandingLine(
    int Rank,
    int UserId,
    string PlayerName,
    int Elo,
    decimal Score,
    decimal Buchholz,
    decimal SonnebornBerger,
    int Wins,
    int Draws,
    int Losses,
    int EloDelta,
    string? Prize);
