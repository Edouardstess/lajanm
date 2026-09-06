using System.ComponentModel.DataAnnotations;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Web.Models;

/// <summary>Création ou modification d'un exercice.</summary>
public sealed class ExerciseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200)]
    [Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La position FEN est obligatoire.")]
    [StringLength(120)]
    [Display(Name = "Position (FEN)")]
    public string Fen { get; set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    [Required(ErrorMessage = "La solution est obligatoire.")]
    [StringLength(500)]
    [Display(Name = "Solution (notation par coordonnées, ex. « a1a8 »)")]
    public string Solution { get; set; } = string.Empty;

    [Display(Name = "Thème")]
    public ExerciseTheme Theme { get; set; } = ExerciseTheme.MatEnUn;

    [Display(Name = "Difficulté")]
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Moyen;

    [Range(0, 3600)]
    [Display(Name = "Temps estimé (secondes)")]
    public int? EstimatedTimeSeconds { get; set; }

    [StringLength(500)]
    [Display(Name = "Indice 1")]
    public string? Hint1 { get; set; }

    [StringLength(500)]
    [Display(Name = "Indice 2")]
    public string? Hint2 { get; set; }

    [StringLength(500)]
    [Display(Name = "Indice 3")]
    public string? Hint3 { get; set; }

    [StringLength(4000)]
    [Display(Name = "Correction détaillée")]
    public string? Explanation { get; set; }

    [StringLength(200)]
    [Display(Name = "Source")]
    public string? Source { get; set; }

    [Display(Name = "Recueil")]
    public int? CollectionId { get; set; }

    [Display(Name = "Publié")]
    public bool IsPublished { get; set; }

    [Display(Name = "Visible des visiteurs non connectés")]
    public bool IsPublic { get; set; }

    public IReadOnlyList<(int Id, string Title)> Collections { get; set; } = [];
}

/// <summary>Création ou modification d'un article.</summary>
public sealed class BlogPostFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200)]
    [Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Identifiant d'URL")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Le chapeau est obligatoire.")]
    [StringLength(500)]
    [Display(Name = "Chapeau (résumé)")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le contenu est obligatoire.")]
    [Display(Name = "Contenu (HTML)")]
    public string Content { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Image à la une (URL)")]
    public string? FeaturedImageUrl { get; set; }

    [StringLength(200)]
    [Display(Name = "Texte alternatif de l'image")]
    public string? FeaturedImageAlt { get; set; }

    [Display(Name = "Rubrique")]
    public int? CategoryId { get; set; }

    [Display(Name = "Étiquettes")]
    public List<int> TagIds { get; set; } = [];

    [Display(Name = "Autoriser les commentaires")]
    public bool AllowComments { get; set; } = true;

    [Display(Name = "Mettre en avant")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Publication programmée")]
    [DataType(DataType.DateTime)]
    public DateTime? ScheduledFor { get; set; }

    [StringLength(320)]
    [Display(Name = "Description pour les moteurs de recherche")]
    public string? MetaDescription { get; set; }

    public BlogStatus Status { get; set; } = BlogStatus.Brouillon;

    public IReadOnlyList<(int Id, string Name)> Categories { get; set; } = [];

    public IReadOnlyList<(int Id, string Name)> Tags { get; set; } = [];
}

/// <summary>Création ou modification d'un tournoi.</summary>
public sealed class TournamentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200)]
    [Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Présentation")]
    public string? Description { get; set; }

    [Display(Name = "Règlement")]
    public string? Regulations { get; set; }

    [Display(Name = "Formule")]
    public TournamentType Type { get; set; } = TournamentType.SystemeSuisse;

    [Display(Name = "Cadence")]
    public TimeControl TimeControl { get; set; } = TimeControl.Rapide;

    [StringLength(60)]
    [Display(Name = "Cadence détaillée")]
    public string? TimeControlLabel { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Début")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fin")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(14).AddHours(6);

    [DataType(DataType.DateTime)]
    [Display(Name = "Clôture des inscriptions")]
    public DateTime? RegistrationClosesAt { get; set; }

    [StringLength(200)]
    [Display(Name = "Lieu")]
    public string? Location { get; set; }

    [Range(2, 500)]
    [Display(Name = "Nombre maximum de joueurs")]
    public int? MaxPlayers { get; set; }

    [Range(1, 15)]
    [Display(Name = "Nombre de rondes")]
    public int PlannedRounds { get; set; } = 5;

    [Range(0, 1000000)]
    [Display(Name = "Frais d'inscription")]
    public decimal EntryFee { get; set; }

    [Display(Name = "Compte pour le classement ELO")]
    public bool IsRated { get; set; } = true;

    [Display(Name = "Réservé aux membres")]
    public bool MembersOnly { get; set; } = true;

    [Range(200, 3000)]
    [Display(Name = "Classement minimum")]
    public int? MinimumElo { get; set; }

    [Range(200, 3000)]
    [Display(Name = "Classement maximum")]
    public int? MaximumElo { get; set; }
}

/// <summary>Création ou modification d'un événement.</summary>
public sealed class EventFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le titre est obligatoire.")]
    [StringLength(200)]
    [Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Type")]
    public EventType Type { get; set; } = EventType.Entrainement;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Début")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fin")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(7).AddHours(3);

    [StringLength(200)]
    [Display(Name = "Lieu")]
    public string? Location { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Places disponibles")]
    public int? MaxParticipants { get; set; }

    [Range(0, 1000000)]
    [Display(Name = "Tarif")]
    public decimal EntryFee { get; set; }

    [Display(Name = "Inscriptions ouvertes")]
    public bool IsRegistrationOpen { get; set; } = true;

    [Display(Name = "Réservé aux membres")]
    public bool MembersOnly { get; set; }

    [Display(Name = "Publié")]
    public bool IsPublished { get; set; }
}

/// <summary>Tableau de bord d'administration.</summary>
public sealed class AdminDashboardViewModel
{
    public required AdminDashboardStats Stats { get; init; }

    public IReadOnlyList<MembershipApplicationSummary> PendingApplications { get; init; } = [];
}
