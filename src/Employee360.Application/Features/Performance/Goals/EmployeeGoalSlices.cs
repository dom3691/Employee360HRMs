using Employee360.Application.Common.Extensions;
using Employee360.Application.Common.Interfaces;
using Employee360.Application.Common.Models;
using Employee360.Application.Common.Validation;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Performance.Goals;

public sealed record EmployeeGoalDto(
    Guid Id,
    Guid ReviewCycleId,
    string CycleName,
    Guid EmployeeId,
    string? EmployeeName,
    string? DepartmentName,
    string Title,
    string? Description,
    string? Category,
    string? Status,
    decimal Weight,
    DateOnly? DueDate,
    string? TargetValue,
    string? ActualValue,
    decimal? AchievementPercent,
    IReadOnlyList<GoalKeyResultDto>? KeyResults);

public sealed record GoalKeyResultDto(
    Guid Id,
    string Title,
    string Status,
    decimal ProgressPercent,
    string? MetricLabel);

public sealed record GetGoalsPagedQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    string? Category = null,
    string? Status = null) : IRequest<Result<PagedResult<EmployeeGoalDto>>>;

public sealed class GetGoalsPagedValidator : AbstractValidator<GetGoalsPagedQuery>
{
    public GetGoalsPagedValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

public sealed class GetGoalsPagedHandler : IRequestHandler<GetGoalsPagedQuery, Result<PagedResult<EmployeeGoalDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetGoalsPagedHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<EmployeeGoalDto>>> Handle(
        GetGoalsPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.EmployeeGoals.AsNoTracking();

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(g => g.EmployeeId == request.EmployeeId.Value);
        }

        var projected = query
            .OrderByDescending(g => g.ReviewCycle!.StartDate)
            .ThenBy(g => g.Title)
            .Select(g => new
            {
                g.Id,
                g.ReviewCycleId,
                CycleName = g.ReviewCycle!.Name,
                g.EmployeeId,
                EmployeeName = g.Employee!.FirstName + " " + g.Employee.LastName,
                DepartmentName = g.Employee.Department != null ? g.Employee.Department.Name : null,
                g.Title,
                g.Description,
                g.Weight,
                g.TargetValue,
                g.ActualValue,
                CycleEndDate = g.ReviewCycle.EndDate,
            });

        var paged = await projected.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);

        var items = paged.Items.Select(g =>
        {
            var achievement = g.TargetValue > 0 && g.ActualValue.HasValue
                ? Math.Round(Math.Min(g.ActualValue.Value / g.TargetValue, 1m) * 100m, 2, MidpointRounding.AwayFromZero)
                : (decimal?)null;

            var status = achievement switch
            {
                >= 100 => "Complete",
                >= 70 => "OnTrack",
                >= 40 => "AtRisk",
                _ => "Behind",
            };

            return new EmployeeGoalDto(
                g.Id,
                g.ReviewCycleId,
                g.CycleName,
                g.EmployeeId,
                g.EmployeeName,
                g.DepartmentName,
                g.Title,
                g.Description,
                request.Category ?? "Performance",
                request.Status ?? status,
                g.Weight,
                g.CycleEndDate,
                g.TargetValue.ToString("0.##"),
                g.ActualValue?.ToString("0.##"),
                achievement,
                null);
        }).ToList();

        return Result.Success(new PagedResult<EmployeeGoalDto>(
            items, paged.Page, paged.PageSize, paged.TotalCount));
    }
}

public sealed record GoalInputItem(
    string Title,
    string? Description,
    decimal Weight,
    decimal TargetValue,
    decimal? ActualValue);

public sealed record SetEmployeeGoalsCommand(
    Guid ReviewCycleId,
    Guid EmployeeId,
    IReadOnlyList<GoalInputItem> Goals) : IRequest<Result>;

public sealed class SetEmployeeGoalsValidator : AbstractValidator<SetEmployeeGoalsCommand>
{
    public SetEmployeeGoalsValidator()
    {
        RuleFor(c => c.ReviewCycleId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Goals).NotEmpty();
        RuleForEach(c => c.Goals).ChildRules(g =>
        {
            g.RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
            g.RuleFor(x => x.Weight).GreaterThan(0);
            g.RuleFor(x => x.TargetValue).GreaterThan(0);
        });
    }
}

