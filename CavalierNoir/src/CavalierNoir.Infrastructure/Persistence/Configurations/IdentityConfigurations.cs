using CavalierNoir.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Cartographie de l'utilisateur et de ses données rattachées.</summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Pseudonym).HasMaxLength(50);
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(300);
        builder.Property(u => u.Bio).HasMaxLength(2000);
        builder.Property(u => u.City).HasMaxLength(100);
        builder.Property(u => u.Country).HasMaxLength(100);
        builder.Property(u => u.FideId).HasMaxLength(20);
        builder.Property(u => u.LichessUsername).HasMaxLength(50);
        builder.Property(u => u.ChessComUsername).HasMaxLength(50);
        builder.Property(u => u.Notes).HasMaxLength(2000);

        // Index non unique : une contrainte d'unicité sur une colonne nullable se
        // comporte différemment selon le SGBD (SQL Server considère NULL = NULL).
        // L'unicité du pseudonyme est vérifiée côté application.
        builder.HasIndex(u => u.Pseudonym);
        builder.HasIndex(u => u.Elo);
        builder.HasIndex(u => u.LastLoginAt);

        // Propriétés calculées : jamais persistées.
        builder.Ignore(u => u.DisplayName);
        builder.Ignore(u => u.FullName);
        builder.Ignore(u => u.Initials);
        builder.Ignore(u => u.Level);

        builder.HasOne(u => u.Preference)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Progress)
            .WithOne(p => p.User)
            .HasForeignKey<Domain.Learning.UserProgress>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Préférences d'affichage et de notification.</summary>
public sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("Preferences");
        builder.Property(p => p.Language).HasMaxLength(5).IsRequired();
        builder.Property(p => p.Theme).HasMaxLength(20).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();
    }
}

/// <summary>Historique des connexions.</summary>
public sealed class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("HistoriqueConnexions");
        builder.Property(h => h.AttemptedIdentifier).HasMaxLength(256).IsRequired();
        builder.Property(h => h.FailureReason).HasMaxLength(200);
        builder.Property(h => h.IpAddress).HasMaxLength(45);
        builder.Property(h => h.UserAgent).HasMaxLength(400);
        builder.Property(h => h.CorrelationId).HasMaxLength(64);

        builder.HasIndex(h => new { h.UserId, h.OccurredAt });
        builder.HasIndex(h => h.OccurredAt);

        builder.HasOne(h => h.User)
            .WithMany(u => u.LoginHistory)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Journal d'audit : table en écriture seule, jamais modifiée.</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("JournalAudit");
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(50);
        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.CorrelationId).HasMaxLength(64);
        builder.Property(a => a.AffectedColumns).HasMaxLength(1000);

        builder.HasIndex(a => new { a.UserId, a.Timestamp });
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.Timestamp);
    }
}

/// <summary>Notifications internes.</summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.Property(n => n.Title).HasMaxLength(150).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Url).HasMaxLength(300);
        builder.Property(n => n.Icon).HasMaxLength(50);

        builder.Ignore(n => n.IsRead);
        builder.HasIndex(n => new { n.UserId, n.ReadAt });

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
