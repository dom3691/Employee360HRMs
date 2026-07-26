using Employee360.Application.Common.Models;
using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Positions.GetPositions;

/// <summary>List row for positions.</summary>
public sealed record PositionListItem(
    Guid Id,
    string Title,
    string Code,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? GradeId,
    string? GradeName,
    int EmployeeCount);

/// <summary>Paged position list with optional department filter and search.</summary>
public sealed record GetPositionsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? DepartmentId = null,
    string? Search = null) : IRequest<Result<PagedResult<PositionListItem>>>;

/// <summary>Fetches one position by id.</summary>
public sealed record GetPositionByIdQuery(Guid PositionId) : IRequest<Result<PositionListItem>>;
