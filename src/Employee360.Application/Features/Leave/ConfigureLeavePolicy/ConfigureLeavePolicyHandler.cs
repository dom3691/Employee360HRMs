using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.ConfigureLeavePolicy;

/// <summary>Handles <see cref="ConfigureLeavePolicyCommand"/> (upsert per leave type).</summary>
public sealed class ConfigureLeavePolicyHandler : IRequestHandler<ConfigureLeavePolicyCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ConfigureLeavePolicyHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ConfigureLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await _context.LeaveTypes
            .Include(t => t.Policy)
            .FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId, cancellationToken);

        if (leaveType is null)
        {
            return Result.Failure("Leave type not found.");
        }

        if (leaveType.Policy is null)
        {
            _context.LeavePolicies.Add(new LeavePolicy
            {
                LeaveTypeId = leaveType.Id,
                AnnualEntitlement = request.AnnualEntitlement,
                AccrualFrequency = request.AccrualFrequency,
                CarryForwardMax = request.CarryForwardMax,
                ProbationMonths = request.ProbationMonths,
            });
        }
        else
        {
            leaveType.Policy.AnnualEntitlement = request.AnnualEntitlement;
            leaveType.Policy.AccrualFrequency = request.AccrualFrequency;
            leaveType.Policy.CarryForwardMax = request.CarryForwardMax;
            leaveType.Policy.ProbationMonths = request.ProbationMonths;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
