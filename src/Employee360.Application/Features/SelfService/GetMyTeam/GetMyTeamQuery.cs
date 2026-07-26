using Employee360.Application.Common.Models;
using Employee360.Application.Features.Employees.GetEmployeesPaged;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.SelfService.GetMyTeam;

/// <summary>
/// Manager team list (FR-MSS-002): paged, searchable, filterable by status,
/// strictly scoped to the authenticated manager's reporting subtree.
/// </summary>
public sealed record GetMyTeamQuery(
    int Page = 1,
    int PageSize = 20,
    EmployeeStatus? Status = null,
    string? Search = null) : IRequest<Result<PagedResult<EmployeeListItem>>>;
