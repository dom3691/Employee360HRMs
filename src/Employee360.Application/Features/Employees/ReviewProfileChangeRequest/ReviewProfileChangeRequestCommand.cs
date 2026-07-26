using Employee360.Domain.Common;
using MediatR;

namespace Employee360.Application.Features.Employees.ReviewProfileChangeRequest;

/// <summary>
/// HR review of a pending profile change request (FR-EMP-011): approval applies
/// the requested changes to the employee record; rejection requires a comment.
/// </summary>
/// <param name="RequestId">The change request to review.</param>
/// <param name="Approve">True to approve and apply; false to reject.</param>
/// <param name="Comment">Reviewer comment (required when rejecting).</param>
public sealed record ReviewProfileChangeRequestCommand(
    Guid RequestId,
    bool Approve,
    string? Comment) : IRequest<Result>;
