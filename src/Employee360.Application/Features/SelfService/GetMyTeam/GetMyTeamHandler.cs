using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Features.Employees.GetEmployeesPaged;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.SelfService.GetMyTeam;

/// <summary>
/// Handles <see cref="GetMyTeamQuery"/>: the result set is hard-limited to the
/// authenticated manager's reporting subtree (FR-AUTH-008), then filtered,
/// sorted, and paginated.
/// </summary>
public sealed class GetMyTeamHandler
    : IRequestHandler<GetMyTeamQuery, Result<PagedResult<EmployeeListItem>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IManagerScopeService _managerScopeService;

    public GetMyTeamHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IManagerScopeService managerScopeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _managerScopeService = managerScopeService;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EmployeeListItem>>> Handle(
        GetMyTeamQuery request,
        CancellationToken cancellationToken)
    {
        var managerId = _currentUserService.EmployeeId;

        if (managerId is null)
        {
            return Result.Failure<PagedResult<EmployeeListItem>>(
                "No employee record is linked to your account.");
        }

        var teamIds = (await _managerScopeService.GetManagedEmployeeIdsAsync(
            managerId.Value, includeIndirect: true, cancellationToken)).ToList();

        var query = _context.Employees
            .AsNoTracking()
            .Where(e => teamIds.Contains(e.Id));

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(e =>
                (e.FirstName + " " + e.LastName).ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeListItem(
                e.Id,
                e.EmployeeCode,
                e.FirstName + " " + e.LastName,
                e.Email,
                e.Department != null ? e.Department.Name : null,
                e.Position != null ? e.Position.Title : null,
                e.Status,
                e.JoinDate))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<EmployeeListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}
