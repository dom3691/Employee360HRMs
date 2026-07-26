using Employee360.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Default <see cref="IWorkingDaysCalculator"/>: Monday–Friday are working days;
/// public holidays are excluded (a holiday falling on a weekend is not
/// double-counted). PRD FR-LV-003.
/// </summary>
public sealed class WorkingDaysCalculator : IWorkingDaysCalculator
{
    private readonly IApplicationDbContext _context;

    public WorkingDaysCalculator(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            return 0m;
        }

        var holidays = (await _context.PublicHolidays
                .AsNoTracking()
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .Select(h => h.Date)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var workingDays = 0m;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            if (!isWeekend && !holidays.Contains(date))
            {
                workingDays++;
            }
        }

        return workingDays;
    }
}
