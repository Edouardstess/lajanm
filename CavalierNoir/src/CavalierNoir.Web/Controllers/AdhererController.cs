using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>Parcours d'adhésion : offres, formulaire de candidature et suivi.</summary>
[Route("Adherer")]
public sealed class AdhererController(
    MembershipService memberships,
    IApplicationDbContext context,
    ICurrentUser currentUser) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Rejoindre le Cavalier Noir";
        ViewData["MetaDescription"] =
            "Adhérez au club d'échecs Cavalier Noir : formules, tarifs et démarche d'inscription.";

        return View(await memberships.GetActiveTypesAsync(ct));
    }

    [HttpGet("Demande")]
    [Authorize]
    public async Task<IActionResult> Demande(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var pending = await context.MembershipApplications
            .AsNoTracking()
            .Where(a => a.UserId == userId
                        && (a.Status == ApplicationStatus.Soumise
                            || a.Status == ApplicationStatus.EnInstruction
                            || a.Status == ApplicationStatus.ComplementsDemandes))
            .FirstOrDefaultAsync(ct);

        if (pending is not null)
        {
            TempData["Succes"] = "Votre demande est déjà en cours d'instruction par le secrétariat.";
            return RedirectToAction("Index", "Adhesion", new { area = "Membre" });
        }

        return View(new MembershipRequestViewModel
        {
            Types = await memberships.GetActiveTypesAsync(ct)
        });
    }

    [HttpPost("Demande")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Demande(MembershipRequestViewModel model, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        model.Types = await memberships.GetActiveTypesAsync(ct);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await memberships.SubmitApplicationAsync(
            new MembershipApplicationRequest
            {
                UserId = userId,
                MembershipTypeId = model.MembershipTypeId,
                DeclaredElo = model.DeclaredElo,
                Motivation = model.Motivation,
                ParentalConsent = model.ParentalConsent,
                ParentContact = model.ParentContact
            },
            ct);

        if (result.Failed)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Demande impossible.");
            return View(model);
        }

        return RedirectToAction(nameof(Confirmation));
    }

    [HttpGet("Confirmation")]
    [Authorize]
    public IActionResult Confirmation() => View();
}
