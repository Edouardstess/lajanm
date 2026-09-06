using System.ComponentModel.DataAnnotations;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Club;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.Tournaments;

namespace CavalierNoir.Web.Models;

/// <summary>Page d'accueil.</summary>
public sealed class HomeViewModel
{
    public ExerciseCard? DailyExercise { get; init; }

    public IReadOnlyList<BlogPostCard> LatestPosts { get; init; } = [];

    public IReadOnlyList<TournamentCard> UpcomingTournaments { get; init; } = [];

    public IReadOnlyList<ClubEvent> UpcomingEvents { get; init; } = [];

    public IReadOnlyList<Partner> Partners { get; init; } = [];

    public int MemberCount { get; init; }

    public int ExerciseCount { get; init; }

    public int SolvedCount { get; init; }

    public int TournamentCount { get; init; }
}

/// <summary>Liste d'articles.</summary>
public sealed class BlogIndexViewModel
{
    public PagedList<BlogPostCard> Posts { get; init; } = PagedList<BlogPostCard>.Empty();

    public IReadOnlyList<BlogCategory> Categories { get; init; } = [];

    public IReadOnlyList<BlogTag> Tags { get; init; } = [];

    public string? CurrentCategory { get; init; }

    public string? CurrentTag { get; init; }

    public string? Search { get; init; }
}

/// <summary>Article et ses commentaires.</summary>
public sealed class BlogDetailsViewModel
{
    public required BlogPost Post { get; init; }

    public IReadOnlyList<BlogComment> Comments { get; init; } = [];

    public IReadOnlyList<BlogPostCard> Related { get; init; } = [];

    public CommentFormViewModel Form { get; init; } = new();
}

/// <summary>Dépôt d'un commentaire.</summary>
public sealed class CommentFormViewModel
{
    public int PostId { get; set; }

    public int? ParentId { get; set; }

    [Required(ErrorMessage = "Le commentaire ne peut pas être vide.")]
    [StringLength(4000, MinimumLength = 3,
        ErrorMessage = "Le commentaire doit comporter entre 3 et 4000 caractères.")]
    [Display(Name = "Votre commentaire")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>Bibliothèque d'exercices.</summary>
public sealed class ExerciseIndexViewModel
{
    public PagedList<ExerciseCard> Exercises { get; init; } = PagedList<ExerciseCard>.Empty();

    public ExerciseFilter Filter { get; init; } = new();

    public IReadOnlyList<ExerciseCollection> Collections { get; init; } = [];

    public bool IsMember { get; init; }
}

/// <summary>Écran de résolution d'un exercice.</summary>
public sealed class ExerciseSolveViewModel
{
    public required Exercise Exercise { get; init; }

    /// <summary>Jeton du lien reçu par courriel, s'il y a lieu.</summary>
    public string? Token { get; init; }

    public bool CanSubmit { get; init; }

    public bool AlreadySolved { get; init; }

    public IReadOnlyList<ExerciseAttempt> PreviousAttempts { get; init; } = [];

    public ExerciseCorrection? Correction { get; set; }

    public int HintsRevealed { get; set; }
}

/// <summary>Réponse envoyée par le formulaire de résolution.</summary>
public sealed class ExerciseSubmitViewModel
{
    public int ExerciseId { get; set; }

    [Display(Name = "Votre solution")]
    public string? Moves { get; set; }

    public int TimeSpentSeconds { get; set; }

    public int HintsUsed { get; set; }

    public string? Token { get; set; }
}

/// <summary>Fiche d'un tournoi.</summary>
public sealed class TournamentDetailsViewModel
{
    public required Tournament Tournament { get; init; }

    public IReadOnlyList<StandingLine> Standings { get; init; } = [];

    public IReadOnlyList<PairingLine> Pairings { get; init; } = [];

    public int SelectedRound { get; init; }

    public int RoundCount { get; init; }

    public bool IsRegistered { get; init; }

    public bool CanRegister { get; init; }
}

/// <summary>Formulaire de contact public.</summary>
public sealed class ContactFormViewModel
{
    [Required(ErrorMessage = "Votre nom est obligatoire.")]
    [StringLength(120)]
    [Display(Name = "Nom complet")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Votre adresse électronique est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse électronique invalide.")]
    [Display(Name = "Adresse électronique")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Téléphone (facultatif)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "L'objet est obligatoire.")]
    [StringLength(200)]
    [Display(Name = "Objet")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le message est obligatoire.")]
    [StringLength(4000, MinimumLength = 10)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Votre consentement est nécessaire pour traiter votre message.")]
    [Display(Name = "J'accepte que mes données soient utilisées pour répondre à ma demande")]
    public bool ConsentGiven { get; set; }

    /// <summary>
    /// Champ leurre invisible : rempli, il révèle un robot. Alternative discrète
    /// à un CAPTCHA, sans dépendance externe ni traceur.
    /// </summary>
    public string? Website { get; set; }
}

/// <summary>Demande d'adhésion.</summary>
public sealed class MembershipRequestViewModel
{
    public IReadOnlyList<MembershipTypeDto> Types { get; set; } = [];

    [Display(Name = "Formule choisie")]
    [Range(1, int.MaxValue, ErrorMessage = "Choisissez une formule d'adhésion.")]
    public int MembershipTypeId { get; set; }

    [Display(Name = "Votre niveau estimé")]
    [Range(200, 1600, ErrorMessage = "Le classement déclaré doit être compris entre 200 et 1600.")]
    public int DeclaredElo { get; set; } = 1200;

    [Display(Name = "Pourquoi souhaitez-vous rejoindre le club ?")]
    [StringLength(2000)]
    public string? Motivation { get; set; }

    [Display(Name = "Autorisation parentale (candidat mineur)")]
    public bool ParentalConsent { get; set; }

    [Display(Name = "Contact du responsable légal")]
    [StringLength(256)]
    public string? ParentContact { get; set; }
}

/// <summary>Sujet de forum et ses messages.</summary>
public sealed class ForumTopicViewModel
{
    public required ForumTopic Topic { get; init; }

    public IReadOnlyList<ForumPost> Posts { get; init; } = [];

    public ForumReplyViewModel Reply { get; init; } = new();

    public bool CanModerate { get; init; }
}

/// <summary>Réponse à un sujet.</summary>
public sealed class ForumReplyViewModel
{
    public int TopicId { get; set; }

    [Required(ErrorMessage = "Le message ne peut pas être vide.")]
    [StringLength(8000, MinimumLength = 2)]
    [Display(Name = "Votre message")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Position à joindre (FEN, facultatif)")]
    [StringLength(120)]
    public string? Fen { get; set; }
}

/// <summary>Ouverture d'un sujet.</summary>
public sealed class NewTopicViewModel
{
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200, MinimumLength = 5)]
    [Display(Name = "Titre du sujet")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le message est obligatoire.")]
    [StringLength(8000, MinimumLength = 10)]
    [Display(Name = "Message")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Position à joindre (FEN, facultatif)")]
    [StringLength(120)]
    public string? Fen { get; set; }
}

/// <summary>Abonnement à la lettre d'information.</summary>
public sealed class NewsletterViewModel
{
    [Required(ErrorMessage = "L'adresse électronique est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse électronique invalide.")]
    [Display(Name = "Adresse électronique")]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Prénom (facultatif)")]
    public string? Name { get; set; }

    [Display(Name = "Je souhaite aussi recevoir l'exercice du jour")]
    public bool WantsDailyExercise { get; set; } = true;
}
