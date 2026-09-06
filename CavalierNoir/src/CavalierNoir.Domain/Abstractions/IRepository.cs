using System.Linq.Expressions;
using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Abstractions;

/// <summary>
/// Dépôt générique en lecture seule. Défini dans le domaine, implémenté dans
/// l'infrastructure : le domaine ignore Entity Framework.
/// </summary>
public interface IReadRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Dépôt générique en lecture et écriture.</summary>
public interface IRepository<TEntity> : IReadRepository<TEntity> where TEntity : Entity
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
