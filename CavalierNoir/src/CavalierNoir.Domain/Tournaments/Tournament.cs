using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>
/// Racine de l'agrégat Tournoi : rondes, appariements, parties et classement
/// ne sont manipulés qu'à travers elle.
/// </summary>
public class Tournament : AuditableEntity, IAggregateRoot, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Regulations { get; set; }

    public TournamentType Type { get; set; } = TournamentType.SystemeSuisse;

    public TimeControl TimeControl { get; set; } = TimeControl.Rapide;

    /// <summary>Cadence lisible, par exemple « 15 min + 5 s ».</summary>
    public string? TimeControlLabel { get; set; }

    public TournamentStatus Status { get; set; } = TournamentStatus.Brouillon;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? RegistrationOpensAt { get; set; }

    public DateTime? RegistrationClosesAt { get; set; }

    public string? Location { get; set; }

    public string? Address { get; set; }

    public int? MaxPlayers { get; set; }

    public int PlannedRounds { get; set; } = 5;

    public Money EntryFee { get; set; } = Money.Zero();

    public bool IsRated { get; set; } = true;

    public bool MembersOnly { get; set; } = true;

    public int? MinimumElo { get; set; }

    public int? MaximumElo { get; set; }

    public string? ImageUrl { get; set; }

    public int? OrganizerId { get; set; }

    public int? ArbiterId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedById { get; set; }

    public ICollection<TournamentRegistration> Registrations { get; set; } = new List<TournamentRegistration>();

    public ICollection<TournamentRound> Rounds { get; set; } = new List<TournamentRound>();

    public ICollection<TournamentStanding> Standings { get; set; } = new List<TournamentStanding>();

    // --- Comportement métier ---

    public int ConfirmedPlayers => Registrations.Count(r => r.Status == RegistrationStatus.Confirmee);

    public bool IsFull => MaxPlayers.HasValue && ConfirmedPlayers >= MaxPlayers.Value;

    public bool IsRegistrationOpen(DateTime now) =>
        Status == TournamentStatus.InscriptionsOuvertes
        && (RegistrationOpensAt is null || now >= RegistrationOpensAt)
        && (RegistrationClosesAt is null || now <= RegistrationClosesAt);

    public TournamentRound? CurrentRound =>
        Rounds.Where(r => r.Status != RoundStatus.Terminee).OrderBy(r => r.Number).FirstOrDefault()
        ?? Rounds.OrderByDescending(r => r.Number).FirstOrDefault();

    public void OpenRegistration(DateTime when)
    {
        if (Status is not (TournamentStatus.Brouillon or TournamentStatus.InscriptionsCloturees))
        {
            throw new DomainException("Les inscriptions ne peuvent être ouvertes qu'à partir d'un brouillon.");
        }

        Status = TournamentStatus.InscriptionsOuvertes;
        RegistrationOpensAt ??= when;
        UpdatedAt = when;
    }

    public void CloseRegistration(DateTime when)
    {
        if (Status != TournamentStatus.InscriptionsOuvertes)
        {
            throw new DomainException("Les inscriptions ne sont pas ouvertes.");
        }

        Status = TournamentStatus.InscriptionsCloturees;
        RegistrationClosesAt = when;
        UpdatedAt = when;
    }

    /// <summary>BR-13 : un tournoi n'est plus modifiable après la clôture des inscriptions.</summary>
    public bool IsEditable => Status is TournamentStatus.Brouillon or TournamentStatus.InscriptionsOuvertes;

    public void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new DomainException(
                "BR-13",
                "Le tournoi ne peut plus être modifié : les inscriptions sont closes.");
        }
    }

    public void Start(DateTime when)
    {
        if (Status != TournamentStatus.InscriptionsCloturees)
        {
            throw new DomainException("Clôturez les inscriptions avant de démarrer le tournoi.");
        }

        if (ConfirmedPlayers < 2)
        {
            throw new DomainException("Un tournoi requiert au moins deux joueurs confirmés.");
        }

        Status = TournamentStatus.EnCours;
        UpdatedAt = when;
    }

    public void Finish(DateTime when)
    {
        if (Status != TournamentStatus.EnCours)
        {
            throw new DomainException("Seul un tournoi en cours peut être terminé.");
        }

        Status = TournamentStatus.Termine;
        UpdatedAt = when;
    }

    public void Archive(DateTime when)
    {
        if (Status != TournamentStatus.Termine)
        {
            throw new DomainException("Seul un tournoi terminé peut être archivé.");
        }

        Status = TournamentStatus.Archive;
        UpdatedAt = when;
    }

    public void Cancel(DateTime when)
    {
        if (Status is TournamentStatus.Termine or TournamentStatus.Archive)
        {
            throw new DomainException("Un tournoi terminé ne peut plus être annulé.");
        }

        Status = TournamentStatus.Annule;
        UpdatedAt = when;
    }

    /// <summary>Nombre de rondes conseillé pour un système suisse (log2 du nombre de joueurs).</summary>
    public static int RecommendedRounds(int playerCount) => playerCount switch
    {
        <= 2 => 1,
        <= 4 => 2,
        <= 8 => 3,
        <= 16 => 4,
        <= 32 => 5,
        <= 64 => 6,
        <= 128 => 7,
        _ => 9
    };
}
