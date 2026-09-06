using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Areas.Admin.Controllers;

/// <summary>Rédaction et workflow éditorial du blog.</summary>
[Area("Admin")]
[Authorize(Policy = Permissions.Content.Write)]
public sealed class BlogController(
    BlogService blog,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : Controller
{
    public async Task<IActionResult> Index(BlogStatus? statut, int page = 1, CancellationToken ct = default)
    {
        ViewData["Title"] = "Articles";
        ViewData["Statut"] = statut;

        return View(await blog.SearchAsync(
            new BlogFilter { Status = statut ?? BlogStatus.Brouillon, Page = page, PageSize = 20 },
            ct));
    }

    public async Task<IActionResult> Creer(CancellationToken ct) =>
        View("Formulaire", await FillListsAsync(new BlogPostFormViewModel(), ct));

    public async Task<IActionResult> Modifier(int id, CancellationToken ct)
    {
        var post = await context.BlogPosts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post is null)
        {
            return NotFound();
        }

        var model = new BlogPostFormViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Content = post.Content,
            FeaturedImageUrl = post.FeaturedImageUrl,
            FeaturedImageAlt = post.FeaturedImageAlt,
            CategoryId = post.CategoryId,
            TagIds = post.PostTags.Select(t => t.BlogTagId).ToList(),
            AllowComments = post.AllowComments,
            IsFeatured = post.IsFeatured,
            ScheduledFor = post.ScheduledFor,
            MetaDescription = post.MetaDescription,
            Status = post.Status
        };

        return View("Formulaire", await FillListsAsync(model, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enregistrer(BlogPostFormViewModel model, CancellationToken ct)
    {
        if (currentUser.UserId is not { } authorId)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Formulaire", await FillListsAsync(model, ct));
        }

        var post = model.Id > 0
            ? await context.BlogPosts.Include(p => p.PostTags).FirstOrDefaultAsync(p => p.Id == model.Id, ct)
            : new BlogPost { AuthorId = authorId, CreatedAt = clock.UtcNow, CreatedById = authorId };

        if (post is null)
        {
            return NotFound();
        }

        post.Title = model.Title.Trim();
        post.Slug = string.IsNullOrWhiteSpace(model.Slug)
            ? await blog.GenerateUniqueSlugAsync(model.Title, model.Id > 0 ? model.Id : null, ct)
            : model.Slug.Trim();
        post.Summary = model.Summary.Trim();
        post.Content = model.Content;
        post.FeaturedImageUrl = model.FeaturedImageUrl;
        post.FeaturedImageAlt = model.FeaturedImageAlt;
        post.CategoryId = model.CategoryId;
        post.AllowComments = model.AllowComments;
        post.IsFeatured = model.IsFeatured;
        post.ScheduledFor = model.ScheduledFor;
        post.MetaDescription = model.MetaDescription;
        post.UpdatedAt = clock.UtcNow;
        post.UpdatedById = authorId;

        if (model.Id == 0)
        {
            context.BlogPosts.Add(post);
            await context.SaveChangesAsync(ct);
        }

        // Réaligne les étiquettes sur la sélection du formulaire.
        var existing = await context.BlogPostTags
            .Where(t => t.BlogPostId == post.Id)
            .ToListAsync(ct);

        foreach (var link in existing.Where(l => !model.TagIds.Contains(l.BlogTagId)))
        {
            context.BlogPostTags.Remove(link);
        }

        foreach (var tagId in model.TagIds.Where(id => existing.All(l => l.BlogTagId != id)))
        {
            context.BlogPostTags.Add(new BlogPostTag { BlogPostId = post.Id, BlogTagId = tagId });
        }

        await context.SaveChangesAsync(ct);

        TempData["Succes"] = model.Id > 0 ? "Article enregistré." : "Article créé en brouillon.";
        return RedirectToAction(nameof(Modifier), new { id = post.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoumettreRelecture(int id, CancellationToken ct)
    {
        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return NotFound();
        }

        post.SubmitForReview(clock.UtcNow);
        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Article soumis à relecture.";
        return RedirectToAction(nameof(Modifier), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Content.Publish)]
    public async Task<IActionResult> Publier(int id, DateTime? programmePour, CancellationToken ct)
    {
        if (currentUser.UserId is not { } reviewerId)
        {
            return Challenge();
        }

        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return NotFound();
        }

        post.Approve(reviewerId, clock.UtcNow, programmePour);
        await context.SaveChangesAsync(ct);

        TempData["Succes"] = post.Status == BlogStatus.Programme
            ? $"Publication programmée pour le {programmePour:dd/MM/yyyy HH:mm}."
            : "Article publié.";

        return RedirectToAction(nameof(Modifier), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Content.Publish)]
    public async Task<IActionResult> Depublier(int id, CancellationToken ct)
    {
        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null)
        {
            return NotFound();
        }

        post.Unpublish(clock.UtcNow);
        await context.SaveChangesAsync(ct);

        TempData["Succes"] = "Article dépublié et archivé.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<BlogPostFormViewModel> FillListsAsync(BlogPostFormViewModel model, CancellationToken ct)
    {
        var categories = await context.BlogCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        var tags = await context.BlogTags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);

        model.Categories = categories.Select(c => (c.Id, c.Name)).ToList();
        model.Tags = tags.Select(t => (t.Id, t.Name)).ToList();
        return model;
    }
}
