using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Application.Dtos;

/// <summary>Carte d'article pour les listes du blog.</summary>
public sealed record BlogPostCard(
    int Id,
    string Title,
    string Slug,
    string Summary,
    string? FeaturedImageUrl,
    string AuthorName,
    string? CategoryName,
    string? CategorySlug,
    DateTime? PublishedAt,
    int ViewCount,
    int CommentCount,
    int ReadingTimeMinutes);

/// <summary>Critères de recherche du blog.</summary>
public sealed class BlogFilter
{
    public string? Search { get; init; }

    public string? CategorySlug { get; init; }

    public string? TagSlug { get; init; }

    public int? AuthorId { get; init; }

    public BlogStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 9;
}

/// <summary>Sujet de forum affiché dans la liste d'un sous-forum.</summary>
public sealed record ForumTopicCard(
    int Id,
    string Title,
    string Slug,
    string AuthorName,
    DateTime CreatedAt,
    DateTime? LastPostAt,
    int ReplyCount,
    int ViewCount,
    bool IsPinned,
    TopicStatus Status);
