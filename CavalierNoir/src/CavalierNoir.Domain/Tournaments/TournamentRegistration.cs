using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Tournaments;

/// <summary>Inscription d'un joueur à un tournoi, avec gestion de la liste d'attente.</summary>
public class TournamentRegistration : AuditableEntity
{
    public int TournamentId { get; set; }

    public Tournament Tournament { get; set; } = null!;

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public RegistrationStatus Status { get; set; } = RegistrationStatus.EnAttente;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Classement figé à l'inscription : sert d'ordre d'appariement de la ronde 1.</summary>
    public int EloAtRegistration { get; set; }

    /// <summary>Rang dans la liste d'attente (1 = premier appelé).</summary>
    public int? WaitingListPosition { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public int? PaymentId { get; set; }

    /// <summary>Nombre de rondes non jouées consécutives (BR-05 : forfait général à 2).</summary>
    public int ConsecutiveAbsences { get; set; }

    public bool IsForfeited { get; set; }

    public void Confirm(DateTime when)
    {
        Status = RegistrationStatus.Confirmee;
        ConfirmedAt = when;
        WaitingListPosition = null;
        UpdatedAt = when;
    }

    public void PutOnWaitingList(int position, DateTime when)
    {
        Status = RegistrationStatus.ListeAttente;
        WaitingListPosition = position;
        UpdatedAt = when;
    }

    public void Cancel(string? reason, DateTime when)
    {
        Status = RegistrationStatus.Annulee;
        CancellationReason = reason;
        CancelledAt = when;
        UpdatedAt = when;
    }

    /// <summary>BR-05 : deux absences consécutives entraînent le forfait général.</summary>
    public void RecordAbsence(DateTime when)
    {
        ConsecutiveAbsences++;
        if (ConsecutiveAbsences >= 2)
        {
            IsForfeited = true;
            Status = RegistrationStatus.Absente;
        }

        UpdatedAt = when;
    }

    public void RecordAttendance(DateTime when)
    {
        ConsecutiveAbsences = 0;
        CheckedInAt ??= when;
        UpdatedAt = when;
    }
}
