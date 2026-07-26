using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Employees.DeactivateEmployee;

/// <summary>
/// Deactivates an employee (FR-EMP-004 lifecycle): sets an exit/suspension status
/// and disables the linked login account. The record is retained — never
/// hard-deleted (FR-EMP-005) — so history and audit stay intact.
/// </summary>
/// <param name="EmployeeId">The employee to deactivate.</param>
/// <param name="NewStatus">Target status: Suspended, Resigned, or Terminated.</param>
/// <param name="Reason">Reason recorded in the audit trail.</param>
public sealed record DeactivateEmployeeCommand(
    Guid EmployeeId,
    EmployeeStatus NewStatus,
    string Reason) : IRequest<Result>;
