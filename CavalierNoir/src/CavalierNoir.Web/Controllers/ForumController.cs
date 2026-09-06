using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CavalierNoir.Web.Controllers;

/// <summary>Forums de discussion, réservés aux membres à jour de cotisation.</summary>
[Route("Forum")]
[Authorize(Policy = "EspaceMembre")]
public sealed class ForumController(ForumService forum, ICurrentUser currentUser) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await forum.GetCategoriesAsync(true, ct));

    [HttpGet("Rubrique/{slug}")]
    public async Task<IActionResult> Rubrique(string slug, int page = 1, CancellationToken ct = default)
    {
        var category = await forum.GetCategoryBySlugAsync(slug, ct);
        if (category is null)
        {
            return NotFound();
        }

        ViewData["Rubrique"] = category;
        return View(await forum.GetTopicsAsync(category.Id, page, 20, ct));
    }

    [HttpGet("Sujet/{id:int}")]
    public async Task<IActionResult> Sujet(int id, CancellationToken ct)
    {
        var topic = await forum.GetTopicAsync(id, ct);
        if (topic is null)
        {
            return NotFound();
        }

        await forum.IncrementTopicViewsAsync(id, ct);

        return View(new ForumTopicViewModel
        {
            Topic = topic,
            Posts = await forum.GetPostsAsync(id, ct),
            Reply = new ForumReplyViewModel { TopicId = id },
            CanModerate = User.IsInRole(Roles.ResponsableCommunication)
                          || User.IsInRole(Roles.President)
                          || User.IsInRole(Roles.SuperAdmin)
        });
    }

    [HttpGet("Nouveau/{categorieId:int}")]
    public async Task<IActionResult> Nouveau(int categorieId, CancellationToken ct)
    {
        var categories = await forum.GetCategoriesAsync(true, ct);
        var category = categories.FirstOrDefault(c => c.Id == categorieId);

        if (category is null)
        {
            return NotFound();
        }

        return View(new NewTopicViewModel
        {
            CategoryId = category.Id,
            CategoryName = category.Name
        });
    }

    [HttpPost("Nouveau")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Nouveau(NewTopicViewModel model, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await forum.CreateTopicAsync(
            model.CategoryId,
            userId,
            model.Title,
            model.Content,
            model.Fen,
            ct);

        if (result.Failed)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Création impossible.");
            return View(model);
        }

        return RedirectToAction(nameof(Sujet), new { id = result.Value });
    }

    [HttpPost("Repondre")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Repondre(ForumReplyViewModel model, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["Erreur"] = "Votre message n'a pas pu être publié : vérifiez son contenu.";
            return RedirectToAction(nameof(Sujet), new { id = model.TopicId });
        }

        var result = await forum.ReplyAsync(model.TopicId, userId, model.Content, model.Fen, ct);

        if (result.Failed)
        {
            TempData["Erreur"] = result.Error;
        }

        return RedirectToAction(nameof(Sujet), new { id = model.TopicId });
    }

    [HttpPost("Moderer/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "content.moderate")]
    public async Task<IActionResult> Moderer(int id, string action, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await forum.ModerateTopicAsync(id, action, userId, ct);
        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Modération appliquée."
            : result.Error;

        return action == "supprimer"
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Sujet), new { id });
    }
}
