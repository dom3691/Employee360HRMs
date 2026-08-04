using Employee360.Application.Features.Performance.Feedback;
using Employee360.Application.Features.Performance.Goals;
using Employee360.Application.Features.Performance.ReviewCycles;
using Employee360.Application.Features.Performance.Reviews;
using Employee360.Domain.Constants;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee360.API.Controllers;

/// <summary>Performance review cycles and reviews (FR-PERF-001..006).</summary>
[Route("api/v1/reviews")]
public sealed class ReviewsController : ApiControllerBase
{
    // --- Review cycles ---

    [HttpGet("cycles")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewCycleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCycles(
        [FromQuery] ReviewCycleStatus? status,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetReviewCyclesQuery(status), cancellationToken));

    [HttpGet("cycles/{id:guid}")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(typeof(ReviewCycleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCycle([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetReviewCycleByIdQuery(id), cancellationToken));

    [HttpPost("cycles")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfigureCycle(
        [FromBody] ConfigureReviewCycleCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpPut("cycles/{id:guid}")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCycle(
        [FromRoute] Guid id,
        [FromBody] UpdateCycleRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new UpdateReviewCycleCommand(
                id, body.Name, body.StartDate, body.EndDate,
                body.GoalWeightPercent, body.SelfWeightPercent,
                body.ManagerWeightPercent, body.PeerWeightPercent),
            cancellationToken));

    [HttpPost("cycles/{id:guid}/activate")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ActivateCycle([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new ActivateReviewCycleCommand(id), cancellationToken));

    [HttpPost("cycles/{id:guid}/close")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CloseCycle([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new CloseReviewCycleCommand(id), cancellationToken));

    // --- Reviews ---

    [HttpGet]
    [HasPermission(Permissions.Performance.ManageTeamReviews)]
    [ProducesResponseType(typeof(IReadOnlyList<PerformanceReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReviews(
        [FromQuery] Guid? reviewCycleId,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetPerformanceReviewsQuery(reviewCycleId, employeeId), cancellationToken));

    [HttpPost("{id:guid}/self-assessment")]
    [HasPermission(Permissions.Employees.ViewOwnProfile)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitSelfAssessment(
        [FromRoute] Guid id,
        [FromBody] SelfAssessmentRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new SubmitSelfAssessmentCommand(id, body.SelfRating, body.SelfComments, body.GoalUpdates),
            cancellationToken));

    [HttpPost("{id:guid}/manager-review")]
    [HasPermission(Permissions.Performance.ManageTeamReviews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitManagerReview(
        [FromRoute] Guid id,
        [FromBody] ManagerReviewRequest body,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new SubmitManagerReviewCommand(id, body.ManagerRating, body.ManagerComments),
            cancellationToken));

    [HttpPost("{id:guid}/finalize")]
    [HasPermission(Permissions.Performance.Manage)]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<IActionResult> FinalizeReview([FromRoute] Guid id, CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new FinalizePerformanceReviewCommand(id), cancellationToken));

    [HttpGet("employees/{employeeId:guid}/history")]
    [HasPermission(Permissions.Performance.ManageTeamReviews)]
    [ProducesResponseType(typeof(IReadOnlyList<PerformanceHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PerformanceHistory(
        [FromRoute] Guid employeeId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetEmployeePerformanceHistoryQuery(employeeId), cancellationToken));

    public sealed record UpdateCycleRequest(
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        decimal GoalWeightPercent,
        decimal SelfWeightPercent,
        decimal ManagerWeightPercent,
        decimal PeerWeightPercent);

    public sealed record SelfAssessmentRequest(
        decimal SelfRating,
        string? SelfComments,
        IReadOnlyList<GoalActualUpdate>? GoalUpdates);

    public sealed record ManagerReviewRequest(decimal ManagerRating, string? ManagerComments);
}

/// <summary>Employee goals endpoints (FR-PERF-002).</summary>
[Route("api/v1/goals")]
public sealed class GoalsController : ApiControllerBase
{
    [HttpGet("my")]
    [HasPermission(Permissions.Employees.ViewOwnProfile)]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyGoals(
        [FromQuery] Guid? reviewCycleId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(new GetMyGoalsQuery(reviewCycleId), cancellationToken));

    [HttpGet]
    [HasPermission(Permissions.Performance.ManageTeamReviews)]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeGoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(
        [FromQuery] Guid reviewCycleId,
        [FromQuery] Guid employeeId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetEmployeeGoalsQuery(reviewCycleId, employeeId), cancellationToken));

    [HttpPut]
    [HasPermission(Permissions.Performance.ManageTeamReviews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetGoals(
        [FromBody] SetEmployeeGoalsCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));
}

/// <summary>360 peer feedback endpoints (FR-PERF-006 Should).</summary>
[Route("api/v1/feedback")]
public sealed class FeedbackController : ApiControllerBase
{
    [HttpPost]
    [HasPermission(Permissions.Employees.ViewOwnProfile)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitPeerFeedback(
        [FromBody] SubmitPeerFeedbackCommand command,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(command, cancellationToken));

    [HttpGet("{performanceReviewId:guid}/aggregate")]
    [HasPermission(Permissions.Performance.ManageTeamReviews)]
    [ProducesResponseType(typeof(PeerFeedbackAggregateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAggregate(
        [FromRoute] Guid performanceReviewId,
        CancellationToken cancellationToken)
        => FromResult(await Sender.Send(
            new GetPeerFeedbackAggregateQuery(performanceReviewId), cancellationToken));
}
