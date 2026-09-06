using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Blog : consultation publique, workflow éditorial (brouillon → relecture →
/// programmé → publié) et modération des commentaires (BR-09).
/// </summary>
public sealed class BlogService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    INotificationService notifications)
{
    public async Task<PagedList<BlogPostCard>> SearchAsync(BlogFilter filter, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var query = context.BlogPosts.AsNoTracking().Where(p => !p.IsDeleted);

        if (filter.Status.HasValue)
        {
            query = query.Where(p => p.Status == filter.Status.Value);
        }
        else
        {
            query = query.Where(p => p.Status == BlogStatus.Publie && p.PublishedAt != null && p.PublishedAt <= now);
        }

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(p => p.Category != null && p.Category.Slug == filter.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(filter.TagSlug))
        {
            query = query.Where(p => p.PostTags.Any(pt => pt.BlogTag.Slug == filter.TagSlug));
        }

        if (filter.AuthorId.HasValue)
        {
            query = query.Where(p => p.AuthorId == filter.AuthorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(p => p.Title.Contains(term) || p.Summary.Contains(term));
        }

        var projected = query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Select(p => new BlogPostCard(
                p.Id,
                p.Title,
                p.Slug,
                p.Summary,
                p.FeaturedImageUrl,
                p.Author.Pseudonym ?? (p.Author.FirstName + " " + p.Author.LastName),
                p.Category != null ? p.Category.Name : null,
                p.Category != null ? p.Category.Slug : null,
                p.PublishedAt,
                p.ViewCount,
                p.Comments.Count(c => c.Status == CommentStatus.Approuve),
                // Estimation du temps de lecture : ~1 200 caractères par minute.
                (p.Content.Length / 1200) + 1));

        return await PagedList<BlogPostCard>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<BlogPost?> GetBySlugAsync(string slug, bool includeUnpublished = false, CancellationToken ct = default)
    {
        var query = context.BlogPosts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.BlogTag)
            .Where(p => p.Slug == slug && !p.IsDeleted);

        if (!includeUnpublished)
        {
            var now = clock.UtcNow;
            query = query.Where(p => p.Status == BlogStatus.Publie && p.PublishedAt != null && p.PublishedAt <= now);
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    /// <summary>Commentaires approuvés d'un article, arborescence à deux niveaux.</summary>
    public Task<List<BlogComment>> GetCommentsAsync(int postId, CancellationToken ct = default) =>
        context.BlogComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.BlogPostId == postId && c.Status == CommentStatus.Approuve)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Incrémente le compteur de vues sans charger l'agrégat complet.</summary>
    public async Task IncrementViewsAsync(int postId, CancellationToken ct = default)
    {
        await context.BlogPosts
            .Where(p => p.Id == postId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);
    }

    /// <summary>
    /// Dépose un commentaire. La modération a priori est activée pour les membres
    /// dont c'est le premier commentaire.
    /// </summary>
    public async Task<Result<int>> AddCommentAsync(
        int postId,
        int authorId,
        string content,
        int? parentId,
        string? ipAddress,
        bool autoApprove,
        CancellationToken ct = default)
    {
        var post = await context.BlogPosts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct);
        if (post is null)
        {
            return Result<int>.Failure("Article introuvable.", "NOTFOUND_008");
        }

        if (!post.AllowComments)
        {
            return Result<int>.Failure("Les commentaires sont fermés sur cet article.");
        }

        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length < 3)
        {
            return Result<int>.Failure("Le commentaire est trop court.", "VALIDATION_001");
        }

        var comment = new BlogComment
        {
            BlogPostId = postId,
            AuthorId = authorId,
            Content = content.Trim(),
            ParentId = parentId,
            IpAddress = ipAddress,
            Status = autoApprove ? CommentStatus.Approuve : CommentStatus.EnAttente,
            CreatedAt = clock.UtcNow
        };

        context.BlogComments.Add(comment);
        await context.SaveChangesAsync(ct);

        if (!autoApprove)
        {
            await notifications.NotifyRoleAsync(
                Roles.ResponsableCommunication,
                "Commentaire en attente de modération",
                $"Un commentaire attend votre validation sur « {post.Title} ».",
                $"/Admin/Moderation",
                "chat-dots",
                ct);
        }

        if (parentId is { } parent)
        {
            var parentAuthorId = await context.BlogComments
                .Where(c => c.Id == parent)
                .Select(c => (int?)c.AuthorId)
                .FirstOrDefaultAsync(ct);

            if (parentAuthorId is { } notifyId && notifyId != authorId)
            {
                await notifications.NotifyAsync(
                    notifyId,
                    "Réponse à votre commentaire",
                    $"Un membre a répondu à votre commentaire sur « {post.Title} ».",
                    $"/Blog/{post.Slug}#commentaire-{comment.Id}",
                    "reply",
                    3,
                    NotificationChannel.InApp,
                    ct);
            }
        }

        return Result<int>.Success(comment.Id);
    }

