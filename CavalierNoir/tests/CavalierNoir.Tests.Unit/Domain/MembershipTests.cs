using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Memberships;
using CavalierNoir.Domain.ValueObjects;
using Xunit;

namespace CavalierNoir.Tests.Unit.Domain;

/// <summary>Règles de gestion du cycle de vie d'une adhésion (BR-03, BR-04).</summary>
public class MembershipTests
{
    private static readonly DateOnly Depart = new(2026, 1, 1);

    private static Membership Adhesion(int dureeJours = 365) => new()
    {
        UserId = 1,
        MembershipTypeId = 1,
        MembershipType = new MembershipType { DurationDays = dureeJours, Price = Money.Htg(1500m) },
        Amount = Money.Htg(1500m),
        Period = new DateRange(Depart, Depart.AddDays(dureeJours - 1))
    };

    [Fact]
    public void Activate_DonneUnePeriodeDe365Jours()
    {
        var adhesion = Adhesion();

        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        Assert.Equal(MembershipStatus.Active, adhesion.Status);
        Assert.Equal(Depart, adhesion.Period.Start);
        Assert.Equal(new DateOnly(2026, 12, 31), adhesion.Period.End);
        Assert.Equal(365, adhesion.Period.DurationInDays);
    }

    [Fact]
    public void IsCurrentlyActive_VraiPendantLaPeriode()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        Assert.True(adhesion.IsCurrentlyActive(new DateOnly(2026, 6, 15)));
        Assert.False(adhesion.IsCurrentlyActive(new DateOnly(2027, 1, 2)));
    }

    [Fact]
    public void Refresh_PasseEnDelaiDeGraceApresEcheance()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        // Lendemain de l'échéance : la période est finie, le délai de grâce commence.
        var statut = adhesion.Refresh(new DateOnly(2027, 1, 1));

        Assert.Equal(MembershipStatus.DelaiDeGrace, statut);
    }

    [Fact]
    public void Refresh_ExpireApresLeDelaiDeGraceDeQuinzeJours()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        Assert.Equal(new DateOnly(2027, 1, 15), adhesion.GraceEndDate);

        Assert.Equal(MembershipStatus.DelaiDeGrace, adhesion.Refresh(new DateOnly(2027, 1, 15)));
        Assert.Equal(MembershipStatus.Expiree, adhesion.Refresh(new DateOnly(2027, 1, 16)));
    }

    [Fact]
    public void Refresh_NeTouchePasAUneAdhesionSuspendue()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);
        adhesion.Suspend("Sanction disciplinaire", DateTime.UtcNow);

        Assert.Equal(MembershipStatus.Suspendue, adhesion.Refresh(new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void NeedsRenewalReminder_TrenteJoursAvantEcheance()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        Assert.False(adhesion.NeedsRenewalReminder(new DateOnly(2026, 11, 30)));
        Assert.True(adhesion.NeedsRenewalReminder(new DateOnly(2026, 12, 5)));
        Assert.True(adhesion.NeedsRenewalReminder(new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void NeedsRenewalReminder_UneSeuleFois()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);
        adhesion.RenewalReminderSentAt = DateTime.UtcNow;

        Assert.False(adhesion.NeedsRenewalReminder(new DateOnly(2026, 12, 10)));
    }

    [Fact]
    public void Renew_ProlongeSansPerdreDeJoursSiAnticipe()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        // Renouvellement anticipé, deux mois avant l'échéance.
        adhesion.Renew(365, Money.Htg(1500m), new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2027, 1, 1), adhesion.Period.Start);
        Assert.Equal(new DateOnly(2027, 12, 31), adhesion.Period.End);
        Assert.Equal(MembershipStatus.Active, adhesion.Status);
    }

    [Fact]
    public void Renew_ApresExpiration_RepartDuJour()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        adhesion.Renew(365, Money.Htg(1500m), new DateTime(2027, 3, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(2027, 3, 1), adhesion.Period.Start);
    }

    [Fact]
    public void Reactivate_RefuseUneAdhesionNonSuspendue()
    {
        var adhesion = Adhesion();
        adhesion.Activate(Depart.ToDateTime(TimeOnly.MinValue), Depart);

        Assert.Throws<DomainException>(() => adhesion.Reactivate(DateTime.UtcNow));
    }

    [Fact]
    public void BuildMemberNumber_SuitLeFormatAttendu()
    {
        Assert.Equal("CN-2026-0042", Membership.BuildMemberNumber(2026, 42));
    }
}
