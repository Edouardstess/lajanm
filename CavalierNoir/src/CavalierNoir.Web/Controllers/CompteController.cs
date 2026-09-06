using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Web.Controllers;

/// <summary>
/// Authentification et cycle de vie du compte. Toutes les actions sensibles
/// sont limitées en débit (20 requêtes/minute/IP) et tracées dans
/// l'historique des connexions.
/// </summary>
[Route("Compte")]
[EnableRateLimiting("authentification")]
public sealed class CompteController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IApplicationDbContext context,
    IDateTimeProvider clock,
    NewsletterService newsletter,
    IEmailSender emailSender,
    EmailTemplateFactory templates,
    IOptions<SiteOptions> siteOptions,
    ILogger<CompteController> logger) : Controller
{
    private readonly SiteOptions _site = siteOptions.Value;

    [HttpGet("Connexion")]
    public IActionResult Connexion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "TableauDeBord", new { area = "Membre" });
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("Connexion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Connexion(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        await RecordLoginAttemptAsync(user?.Id, model.Email, result.Succeeded, DescribeFailure(result), ct);

        if (result.Succeeded && user is not null)
        {
            user.RecordLogin(clock.UtcNow);
            await userManager.UpdateAsync(user);

            logger.LogInformation("Connexion réussie pour l'utilisateur {UserId}.", user.Id);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Votre compte est temporairement verrouillé après plusieurs échecs. Réessayez dans quinze minutes.");
            return View(model);
        }

        // Message volontairement identique que l'adresse existe ou non :
        // ne pas révéler quelles adresses sont inscrites (STRIDE — divulgation).
        ModelState.AddModelError(string.Empty, "Adresse électronique ou mot de passe incorrect.");
        return View(model);
    }

    [HttpGet("Inscription")]
    public IActionResult Inscription() => View(new RegisterViewModel());

    [HttpPost("Inscription")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscription(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // BR-02 : âge minimum de six ans.
        if (model.BirthDate is { } birthDate)
        {
            var age = clock.Today.Year - birthDate.Year;
            if (birthDate.AddYears(age) > clock.Today)
            {
                age--;
            }

            if (age < 6)
            {
                ModelState.AddModelError(
                    nameof(model.BirthDate),
                    "L'inscription est ouverte à partir de six ans.");
                return View(model);
            }
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            BirthDate = model.BirthDate,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = clock.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, Roles.Candidat);

        context.UserPreferences.Add(new UserPreference
        {
            UserId = user.Id,
            ReceiveDailyExercise = model.WantsDailyExercise
        });
        context.UserProgress.Add(new UserProgress { UserId = user.Id });
        await context.SaveChangesAsync(ct);

        if (model.WantsDailyExercise)
        {
            await newsletter.SubscribeAsync(
                model.Email,
                model.FirstName,
                true,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ct);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation("Nouveau compte créé : {UserId}.", user.Id);

        TempData["Succes"] =
            "Votre compte est créé. Déposez maintenant votre demande d'adhésion pour accéder à l'espace membre.";

        return RedirectToAction("Index", "Adherer");
    }

    [HttpPost("Deconnexion")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Deconnexion()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("MotDePasseOublie")]
    public IActionResult MotDePasseOublie() => View(new ForgotPasswordViewModel());

    [HttpPost("MotDePasseOublie")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MotDePasseOublie(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        // La réponse est identique dans tous les cas : elle ne doit pas indiquer
        // si l'adresse correspond à un compte existant.
        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action(
                nameof(Reinitialiser),
                "Compte",
                new { email = user.Email, token },
                Request.Scheme) ?? _site.BaseUrl;

            var body = $"<p>Bonjour {user.FirstName},</p>"
                       + "<p>Vous avez demandé la réinitialisation de votre mot de passe. "
                       + "Ce lien est valable une heure.</p>"
                       + $"<p style=\"text-align:center;margin:24px 0;\"><a href=\"{url}\" "
                       + "style=\"display:inline-block;background:#d4af37;color:#1a1a1a;text-decoration:none;"
                       + "padding:12px 24px;font-weight:600;border-radius:6px;\">Choisir un nouveau mot de passe</a></p>"
                       + "<p>Si vous n'êtes pas à l'origine de cette demande, ignorez ce message : "
                       + "votre mot de passe reste inchangé.</p>";

            await emailSender.SendAsync(new EmailMessage
            {
                To = user.Email!,
                ToName = user.FullName,
                Subject = "Réinitialisation de votre mot de passe",
                HtmlBody = templates.Layout("Réinitialisation du mot de passe", body),
                Template = "password-reset",
                UserId = user.Id
            }, ct);
        }

        TempData["Succes"] =
            "Si un compte correspond à cette adresse, un message vient d'être envoyé avec la marche à suivre.";

        return RedirectToAction(nameof(Connexion));
    }

    [HttpGet("Reinitialiser")]
    public IActionResult Reinitialiser(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(MotDePasseOublie));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost("Reinitialiser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reinitialiser(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            TempData["Succes"] = "Mot de passe mis à jour. Vous pouvez vous connecter.";
            return RedirectToAction(nameof(Connexion));
        }

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            await userManager.ResetAccessFailedCountAsync(user);
            TempData["Succes"] = "Mot de passe mis à jour. Vous pouvez vous connecter.";
            return RedirectToAction(nameof(Connexion));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, TranslateIdentityError(error));
        }

        return View(model);
    }

    [HttpGet("AccesRefuse")]
    public IActionResult AccesRefuse()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View(new ErrorViewModel
        {
            StatusCode = 403,
            Title = "Accès refusé",
            Message = "Votre compte ne dispose pas des droits nécessaires pour consulter cette page. "
                      + "Si vous pensez qu'il s'agit d'une erreur, contactez le secrétariat du club."
        });
    }

    private async Task RecordLoginAttemptAsync(
        int? userId,
        string identifier,
        bool succeeded,
        string? failureReason,
        CancellationToken ct)
    {
        context.LoginHistory.Add(new LoginHistory
        {
            UserId = userId,
            AttemptedIdentifier = identifier,
            IsSuccessful = succeeded,
            FailureReason = failureReason,
            OccurredAt = clock.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            CorrelationId = HttpContext.TraceIdentifier
        });

        await context.SaveChangesAsync(ct);
    }

    private static string? DescribeFailure(Microsoft.AspNetCore.Identity.SignInResult result) => result switch
    {
        { Succeeded: true } => null,
        { IsLockedOut: true } => "Compte verrouillé",
        { IsNotAllowed: true } => "Connexion non autorisée",
        { RequiresTwoFactor: true } => "Double authentification requise",
        _ => "Identifiants invalides"
    };

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Cette adresse électronique est déjà utilisée.",
        "PasswordTooShort" => "Le mot de passe doit comporter au moins douze caractères.",
        "PasswordRequiresDigit" => "Le mot de passe doit contenir au moins un chiffre.",
        "PasswordRequiresLower" => "Le mot de passe doit contenir au moins une minuscule.",
        "PasswordRequiresUpper" => "Le mot de passe doit contenir au moins une majuscule.",
        "PasswordRequiresNonAlphanumeric" => "Le mot de passe doit contenir au moins un caractère spécial.",
        "PasswordRequiresUniqueChars" => "Le mot de passe doit contenir au moins quatre caractères distincts.",
        "InvalidToken" => "Ce lien n'est plus valable. Demandez-en un nouveau.",
        _ => error.Description
    };

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "TableauDeBord", new { area = "Membre" });
}
