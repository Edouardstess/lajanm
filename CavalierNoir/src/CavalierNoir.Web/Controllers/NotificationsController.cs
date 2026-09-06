using CavalierNoir.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>Notifications internes affichées dans l'en-tête du site.</summary>
[Route("Notifications")]
[Authorize]
public sealed class NotificationsController(
    IApplicationDbContext context,
    INotificationService notifications,
    ICurrentUser currentUser) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        ViewData["Title"] = "Mes notifications";

        var items = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return View(items);
    }

    [HttpPost("Lire/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lire(int id, string? url, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        await notifications.MarkAsReadAsync(id, userId, ct);

        return !string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url)
            ? Redirect(url)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost("ToutLire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToutLire(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        await notifications.MarkAllAsReadAsync(userId, ct);
        return RedirectToAction(nameof(Index));
    }
}
