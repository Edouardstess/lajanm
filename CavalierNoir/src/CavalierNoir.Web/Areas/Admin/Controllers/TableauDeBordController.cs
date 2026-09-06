using CavalierNoir.Application.Services;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Tableau de bord du back-office : indicateurs et files d'attente.</summary>
[Area("Admin")]
[Authorize(Policy = "EspaceAdmin")]
public sealed class TableauDeBordController(
    DashboardService dashboard,
    MembershipService memberships) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Tableau de bord";

        var pending = await memberships.GetApplicationsAsync(
            CavalierNoir.Domain.Enums.ApplicationStatus.Soumise,
            1,
            5,
            ct);

        return View(new AdminDashboardViewModel
        {
            Stats = await dashboard.GetAdminStatsAsync(ct),
            PendingApplications = pending.Items
        });
    }
}
