using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Modération des commentaires et traitement des signalements (BR-09).</summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Content.Moderate)]
public sealed class ModerationController(
    BlogService blog,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(CommentStatus? statut, CancellationToken ct)
    {
        ViewData["Title"] = "Modération";
        ViewData["Statut"] = statut ?? CommentStatus.EnAttente;

        var target = statut ?? CommentStatus.EnAttente;

        var comments = await context.BlogComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.BlogPost)
            .Where(c => c.Status == target)
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        ViewData["Signalements"] = await context.CommentReports
            .AsNoTracking()
            .Include(r => r.Reporter)
            .Include(r => r.BlogComment)
            .Where(r => !r.IsHandled)
            .OrderByDescending(r => r.ReportedAt)
            .Take(50)
            .ToListAsync(ct);

        return View(comments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Traiter(int id, bool approuver, CancellationToken ct)
    {
        if (currentUser.UserId is not { } moderatorId)
        {
            return Challenge();
        }

        var result = await blog.ModerateCommentAsync(id, approuver, moderatorId, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? approuver ? "Commentaire publié." : "Commentaire masqué."
            : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClasserSignalement(int id, CancellationToken ct)
    {
        var report = await context.CommentReports.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (report is null)
        {
            return NotFound();
        }

        report.IsHandled = true;
        report.HandledById = currentUser.UserId;
        report.HandledAt = clock.UtcNow;

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Signalement classé.";
        return RedirectToAction(nameof(Index));
    }
}
