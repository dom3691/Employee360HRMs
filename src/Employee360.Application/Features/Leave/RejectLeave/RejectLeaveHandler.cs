using Employee360.Application.Features.Leave.Common;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Leave.RejectLeave;

/// <summary>Handles <see cref="RejectLeaveCommand"/> via the shared decision service.</summary>
public sealed class RejectLeaveHandler : IRequestHandler<RejectLeaveCommand, Result>
{
    private readonly LeaveDecisionService _decisionService;

    public RejectLeaveHandler(LeaveDecisionService decisionService)
    {
        _decisionService = decisionService;
    }

    /// <inheritdoc />
    public Task<Result> Handle(RejectLeaveCommand request, CancellationToken cancellationToken)
        => _decisionService.DecideAsync(
            request.RequestId, ApprovalAction.Reject, request.Comments, cancellationToken);
}
