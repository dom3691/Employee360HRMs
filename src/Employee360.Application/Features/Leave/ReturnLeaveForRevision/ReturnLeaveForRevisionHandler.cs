using Employee360.Application.Features.Leave.Common;
using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Leave.ReturnLeaveForRevision;

/// <summary>Handles <see cref="ReturnLeaveForRevisionCommand"/> via the shared decision service.</summary>
public sealed class ReturnLeaveForRevisionHandler
    : IRequestHandler<ReturnLeaveForRevisionCommand, Result>
{
    private readonly LeaveDecisionService _decisionService;

    public ReturnLeaveForRevisionHandler(LeaveDecisionService decisionService)
    {
        _decisionService = decisionService;
    }

    /// <inheritdoc />
    public Task<Result> Handle(ReturnLeaveForRevisionCommand request, CancellationToken cancellationToken)
        => _decisionService.DecideAsync(
            request.RequestId, ApprovalAction.ReturnForRevision, request.Comments, cancellationToken);
}
