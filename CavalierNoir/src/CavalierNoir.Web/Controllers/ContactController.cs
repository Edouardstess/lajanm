using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Club;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CavalierNoir.Web.Controllers;

/// <summary>Formulaire de contact public, protégé par un champ leurre et une limitation de débit.</summary>
[Route("Contact")]
public sealed class ContactController(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    INotificationService notifications,
    ILogger<ContactController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Nous contacter";
        return View(new ContactFormViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Index(ContactFormViewModel model, CancellationToken ct)
    {
        // Champ leurre : invisible pour un humain, rempli par la plupart des robots.
        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            logger.LogInformation("Message de contact rejeté : champ leurre rempli.");
            TempData["Succes"] = "Merci, votre message a bien été transmis.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        context.ContactMessages.Add(new ContactMessage
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Phone = model.Phone,
            Subject = model.Subject.Trim(),
            Message = model.Message.Trim(),
            ConsentGiven = model.ConsentGiven,
            ReceivedAt = clock.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        await context.SaveChangesAsync(ct);

        await notifications.NotifyRoleAsync(
            Domain.Identity.Roles.Secretaire,
            "Nouveau message de contact",
            $"{model.Name} a écrit au sujet de « {model.Subject} ».",
            "/Admin/Messages",
            "envelope",
            ct);

        TempData["Succes"] = "Merci, votre message a bien été transmis. Nous répondons sous 48 heures ouvrées.";
        return RedirectToAction(nameof(Index));
    }
}
