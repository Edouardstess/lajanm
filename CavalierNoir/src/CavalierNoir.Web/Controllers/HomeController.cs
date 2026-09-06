using System.Diagnostics;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Application.Services;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;
using CavalierNoir.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Web.Controllers;

/// <summary>Pages d'entrée du site public : accueil et gestion des erreurs.</summary>
public sealed class HomeController(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    ExerciseService exercises,
    BlogService blog,
    TournamentService tournaments,
    EventService events) : Controller
{
    /// <summary>
    /// Accueil : exercice du jour, derniers articles, prochains rendez-vous et
    /// chiffres clés du club.
    /// </summary>
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var today = clock.Today;
        var daily = await exercises.GetOrCreateDailyAsync(today, ct);

        ExerciseCard? dailyCard = null;
        if (daily?.Exercise is { } exercise)
        {
            dailyCard = new ExerciseCard(
                exercise.Id,
                exercise.Title,
                exercise.Fen,
                exercise.Theme,
                exercise.Difficulty,
                exercise.WhiteToMove,
                exercise.SuccessRate,
                exercise.AttemptCount,
                exercise.IsPublic,
                exercise.IsPublished,
                false);
        }

        var posts = await blog.SearchAsync(new BlogFilter { Page = 1, PageSize = 3 }, ct);

        var model = new HomeViewModel
        {
            DailyExercise = dailyCard,
            LatestPosts = posts.Items,
            UpcomingTournaments = await tournaments.GetUpcomingAsync(3, ct),
            UpcomingEvents = await events.GetUpcomingAsync(4, ct),
            Partners = await context.Partners
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Take(8)
                .ToListAsync(ct),
            MemberCount = await context.Memberships
                .CountAsync(m => m.Status == MembershipStatus.Active, ct),
            ExerciseCount = await context.Exercises
                .CountAsync(e => e.IsPublished && !e.IsDeleted, ct),
            SolvedCount = await context.ExerciseAttempts.CountAsync(a => a.IsCorrect, ct),
            TournamentCount = await context.Tournaments
                .CountAsync(t => !t.IsDeleted && t.Status == TournamentStatus.Termine, ct)
        };

        return View(model);
    }

    /// <summary>Page d'erreur, avec l'identifiant de corrélation à communiquer au support.</summary>
    [Route("/Erreur")]
    [Route("/Erreur/{code:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Erreur(int? code)
    {
        var status = code ?? 500;

        var (title, message) = status switch
        {
            400 => ("Requête invalide", "La demande envoyée n'a pas pu être interprétée."),
            403 => ("Accès refusé", "Vous n'avez pas les droits nécessaires pour consulter cette page."),
            404 => ("Page introuvable", "Cette page n'existe pas ou a été déplacée."),
            422 => ("Opération impossible", "Une règle de gestion empêche cette opération."),
            429 => ("Trop de requêtes", "Vous avez effectué trop de demandes. Réessayez dans une minute."),
            _ => ("Une erreur est survenue", "Une erreur inattendue s'est produite. L'incident a été enregistré.")
        };

        Response.StatusCode = status;

        return View(new ErrorViewModel
        {
            StatusCode = status,
            Title = title,
            Message = message,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    /// <summary>Fichier robots.txt généré : le back-office reste hors des moteurs.</summary>
    [Route("/robots.txt")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var content = string.Join('\n',
            "User-agent: *",
            "Disallow: /Admin/",
            "Disallow: /Membre/",
            "Disallow: /Compte/",
            "Disallow: /health",
            string.Empty,
            $"Sitemap: {baseUrl}/sitemap.xml");

        return Content(content, "text/plain");
    }

    /// <summary>Plan du site dynamique : pages statiques, articles, tournois et événements publiés.</summary>
    [Route("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var now = clock.UtcNow;

        var urls = new List<(string Location, DateTime LastModified, string Priority)>
        {
            ($"{baseUrl}/", now, "1.0"),
            ($"{baseUrl}/Blog", now, "0.8"),
            ($"{baseUrl}/Exercices", now, "0.8"),
            ($"{baseUrl}/Tournois", now, "0.8"),
            ($"{baseUrl}/Evenements", now, "0.7"),
            ($"{baseUrl}/Adherer", now, "0.9"),
            ($"{baseUrl}/Contact", now, "0.5")
        };

        var pages = await context.StaticPages
            .Where(p => p.IsPublished)
            .Select(p => new { p.Slug, Modified = p.UpdatedAt ?? p.CreatedAt })
            .ToListAsync(ct);
        urls.AddRange(pages.Select(p => ($"{baseUrl}/Infos/{p.Slug}", p.Modified, "0.6")));

        var posts = await context.BlogPosts
            .Where(p => !p.IsDeleted && p.Status == BlogStatus.Publie && p.PublishedAt != null)
            .OrderByDescending(p => p.PublishedAt)
            .Take(500)
            .Select(p => new { p.Slug, Modified = p.UpdatedAt ?? p.PublishedAt })
            .ToListAsync(ct);
        urls.AddRange(posts.Select(p => ($"{baseUrl}/Blog/{p.Slug}", p.Modified ?? now, "0.7")));

        var competitions = await context.Tournaments
            .Where(t => !t.IsDeleted && t.Status != TournamentStatus.Brouillon)
            .OrderByDescending(t => t.StartDate)
            .Take(200)
            .Select(t => new { t.Slug, Modified = t.UpdatedAt ?? t.CreatedAt })
            .ToListAsync(ct);
        urls.AddRange(competitions.Select(t => ($"{baseUrl}/Tournois/{t.Slug}", t.Modified, "0.6")));

        var xml = new System.Text.StringBuilder();
        xml.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        xml.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var (location, lastModified, priority) in urls)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(location)}</loc>");
            xml.AppendLine($"    <lastmod>{lastModified:yyyy-MM-dd}</lastmod>");
            xml.AppendLine($"    <priority>{priority}</priority>");
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");
        return Content(xml.ToString(), "application/xml");
    }
}
