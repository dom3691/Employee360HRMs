using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Mapping;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Attendance;

// ---------------------------------------------------------------------------
// Timesheet DTO
// ---------------------------------------------------------------------------

/// <summary>Weekly timesheet item (FR-ATT-004).</summary>
public sealed record TimesheetDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly WeekStart,
    decimal TotalHours,
    TimesheetStatus Status,
    string? Notes,
    string? ReviewComments);

/// <summary>Timesheet list item aligned with frontend contract.</summary>
public sealed record TimesheetListItem(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalHours,
    decimal RegularHours,
    decimal OvertimeHours,
    string Status);

// ---------------------------------------------------------------------------
// UpsertTimesheet
// ---------------------------------------------------------------------------

/// <summary>Creates or updates a draft weekly timesheet.</summary>
public sealed record UpsertTimesheetCommand(
    DateOnly WeekStart,
    decimal TotalHours,
    string? Notes) : IRequest<Result<TimesheetDto>>;

/// <summary>Input validation for <see cref="UpsertTimesheetCommand"/>.</summary>
public sealed class UpsertTimesheetValidator : AbstractValidator<UpsertTimesheetCommand>
{
    public UpsertTimesheetValidator()
    {
        RuleFor(c => c.WeekStart).NotEmpty();
        RuleFor(c => c.TotalHours).GreaterThanOrEqualTo(0).LessThanOrEqualTo(168);
    }
}

/// <summary>Handles <see cref="UpsertTimesheetCommand"/>.</summary>
public sealed class UpsertTimesheetHandler : IRequestHandler<UpsertTimesheetCommand, Result<TimesheetDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpsertTimesheetHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<TimesheetDto>> Handle(
        UpsertTimesheetCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<TimesheetDto>("No employee record is linked to your account.");
        }

        var weekStart = StartOfWeek(request.WeekStart);

        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(
                t => t.EmployeeId == employeeId && t.WeekStart == weekStart,
                cancellationToken);

        if (timesheet is not null && timesheet.Status is not TimesheetStatus.Draft and not TimesheetStatus.Rejected)
        {
            return Result.Failure<TimesheetDto>("This timesheet can no longer be edited.");
        }

        var isNew = timesheet is null;

        if (isNew)
        {
            timesheet = new Timesheet
            {
                EmployeeId = employeeId.Value,
                WeekStart = weekStart,
            };
            _context.Timesheets.Add(timesheet);
        }

        timesheet.TotalHours = request.TotalHours;
        timesheet.Notes = request.Notes?.Trim();
        timesheet.Status = TimesheetStatus.Draft;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(timesheet));
    }

    internal static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-offset);
    }

    internal static TimesheetDto Map(Timesheet timesheet) =>
        new(
            timesheet.Id,
            timesheet.EmployeeId,
            timesheet.WeekStart,
            timesheet.TotalHours,
            timesheet.Status,
            timesheet.Notes,
            timesheet.ReviewComments);
}

// ---------------------------------------------------------------------------
// SubmitTimesheet
// ---------------------------------------------------------------------------

/// <summary>Submits a draft timesheet for manager approval.</summary>
public sealed record SubmitTimesheetCommand(Guid TimesheetId) : IRequest<Result>;

/// <summary>Handles <see cref="SubmitTimesheetCommand"/>.</summary>
public sealed class SubmitTimesheetHandler : IRequestHandler<SubmitTimesheetCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitTimesheetHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(SubmitTimesheetCommand request, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.Id == request.TimesheetId, cancellationToken);

        if (timesheet is null)
        {
            return Result.Failure("Timesheet not found.");
        }

        if (timesheet.EmployeeId != _currentUser.EmployeeId)
        {
            return Result.Failure("You can only submit your own timesheet.");
        }

        if (timesheet.Status is not (TimesheetStatus.Draft or TimesheetStatus.Rejected))
        {
            return Result.Failure("Only draft or rejected timesheets can be submitted.");
        }

        timesheet.Status = TimesheetStatus.Submitted;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// ---------------------------------------------------------------------------
