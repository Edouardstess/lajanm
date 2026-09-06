using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.Memberships;
using CavalierNoir.Domain.Tournaments;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace CavalierNoir.Domain.Identity;

/// <summary>
/// Racine de l'agrégat Utilisateur. Étend <see cref="IdentityUser{TKey}"/> :
/// l'authentification, le hachage du mot de passe, le verrouillage de compte et la
/// double authentification sont pris en charge par ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser<int>, IAggregateRoot
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Pseudonyme affiché sur le forum et les classements, à défaut du nom réel.</summary>
    public string? Pseudonym { get; set; }

    public DateOnly? BirthDate { get; set; }

    /// <summary>Classement ELO interne du club (BR-07).</summary>
    public int Elo { get; set; } = EloRating.DefaultRating;

    /// <summary>Nombre de parties classées jouées : détermine le facteur K (BR-07).</summary>
    public int RatedGamesPlayed { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? Bio { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; } = "Haïti";

    public string? FideId { get; set; }

    public string? LichessUsername { get; set; }

    public string? ChessComUsername { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Date d'anonymisation RGPD ; l'enregistrement est conservé pour l'historique sportif.</summary>
    public DateTime? AnonymizedAt { get; set; }

    public string? Notes { get; set; }

    // --- Navigations ---
    public UserPreference? Preference { get; set; }

    public UserProgress? Progress { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();

    public ICollection<MembershipApplication> Applications { get; set; } = new List<MembershipApplication>();

    public ICollection<ExerciseAttempt> ExerciseAttempts { get; set; } = new List<ExerciseAttempt>();

    public ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();

    public ICollection<EloHistory> EloHistory { get; set; } = new List<EloHistory>();

    public ICollection<TournamentRegistration> TournamentRegistrations { get; set; } = new List<TournamentRegistration>();

    public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<LoginHistory> LoginHistory { get; set; } = new List<LoginHistory>();

    public ICollection<ForumTopic> ForumTopics { get; set; } = new List<ForumTopic>();

    public ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();

    // --- Comportement métier ---

    /// <summary>Nom affiché : pseudonyme s'il existe, sinon « Prénom NOM ».</summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Pseudonym) ? Pseudonym! : $"{FirstName} {LastName}".Trim();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Initials =>
        $"{(FirstName.Length > 0 ? FirstName[0] : '?')}{(LastName.Length > 0 ? LastName[0] : ' ')}".Trim();

    /// <summary>Niveau pédagogique déduit du classement ELO.</summary>
    public PlayerLevel Level => Elo switch
    {
        <= 1200 => PlayerLevel.Debutant,
        <= 1600 => PlayerLevel.Intermediaire,
        <= 2000 => PlayerLevel.Avance,
        _ => PlayerLevel.Expert
    };

    public int? Age(DateOnly today)
    {
        if (BirthDate is null)
        {
            return null;
        }

        var age = today.Year - BirthDate.Value.Year;
        if (BirthDate.Value.AddYears(age) > today)
        {
            age--;
        }

        return age;
    }

    /// <summary>BR-02 : âge minimum de 6 ans, sauf autorisation parentale.</summary>
    public bool MeetsMinimumAge(DateOnly today, int minimumAge = 6) => (Age(today) ?? minimumAge) >= minimumAge;

    public void RecordLogin(DateTime when)
    {
        LastLoginAt = when;
    }

    /// <summary>
    /// Anonymise le compte (droit à l'oubli) sans casser les références historiques :
    /// les parties, classements et résultats de tournoi restent cohérents.
    /// </summary>
    public void Anonymize(DateTime when)
    {
        FirstName = "Membre";
        LastName = "anonymisé";
        Pseudonym = $"anonyme-{Id}";
        Email = $"anonyme-{Id}@cavaliernoir.invalid";
        NormalizedEmail = Email.ToUpperInvariant();
        UserName = Pseudonym;
        NormalizedUserName = Pseudonym.ToUpperInvariant();
        PhoneNumber = null;
        Bio = null;
        Notes = null;
        City = null;
        BirthDate = null;
        ProfilePictureUrl = null;
        LichessUsername = null;
        ChessComUsername = null;
        FideId = null;
        IsActive = false;
        AnonymizedAt = when;
    }
}
