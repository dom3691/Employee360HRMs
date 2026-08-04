using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Performance.ReviewCycles;

public sealed record ReviewCycleDto(
    Guid Id,
    string Name,
    ReviewCycleType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    ReviewCycleStatus Status,
    decimal GoalWeightPercent,
    decimal SelfWeightPercent,
    decimal ManagerWeightPercent,
    decimal PeerWeightPercent);

public sealed record ConfigureReviewCycleCommand(
    string Name,
    ReviewCycleType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal GoalWeightPercent,
    decimal SelfWeightPercent,
    decimal ManagerWeightPercent,
    decimal PeerWeightPercent) : IRequest<Result<Guid>>;

public sealed class ConfigureReviewCycleValidator : AbstractValidator<ConfigureReviewCycleCommand>
{
    public ConfigureReviewCycleValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.EndDate).GreaterThanOrEqualTo(c => c.StartDate);
        RuleFor(c => c.GoalWeightPercent).InclusiveBetween(0, 100);
        RuleFor(c => c.SelfWeightPercent).InclusiveBetween(0, 100);
        RuleFor(c => c.ManagerWeightPercent).InclusiveBetween(0, 100);
        RuleFor(c => c.PeerWeightPercent).InclusiveBetween(0, 100);
    }
}

public sealed class ConfigureReviewCycleHandler : IRequestHandler<ConfigureReviewCycleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public ConfigureReviewCycleHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<Guid>> Handle(
        ConfigureReviewCycleCommand request,
        CancellationToken cancellationToken)
    {
        var weights = ReviewCycleWorkflow.EnsureWeightsSumTo100(
            request.GoalWeightPercent,
            request.SelfWeightPercent,
            request.ManagerWeightPercent,
            request.PeerWeightPercent);

        if (weights.IsFailure)
        {
            return Result.Failure<Guid>(weights.Error!);
        }

        var cycle = new ReviewCycle
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            GoalWeightPercent = request.GoalWeightPercent,
            SelfWeightPercent = request.SelfWeightPercent,
            ManagerWeightPercent = request.ManagerWeightPercent,
            PeerWeightPercent = request.PeerWeightPercent,
            Status = ReviewCycleStatus.Draft,
        };

        _context.ReviewCycles.Add(cycle);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(cycle.Id);
    }
}

public sealed record UpdateReviewCycleCommand(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal GoalWeightPercent,
    decimal SelfWeightPercent,
    decimal ManagerWeightPercent,
    decimal PeerWeightPercent) : IRequest<Result>;

public sealed class UpdateReviewCycleHandler : IRequestHandler<UpdateReviewCycleCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateReviewCycleHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateReviewCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.ReviewCycles.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cycle is null)
        {
            return Result.Failure("Review cycle not found.");
        }

        if (cycle.Status != ReviewCycleStatus.Draft)
        {
            return Result.Failure("Only draft review cycles can be edited.");
        }

        var weights = ReviewCycleWorkflow.EnsureWeightsSumTo100(
            request.GoalWeightPercent,
            request.SelfWeightPercent,
            request.ManagerWeightPercent,
            request.PeerWeightPercent);

        if (weights.IsFailure)
        {
            return weights;
        }

        cycle.Name = request.Name.Trim();
        cycle.StartDate = request.StartDate;
        cycle.EndDate = request.EndDate;
        cycle.GoalWeightPercent = request.GoalWeightPercent;
        cycle.SelfWeightPercent = request.SelfWeightPercent;
        cycle.ManagerWeightPercent = request.ManagerWeightPercent;
        cycle.PeerWeightPercent = request.PeerWeightPercent;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record ActivateReviewCycleCommand(Guid Id) : IRequest<Result>;

public sealed class ActivateReviewCycleHandler : IRequestHandler<ActivateReviewCycleCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ActivateReviewCycleHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(ActivateReviewCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.ReviewCycles.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cycle is null)
        {
            return Result.Failure("Review cycle not found.");
        }

        var transition = ReviewCycleWorkflow.EnsureTransition(cycle.Status, ReviewCycleStatus.Active);
        if (transition.IsFailure)
        {
            return transition;
        }

        cycle.Status = ReviewCycleStatus.Active;

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave)
            .Select(e => new { e.Id, e.ManagerId })
            .ToListAsync(cancellationToken);

        var existingReviewEmployeeIds = await _context.PerformanceReviews
            .Where(r => r.ReviewCycleId == cycle.Id)
            .Select(r => r.EmployeeId)
            .ToListAsync(cancellationToken);

        foreach (var employee in employees.Where(e => !existingReviewEmployeeIds.Contains(e.Id)))
        {
            _context.PerformanceReviews.Add(new PerformanceReview
            {
                ReviewCycleId = cycle.Id,
                EmployeeId = employee.Id,
                ManagerEmployeeId = employee.ManagerId,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record CloseReviewCycleCommand(Guid Id) : IRequest<Result>;

public sealed class CloseReviewCycleHandler : IRequestHandler<CloseReviewCycleCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public CloseReviewCycleHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(CloseReviewCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _context.ReviewCycles.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cycle is null)
        {
            return Result.Failure("Review cycle not found.");
        }

        var transition = ReviewCycleWorkflow.EnsureTransition(cycle.Status, ReviewCycleStatus.Closed);
        if (transition.IsFailure)
        {
            return transition;
        }

        cycle.Status = ReviewCycleStatus.Closed;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record GetReviewCyclesQuery(ReviewCycleStatus? Status) : IRequest<Result<IReadOnlyList<ReviewCycleDto>>>;

public sealed class GetReviewCyclesHandler : IRequestHandler<GetReviewCyclesQuery, Result<IReadOnlyList<ReviewCycleDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetReviewCyclesHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<ReviewCycleDto>>> Handle(
        GetReviewCyclesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ReviewCycles.AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        var items = await query
            .OrderByDescending(c => c.StartDate)
            .Select(c => new ReviewCycleDto(
                c.Id, c.Name, c.Type, c.StartDate, c.EndDate, c.Status,
                c.GoalWeightPercent, c.SelfWeightPercent, c.ManagerWeightPercent, c.PeerWeightPercent))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ReviewCycleDto>>(items);
    }

    internal static ReviewCycleDto MapCycle(ReviewCycle c) =>
        new(c.Id, c.Name, c.Type, c.StartDate, c.EndDate, c.Status,
            c.GoalWeightPercent, c.SelfWeightPercent, c.ManagerWeightPercent, c.PeerWeightPercent);
}

public sealed record GetReviewCycleByIdQuery(Guid Id) : IRequest<Result<ReviewCycleDto>>;

public sealed class GetReviewCycleByIdHandler : IRequestHandler<GetReviewCycleByIdQuery, Result<ReviewCycleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReviewCycleByIdHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<ReviewCycleDto>> Handle(
        GetReviewCycleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var cycle = await _context.ReviewCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        return cycle is null
            ? Result.Failure<ReviewCycleDto>("Review cycle not found.")
            : Result.Success(GetReviewCyclesHandler.MapCycle(cycle));
    }
}