    /// <summary>Signale un commentaire ; BR-09 le masque au troisième signalement.</summary>
    public async Task<Result> ReportCommentAsync(
        int commentId,
        int reporterId,
        ReportReason reason,
        string? details,
        CancellationToken ct = default)
    {
        var comment = await context.BlogComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null)
        {
            return Result.Failure("Commentaire introuvable.", "NOTFOUND_009");
        }

        var alreadyReported = await context.CommentReports
            .AnyAsync(r => r.BlogCommentId == commentId && r.ReporterId == reporterId, ct);

        if (alreadyReported)
        {
            return Result.Failure("Vous avez déjà signalé ce commentaire.");
        }

        var now = clock.UtcNow;
        context.CommentReports.Add(new CommentReport
        {
            BlogCommentId = commentId,
            ReporterId = reporterId,
            Reason = reason,
            Details = details,
            ReportedAt = now
        });

        var hidden = comment.Report(now);
        await context.SaveChangesAsync(ct);

        if (hidden)
        {
            await notifications.NotifyRoleAsync(
                Roles.ResponsableCommunication,
                "Commentaire masqué automatiquement",
                "Un commentaire a atteint trois signalements et a été masqué (BR-09).",
                "/Admin/Moderation",
                "shield-exclamation",
                ct);
        }

        return Result.Success();
    }

    public async Task<Result> ModerateCommentAsync(
        int commentId,
        bool approve,
        int moderatorId,
        CancellationToken ct = default)
    {
        var comment = await context.BlogComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null)
        {
            return Result.Failure("Commentaire introuvable.", "NOTFOUND_009");
        }

        var now = clock.UtcNow;
        if (approve)
        {
            comment.Approve(moderatorId, now);
        }
        else
        {
            comment.Hide(moderatorId, now);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Génère un identifiant d'URL unique pour un article.</summary>
    public async Task<string> GenerateUniqueSlugAsync(string title, int? excludePostId = null, CancellationToken ct = default)
    {
        var baseSlug = Slug.From(title, "article");
        var candidate = baseSlug;
        var suffix = 2;

        while (await context.BlogPosts.AnyAsync(
                   p => p.Slug == candidate && (excludePostId == null || p.Id != excludePostId), ct))
        {
            candidate = Slug.WithSuffix(baseSlug, suffix++);
        }

        return candidate;
    }

    /// <summary>Publie les articles dont la date de programmation est atteinte.</summary>
    public async Task<int> PublishScheduledAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var due = await context.BlogPosts
            .Where(p => p.Status == BlogStatus.Programme && p.ScheduledFor != null && p.ScheduledFor <= now)
            .ToListAsync(ct);

        foreach (var post in due)
        {
            post.PublishedAt = post.ScheduledFor;
            post.Publish(now);
        }

        if (due.Count > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        return due.Count;
    }
}
