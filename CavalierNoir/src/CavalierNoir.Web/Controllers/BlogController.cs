using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Web.Controllers;

/// <summary>Blog public : liste, article, commentaires et signalements.</summary>
[Route("Blog")]
public sealed class BlogController(
    BlogService blog,
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IOptions<SiteOptions> siteOptions) : Controller
{
    private readonly SiteOptions _site = siteOptions.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? rubrique,
        string? etiquette,
        string? q,
        int page = 1,
        CancellationToken ct = default)
    {
        var filter = new BlogFilter
        {
            CategorySlug = rubrique,
            TagSlug = etiquette,
            Search = q,
            Page = page,
            PageSize = 9
        };

        var model = new BlogIndexViewModel
        {
            Posts = await blog.SearchAsync(filter, ct),
            Categories = await context.BlogCategories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(ct),
            Tags = await context.BlogTags
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Take(20)
                .ToListAsync(ct),
            CurrentCategory = rubrique,
            CurrentTag = etiquette,
            Search = q
        };

        return View(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var post = await blog.GetBySlugAsync(slug, ct: ct);
        if (post is null)
        {
            return NotFound();
        }

        await blog.IncrementViewsAsync(post.Id, ct);

        var related = await blog.SearchAsync(
            new BlogFilter
            {
                CategorySlug = post.Category?.Slug,
                PageSize = 4
            },
            ct);

        ViewData["Title"] = post.MetaTitle ?? post.Title;
        ViewData["MetaDescription"] = post.MetaDescription ?? post.Summary;
        ViewData["OgImage"] = post.FeaturedImageUrl;

        return View(new BlogDetailsViewModel
        {
            Post = post,
            Comments = await blog.GetCommentsAsync(post.Id, ct),
            Related = related.Items.Where(p => p.Id != post.Id).Take(3).ToList(),
            Form = new CommentFormViewModel { PostId = post.Id }
        });
    }

    [HttpPost("Commenter")]
    [Authorize(Policy = "EspaceMembre")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Commenter(CommentFormViewModel form, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var slug = await context.BlogPosts
            .Where(p => p.Id == form.PostId)
            .Select(p => p.Slug)
            .FirstOrDefaultAsync(ct);

        if (slug is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Erreur"] = "Votre commentaire n'a pas pu être enregistré : "
                                 + string.Join(" ", ModelState.Values
                                     .SelectMany(v => v.Errors)
                                     .Select(e => e.ErrorMessage));

            return RedirectToAction(nameof(Details), new { slug });
        }

        var result = await blog.AddCommentAsync(
            form.PostId,
            userId,
            form.Content,
            form.ParentId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            _site.AutoApproveComments,
            ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? _site.AutoApproveComments
                ? "Votre commentaire est publié."
                : "Votre commentaire a été transmis à la modération."
            : result.Error;

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("Signaler/{commentaireId:int}")]
    [Authorize(Policy = "EspaceMembre")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("formulaire")]
    public async Task<IActionResult> Signaler(
        int commentaireId,
        ReportReason motif,
        string? details,
        CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Challenge();
        }

        var result = await blog.ReportCommentAsync(commentaireId, userId, motif, details, ct);

        TempData[result.Succeeded ? "Succes" : "Erreur"] = result.Succeeded
            ? "Signalement transmis à l'équipe de modération. Merci."
            : result.Error;

        var slug = await context.BlogComments
            .Where(c => c.Id == commentaireId)
            .Select(c => c.BlogPost.Slug)
            .FirstOrDefaultAsync(ct);

        return slug is null
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Details), new { slug });
    }

    /// <summary>Flux RSS des vingt derniers articles publiés.</summary>
    [HttpGet("/blog/rss")]
    [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Rss(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var posts = await blog.SearchAsync(new BlogFilter { PageSize = 20 }, ct);

        var xml = new System.Text.StringBuilder();
        xml.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        xml.AppendLine("""<rss version="2.0">""");
        xml.AppendLine("  <channel>");
        xml.AppendLine($"    <title>{Escape(_site.Name)} — Actualités</title>");
        xml.AppendLine($"    <link>{baseUrl}/Blog</link>");
        xml.AppendLine($"    <description>{Escape(_site.Tagline)}</description>");
        xml.AppendLine("    <language>fr-FR</language>");

        foreach (var post in posts.Items)
        {
            xml.AppendLine("    <item>");
            xml.AppendLine($"      <title>{Escape(post.Title)}</title>");
            xml.AppendLine($"      <link>{baseUrl}/Blog/{post.Slug}</link>");
            xml.AppendLine($"      <guid>{baseUrl}/Blog/{post.Slug}</guid>");
            xml.AppendLine($"      <description>{Escape(post.Summary)}</description>");

            if (post.PublishedAt is { } published)
            {
                xml.AppendLine($"      <pubDate>{published:R}</pubDate>");
            }

            xml.AppendLine("    </item>");
        }

        xml.AppendLine("  </channel>");
        xml.AppendLine("</rss>");

        return Content(xml.ToString(), "application/rss+xml");
    }

    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? value;
}
