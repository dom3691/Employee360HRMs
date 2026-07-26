using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Departments.UpdateDepartment;

/// <summary>Updates a department, guarding against circular hierarchy.</summary>
public sealed record UpdateDepartmentCommand(
    Guid DepartmentId,
    string Name,
    string Code,
    Guid? ParentDepartmentId,
    Guid? HeadEmployeeId) : IRequest<Result>;
