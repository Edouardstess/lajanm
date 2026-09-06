using System.Text.Json;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Membre.Controllers;

/// <summary>
/// Profil du membre, préférences, mot de passe et exercice des droits RGPD
/// (export et suppression du compte).
/// </summary>
[Area("Membre")]
[Authorize]
public sealed class ProfilController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IApplicationDbContext context,
    IDateTimeProvider clock,
    IFileStorage storage,
    ILogger<ProfilController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        ViewData["Title"] = "Mon profil";

        return View(new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Pseudonym = user.Pseudonym,
            BirthDate = user.BirthDate,
            PhoneNumber = user.PhoneNumber,
            City = user.City,
            Bio = user.Bio,
            LichessUsername = user.LichessUsername,
            ChessComUsername = user.ChessComUsername,
            FideId = user.FideId,
            Elo = user.Elo,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, IFormFile? photo, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.Elo = user.Elo;
            model.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        // Le pseudonyme doit rester unique : il identifie le membre sur le forum.
        if (!string.IsNullOrWhiteSpace(model.Pseudonym))
        {
            var taken = await context.Users
                .AnyAsync(u => u.Id != user.Id && u.Pseudonym == model.Pseudonym, ct);

            if (taken)
            {
                ModelState.AddModelError(nameof(model.Pseudonym), "Ce pseudonyme est déjà utilisé.");
                model.Elo = user.Elo;
                return View(model);
            }
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Pseudonym = string.IsNullOrWhiteSpace(model.Pseudonym) ? null : model.Pseudonym.Trim();
        user.BirthDate = model.BirthDate;
        user.PhoneNumber = model.PhoneNumber;
        user.City = model.City;
        user.Bio = model.Bio;
        user.LichessUsername = model.LichessUsername;
        user.ChessComUsername = model.ChessComUsername;
        user.FideId = model.FideId;

        if (photo is { Length: > 0 })
        {
            if (photo.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "La photo ne doit pas dépasser 2 Mo.");
                model.Elo = user.Elo;
                return View(model);
            }

            await using var stream = photo.OpenReadStream();
            var stored = await storage.SaveAsync(stream, photo.FileName, "profils", ct);
            user.ProfilePictureUrl = stored.RelativeUrl;
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Elo = user.Elo;
            return View(model);
        }

        TempData["Succes"] = "Votre profil a été mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Preferences(CancellationToken ct)
    {
        var userId = userManager.GetUserId(User);
        if (!int.TryParse(userId, out var id))
        {
            return Challenge();
        }

        var preference = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == id, ct)
                         ?? new UserPreference { UserId = id };

        ViewData["Title"] = "Mes préférences";

        return View(new PreferencesViewModel
        {
            Language = preference.Language,
            Theme = preference.Theme,
            ReceiveDailyExercise = preference.ReceiveDailyExercise,
            ReceiveNewsletter = preference.ReceiveNewsletter,
            ReceiveTournamentAlerts = preference.ReceiveTournamentAlerts,
            ReceiveForumReplies = preference.ReceiveForumReplies,
            ReceiveMembershipReminders = preference.ReceiveMembershipReminders,
            ShowInPublicRanking = preference.ShowInPublicRanking
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preferences(PreferencesViewModel model, CancellationToken ct)
    {
        var userId = userManager.GetUserId(User);
        if (!int.TryParse(userId, out var id))
        {
            return Challenge();
        }

        var preference = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == id, ct);
        if (preference is null)
        {
            preference = new UserPreference { UserId = id };
            context.UserPreferences.Add(preference);
        }

        preference.Language = model.Language;
        preference.Theme = model.Theme;
        preference.ReceiveDailyExercise = model.ReceiveDailyExercise;
        preference.ReceiveNewsletter = model.ReceiveNewsletter;
        preference.ReceiveTournamentAlerts = model.ReceiveTournamentAlerts;
        preference.ReceiveForumReplies = model.ReceiveForumReplies;
        preference.ReceiveMembershipReminders = model.ReceiveMembershipReminders;
        preference.ShowInPublicRanking = model.ShowInPublicRanking;
        preference.UpdatedAt = clock.UtcNow;

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Vos préférences ont été enregistrées.";
        return RedirectToAction(nameof(Preferences));
    }

    public IActionResult MotDePasse()
    {
        ViewData["Title"] = "Changer mon mot de passe";
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MotDePasse(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["Succes"] = "Votre mot de passe a été modifié.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Récapitulatif des données détenues sur le membre (droit d'accès).</summary>
    public async Task<IActionResult> Donnees(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        ViewData["Title"] = "Mes données personnelles";

        return View(new PersonalDataViewModel
        {
            User = user,
            AttemptCount = await context.ExerciseAttempts.CountAsync(a => a.UserId == user.Id, ct),
            GameCount = await context.TournamentGames
                .CountAsync(g => g.WhitePlayerId == user.Id || g.BlackPlayerId == user.Id, ct),
            CommentCount = await context.BlogComments.CountAsync(c => c.AuthorId == user.Id, ct),
            PaymentCount = await context.Payments.CountAsync(p => p.UserId == user.Id, ct),
            OldestRecord = user.CreatedAt
        });
    }

    /// <summary>Export complet au format JSON (droit à la portabilité).</summary>
    public async Task<IActionResult> Exporter(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var export = new
        {
            exporteLe = clock.UtcNow,
            identite = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Pseudonym,
                user.BirthDate,
                user.PhoneNumber,
                user.City,
                user.Bio,
                user.Elo,
                user.CreatedAt,
                user.LastLoginAt
            },
            adhesions = await context.Memberships
                .Where(m => m.UserId == user.Id)
                .Select(m => new { m.MemberNumber, m.Status, m.Period.Start, m.Period.End })
                .ToListAsync(ct),
            paiements = await context.Payments
                .Where(p => p.UserId == user.Id)
                .Select(p => new { p.ReceiptNumber, p.Amount.Amount, p.Amount.Currency, p.Method, p.PaidAt })
                .ToListAsync(ct),
            exercices = await context.ExerciseAttempts
                .Where(a => a.UserId == user.Id)
                .Select(a => new { a.ExerciseId, a.IsCorrect, a.TimeSpentSeconds, a.AttemptedAt })
                .ToListAsync(ct),
            classement = await context.EloHistory
                .Where(h => h.UserId == user.Id)
                .Select(h => new { h.OldElo, h.NewElo, h.RecordedAt })
                .ToListAsync(ct),
            commentaires = await context.BlogComments
                .Where(c => c.AuthorId == user.Id)
                .Select(c => new { c.Content, c.CreatedAt, c.Status })
                .ToListAsync(ct)
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        logger.LogInformation("Export RGPD généré pour l'utilisateur {UserId}.", user.Id);

        return File(json, "application/json", $"cavaliernoir-donnees-{user.Id}.json");
    }

    /// <summary>
    /// Suppression du compte : les données personnelles sont effacées, les
    /// résultats sportifs conservés sous forme anonymisée.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Supprimer(string confirmation, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!string.Equals(confirmation, "SUPPRIMER", StringComparison.Ordinal))
        {
            TempData["Erreur"] = "Saisissez « SUPPRIMER » en majuscules pour confirmer.";
            return RedirectToAction(nameof(Donnees));
        }

        user.Anonymize(clock.UtcNow);
        await userManager.UpdateAsync(user);

        var preference = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (preference is not null)
        {
            preference.ReceiveDailyExercise = false;
            preference.ReceiveNewsletter = false;
            preference.ShowInPublicRanking = false;
        }

        var subscriber = await context.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.UserId == user.Id, ct);
        subscriber?.Unsubscribe(clock.UtcNow);

        await context.SaveChangesAsync(ct);
        await signInManager.SignOutAsync();

        logger.LogWarning("Compte {UserId} anonymisé à la demande de son titulaire.", user.Id);

        TempData["Succes"] = "Votre compte a été supprimé. Vos données personnelles ont été effacées.";
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
