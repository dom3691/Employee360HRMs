using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Employees.GetEmployeesPaged;

/// <summary>Directory row for the employee list (SCR-005).</summary>
public sealed record EmployeeListItem(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string? DepartmentName,
    string? PositionTitle,
    EmployeeStatus Status,
    DateOnly? JoinDate);

/// <summary>
/// Paged, filterable employee directory query (API conventions: page/pageSize
/// default 20 max 100, sortBy + sortDirection).
/// </summary>
/// <param name="Page">1-based page (default 1).</param>
/// <param name="PageSize">Page size (default 20, max 100).</param>
/// <param name="DepartmentId">Optional department filter.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="Search">Optional search over name, email, and employee code.</param>
/// <param name="SortBy">One of: name (default), code, joinDate.</param>
/// <param name="SortDirection">asc (default) or desc.</param>
public sealed record GetEmployeesPagedQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? DepartmentId = null,
    EmployeeStatus? Status = null,
    string? Search = null,
    string SortBy = "name",
    string SortDirection = "asc") : IRequest<Result<PagedResult<EmployeeListItem>>>;
