using System.Linq.Expressions;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>. All write operations are
/// staged on the context and committed by <see cref="IUnitOfWork.SaveChangesAsync"/>;
/// soft-deletable entities are converted to soft deletes by the audit interceptor.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    /// <summary>The backing context (exposed to derived, entity-specific repositories).</summary>
    protected readonly Employee360DbContext Context;

    /// <summary>The entity set (exposed to derived, entity-specific repositories).</summary>
    protected readonly DbSet<T> DbSet;

    public Repository(Employee360DbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? await DbSet.CountAsync(cancellationToken)
            : await DbSet.CountAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await DbSet.AddRangeAsync(entities, cancellationToken);

    /// <inheritdoc />
    public void Update(T entity) => DbSet.Update(entity);

    /// <inheritdoc />
    public void Remove(T entity) => DbSet.Remove(entity);
}
