using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Paramètres du site modifiables sans redéploiement.</summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Administration.ManageSettings)]
public sealed class ParametresController(
    SettingsService settings,
    ICurrentUser currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Paramètres";
        return View(await settings.GetAllAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(
        [FromForm] Dictionary<string, string?> valeurs,
        CancellationToken ct)
    {
        if (valeurs.Count == 0)
        {
            TempData["Erreur"] = "Aucune modification à enregistrer.";
            return RedirectToAction(nameof(Index));
        }

        await settings.SetManyAsync(valeurs, currentUser.UserId, ct);

        TempData["Succes"] = $"{valeurs.Count} paramètre(s) enregistré(s).";
        return RedirectToAction(nameof(Index));
    }
}
