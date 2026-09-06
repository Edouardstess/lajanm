namespace CavalierNoir.Domain.Common;

/// <summary>
/// Événement métier publié par le domaine et consommé par la couche Application.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base commune des événements de domaine.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
