using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.ValueObjects;

/// <summary>
/// Classement ELO borné à l'intervalle [200 ; 3000] (BR-07).
/// Persisté comme un simple entier ; le type sert aux calculs et aux invariants.
/// </summary>
public readonly record struct EloRating
{
    public const int Minimum = 200;
    public const int Maximum = 3000;
    public const int DefaultRating = 1200;

    public EloRating(int value)
    {
        if (value < Minimum || value > Maximum)
        {
            throw new DomainException(
                "BR-07",
                $"Un classement ELO doit être compris entre {Minimum} et {Maximum} (valeur reçue : {value}).");
        }

        Value = value;
    }

    public int Value { get; }

    public static EloRating Default => new(DefaultRating);

    public EloRating Shift(int delta) => new(Math.Clamp(Value + delta, Minimum, Maximum));

    public static implicit operator int(EloRating rating) => rating.Value;

    public static explicit operator EloRating(int value) => new(value);

    public override string ToString() => Value.ToString();
}
