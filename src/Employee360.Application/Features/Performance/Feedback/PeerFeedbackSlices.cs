using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Performance.Feedback;

/// <summary>
/// Aggregated 360 feedback — no reviewer identity exposed (FR-PERF-006 Should).
/// </summary>
public sealed record PeerFeedbackAggregateDto(
    int ResponseCount,
    decimal AverageRating,
    IReadOnlyList<string> SampleComments);

public sealed record SubmitPeerFeedbackCommand(
    Guid PerformanceReviewId,
    decimal Rating,
    string? Comments) : IRequest<Result>;

public sealed class SubmitPeerFeedbackValidator : AbstractValidator<SubmitPeerFeedbackCommand>
{
    public SubmitPeerFeedbackValidator()
    {
        RuleFor(c => c.PerformanceReviewId).NotEmpty();
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
    }
}

public sealed class SubmitPeerFeedbackHandler : IRequestHandler<SubmitPeerFeedbackCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitPeerFeedbackHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SubmitPeerFeedbackCommand request, CancellationToken cancellationToken)
    {
        var reviewerId = _currentUser.EmployeeId;
        if (reviewerId is null)
        {
            return Result.Failure("No employee record linked to your account.");
        }

        var review = await _context.PerformanceReviews
            .Include(r => r.ReviewCycle)
            .FirstOrDefaultAsync(r => r.Id == request.PerformanceReviewId, cancellationToken);

        if (review is null)
        {
            return Result.Failure("Performance review not found.");
        }

        if (review.ReviewCycle.Status != ReviewCycleStatus.Active)
        {
            return Result.Failure("Peer feedback is only accepted during an active review cycle.");
        }

        if (review.EmployeeId == reviewerId.Value)
        {
            return Result.Failure("You cannot submit peer feedback for your own review.");
        }

        var alreadySubmitted = await _context.PeerFeedbacks.AnyAsync(
            f => f.PerformanceReviewId == request.PerformanceReviewId &&
                 f.ReviewerEmployeeId == reviewerId.Value,
            cancellationToken);

        if (alreadySubmitted)
        {
            return Result.Failure("You have already submitted feedback for this review.");
        }

        _context.PeerFeedbacks.Add(new PeerFeedback
        {
            PerformanceReviewId = request.PerformanceReviewId,
            ReviewerEmployeeId = reviewerId.Value,
            Rating = request.Rating,
            Comments = request.Comments?.Trim(),
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record GetPeerFeedbackAggregateQuery(Guid PerformanceReviewId)
    : IRequest<Result<PeerFeedbackAggregateDto>>;

public sealed class GetPeerFeedbackAggregateHandler
    : IRequestHandler<GetPeerFeedbackAggregateQuery, Result<PeerFeedbackAggregateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPeerFeedbackAggregateHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<PeerFeedbackAggregateDto>> Handle(
        GetPeerFeedbackAggregateQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await _context.PerformanceReviews.AnyAsync(
            r => r.Id == request.PerformanceReviewId, cancellationToken);

        if (!exists)
        {
            return Result.Failure<PeerFeedbackAggregateDto>("Performance review not found.");
        }

        var feedbacks = await _context.PeerFeedbacks
            .AsNoTracking()
            .Where(f => f.PerformanceReviewId == request.PerformanceReviewId)
            .Select(f => new { f.Rating, f.Comments })
            .ToListAsync(cancellationToken);

        if (feedbacks.Count == 0)
        {
            return Result.Success(new PeerFeedbackAggregateDto(0, 0m, Array.Empty<string>()));
        }

        var avg = Math.Round(
            feedbacks.Average(f => f.Rating), 2, MidpointRounding.AwayFromZero);

        var comments = feedbacks
            .Where(f => !string.IsNullOrWhiteSpace(f.Comments))
            .Select(f => f.Comments!)
            .Take(5)
            .ToList();

        return Result.Success(new PeerFeedbackAggregateDto(feedbacks.Count, avg, comments));
    }
}
