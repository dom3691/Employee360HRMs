using Employee360.Application.Common.Validation;
using FluentValidation;

namespace Employee360.Application.Features.Leave.GetLeaveRequests;

/// <summary>Input validation for <see cref="GetMyLeaveRequestsQuery"/>.</summary>
public sealed class GetMyLeaveRequestsValidator : AbstractValidator<GetMyLeaveRequestsQuery>
{
    public GetMyLeaveRequestsValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}

/// <summary>Input validation for <see cref="GetApprovalQueueQuery"/>.</summary>
public sealed class GetApprovalQueueValidator : AbstractValidator<GetApprovalQueueQuery>
{
    public GetApprovalQueueValidator()
    {
        RuleFor(q => q.Page).ValidPage();
        RuleFor(q => q.PageSize).ValidPageSize();
    }
}
