using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring leave accrual (FR-LV-002), designed to run daily and be idempotent:
/// <list type="bullet">
/// <item>Ensures every active employee has a current-year balance per paid leave
/// type with a policy, applying carry-forward from the previous year capped at
/// the policy maximum.</item>
/// <item>For monthly-accrual policies, entitlement to date is
/// (annual / 12) × months elapsed; annual policies grant the full amount.</item>
/// </list>
/// </summary>
public class LeaveAccrualJob
{
    private readonly Employee360DbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LeaveAccrualJob> _logger;

    public LeaveAccrualJob(
        Employee360DbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<LeaveAccrualJob> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    /// <summary>Runs one accrual pass for the current WAT date.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var today = _dateTimeProvider.TodayWat;
        var year = today.Year;

        var policies = await _context.LeavePolicies
            .Include(p => p.LeaveType)
            .Where(p => p.LeaveType.IsPaid && p.LeaveType.IsActive)
            .ToListAsync(cancellationToken);

        var activeEmployeeIds = await _context.Employees
            .Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var currentBalances = await _context.LeaveBalances
            .Where(b => b.Year == year)
            .ToListAsync(cancellationToken);
        var currentByKey = currentBalances.ToDictionary(b => (b.EmployeeId, b.LeaveTypeId));

        var previousBalances = await _context.LeaveBalances
            .Where(b => b.Year == year - 1)
            .ToListAsync(cancellationToken);
        var previousByKey = previousBalances.ToDictionary(b => (b.EmployeeId, b.LeaveTypeId));

        var changes = 0;

        foreach (var policy in policies)
        {
            var accruedToDate = AccruedToDate(policy.AnnualEntitlement, policy.AccrualFrequency, today);

            foreach (var employeeId in activeEmployeeIds)
            {
                var key = (employeeId, policy.LeaveTypeId);

                if (!currentByKey.TryGetValue(key, out var balance))
                {
                    balance = new LeaveBalance
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = policy.LeaveTypeId,
                        Year = year,
                        CarriedForward = CarryForward(previousByKey, key, policy.CarryForwardMax),
                    };
                    _context.LeaveBalances.Add(balance);
                    currentByKey[key] = balance;
                }

                // Accrual only raises entitlement (never claws back manual grants).
                if (balance.Entitled < accruedToDate)
                {
                    balance.Entitled = accruedToDate;
                    changes++;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Leave accrual pass complete for {Year}: {PolicyCount} policies, {EmployeeCount} employees, {Changes} entitlement updates",
            year, policies.Count, activeEmployeeIds.Count, changes);
    }

    /// <summary>Entitlement accrued as of <paramref name="today"/> per the frequency.</summary>
    public static decimal AccruedToDate(decimal annualEntitlement, AccrualFrequency frequency, DateOnly today)
        => frequency switch
        {
            AccrualFrequency.Monthly => Math.Round(annualEntitlement / 12m * today.Month, 1),
            _ => annualEntitlement,
        };

    /// <summary>Unused previous-year days capped at the policy maximum (FR-LV-002).</summary>
    private static decimal CarryForward(
        Dictionary<(Guid, Guid), LeaveBalance> previousByKey,
        (Guid, Guid) key,
        decimal carryForwardMax)
    {
        if (carryForwardMax <= 0 || !previousByKey.TryGetValue(key, out var previous))
        {
            return 0m;
        }

        var unused = previous.Entitled + previous.CarriedForward - previous.Used;

        return Math.Max(0m, Math.Min(unused, carryForwardMax));
    }
}
