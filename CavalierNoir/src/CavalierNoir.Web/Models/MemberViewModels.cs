using System.ComponentModel.DataAnnotations;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Finance;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.Memberships;

namespace CavalierNoir.Web.Models;

/// <summary>Modification du profil public d'un membre.</summary>
public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "Le prénom est obligatoire.")]
    [StringLength(50)]
    [Display(Name = "Prénom")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(50)]
    [Display(Name = "Nom")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Pseudonyme affiché")]
    public string? Pseudonym { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date de naissance")]
    public DateOnly? BirthDate { get; set; }

    [Phone]
    [Display(Name = "Téléphone")]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Ville")]
    public string? City { get; set; }

    [StringLength(2000)]
    [Display(Name = "Présentation")]
    public string? Bio { get; set; }

    [StringLength(50)]
    [Display(Name = "Pseudo Lichess")]
    public string? LichessUsername { get; set; }

    [StringLength(50)]
    [Display(Name = "Pseudo Chess.com")]
    public string? ChessComUsername { get; set; }

    [StringLength(20)]
    [Display(Name = "Identifiant FIDE")]
    public string? FideId { get; set; }

    public int Elo { get; set; }

    public string? ProfilePictureUrl { get; set; }
}

/// <summary>Préférences d'affichage et de notification.</summary>
public sealed class PreferencesViewModel
{
    [Display(Name = "Langue de l'interface")]
    public string Language { get; set; } = "fr";

    [Display(Name = "Thème")]
    public string Theme { get; set; } = "systeme";

    [Display(Name = "Recevoir l'exercice du jour")]
    public bool ReceiveDailyExercise { get; set; } = true;

    [Display(Name = "Recevoir la lettre d'information")]
    public bool ReceiveNewsletter { get; set; } = true;

    [Display(Name = "Être averti des tournois et des appariements")]
    public bool ReceiveTournamentAlerts { get; set; } = true;

    [Display(Name = "Être averti des réponses sur le forum")]
    public bool ReceiveForumReplies { get; set; } = true;

    [Display(Name = "Recevoir les rappels d'échéance d'adhésion")]
    public bool ReceiveMembershipReminders { get; set; } = true;

    [Display(Name = "Apparaître dans le classement public du club")]
    public bool ShowInPublicRanking { get; set; } = true;
}

/// <summary>Espace « Mon adhésion ».</summary>
public sealed class MemberMembershipViewModel
{
    public Membership? Current { get; init; }

    public IReadOnlyList<Payment> Payments { get; init; } = [];

    public MembershipApplication? PendingApplication { get; init; }

    public IReadOnlyList<MembershipTypeDto> Types { get; init; } = [];
}

/// <summary>Historique d'entraînement d'un membre.</summary>
public sealed class TrainingHistoryViewModel
{
    public ProgressSummary Progress { get; init; } = new(0, 0, 0, 0, 0, 0, null);

    public PagedList<ExerciseAttempt> Attempts { get; init; } = PagedList<ExerciseAttempt>.Empty();

    public IReadOnlyList<UserBadge> Badges { get; init; } = [];

    public IReadOnlyList<ThemeStat> ByTheme { get; init; } = [];
}

/// <summary>Taux de réussite d'un membre pour un thème donné.</summary>
/// <param name="Theme">Libellé du thème.</param>
/// <param name="Attempts">Nombre de tentatives.</param>
/// <param name="Correct">Nombre de réussites.</param>
public sealed record ThemeStat(string Theme, int Attempts, int Correct)
{
    public int SuccessRate => Attempts == 0 ? 0 : (int)Math.Round(Correct * 100.0 / Attempts);
}

/// <summary>Export des données personnelles (RGPD).</summary>
public sealed class PersonalDataViewModel
{
    public required ApplicationUser User { get; init; }

    public int AttemptCount { get; init; }

    public int GameCount { get; init; }

    public int CommentCount { get; init; }

    public int PaymentCount { get; init; }

    public DateTime? OldestRecord { get; init; }
}
