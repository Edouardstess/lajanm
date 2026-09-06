using CavalierNoir.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>Pages éditoriales gérées depuis le back-office, et foire aux questions.</summary>
[Route("Infos")]
public sealed class InfosController(IApplicationDbContext context) : Controller
{
    [HttpGet("faq")]
    public async Task<IActionResult> Faq(CancellationToken ct)
    {
        ViewData["Title"] = "Questions fréquentes";

        var items = await context.FaqItems
            .AsNoTracking()
            .Where(f => f.IsPublished)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.DisplayOrder)
            .ToListAsync(ct);

        return View(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Page(string slug, CancellationToken ct)
    {
        var page = await context.StaticPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished, ct);

        if (page is null)
        {
            return NotFound();
        }

        ViewData["Title"] = page.MetaTitle ?? page.Title;
        ViewData["MetaDescription"] = page.MetaDescription;

        return View(page);
    }
}
