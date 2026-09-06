using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>
/// Gestion des comptes : recherche, activation, affectation des rôles.
/// L'attribution des rôles d'administration est réservée au super-administrateur.
/// </summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Administration.ManageUsers)]
public sealed class UtilisateursController(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext context,
    IDateTimeProvider clock,
    ILogger<UtilisateursController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, string? role, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Utilisateurs";
        ViewData["Recherche"] = q;
        ViewData["Role"] = role;
        ViewData["Roles"] = Roles.All.Select(r => r.Name).ToList();

        var query = context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u => u.FirstName.Contains(term)
                                     || u.LastName.Contains(term)
                                     || (u.Email != null && u.Email.Contains(term))
                                     || (u.Pseudonym != null && u.Pseudonym.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => context.UserRoles
                .Any(ur => ur.UserId == u.Id
                           && context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }

        var ordered = query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
        return View(await PagedList<ApplicationUser>.CreateAsync(ordered, page, 25, ct));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return NotFound();
        }

        ViewData["Title"] = user.FullName;
        ViewData["Roles"] = await userManager.GetRolesAsync(user);
        ViewData["TousRoles"] = Roles.All.Select(r => r.Name).ToList();
        ViewData["Connexions"] = await context.LoginHistory
            .AsNoTracking()
            .Where(h => h.UserId == id)
            .OrderByDescending(h => h.OccurredAt)
            .Take(10)
            .ToListAsync(ct);
        ViewData["Adhesions"] = await context.Memberships
            .AsNoTracking()
            .Include(m => m.MembershipType)
            .Where(m => m.UserId == id)
            .OrderByDescending(m => m.Period.End)
            .ToListAsync(ct);

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Administration.ManageRoles)]
    public async Task<IActionResult> ChangerRole(int id, string role, bool accorder, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        // Le rôle de super-administrateur ne s'accorde qu'entre super-administrateurs.
        if (role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
        {
            TempData["Erreur"] = "Seul un super-administrateur peut accorder ce rôle.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = accorder
            ? await userManager.AddToRoleAsync(user, role)
            : await userManager.RemoveFromRoleAsync(user, role);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? $"Rôle « {role} » {(accorder ? "accordé" : "retiré")}."
            : string.Join(", ", result.Errors.Select(e => e.Description));

        logger.LogInformation(
            "Rôle {Role} {Action} pour l'utilisateur {UserId}.",
            role,
            accorder ? "accordé" : "retiré",
            id);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangerActivation(int id, CancellationToken ct)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        await context.SaveChangesAsync(ct);

        TempData["Succes"] = user.IsActive ? "Compte réactivé." : "Compte désactivé.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Déverrouille un compte bloqué après des échecs de connexion.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deverrouiller(int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        TempData["Succes"] = "Compte déverrouillé.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Ajustement manuel du classement, avec justification obligatoire.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjusterElo(int id, int nouvelElo, string motif, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            TempData["Erreur"] = "Un motif est obligatoire pour ajuster un classement.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return NotFound();
        }

        var old = user.Elo;
        user.Elo = Math.Clamp(nouvelElo, 200, 3000);

        context.EloHistory.Add(new Domain.Tournaments.EloHistory
        {
            UserId = id,
            OldElo = old,
            NewElo = user.Elo,
            KFactor = 0,
            ExpectedScore = 0m,
            ActualScore = 0m,
            RecordedAt = clock.UtcNow,
            Reason = motif
        });

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = $"Classement ajusté de {old} à {user.Elo}.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