public sealed class SetEmployeeGoalsHandler : IRequestHandler<SetEmployeeGoalsCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public SetEmployeeGoalsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(SetEmployeeGoalsCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.ReviewCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ReviewCycleId, cancellationToken);

        if (cycle is null)
        {
            return Result.Failure("Review cycle not found.");
        }

        if (cycle.Status is ReviewCycleStatus.Closed)
        {
            return Result.Failure("Goals cannot be modified in a closed review cycle.");
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure("Employee not found.");
        }

        var totalWeight = request.Goals.Sum(g => g.Weight);
        if (Math.Abs(totalWeight - 100m) > 0.01m)
        {
            return Result.Failure($"Goal weights must sum to 100 (current total: {totalWeight}).");
        }

        var existing = await _context.EmployeeGoals
            .Where(g => g.ReviewCycleId == request.ReviewCycleId && g.EmployeeId == request.EmployeeId)
            .ToListAsync(cancellationToken);

        _context.EmployeeGoals.RemoveRange(existing);

        foreach (var goal in request.Goals)
        {
            _context.EmployeeGoals.Add(new EmployeeGoal
            {
                ReviewCycleId = request.ReviewCycleId,
                EmployeeId = request.EmployeeId,
                Title = goal.Title.Trim(),
                Description = goal.Description?.Trim(),
                Weight = goal.Weight,
                TargetValue = goal.TargetValue,
                ActualValue = goal.ActualValue,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record GetEmployeeGoalsQuery(Guid ReviewCycleId, Guid EmployeeId)
    : IRequest<Result<IReadOnlyList<EmployeeGoalDto>>>;

public sealed class GetEmployeeGoalsHandler : IRequestHandler<GetEmployeeGoalsQuery, Result<IReadOnlyList<EmployeeGoalDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeeGoalsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<EmployeeGoalDto>>> Handle(
        GetEmployeeGoalsQuery request,
        CancellationToken cancellationToken)
    {
        var goals = await _context.EmployeeGoals
            .AsNoTracking()
            .Where(g => g.ReviewCycleId == request.ReviewCycleId && g.EmployeeId == request.EmployeeId)
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);

        var cycleName = await _context.ReviewCycles
            .AsNoTracking()
            .Where(c => c.Id == request.ReviewCycleId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var dtos = goals.Select(g => MapGoal(g, cycleName)).ToList();
        return Result.Success<IReadOnlyList<EmployeeGoalDto>>(dtos);
    }

    internal static EmployeeGoalDto MapGoal(EmployeeGoal g, string cycleName, string? employeeName = null, string? departmentName = null)
    {
        var achievement = g.TargetValue > 0 && g.ActualValue.HasValue
            ? Math.Round(Math.Min(g.ActualValue.Value / g.TargetValue, 1m) * 100m, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        var status = achievement switch
        {
            >= 100 => "Complete",
            >= 70 => "OnTrack",
            >= 40 => "AtRisk",
            _ => "Behind",
        };

        return new EmployeeGoalDto(
            g.Id, g.ReviewCycleId, cycleName, g.EmployeeId,
            employeeName, departmentName,
            g.Title, g.Description, "Performance", status,
            g.Weight, null,
            g.TargetValue.ToString("0.##"),
            g.ActualValue?.ToString("0.##"),
            achievement,
            null);
    }
}

/// <summary>ESS: current employee's goals for active cycle (FR-PERF-002).</summary>
public sealed record GetMyGoalsQuery(Guid? ReviewCycleId) : IRequest<Result<IReadOnlyList<EmployeeGoalDto>>>;

public sealed class GetMyGoalsHandler : IRequestHandler<GetMyGoalsQuery, Result<IReadOnlyList<EmployeeGoalDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyGoalsHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<EmployeeGoalDto>>> Handle(
        GetMyGoalsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId;
        if (employeeId is null)
        {
            return Result.Failure<IReadOnlyList<EmployeeGoalDto>>("No employee record linked to your account.");
        }

        Guid cycleId;

        if (request.ReviewCycleId.HasValue)
        {
            cycleId = request.ReviewCycleId.Value;
        }
        else
        {
            var activeCycle = await _context.ReviewCycles
                .AsNoTracking()
                .Where(c => c.Status == ReviewCycleStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (activeCycle is null)
            {
                return Result.Success<IReadOnlyList<EmployeeGoalDto>>(Array.Empty<EmployeeGoalDto>());
            }

            cycleId = activeCycle.Id;
        }

        return await new GetEmployeeGoalsHandler(_context).Handle(
            new GetEmployeeGoalsQuery(cycleId, employeeId.Value),
            cancellationToken);
    }
}
