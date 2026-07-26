namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Seeds reference data required for the system to operate (roles, permission
/// catalog, RBAC matrix). Implementations must be idempotent — safe to run on
/// every startup.
/// </summary>
public interface IDataSeeder
{
    /// <summary>Applies all pending seed data.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
