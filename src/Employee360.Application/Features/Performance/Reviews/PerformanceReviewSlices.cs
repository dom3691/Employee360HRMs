using Employee360.Application.Common.Interfaces;
using Employee360.Application.Features.Performance;
using Employee360.Domain.Common;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Performance.Reviews;

public sealed record PerformanceReviewDto(
    Guid Id,
    Guid ReviewCycleId,
    string CycleName,
    Guid EmployeeId,
    string EmployeeName,
    Guid? ManagerEmployeeId,
    decimal? SelfRating,
    string? SelfComments,
    decimal? ManagerRating,
    string? ManagerComments,
    decimal? FinalRating,
    PerformanceReviewStatus Status,
    DateTime? FinalizedAtUtc);

public sealed record PerformanceHistoryItemDto(
    Guid ReviewCycleId,
    string CycleName,
    ReviewCycleType CycleType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? FinalRating,
    PerformanceReviewStatus Status);

public sealed record SubmitSelfAssessmentCommand(
    Guid ReviewId,
    decimal SelfRating,
    string? SelfComments,
    IReadOnlyList<GoalActualUpdate>? GoalUpdates) : IRequest<Result>;

public sealed record GoalActualUpdate(Guid GoalId, decimal ActualValue);

public sealed class SubmitSelfAssessmentValidator : AbstractValidator<SubmitSelfAssessmentCommand>
{
    public SubmitSelfAssessmentValidator()
    {
        RuleFor(c => c.ReviewId).NotEmpty();
        RuleFor(c => c.SelfRating).InclusiveBetween(1, 5);
    }
}

public sealed class SubmitSelfAssessmentHandler : IRequestHandler<SubmitSelfAssessmentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SubmitSelfAssessmentHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(SubmitSelfAssessmentCommand request, CancellationToken cancellationToken)
    {
        var review = await _context.PerformanceReviews
            .Include(r => r.ReviewCycle)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure("Performance review not found.");
        }

        if (_currentUser.EmployeeId != review.EmployeeId)
        {
            return Result.Failure("You can only submit your own self-assessment.");
        }

        var check = PerformanceReviewWorkflow.EnsureSelfSubmission(
            review.Status, review.ReviewCycle.Status);

        if (check.IsFailure)
        {
            return check;
        }

        if (request.GoalUpdates is not null)
        {
            foreach (var update in request.GoalUpdates)
            {
                var goal = await _context.EmployeeGoals.FirstOrDefaultAsync(
                    g => g.Id == update.GoalId &&
                         g.EmployeeId == review.EmployeeId &&
                         g.ReviewCycleId == review.ReviewCycleId,
                    cancellationToken);

                if (goal is not null)
                {
                    goal.ActualValue = update.ActualValue;
                }
            }
        }

        review.SelfRating = request.SelfRating;
        review.SelfComments = request.SelfComments?.Trim();
        review.SelfSubmittedAtUtc = _clock.UtcNow;
        review.Status = PerformanceReviewStatus.SelfAssessmentSubmitted;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record SubmitManagerReviewCommand(
    Guid ReviewId,
    decimal ManagerRating,
    string? ManagerComments) : IRequest<Result>;

public sealed class SubmitManagerReviewValidator : AbstractValidator<SubmitManagerReviewCommand>
{
    public SubmitManagerReviewValidator()
    {
        RuleFor(c => c.ReviewId).NotEmpty();
        RuleFor(c => c.ManagerRating).InclusiveBetween(1, 5);
    }
}

public sealed class SubmitManagerReviewHandler : IRequestHandler<SubmitManagerReviewCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IManagerScopeService _managerScope;
    private readonly IDateTimeProvider _clock;

    public SubmitManagerReviewHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IManagerScopeService managerScope,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _managerScope = managerScope;
        _clock = clock;
    }

    public async Task<Result> Handle(SubmitManagerReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _context.PerformanceReviews
            .Include(r => r.ReviewCycle)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure("Performance review not found.");
        }

        var managerId = _currentUser.EmployeeId;
        if (managerId is null)
        {
            return Result.Failure("No employee record linked to your account.");
        }

        var isManager = review.ManagerEmployeeId == managerId ||
                        await _managerScope.IsManagerOfAsync(managerId.Value, review.EmployeeId, cancellationToken);

        if (!isManager && !_currentUser.Roles.Contains(RoleNames.HRManager))
        {
            return Result.Failure("You are not authorized to submit a manager review for this employee.");
        }

        var check = PerformanceReviewWorkflow.EnsureManagerSubmission(
            review.Status, review.ReviewCycle.Status);

        if (check.IsFailure)
        {
            return check;
        }

        review.ManagerRating = request.ManagerRating;
        review.ManagerComments = request.ManagerComments?.Trim();
        review.ManagerSubmittedAtUtc = _clock.UtcNow;
        review.Status = PerformanceReviewStatus.ManagerReviewSubmitted;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record FinalizePerformanceReviewCommand(Guid ReviewId) : IRequest<Result<decimal>>;

