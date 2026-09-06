namespace CavalierNoir.Domain.Abstractions;

/// <summary>
/// Unité de travail : valide en une seule transaction l'ensemble des
/// modifications apportées aux agrégats.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
