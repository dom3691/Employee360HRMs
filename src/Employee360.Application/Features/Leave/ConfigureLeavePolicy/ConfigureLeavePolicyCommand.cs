using Employee360.Domain.Common;
using Employee360.Domain.Enums;
using MediatR;

namespace Employee360.Application.Features.Leave.ConfigureLeavePolicy;

/// <summary>Creates or updates the policy for a leave type (FR-LV-002).</summary>
public sealed record ConfigureLeavePolicyCommand(
    Guid LeaveTypeId,
    decimal AnnualEntitlement,
    AccrualFrequency AccrualFrequency,
    decimal CarryForwardMax,
    int ProbationMonths) : IRequest<Result>;
