using CavalierNoir.Domain.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Tournois.</summary>
public sealed class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.ToTable("Tournois");
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(120).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.Regulations);
        builder.Property(t => t.TimeControlLabel).HasMaxLength(60);
        builder.Property(t => t.Location).HasMaxLength(200);
        builder.Property(t => t.Address).HasMaxLength(300);
        builder.Property(t => t.ImageUrl).HasMaxLength(300);

        builder.OwnsOne(t => t.EntryFee, money =>
        {
            money.Property(m => m.Amount).HasColumnName("FraisInscription").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("FraisDevise").HasMaxLength(3);
        });
        builder.Navigation(t => t.EntryFee).IsRequired();

        builder.Ignore(t => t.ConfirmedPlayers);
        builder.Ignore(t => t.IsFull);
        builder.Ignore(t => t.CurrentRound);
        builder.Ignore(t => t.IsEditable);

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.StartDate);
        builder.HasIndex(t => t.Status);
    }
}

/// <summary>Inscriptions aux tournois.</summary>
public sealed class TournamentRegistrationConfiguration : IEntityTypeConfiguration<TournamentRegistration>
{
    public void Configure(EntityTypeBuilder<TournamentRegistration> builder)
    {
        builder.ToTable("TournoiInscriptions");
        builder.Property(r => r.CancellationReason).HasMaxLength(500);

        builder.HasIndex(r => new { r.TournamentId, r.UserId }).IsUnique();
        builder.HasIndex(r => new { r.TournamentId, r.Status });

        builder.HasOne(r => r.Tournament)
            .WithMany(t => t.Registrations)
            .HasForeignKey(r => r.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.TournamentRegistrations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Rondes.</summary>
public sealed class TournamentRoundConfiguration : IEntityTypeConfiguration<TournamentRound>
{
    public void Configure(EntityTypeBuilder<TournamentRound> builder)
    {
        builder.ToTable("TournoiRondes");

        builder.Ignore(r => r.AllGamesPlayed);
        builder.HasIndex(r => new { r.TournamentId, r.Number }).IsUnique();

        builder.HasOne(r => r.Tournament)
            .WithMany(t => t.Rounds)
            .HasForeignKey(r => r.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Parties et appariements.</summary>
public sealed class TournamentGameConfiguration : IEntityTypeConfiguration<TournamentGame>
{
    public void Configure(EntityTypeBuilder<TournamentGame> builder)
    {
        builder.ToTable("TournoiParties");
        builder.Property(g => g.Pgn);
        builder.Property(g => g.OpeningName).HasMaxLength(150);
        builder.Property(g => g.ResultCorrectionReason).HasMaxLength(500);

        builder.Ignore(g => g.ScoreForWhite);
        builder.Ignore(g => g.ScoreForBlack);
        builder.Ignore(g => g.CountsForElo);

        builder.HasIndex(g => new { g.TournamentId, g.RoundId, g.BoardNumber }).IsUnique();
        builder.HasIndex(g => g.WhitePlayerId);
        builder.HasIndex(g => g.BlackPlayerId);

        builder.HasOne(g => g.Tournament)
            .WithMany()
            .HasForeignKey(g => g.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Round)
            .WithMany(r => r.Games)
            .HasForeignKey(g => g.RoundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.WhitePlayer)
            .WithMany()
            .HasForeignKey(g => g.WhitePlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.BlackPlayer)
            .WithMany()
            .HasForeignKey(g => g.BlackPlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Classement d'un tournoi.</summary>
public sealed class TournamentStandingConfiguration : IEntityTypeConfiguration<TournamentStanding>
{
    public void Configure(EntityTypeBuilder<TournamentStanding> builder)
    {
        builder.ToTable("TournoiClassements");
        builder.Property(s => s.Score).HasPrecision(6, 2);
        builder.Property(s => s.Buchholz).HasPrecision(8, 2);
        builder.Property(s => s.BuchholzCut1).HasPrecision(8, 2);
        builder.Property(s => s.SonnebornBerger).HasPrecision(8, 2);
        builder.Property(s => s.Prize).HasMaxLength(150);

        builder.Ignore(s => s.GamesPlayed);
        builder.Ignore(s => s.EloDelta);

        builder.HasIndex(s => new { s.TournamentId, s.UserId }).IsUnique();
        builder.HasIndex(s => new { s.TournamentId, s.Rank });

        builder.HasOne(s => s.Tournament)
            .WithMany(t => t.Standings)
            .HasForeignKey(s => s.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Historique ELO.</summary>
public sealed class EloHistoryConfiguration : IEntityTypeConfiguration<EloHistory>
{
    public void Configure(EntityTypeBuilder<EloHistory> builder)
    {
        builder.ToTable("HistoriqueElo");
        builder.Property(h => h.ExpectedScore).HasPrecision(6, 4);
        builder.Property(h => h.ActualScore).HasPrecision(4, 2);
        builder.Property(h => h.Reason).HasMaxLength(300);

        builder.Ignore(h => h.Delta);

        builder.HasIndex(h => new { h.UserId, h.RecordedAt });
        builder.HasIndex(h => h.GameId);

        builder.HasOne(h => h.User)
            .WithMany(u => u.EloHistory)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Game)
            .WithMany()
            .HasForeignKey(h => h.GameId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Événements du calendrier.</summary>
public sealed class ClubEventConfiguration : IEntityTypeConfiguration<ClubEvent>
{
    public void Configure(EntityTypeBuilder<ClubEvent> builder)
    {
        builder.ToTable("Evenements");
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Address).HasMaxLength(300);
        builder.Property(e => e.ImageUrl).HasMaxLength(300);

        builder.OwnsOne(e => e.EntryFee, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Tarif").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("TarifDevise").HasMaxLength(3);
        });
        builder.Navigation(e => e.EntryFee).IsRequired();

        builder.Ignore(e => e.ConfirmedParticipants);
        builder.Ignore(e => e.IsFull);

        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.StartDate);

        builder.HasOne(e => e.Tournament)
            .WithMany()
            .HasForeignKey(e => e.TournamentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Inscriptions aux événements.</summary>
public sealed class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.ToTable("EvenementInscriptions");
        builder.Property(r => r.GuestName).HasMaxLength(150);
        builder.Property(r => r.GuestEmail).HasMaxLength(256);
        builder.Property(r => r.CheckInToken).HasMaxLength(40);
        builder.Property(r => r.Comment).HasMaxLength(1000);

        builder.HasIndex(r => new { r.ClubEventId, r.UserId });
        builder.HasIndex(r => r.CheckInToken);

        builder.HasOne(r => r.ClubEvent)
            .WithMany(e => e.Registrations)
            .HasForeignKey(r => r.ClubEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
