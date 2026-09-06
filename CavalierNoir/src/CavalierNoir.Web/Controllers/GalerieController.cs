using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>Galerie photo et vidéo du club.</summary>
[Route("Galerie")]
public sealed class GalerieController(IApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Galerie";

        var maxVisibility = Roles.MemberRoles.Split(',').Any(User.IsInRole)
            ? Visibility.Membres
            : Visibility.Public;

        var albums = await context.Albums
            .AsNoTracking()
            .Include(a => a.CoverMedia)
            .Where(a => a.IsPublished && a.Visibility <= maxVisibility)
            .OrderByDescending(a => a.TakenOn ?? DateOnly.MinValue)
            .ThenByDescending(a => a.Id)
            .ToListAsync(ct);

        return View(albums);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Album(string slug, CancellationToken ct)
    {
        var maxVisibility = Roles.MemberRoles.Split(',').Any(User.IsInRole)
            ? Visibility.Membres
            : Visibility.Public;

        var album = await context.Albums
            .AsNoTracking()
            .Include(a => a.Items.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished && a.Visibility <= maxVisibility, ct);

        if (album is null)
        {
            return NotFound();
        }

        ViewData["Title"] = album.Title;
        return View(album);
    }
}
