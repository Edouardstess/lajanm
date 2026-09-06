using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CavalierNoir.Web.Areas.Membre.Controllers;

/// <summary>Tableau de bord de l'espace membre.</summary>
[Area("Membre")]
[Authorize(Policy = "EspaceMembre")]
public sealed class TableauDeBordController(
    DashboardService dashboard,
    ICurrentUser currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        ViewData["Title"] = "Mon tableau de bord";
        return View(await dashboard.GetMemberDashboardAsync(userId, ct));
    }
}