// ReviewTimesheet
// ---------------------------------------------------------------------------

/// <summary>Manager/HR approves or rejects a submitted timesheet.</summary>
public sealed record ReviewTimesheetCommand(
    Guid TimesheetId,
    bool Approve,
    string? Comments) : IRequest<Result>;

/// <summary>Handles <see cref="ReviewTimesheetCommand"/>.</summary>
public sealed class ReviewTimesheetHandler : IRequestHandler<ReviewTimesheetCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IManagerScopeService _managerScope;

    public ReviewTimesheetHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IManagerScopeService managerScope)
    {
        _context = context;
        _currentUser = currentUser;
        _managerScope = managerScope;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ReviewTimesheetCommand request, CancellationToken cancellationToken)
    {
        var timesheet = await _context.Timesheets
            .FirstOrDefaultAsync(t => t.Id == request.TimesheetId, cancellationToken);

        if (timesheet is null)
        {
            return Result.Failure("Timesheet not found.");
        }

        if (timesheet.Status != TimesheetStatus.Submitted)
        {
            return Result.Failure("Only submitted timesheets can be reviewed.");
        }

        var authorized = await CanReviewAsync(timesheet.EmployeeId, cancellationToken);

        if (!authorized)
        {
            return Result.Failure("You are not authorized to review this timesheet.");
        }

        timesheet.Status = request.Approve ? TimesheetStatus.Approved : TimesheetStatus.Rejected;
        timesheet.ReviewComments = request.Comments?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<bool> CanReviewAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole(RoleNames.HRAdmin) ||
            _currentUser.IsInRole(RoleNames.HRManager))
        {
            return true;
        }

        var managerId = _currentUser.EmployeeId;

        return managerId.HasValue &&
               await _managerScope.IsManagerOfAsync(managerId.Value, employeeId, cancellationToken);
    }
}

// ---------------------------------------------------------------------------
// GetTimesheets
// ---------------------------------------------------------------------------

/// <summary>Lists timesheets for the current employee, team, or org-wide (HR).</summary>
public sealed record GetTimesheetsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? EmployeeId = null) : IRequest<Result<PagedResult<TimesheetListItem>>>;

public sealed class GetTimesheetsValidator : AbstractValidator<GetTimesheetsQuery>
{
    public GetTimesheetsValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

/// <summary>Handles <see cref="GetTimesheetsQuery"/>.</summary>
public sealed class GetTimesheetsHandler : IRequestHandler<GetTimesheetsQuery, Result<PagedResult<TimesheetListItem>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTimesheetsHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<TimesheetListItem>>> Handle(
        GetTimesheetsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Timesheets.AsNoTracking();

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == request.EmployeeId.Value);
        }
        else if (!(_currentUser.IsInRole(RoleNames.HRAdmin) ||
                   _currentUser.IsInRole(RoleNames.HRManager) ||
                   _currentUser.IsInRole(RoleNames.SystemAdmin)))
        {
            var employeeId = _currentUser.EmployeeId;
            if (employeeId is null)
            {
                return Result.Failure<PagedResult<TimesheetListItem>>(
                    "No employee record is linked to your account.");
            }

            query = query.Where(t => t.EmployeeId == employeeId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(t => t.WeekStart)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new
            {
                t.Id,
                t.EmployeeId,
                EmployeeName = t.Employee.FirstName + " " + t.Employee.LastName,
                t.WeekStart,
                t.TotalHours,
                t.Status,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r =>
        {
            const decimal standardHours = 40m;
            var regular = Math.Min(r.TotalHours, standardHours);
            var overtime = Math.Max(0m, r.TotalHours - standardHours);

            return new TimesheetListItem(
                r.Id,
                r.EmployeeId,
                r.EmployeeName,
                r.WeekStart,
                r.WeekStart.AddDays(6),
                r.TotalHours,
                regular,
                overtime,
                RecruitmentApiMapping.ToApiTimesheetStatus(r.Status));
        }).ToList();

        return Result.Success(new PagedResult<TimesheetListItem>(
            items, request.Page, request.PageSize, totalCount));
    }
}
