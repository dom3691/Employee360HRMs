using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Attendance;

// ---------------------------------------------------------------------------
// GetAttendanceRecords
// ---------------------------------------------------------------------------

/// <summary>Queries attendance records with optional filters.</summary>
public sealed record GetAttendanceRecordsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<Result<PagedResult<AttendanceRecordDto>>>;

/// <summary>Input validation for <see cref="GetAttendanceRecordsQuery"/>.</summary>
public sealed class GetAttendanceRecordsValidator : AbstractValidator<GetAttendanceRecordsQuery>
{
    public GetAttendanceRecordsValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

/// <summary>Handles <see cref="GetAttendanceRecordsQuery"/>.</summary>
public sealed class GetAttendanceRecordsHandler
    : IRequestHandler<GetAttendanceRecordsQuery, Result<PagedResult<AttendanceRecordDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IManagerScopeService _managerScope;

    public GetAttendanceRecordsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IManagerScopeService managerScope)
    {
        _context = context;
        _currentUser = currentUser;
        _managerScope = managerScope;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AttendanceRecordDto>>> Handle(
        GetAttendanceRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveEmployeeScopeAsync(request.EmployeeId, cancellationToken);

        if (scope.IsFailure)
        {
            return Result.Failure<PagedResult<AttendanceRecordDto>>(scope.Error!);
        }

        var allowedEmployeeIds = scope.Value;

        var query = _context.AttendanceRecords.AsNoTracking();

        if (allowedEmployeeIds is not null)
        {
            query = query.Where(r => allowedEmployeeIds.Contains(r.EmployeeId));
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == request.EmployeeId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(r => r.Date >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(r => r.Date <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => ClockInHandler.Map(r))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<AttendanceRecordDto>(
            items, request.Page, request.PageSize, totalCount));
    }

    private async Task<Result<HashSet<Guid>?>> ResolveEmployeeScopeAsync(
        Guid? requestedEmployeeId,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole(RoleNames.HRAdmin) ||
            _currentUser.IsInRole(RoleNames.HRManager))
        {
            return Result.Success<HashSet<Guid>?>(null);
        }

        var currentEmployeeId = _currentUser.EmployeeId;

        if (currentEmployeeId is null)
        {
            return Result.Failure<HashSet<Guid>?>("No employee record is linked to your account.");
        }

        if (_currentUser.IsInRole(RoleNames.LineManager))
        {
            var team = await _managerScope.GetManagedEmployeeIdsAsync(
                currentEmployeeId.Value, cancellationToken: cancellationToken);
            var allowed = team.ToHashSet();
            allowed.Add(currentEmployeeId.Value);

            if (requestedEmployeeId.HasValue && !allowed.Contains(requestedEmployeeId.Value))
            {
                return Result.Failure<HashSet<Guid>?>("You are not authorized to view this employee's attendance.");
            }

            return Result.Success<HashSet<Guid>?>(allowed);
        }

        if (requestedEmployeeId.HasValue && requestedEmployeeId != currentEmployeeId)
        {
            return Result.Failure<HashSet<Guid>?>("You can only view your own attendance.");
        }

        return Result.Success<HashSet<Guid>?>([currentEmployeeId.Value]);
    }
}
