using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Departments.GetDepartments;

/// <summary>Directory row for the department list.</summary>
public sealed record DepartmentListItem(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    Guid? HeadEmployeeId,
    string? HeadEmployeeName,
    int EmployeeCount);

/// <summary>Paged department list with optional name/code search.</summary>
public sealed record GetDepartmentsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<Result<PagedResult<DepartmentListItem>>>;

/// <summary>Fetches one department by id.</summary>
public sealed record GetDepartmentByIdQuery(Guid DepartmentId)
    : IRequest<Result<DepartmentListItem>>;

/// <summary>A node in the department hierarchy tree.</summary>
public sealed record DepartmentTreeNode(
    Guid Id,
    string Name,
    string Code,
    string? HeadEmployeeName,
    IReadOnlyList<DepartmentTreeNode> Children);

/// <summary>Builds the full department hierarchy tree.</summary>
public sealed record GetDepartmentTreeQuery : IRequest<Result<IReadOnlyList<DepartmentTreeNode>>>;
