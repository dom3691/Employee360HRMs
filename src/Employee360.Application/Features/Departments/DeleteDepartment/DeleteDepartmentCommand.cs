using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Departments.DeleteDepartment;

/// <summary>
/// Soft-deletes a department. Blocked while child departments or assigned
/// employees exist, so the org structure stays consistent.
/// </summary>
/// <param name="DepartmentId">The department to delete.</param>
public sealed record DeleteDepartmentCommand(Guid DepartmentId) : IRequest<Result>;
