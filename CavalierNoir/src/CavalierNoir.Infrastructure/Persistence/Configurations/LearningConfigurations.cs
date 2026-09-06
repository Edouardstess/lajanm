using CavalierNoir.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Recueils d'exercices.</summary>
public sealed class ExerciseCollectionConfiguration : IEntityTypeConfiguration<ExerciseCollection>
{
    public void Configure(EntityTypeBuilder<ExerciseCollection> builder)
    {
        builder.ToTable("RecueilsExercices");
        builder.Property(c => c.Title).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

/// <summary>Exercices tactiques.</summary>
public sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercices");
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Fen).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Solution).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Hint1).HasMaxLength(500);
        builder.Property(e => e.Hint2).HasMaxLength(500);
        builder.Property(e => e.Hint3).HasMaxLength(500);
        builder.Property(e => e.Explanation).HasMaxLength(4000);
        builder.Property(e => e.Source).HasMaxLength(200);
        builder.Property(e => e.RatingSum).HasPrecision(10, 2);

        builder.Ignore(e => e.SuccessRate);
        builder.Ignore(e => e.AverageTimeSeconds);
        builder.Ignore(e => e.AverageRating);
        builder.Ignore(e => e.WhiteToMove);
        builder.Ignore(e => e.SideToMoveLabel);
        builder.Ignore(e => e.HintCount);

        builder.HasIndex(e => new { e.Theme, e.Difficulty });
        builder.HasIndex(e => e.IsPublished);
        builder.HasIndex(e => e.LastUsedAsDailyOn);

        builder.HasOne(e => e.Collection)
            .WithMany(c => c.Exercises)
            .HasForeignKey(e => e.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Tentatives de résolution.</summary>
public sealed class ExerciseAttemptConfiguration : IEntityTypeConfiguration<ExerciseAttempt>
{
    public void Configure(EntityTypeBuilder<ExerciseAttempt> builder)
    {
        builder.ToTable("TentativesExercices");
        builder.Property(a => a.SubmittedMoves).HasMaxLength(500);

        builder.HasIndex(a => new { a.UserId, a.ExerciseId });
        builder.HasIndex(a => a.AttemptedAt);

        builder.HasOne(a => a.Exercise)
            .WithMany(e => e.Attempts)
            .HasForeignKey(a => a.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany(u => u.ExerciseAttempts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Progression cumulée d'un membre.</summary>
public sealed class UserProgressConfiguration : IEntityTypeConfiguration<UserProgress>
{
    public void Configure(EntityTypeBuilder<UserProgress> builder)
    {
        builder.ToTable("Progression");

        builder.Ignore(p => p.SuccessRate);
        builder.Ignore(p => p.AverageTimeSeconds);

        builder.HasIndex(p => p.UserId).IsUnique();
    }
}

/// <summary>Badges.</summary>
public sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");
        builder.Property(b => b.Name).HasMaxLength(120).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(60).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.Icon).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Color).HasMaxLength(30).IsRequired();

        builder.HasIndex(b => b.Code).IsUnique();
    }
}

/// <summary>Badges attribués.</summary>
public sealed class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("BadgesObtenus");
        builder.Property(b => b.Comment).HasMaxLength(500);

        builder.HasIndex(b => new { b.UserId, b.BadgeId }).IsUnique();

        builder.HasOne(b => b.User)
            .WithMany(u => u.Badges)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Badge)
            .WithMany(b => b.AwardedTo)
            .HasForeignKey(b => b.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Planification de l'exercice du jour.</summary>
public sealed class DailyExerciseConfiguration : IEntityTypeConfiguration<DailyExercise>
{
    public void Configure(EntityTypeBuilder<DailyExercise> builder)
    {
        builder.ToTable("ExercicesQuotidiens");
        builder.Property(d => d.DispatchError).HasMaxLength(1000);

        builder.Ignore(d => d.IsDispatched);

        // Un seul exercice par date : garantit l'idempotence de l'envoi (BR-08).
        builder.HasIndex(d => d.ScheduledOn).IsUnique();

        builder.HasOne(d => d.Exercise)
            .WithMany()
            .HasForeignKey(d => d.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Cours.</summary>
public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Cours");
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(4000);

        builder.OwnsOne(c => c.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("DateDebut");
            period.Property(p => p.End).HasColumnName("DateFin");
        });
        builder.Navigation(c => c.Period).IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}

/// <summary>Chapitres de cours.</summary>
public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lecons");
        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Summary).HasMaxLength(1000);
        builder.Property(l => l.AttachmentUrl).HasMaxLength(300);

        builder.HasIndex(l => new { l.CourseId, l.Order });

        builder.HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Séances d'entraînement.</summary>
public sealed class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.ToTable("Seances");
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Location).HasMaxLength(200);
        builder.Property(s => s.Topic).HasMaxLength(300);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Ignore(s => s.PresentCount);
        builder.HasIndex(s => s.Date);

        builder.HasOne(s => s.Course)
            .WithMany(c => c.Sessions)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Feuilles de présence.</summary>
public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Presences");
        builder.Property(a => a.Comment).HasMaxLength(500);

        builder.HasIndex(a => new { a.TrainingSessionId, a.UserId }).IsUnique();

        builder.HasOne(a => a.TrainingSession)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
