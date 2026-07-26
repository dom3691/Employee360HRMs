namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Resolves the set of employees a manager is responsible for via the reporting
/// hierarchy (Employee.ManagerId), enforcing manager-scoped data access
/// (PRD FR-AUTH-008). Used by query handlers to filter team data.
/// </summary>
public interface IManagerScopeService
{
    /// <summary>
    /// Returns the employee ids reporting to <paramref name="managerEmployeeId"/> —
    /// direct reports only, or the full subtree when <paramref name="includeIndirect"/>
    /// is true. The manager's own id is never included.
    /// </summary>
    /// <param name="managerEmployeeId">The manager's employee id.</param>
    /// <param name="includeIndirect">True to include indirect reports (default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyCollection<Guid>> GetManagedEmployeeIdsAsync(
        Guid managerEmployeeId,
        bool includeIndirect = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when <paramref name="employeeId"/> reports (directly or
    /// indirectly) to <paramref name="managerEmployeeId"/>.
    /// </summary>
    /// <param name="managerEmployeeId">The manager's employee id.</param>
    /// <param name="employeeId">The employee to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsManagerOfAsync(
        Guid managerEmployeeId,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
