namespace CavalierNoir.Domain.Common;

/// <summary>
/// Racine de toutes les entités persistées : identité technique entière auto-incrémentée.
/// </summary>
public abstract class Entity
{
    public int Id { get; set; }

    public bool IsTransient() => Id == 0;
}
