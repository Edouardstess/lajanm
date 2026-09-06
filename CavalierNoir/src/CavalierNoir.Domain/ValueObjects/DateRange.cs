using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.ValueObjects;

/// <summary>
/// Période fermée [Start ; End]. Utilisée pour la validité d'une adhésion,
/// d'un mandat de bureau ou d'un cours.
/// </summary>
public sealed record DateRange
{
    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new DomainException("La date de fin ne peut pas précéder la date de début.");
        }

        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public int DurationInDays => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;

    /// <summary>Période d'un an à compter de <paramref name="start"/> (BR-03 : 365 jours).</summary>
    public static DateRange OneYearFrom(DateOnly start) => new(start, start.AddDays(364));

    public override string ToString() => $"{Start:dd/MM/yyyy} → {End:dd/MM/yyyy}";
}
