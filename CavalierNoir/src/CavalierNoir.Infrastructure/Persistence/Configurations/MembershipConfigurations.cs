using CavalierNoir.Domain.Finance;
using CavalierNoir.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CavalierNoir.Infrastructure.Persistence.Configurations;

/// <summary>Formules d'adhésion.</summary>
public sealed class MembershipTypeConfiguration : IEntityTypeConfiguration<MembershipType>
{
    public void Configure(EntityTypeBuilder<MembershipType> builder)
    {
        builder.ToTable("FormulesAdhesion");
        builder.Property(t => t.Name).HasMaxLength(120).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(40).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);

        builder.OwnsOne(t => t.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Tarif").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("TarifDevise").HasMaxLength(3);
        });
        builder.Navigation(t => t.Price).IsRequired();

        builder.HasIndex(t => t.Code).IsUnique();
    }
}

/// <summary>Demandes d'adhésion.</summary>
public sealed class MembershipApplicationConfiguration : IEntityTypeConfiguration<MembershipApplication>
{
    public void Configure(EntityTypeBuilder<MembershipApplication> builder)
    {
        builder.ToTable("DemandesAdhesion");
        builder.Property(a => a.Motivation).HasMaxLength(2000);
        builder.Property(a => a.SupportingDocuments).HasMaxLength(1000);
        builder.Property(a => a.ParentContact).HasMaxLength(256);
        builder.Property(a => a.ReviewComment).HasMaxLength(1000);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);

        builder.Ignore(a => a.IsPending);

        builder.HasIndex(a => new { a.Status, a.SubmittedAt });
        builder.HasIndex(a => a.UserId);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.MembershipType)
            .WithMany(t => t.Applications)
            .HasForeignKey(a => a.MembershipTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ResultingMembership)
            .WithMany()
            .HasForeignKey(a => a.ResultingMembershipId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Adhésions.</summary>
public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Adhesions");
        builder.Property(m => m.MemberNumber).HasMaxLength(30).IsRequired();
        builder.Property(m => m.SuspensionReason).HasMaxLength(500);

        builder.OwnsOne(m => m.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("DateDebut");
            period.Property(p => p.End).HasColumnName("DateFin");
            period.HasIndex(p => p.End);
        });
        builder.Navigation(m => m.Period).IsRequired();

        builder.OwnsOne(m => m.Amount, money =>
        {
            money.Property(p => p.Amount).HasColumnName("Montant").HasPrecision(18, 2);
            money.Property(p => p.Currency).HasColumnName("Devise").HasMaxLength(3);
        });
        builder.Navigation(m => m.Amount).IsRequired();

        builder.Ignore(m => m.GraceEndDate);

        builder.HasIndex(m => m.MemberNumber).IsUnique();
        builder.HasIndex(m => new { m.UserId, m.Status });

        builder.HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.MembershipType)
            .WithMany(t => t.Memberships)
            .HasForeignKey(m => m.MembershipTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Encaissements.</summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Paiements");
        builder.Property(p => p.TransactionId).HasMaxLength(120);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(80);
        builder.Property(p => p.ReceiptNumber).HasMaxLength(30);
        builder.Property(p => p.ReceiptUrl).HasMaxLength(300);
        builder.Property(p => p.Reference).HasMaxLength(120);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.RefundReason).HasMaxLength(500);

        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Montant").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Devise").HasMaxLength(3);
        });
        builder.Navigation(p => p.Amount).IsRequired();

        builder.HasIndex(p => p.IdempotencyKey);
        builder.HasIndex(p => p.ReceiptNumber);
        builder.HasIndex(p => new { p.Status, p.PaidAt });

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Membership)
            .WithMany(m => m.Payments)
            .HasForeignKey(p => p.MembershipId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Dons.</summary>
public sealed class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Dons");
        builder.Property(d => d.DonorName).HasMaxLength(150).IsRequired();
        builder.Property(d => d.DonorEmail).HasMaxLength(256);
        builder.Property(d => d.Message).HasMaxLength(1000);
        builder.Property(d => d.ReceiptNumber).HasMaxLength(30);

        builder.OwnsOne(d => d.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Montant").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Devise").HasMaxLength(3);
        });
        builder.Navigation(d => d.Amount).IsRequired();

        builder.HasIndex(d => d.DonatedOn);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Payment)
            .WithMany()
            .HasForeignKey(d => d.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Dépenses.</summary>
public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Depenses");
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Supplier).HasMaxLength(150);
        builder.Property(e => e.ReceiptUrl).HasMaxLength(300);

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Montant").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Devise").HasMaxLength(3);
        });
        builder.Navigation(e => e.Amount).IsRequired();

        builder.Ignore(e => e.IsApproved);
        builder.HasIndex(e => new { e.Category, e.IncurredOn });
    }
}
