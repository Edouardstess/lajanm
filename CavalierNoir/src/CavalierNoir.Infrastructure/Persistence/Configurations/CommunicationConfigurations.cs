using CavalierNoir.Domain.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Abonnés à la lettre d'information.</summary>
public sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("Abonnes");
        builder.Property(s => s.Email).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(150);
        builder.Property(s => s.ConfirmationToken).HasMaxLength(64);
        builder.Property(s => s.UnsubscribeToken).HasMaxLength(64).IsRequired();
        builder.Property(s => s.SubscriptionIp).HasMaxLength(45);

        builder.HasIndex(s => s.Email).IsUnique();
        builder.HasIndex(s => s.UnsubscribeToken);
        builder.HasIndex(s => s.ConfirmationToken);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Campagnes d'e-mailing.</summary>
public sealed class NewsletterCampaignConfiguration : IEntityTypeConfiguration<NewsletterCampaign>
{
    public void Configure(EntityTypeBuilder<NewsletterCampaign> builder)
    {
        builder.ToTable("Campagnes");
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Subject).HasMaxLength(200).IsRequired();
        builder.Property(c => c.BodyHtml).IsRequired();
        builder.Property(c => c.Audience).HasMaxLength(30).IsRequired();

        builder.Ignore(c => c.OpenRate);
        builder.Ignore(c => c.ClickRate);

        builder.HasIndex(c => c.Status);
    }
}

/// <summary>Journal des courriels expédiés.</summary>
public sealed class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("JournalEmails");
        builder.Property(l => l.To).HasMaxLength(256).IsRequired();
        builder.Property(l => l.Subject).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Template).HasMaxLength(80);
        builder.Property(l => l.ProviderMessageId).HasMaxLength(200);
        builder.Property(l => l.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(l => new { l.Status, l.CreatedAt });
        builder.HasIndex(l => l.UserId);

        builder.HasOne(l => l.Campaign)
            .WithMany()
            .HasForeignKey(l => l.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Sous-forums.</summary>
public sealed class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> builder)
    {
        builder.ToTable("ForumRubriques");
        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Icon).HasMaxLength(50).IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

/// <summary>Sujets de discussion.</summary>
public sealed class ForumTopicConfiguration : IEntityTypeConfiguration<ForumTopic>
{
    public void Configure(EntityTypeBuilder<ForumTopic> builder)
    {
        builder.ToTable("ForumSujets");
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(120).IsRequired();

        builder.Ignore(t => t.CanReply);

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => new { t.ForumCategoryId, t.LastPostAt });

        builder.HasOne(t => t.ForumCategory)
            .WithMany(c => c.Topics)
            .HasForeignKey(t => t.ForumCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Author)
            .WithMany(u => u.ForumTopics)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Messages de forum.</summary>
public sealed class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> builder)
    {
        builder.ToTable("ForumMessages");
        builder.Property(p => p.Content).HasMaxLength(8000).IsRequired();
        builder.Property(p => p.Fen).HasMaxLength(120);

        builder.HasIndex(p => new { p.ForumTopicId, p.CreatedAt });

        builder.HasOne(p => p.ForumTopic)
            .WithMany(t => t.Posts)
            .HasForeignKey(p => p.ForumTopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Author)
            .WithMany(u => u.ForumPosts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Messagerie privée.</summary>
public sealed class PrivateMessageConfiguration : IEntityTypeConfiguration<PrivateMessage>
{
    public void Configure(EntityTypeBuilder<PrivateMessage> builder)
    {
        builder.ToTable("MessagesPrives");
        builder.Property(m => m.Subject).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Content).HasMaxLength(8000).IsRequired();

        builder.Ignore(m => m.IsRead);

        builder.HasIndex(m => new { m.RecipientId, m.ReadAt });
        builder.HasIndex(m => m.SenderId);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Recipient)
            .WithMany()
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Parent)
            .WithMany()
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
