using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Features.Leave.AdjustLeaveBalance;

/// <summary>
/// Handles <see cref="AdjustLeaveBalanceCommand"/>: applies the delta (creating
/// the balance row if needed) and writes an explicit AuditLog entry with the
/// reason (FR-LV-012).
/// </summary>
public sealed class AdjustLeaveBalanceHandler : IRequestHandler<AdjustLeaveBalanceCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AdjustLeaveBalanceHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AdjustLeaveBalanceCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            return Result.Failure("Employee not found.");
        }

        if (!await _context.LeaveTypes.AnyAsync(t => t.Id == request.LeaveTypeId, cancellationToken))
        {
            return Result.Failure("Leave type not found.");
        }

        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(
            b => b.EmployeeId == request.EmployeeId &&
                 b.LeaveTypeId == request.LeaveTypeId &&
                 b.Year == request.Year,
            cancellationToken);

        if (balance is null)
        {
            balance = new LeaveBalance
            {
                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,
                Year = request.Year,
            };
            _context.LeaveBalances.Add(balance);
        }

        var oldEntitled = balance.Entitled;
        var newEntitled = balance.Entitled + request.AdjustmentDays;

        if (newEntitled < 0)
        {
            return Result.Failure("Adjustment would make the entitled balance negative.");
        }

        balance.Entitled = newEntitled;

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(LeaveBalance),
            EntityId = balance.Id.ToString(),
            Action = "BalanceAdjusted",
            OldValues = $$"""{"entitled":{{oldEntitled}}}""",
            NewValues = $$"""{"entitled":{{newEntitled}},"adjustment":{{request.AdjustmentDays}},"reason":"{{request.Reason.Replace("\"", "'")}}"}""",
            UserId = _currentUserService.UserId,
            Timestamp = _dateTimeProvider.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
