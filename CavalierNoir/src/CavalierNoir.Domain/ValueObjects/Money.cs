using System.Globalization;
using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.ValueObjects;

/// <summary>
/// Montant monétaire immuable. Persisté comme type possédé (owned type) :
/// deux colonnes <c>*_Amount</c> et <c>*_Currency</c>.
/// </summary>
public sealed record Money
{
    public const string Gourde = "HTG";
    public const string Dollar = "USD";
    public const string Euro = "EUR";

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("BR-14", "La devise est obligatoire.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = Gourde) => new(0m, currency);

    public static Money Htg(decimal amount) => new(amount, Gourde);

    public static Money Usd(decimal amount) => new(amount, Dollar);

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator *(Money left, decimal factor) => left.Multiply(factor);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException(
                "BR-14",
                $"Impossible de combiner des montants exprimés en {Currency} et en {other.Currency}.");
        }
    }

    public string Format(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.GetCultureInfo("fr-FR");
        var symbol = Currency switch
        {
            Gourde => "HTG",
            Dollar => "USD",
            Euro => "EUR",
            _ => Currency
        };
        return string.Create(culture, $"{Amount:N2} {symbol}");
    }

    public override string ToString() => Format();
}
