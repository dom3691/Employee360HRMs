using Employee360.Application.Features.Leave.Common;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Leave.ApproveLeave;

/// <summary>Handles <see cref="ApproveLeaveCommand"/> via the shared decision service.</summary>
public sealed class ApproveLeaveHandler : IRequestHandler<ApproveLeaveCommand, Result>
{
    private readonly LeaveDecisionService _decisionService;

    public ApproveLeaveHandler(LeaveDecisionService decisionService)
    {
        _decisionService = decisionService;
    }

    /// <inheritdoc />
    public Task<Result> Handle(ApproveLeaveCommand request, CancellationToken cancellationToken)
        => _decisionService.DecideAsync(
            request.RequestId, ApprovalAction.Approve, request.Comments, cancellationToken);
}
