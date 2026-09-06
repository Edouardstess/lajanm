using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Boîte de réception des messages du formulaire de contact.</summary>
[Area("Admin")]
[Authorize(Policy = "EspaceAdmin")]
public sealed class MessagesController(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(ContactMessageStatus? statut, CancellationToken ct)
    {
        ViewData["Title"] = "Messages reçus";
        ViewData["Statut"] = statut;

        var query = context.ContactMessages.AsNoTracking().AsQueryable();

        if (statut.HasValue)
        {
            query = query.Where(m => m.Status == statut.Value);
        }

        var messages = await query
            .OrderByDescending(m => m.ReceivedAt)
            .Take(200)
            .ToListAsync(ct);

        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Traiter(int id, ContactMessageStatus statut, string? note, CancellationToken ct)
    {
        var message = await context.ContactMessages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null)
        {
            return NotFound();
        }

        message.Status = statut;
        message.InternalNote = note;
        message.HandledById = currentUser.UserId;
        message.HandledAt = clock.UtcNow;

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Message mis à jour.";
        return RedirectToAction(nameof(Index));
    }
}
