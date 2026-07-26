using System.Linq.Expressions;
using Employee360.Domain.Common;

namespace Employee360.Domain.Interfaces.Repositories;

/// <summary>
/// Generic repository contract for aggregate persistence. Implemented over
/// EF Core in Infrastructure; write operations are staged and only persisted
/// when <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Returns the entity with the given id, or null when not found.</summary>
    /// <param name="id">Entity primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all entities (soft-deleted rows excluded by the global filter).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns entities matching <paramref name="predicate"/>.</summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the first entity matching <paramref name="predicate"/>, or null.</summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true when any entity matches <paramref name="predicate"/>.</summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Counts entities matching <paramref name="predicate"/> (all when null).</summary>
    /// <param name="predicate">Optional filter expression.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stages a new entity for insertion.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Stages multiple new entities for insertion.</summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>Stages an update to an existing entity.</summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>
    /// Stages removal of an entity. For <see cref="ISoftDelete"/> entities the
    /// persistence layer converts this into a soft delete.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(T entity);
}
