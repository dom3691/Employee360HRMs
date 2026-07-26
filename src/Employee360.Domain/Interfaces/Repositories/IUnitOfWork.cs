namespace Employee360.Domain.Interfaces.Repositories;

/// <summary>
/// Unit of Work contract. The EF Core DbContext is the implementation: all staged
/// repository changes are committed atomically by a single
/// <see cref="SaveChangesAsync"/> call, which also triggers audit-column population,
/// audit logging, and domain-event dispatch.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all staged changes in one transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
