using System.ComponentModel.DataAnnotations;

namespace CavalierNoir.Web.Models;

/// <summary>Formulaire de connexion.</summary>
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "L'adresse électronique est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse électronique invalide.")]
    [Display(Name = "Adresse électronique")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Rester connecté sur cet appareil")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>Formulaire de création de compte.</summary>
public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Le prénom est obligatoire.")]
    [StringLength(50)]
    [Display(Name = "Prénom")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(50)]
    [Display(Name = "Nom")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'adresse électronique est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse électronique invalide.")]
    [Display(Name = "Adresse électronique")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Date de naissance")]
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; set; }

    [Phone(ErrorMessage = "Numéro de téléphone invalide.")]
    [Display(Name = "Téléphone")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [StringLength(100, MinimumLength = 12,
        ErrorMessage = "Le mot de passe doit comporter au moins 12 caractères.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmation du mot de passe")]
    [Compare(nameof(Password), ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Je souhaite recevoir l'exercice du jour par courriel")]
    public bool WantsDailyExercise { get; set; } = true;

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Vous devez accepter la politique de confidentialité pour créer un compte.")]
    [Display(Name = "J'ai lu et j'accepte la politique de confidentialité")]
    public bool AcceptsPrivacyPolicy { get; set; }
}

/// <summary>Demande de réinitialisation de mot de passe.</summary>
public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "L'adresse électronique est obligatoire.")]
    [EmailAddress(ErrorMessage = "Adresse électronique invalide.")]
    [Display(Name = "Adresse électronique")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Définition d'un nouveau mot de passe à partir d'un jeton.</summary>
public sealed class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [StringLength(100, MinimumLength = 12,
        ErrorMessage = "Le mot de passe doit comporter au moins 12 caractères.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nouveau mot de passe")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmation")]
    [Compare(nameof(Password), ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Changement de mot de passe depuis l'espace membre.</summary>
public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Le mot de passe actuel est obligatoire.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe actuel")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
    [StringLength(100, MinimumLength = 12,
        ErrorMessage = "Le mot de passe doit comporter au moins 12 caractères.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nouveau mot de passe")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmation")]
    [Compare(nameof(NewPassword), ErrorMessage = "Les deux mots de passe ne correspondent pas.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
