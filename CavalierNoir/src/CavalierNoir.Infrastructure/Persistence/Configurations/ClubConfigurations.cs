using CavalierNoir.Domain.Club;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Bureau exécutif et mandats.</summary>
public sealed class CommitteeConfiguration : IEntityTypeConfiguration<Committee>
{
    public void Configure(EntityTypeBuilder<Committee> builder)
    {
        builder.ToTable("Bureaux");
        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);

        builder.OwnsOne(c => c.Mandate, mandate =>
        {
            mandate.Property(m => m.Start).HasColumnName("MandatDebut");
            mandate.Property(m => m.End).HasColumnName("MandatFin");
        });
        builder.Navigation(c => c.Mandate).IsRequired();

        builder.HasIndex(c => c.IsCurrent);
    }
}

/// <summary>Affectation d'un membre à un poste du bureau.</summary>
public sealed class CommitteeMemberConfiguration : IEntityTypeConfiguration<CommitteeMember>
{
    public void Configure(EntityTypeBuilder<CommitteeMember> builder)
    {
        builder.ToTable("BureauMembres");
        builder.Property(m => m.Biography).HasMaxLength(2000);
        builder.Property(m => m.PhotoUrl).HasMaxLength(300);

        builder.HasIndex(m => new { m.CommitteeId, m.UserId, m.Position }).IsUnique();

        builder.HasOne(m => m.Committee)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.CommitteeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Réunions statutaires et procès-verbaux.</summary>
public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Reunions");
        builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Location).HasMaxLength(200);
        builder.Property(m => m.Agenda).HasMaxLength(4000);
        builder.Property(m => m.Minutes);

        builder.HasIndex(m => m.ScheduledAt);

        builder.HasOne(m => m.Document)
            .WithMany()
            .HasForeignKey(m => m.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Documents officiels versionnés.</summary>
public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000);
        builder.Property(d => d.FileUrl).HasMaxLength(400).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(120);
        builder.Property(d => d.ArchiveReason).HasMaxLength(300);

        builder.Ignore(d => d.SizeLabel);

        builder.HasIndex(d => new { d.Category, d.Visibility });
        builder.HasIndex(d => d.IsArchived);

        builder.HasOne(d => d.PreviousVersion)
            .WithMany()
            .HasForeignKey(d => d.PreviousVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Partenaires et sponsors.</summary>
public sealed class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partenaires");
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.LogoUrl).HasMaxLength(300);
        builder.Property(p => p.Website).HasMaxLength(300);
        builder.Property(p => p.ContactName).HasMaxLength(120);
        builder.Property(p => p.ContactEmail).HasMaxLength(256);
        builder.Property(p => p.ContactPhone).HasMaxLength(30);

        builder.OwnsOne(p => p.AnnualContribution, money =>
        {
            money.Property(m => m.Amount).HasColumnName("ContributionAnnuelle").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("ContributionDevise").HasMaxLength(3);
        });
        builder.Navigation(p => p.AnnualContribution).IsRequired();

        builder.HasIndex(p => new { p.IsActive, p.DisplayOrder });
    }
}

/// <summary>Questions fréquentes.</summary>
public sealed class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        builder.ToTable("Faq");
        builder.Property(f => f.Question).HasMaxLength(300).IsRequired();
        builder.Property(f => f.Answer).IsRequired();
        builder.Property(f => f.Category).HasMaxLength(80).IsRequired();

        builder.HasIndex(f => new { f.Category, f.DisplayOrder });
    }
}

/// <summary>Pages éditoriales.</summary>
public sealed class StaticPageConfiguration : IEntityTypeConfiguration<StaticPage>
{
    public void Configure(EntityTypeBuilder<StaticPage> builder)
    {
        builder.ToTable("Pages");
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Content).IsRequired();
        builder.Property(p => p.MetaTitle).HasMaxLength(200);
        builder.Property(p => p.MetaDescription).HasMaxLength(320);

        builder.HasIndex(p => p.Slug).IsUnique();
    }
}

/// <summary>Messages du formulaire de contact.</summary>
public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("MessagesContact");
        builder.Property(m => m.Name).HasMaxLength(120).IsRequired();
        builder.Property(m => m.Email).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Phone).HasMaxLength(30);
        builder.Property(m => m.Subject).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Message).HasMaxLength(4000).IsRequired();
        builder.Property(m => m.IpAddress).HasMaxLength(45);
        builder.Property(m => m.InternalNote).HasMaxLength(2000);

        builder.HasIndex(m => new { m.Status, m.ReceivedAt });
    }
}

/// <summary>Paramètres système.</summary>
public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("Parametres");
        builder.Property(s => s.Key).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(4000);
        builder.Property(s => s.Description).HasMaxLength(400);
        builder.Property(s => s.Group).HasMaxLength(80).IsRequired();
        builder.Property(s => s.DataType).HasMaxLength(20).IsRequired();

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
