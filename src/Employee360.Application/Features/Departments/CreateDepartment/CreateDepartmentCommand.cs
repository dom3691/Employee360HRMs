using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Departments.CreateDepartment;

/// <summary>Creates a department (FR-EMP-009).</summary>
/// <param name="Name">Department name.</param>
/// <param name="Code">Unique short code, e.g. "ENG".</param>
/// <param name="ParentDepartmentId">Optional parent for hierarchy.</param>
/// <param name="HeadEmployeeId">Optional department head.</param>
public sealed record CreateDepartmentCommand(
    string Name,
    string Code,
    Guid? ParentDepartmentId,
    Guid? HeadEmployeeId) : IRequest<Result<Guid>>;
