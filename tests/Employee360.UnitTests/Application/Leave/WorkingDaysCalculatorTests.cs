using Employee360.Application.Common.Services;
using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Application.Leave;

/// <summary>Tests for <see cref="WorkingDaysCalculator"/> (FR-LV-003).</summary>
public class WorkingDaysCalculatorTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"wdays-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    [Fact]
    public async Task FullWeek_ShouldCountFiveWorkingDays()
    {
        await using var context = CreateContext();
        var calculator = new WorkingDaysCalculator(context);

        // Mon 6 Jul 2026 – Sun 12 Jul 2026.
        var days = await calculator.CalculateAsync(new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 12));

        days.Should().Be(5m);
    }

    [Fact]
    public async Task RangeWithPublicHoliday_ShouldExcludeIt()
    {
        await using var context = CreateContext();
        // Independence Day: Thu 1 Oct 2026.
        context.PublicHolidays.Add(new PublicHoliday
        {
            Date = new DateOnly(2026, 10, 1),
            Name = "Independence Day",
            Year = 2026,
        });
        await context.SaveChangesAsync();
        var calculator = new WorkingDaysCalculator(context);

        // Mon 28 Sep – Fri 2 Oct 2026: 5 weekdays minus the holiday.
        var days = await calculator.CalculateAsync(new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 2));

        days.Should().Be(4m);
    }

    [Fact]
    public async Task HolidayOnWeekend_ShouldNotDoubleDiscount()
    {
        await using var context = CreateContext();
        // Holiday falling on Saturday 4 Jul 2026.
        context.PublicHolidays.Add(new PublicHoliday
        {
            Date = new DateOnly(2026, 7, 4),
            Name = "Weekend Holiday",
            Year = 2026,
        });
        await context.SaveChangesAsync();
        var calculator = new WorkingDaysCalculator(context);

        // Wed 1 Jul – Tue 7 Jul: weekdays are 1,2,3,6,7 = 5 (weekend holiday already excluded).
        var days = await calculator.CalculateAsync(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 7));

        days.Should().Be(5m);
    }

    [Fact]
    public async Task SingleWorkingDay_ShouldCountOne()
    {
        await using var context = CreateContext();
        var calculator = new WorkingDaysCalculator(context);

        var days = await calculator.CalculateAsync(new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 6));

        days.Should().Be(1m);
    }

    [Fact]
    public async Task WeekendOnly_ShouldCountZero()
    {
        await using var context = CreateContext();
        var calculator = new WorkingDaysCalculator(context);

        var days = await calculator.CalculateAsync(new DateOnly(2026, 7, 11), new DateOnly(2026, 7, 12));

        days.Should().Be(0m);
    }

    [Fact]
    public async Task EndBeforeStart_ShouldCountZero()
    {
        await using var context = CreateContext();
        var calculator = new WorkingDaysCalculator(context);

        var days = await calculator.CalculateAsync(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 6));

        days.Should().Be(0m);
    }
}
