using CavalierNoir.Domain.Blog;
using CavalierNoir.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Rubriques du blog.</summary>
public sealed class BlogCategoryConfiguration : IEntityTypeConfiguration<BlogCategory>
{
    public void Configure(EntityTypeBuilder<BlogCategory> builder)
    {
        builder.ToTable("BlogRubriques");
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Color).HasMaxLength(10);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

/// <summary>Étiquettes.</summary>
public sealed class BlogTagConfiguration : IEntityTypeConfiguration<BlogTag>
{
    public void Configure(EntityTypeBuilder<BlogTag> builder)
    {
        builder.ToTable("BlogEtiquettes");
        builder.Property(t => t.Name).HasMaxLength(60).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(80).IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}

/// <summary>Articles.</summary>
public sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogArticles");
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Summary).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Content).IsRequired();
        builder.Property(p => p.FeaturedImageUrl).HasMaxLength(300);
        builder.Property(p => p.FeaturedImageAlt).HasMaxLength(200);
        builder.Property(p => p.ReviewComment).HasMaxLength(1000);
        builder.Property(p => p.MetaTitle).HasMaxLength(200);
        builder.Property(p => p.MetaDescription).HasMaxLength(320);

        builder.Ignore(p => p.ReadingTimeMinutes);
        builder.Ignore(p => p.ApprovedCommentCount);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.Status, p.PublishedAt });

        builder.HasOne(p => p.Author)
            .WithMany(u => u.BlogPosts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Jonction article ↔ étiquette.</summary>
public sealed class BlogPostTagConfiguration : IEntityTypeConfiguration<BlogPostTag>
{
    public void Configure(EntityTypeBuilder<BlogPostTag> builder)
    {
        builder.ToTable("BlogArticleEtiquettes");
        builder.HasKey(pt => new { pt.BlogPostId, pt.BlogTagId });

        builder.HasOne(pt => pt.BlogPost)
            .WithMany(p => p.PostTags)
            .HasForeignKey(pt => pt.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.BlogTag)
            .WithMany(t => t.PostTags)
            .HasForeignKey(pt => pt.BlogTagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Commentaires.</summary>
public sealed class BlogCommentConfiguration : IEntityTypeConfiguration<BlogComment>
{
    public void Configure(EntityTypeBuilder<BlogComment> builder)
    {
        builder.ToTable("BlogCommentaires");
        builder.Property(c => c.Content).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(45);

        builder.HasIndex(c => new { c.BlogPostId, c.Status });

        builder.HasOne(c => c.BlogPost)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Signalements de commentaires.</summary>
public sealed class CommentReportConfiguration : IEntityTypeConfiguration<CommentReport>
{
    public void Configure(EntityTypeBuilder<CommentReport> builder)
    {
        builder.ToTable("BlogSignalements");
        builder.Property(r => r.Details).HasMaxLength(1000);

        builder.HasIndex(r => new { r.BlogCommentId, r.ReporterId }).IsUnique();

        builder.HasOne(r => r.BlogComment)
            .WithMany(c => c.Reports)
            .HasForeignKey(r => r.BlogCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Albums de la galerie.</summary>
public sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("Albums");
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Slug).HasMaxLength(120).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000);

        builder.Ignore(a => a.ItemCount);
        builder.HasIndex(a => a.Slug).IsUnique();

        builder.HasOne(a => a.ClubEvent)
            .WithMany()
            .HasForeignKey(a => a.ClubEventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.CoverMedia)
            .WithMany()
            .HasForeignKey(a => a.CoverMediaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Fichiers médias.</summary>
public sealed class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("Medias");
        builder.Property(m => m.FileName).HasMaxLength(260).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(400).IsRequired();
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(400);
        builder.Property(m => m.ContentType).HasMaxLength(120);
        builder.Property(m => m.AltText).HasMaxLength(300);
        builder.Property(m => m.Caption).HasMaxLength(500);
        builder.Property(m => m.Checksum).HasMaxLength(64);

        builder.Ignore(m => m.SizeLabel);
        builder.HasIndex(m => m.Checksum);

        builder.HasOne(m => m.Album)
            .WithMany(a => a.Items)
            .HasForeignKey(m => m.AlbumId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
