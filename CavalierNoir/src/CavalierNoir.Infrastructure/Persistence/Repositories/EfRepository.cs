using System.Linq.Expressions;
using CavalierNoir.Domain.Abstractions;
using CavalierNoir.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implémentation Entity Framework du dépôt générique. Les lectures sont
/// détachées du suivi de modifications (<c>AsNoTracking</c>) : un dépôt renvoie
/// des données, les écritures passent par <see cref="AddAsync"/> et
/// <see cref="Update"/>.
/// </summary>
public class EfRepository<TEntity>(ApplicationDbContext context) : IRepository<TEntity>
    where TEntity : Entity
{
    protected ApplicationDbContext Context { get; } = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public virtual Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? Set.CountAsync(cancellationToken)
            : Set.CountAsync(predicate, cancellationToken);

    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? Set.AnyAsync(cancellationToken)
            : Set.AnyAsync(predicate, cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    /// <summary>
    /// Supprime l'entité. Les entités archivables sont marquées comme supprimées
    /// plutôt qu'effacées physiquement.
    /// </summary>
    public virtual void Remove(TEntity entity)
    {
        if (entity is ISoftDeletable soft)
        {
            soft.IsDeleted = true;
            soft.DeletedAt = DateTime.UtcNow;
            Set.Update(entity);
            return;
        }

        Set.Remove(entity);
    }
}
