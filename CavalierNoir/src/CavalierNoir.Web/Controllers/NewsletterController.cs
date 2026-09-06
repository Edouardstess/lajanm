using CavalierNoir.Application.Services;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CavalierNoir.Web.Controllers;

/// <summary>
/// Lettre d'information : abonnement en double opt-in, confirmation et
/// désabonnement en un clic.
/// </summary>
[Route("Newsletter")]
public sealed class NewsletterController(NewsletterService newsletter) : Controller
{
    [HttpPost("Inscription")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Inscription(NewsletterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erreur"] = "Adresse électronique invalide.";
            return RedirectToAction("Index", "Home");
        }

        var result = await newsletter.SubscribeAsync(
            model.Email,
            model.Name,
            model.WantsDailyExercise,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Merci ! Un courriel vient de vous être envoyé : cliquez sur le lien pour confirmer votre abonnement."
            : result.Error;

        return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } referer && Url.IsLocalUrl(referer)
            ? referer
            : Url.Action("Index", "Home")!);
    }

    [HttpGet("Confirmer/{token}")]
    public async Task<IActionResult> Confirmer(string token, CancellationToken ct)
    {
        var result = await newsletter.ConfirmAsync(token, ct);

        ViewData["Title"] = "Abonnement confirmé";
        ViewData["Succes"] = result.Succeeded;
        ViewData["Message"] = result.Succeeded
            ? "Votre abonnement est confirmé. Vous recevrez l'exercice du jour et nos actualités."
            : result.Error;

        return View("Message");
    }

    [HttpGet("Desabonnement/{token}")]
    public async Task<IActionResult> Desabonnement(string token, CancellationToken ct)
    {
        var result = await newsletter.UnsubscribeAsync(token, ct);

        ViewData["Title"] = "Désabonnement";
        ViewData["Succes"] = result.Succeeded;
        ViewData["Message"] = result.Succeeded
            ? "Vous ne recevrez plus nos courriels. Vous pouvez vous réabonner à tout moment depuis le site."
            : result.Error;

        return View("Message");
    }
}
