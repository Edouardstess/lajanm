using CavalierNoir.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CavalierNoir.Web.Controllers;

/// <summary>Classement ELO interne du club.</summary>
[Route("Classement")]
public sealed class ClassementController(DashboardService dashboard) : Controller
{
    [HttpGet("")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Classement du club";
        ViewData["MetaDescription"] =
            "Classement ELO interne des membres du club d'échecs Cavalier Noir.";

        return View(await dashboard.GetClubRankingAsync(page, 25, ct));
    }
}
