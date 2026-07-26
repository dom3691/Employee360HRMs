using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.GetLeaveBalances;

/// <summary>Handles <see cref="GetLeaveBalancesQuery"/>.</summary>
public sealed class GetLeaveBalancesHandler
    : IRequestHandler<GetLeaveBalancesQuery, Result<IReadOnlyList<LeaveBalanceItem>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLeaveBalancesHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LeaveBalanceItem>>> Handle(
        GetLeaveBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId ?? _currentUserService.EmployeeId;

        if (employeeId is null)
        {
            return Result.Failure<IReadOnlyList<LeaveBalanceItem>>(
                "No employee record is linked to your account.");
        }

        var balances = await _context.LeaveBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId.Value && b.Year == request.Year)
            .OrderBy(b => b.LeaveType.Name)
            .Select(b => new LeaveBalanceItem(
                b.LeaveTypeId,
                b.LeaveType.Name,
                b.Year,
                b.Entitled,
                b.Used,
                b.Pending,
                b.CarriedForward,
                b.Entitled + b.CarriedForward - b.Used - b.Pending))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LeaveBalanceItem>>(balances);
    }
}