public sealed class FinalizePerformanceReviewHandler : IRequestHandler<FinalizePerformanceReviewCommand, Result<decimal>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public FinalizePerformanceReviewHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result<decimal>> Handle(
        FinalizePerformanceReviewCommand request,
        CancellationToken cancellationToken)
    {
        var review = await _context.PerformanceReviews
            .Include(r => r.ReviewCycle)
            .Include(r => r.PeerFeedbacks)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure<decimal>("Performance review not found.");
        }

        var check = PerformanceReviewWorkflow.EnsureFinalization(review.Status);
        if (check.IsFailure)
        {
            return Result.Failure<decimal>(check.Error!);
        }

        var goals = await _context.EmployeeGoals
            .AsNoTracking()
            .Where(g => g.ReviewCycleId == review.ReviewCycleId && g.EmployeeId == review.EmployeeId)
            .Select(g => new WeightedRatingCalculator.GoalInput(g.Weight, g.TargetValue, g.ActualValue))
            .ToListAsync(cancellationToken);

        decimal? peerAvg = review.PeerFeedbacks.Count > 0
            ? review.PeerFeedbacks.Average(p => p.Rating)
            : null;

        var calculated = WeightedRatingCalculator.Calculate(new WeightedRatingCalculator.Input(
            review.ReviewCycle.GoalWeightPercent,
            review.ReviewCycle.SelfWeightPercent,
            review.ReviewCycle.ManagerWeightPercent,
            review.ReviewCycle.PeerWeightPercent,
            review.SelfRating,
            review.ManagerRating,
            peerAvg,
            goals));

        review.FinalRating = calculated.WeightedFinalRating;
        review.FinalizedAtUtc = _clock.UtcNow;
        review.Status = PerformanceReviewStatus.Finalized;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(calculated.WeightedFinalRating);
    }
}

public sealed record GetPerformanceReviewsQuery(
    Guid? ReviewCycleId,
    Guid? EmployeeId) : IRequest<Result<IReadOnlyList<PerformanceReviewDto>>>;

public sealed class GetPerformanceReviewsHandler
    : IRequestHandler<GetPerformanceReviewsQuery, Result<IReadOnlyList<PerformanceReviewDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPerformanceReviewsHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<PerformanceReviewDto>>> Handle(
        GetPerformanceReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PerformanceReviews.AsNoTracking();

        if (request.ReviewCycleId.HasValue)
        {
            query = query.Where(r => r.ReviewCycleId == request.ReviewCycleId.Value);
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == request.EmployeeId.Value);
        }

        var items = await query
            .OrderByDescending(r => r.ReviewCycle!.StartDate)
            .Select(r => new PerformanceReviewDto(
                r.Id,
                r.ReviewCycleId,
                r.ReviewCycle!.Name,
                r.EmployeeId,
                r.Employee!.FirstName + " " + r.Employee.LastName,
                r.ManagerEmployeeId,
                r.SelfRating,
                r.SelfComments,
                r.ManagerRating,
                r.ManagerComments,
                r.FinalRating,
                r.Status,
                r.FinalizedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PerformanceReviewDto>>(items);
    }
}

/// <summary>Performance history on employee profile (FR-PERF-005).</summary>
public sealed record GetEmployeePerformanceHistoryQuery(Guid EmployeeId)
    : IRequest<Result<IReadOnlyList<PerformanceHistoryItemDto>>>;

public sealed class GetEmployeePerformanceHistoryHandler
    : IRequestHandler<GetEmployeePerformanceHistoryQuery, Result<IReadOnlyList<PerformanceHistoryItemDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeePerformanceHistoryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<PerformanceHistoryItemDto>>> Handle(
        GetEmployeePerformanceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<PerformanceHistoryItemDto>>("Employee not found.");
        }

        var history = await _context.PerformanceReviews
            .AsNoTracking()
            .Where(r => r.EmployeeId == request.EmployeeId)
            .OrderByDescending(r => r.ReviewCycle!.StartDate)
            .Select(r => new PerformanceHistoryItemDto(
                r.ReviewCycleId,
                r.ReviewCycle!.Name,
                r.ReviewCycle.Type,
                r.ReviewCycle.StartDate,
                r.ReviewCycle.EndDate,
                r.FinalRating,
                r.Status))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PerformanceHistoryItemDto>>(history);
    }
}
