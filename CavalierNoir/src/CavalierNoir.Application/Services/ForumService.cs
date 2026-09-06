using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Application.Dtos;
using CavalierNoir.Domain.Communication;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Services;

/// <summary>Forums de discussion réservés aux membres : sujets, réponses, modération.</summary>
public sealed class ForumService(
    IApplicationDbContext context,
    IDateTimeProvider clock,
    INotificationService notifications)
{
    public Task<List<ForumCategory>> GetCategoriesAsync(bool membersOnly, CancellationToken ct = default) =>
        context.ForumCategories
            .AsNoTracking()
            .Where(c => membersOnly || c.Visibility == Visibility.Public)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);

    public Task<ForumCategory?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default) =>
        context.ForumCategories.FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<PagedList<ForumTopicCard>> GetTopicsAsync(
        int categoryId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = context.ForumTopics
            .AsNoTracking()
            .Where(t => t.ForumCategoryId == categoryId && !t.IsDeleted)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
            .Select(t => new ForumTopicCard(
                t.Id,
                t.Title,
                t.Slug,
                t.Author.Pseudonym ?? (t.Author.FirstName + " " + t.Author.LastName),
                t.CreatedAt,
                t.LastPostAt,
                t.ReplyCount,
                t.ViewCount,
                t.IsPinned,
                t.Status));

        return await PagedList<ForumTopicCard>.CreateAsync(query, page, pageSize, ct);
    }

    public Task<ForumTopic?> GetTopicAsync(int id, CancellationToken ct = default) =>
        context.ForumTopics
            .Include(t => t.Author)
            .Include(t => t.ForumCategory)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public Task<List<ForumPost>> GetPostsAsync(int topicId, CancellationToken ct = default) =>
        context.ForumPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.ForumTopicId == topicId && !p.IsDeleted && p.Status != CommentStatus.Masque)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Ouvre un sujet et publie son premier message.</summary>
    public async Task<Result<int>> CreateTopicAsync(
        int categoryId,
        int authorId,
        string title,
        string content,
        string? fen,
        CancellationToken ct = default)
    {
        var category = await context.ForumCategories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category is null)
        {
            return Result<int>.Failure("Sous-forum introuvable.", "NOTFOUND_010");
        }

        if (category.IsLocked)
        {
            return Result<int>.Failure("Ce sous-forum est verrouillé.");
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
        {
            return Result<int>.Failure("Le titre doit comporter au moins 5 caractères.", "VALIDATION_002");
        }

        if (!string.IsNullOrWhiteSpace(fen) && !Fen.IsValid(fen))
        {
            return Result<int>.Failure("La position FEN jointe est invalide.", "VALIDATION_003");
        }

        var now = clock.UtcNow;
        var baseSlug = Slug.From(title, "sujet");
        var slug = baseSlug;
        var suffix = 2;

        while (await context.ForumTopics.AnyAsync(t => t.Slug == slug, ct))
        {
            slug = Slug.WithSuffix(baseSlug, suffix++);
        }

        var topic = new ForumTopic
        {
            ForumCategoryId = categoryId,
            AuthorId = authorId,
            Title = title.Trim(),
            Slug = slug,
            CreatedAt = now,
            CreatedById = authorId,
            LastPostAt = now,
            LastPostAuthorId = authorId
        };

        context.ForumTopics.Add(topic);
        await context.SaveChangesAsync(ct);

        context.ForumPosts.Add(new ForumPost
        {
            ForumTopicId = topic.Id,
            AuthorId = authorId,
            Content = content.Trim(),
            Fen = string.IsNullOrWhiteSpace(fen) ? null : fen.Trim(),
            CreatedAt = now
        });

        category.TopicCount++;
        category.PostCount++;
        category.LastActivityAt = now;

        await context.SaveChangesAsync(ct);
        return Result<int>.Success(topic.Id);
    }

    /// <summary>Répond à un sujet et notifie l'auteur du fil.</summary>
    public async Task<Result<int>> ReplyAsync(
        int topicId,
        int authorId,
        string content,
        string? fen,
        CancellationToken ct = default)
    {
        var topic = await context.ForumTopics
            .Include(t => t.ForumCategory)
            .FirstOrDefaultAsync(t => t.Id == topicId && !t.IsDeleted, ct);

        if (topic is null)
        {
            return Result<int>.Failure("Sujet introuvable.", "NOTFOUND_011");
        }

        if (!topic.CanReply)
        {
            return Result<int>.Failure("Ce sujet est fermé.");
        }

        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length < 2)
        {
            return Result<int>.Failure("Le message est vide.", "VALIDATION_004");
        }

        if (!string.IsNullOrWhiteSpace(fen) && !Fen.IsValid(fen))
        {
            return Result<int>.Failure("La position FEN jointe est invalide.", "VALIDATION_003");
        }

        var now = clock.UtcNow;
        var post = new ForumPost
        {
            ForumTopicId = topicId,
            AuthorId = authorId,
            Content = content.Trim(),
            Fen = string.IsNullOrWhiteSpace(fen) ? null : fen.Trim(),
            CreatedAt = now
        };

        context.ForumPosts.Add(post);

        topic.ReplyCount++;
        topic.LastPostAt = now;
        topic.LastPostAuthorId = authorId;
        topic.ForumCategory.PostCount++;
        topic.ForumCategory.LastActivityAt = now;

        await context.SaveChangesAsync(ct);

        if (topic.AuthorId != authorId)
        {
            await notifications.NotifyAsync(
                topic.AuthorId,
                "Nouvelle réponse à votre sujet",
                $"Un membre a répondu à « {topic.Title} ».",
                $"/Forum/Sujet/{topic.Id}#message-{post.Id}",
                "chat-left-text",
                3,
                NotificationChannel.InApp,
                ct);
        }

        return Result<int>.Success(post.Id);
    }

    public async Task IncrementTopicViewsAsync(int topicId, CancellationToken ct = default)
    {
        await context.ForumTopics
            .Where(t => t.Id == topicId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ViewCount, t => t.ViewCount + 1), ct);
    }

    /// <summary>Épingle, ferme ou supprime logiquement un sujet.</summary>
    public async Task<Result> ModerateTopicAsync(
        int topicId,
        string action,
        int moderatorId,
        CancellationToken ct = default)
    {
        var topic = await context.ForumTopics.FirstOrDefaultAsync(t => t.Id == topicId, ct);
        if (topic is null)
        {
            return Result.Failure("Sujet introuvable.", "NOTFOUND_011");
        }

        var now = clock.UtcNow;
        switch (action)
        {
            case "epingler":
                topic.IsPinned = !topic.IsPinned;
                break;
            case "fermer":
                topic.Status = topic.Status == TopicStatus.Ferme ? TopicStatus.Ouvert : TopicStatus.Ferme;
                break;
            case "archiver":
                topic.Status = TopicStatus.Archive;
                break;
            case "supprimer":
                topic.IsDeleted = true;
                topic.DeletedAt = now;
                topic.DeletedById = moderatorId;
                break;
            default:
                return Result.Failure("Action de modération inconnue.");
        }

        topic.UpdatedAt = now;
        topic.UpdatedById = moderatorId;
        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
