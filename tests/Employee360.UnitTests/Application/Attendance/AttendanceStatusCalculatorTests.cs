using Employee360.Application.Common.Services;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Attendance;

/// <summary>Tests for daily status evaluation (FR-ATT-003) including late vs grace.</summary>
public class AttendanceStatusCalculatorTests
{
    private static Shift DayShift => new()
    {
        Name = "Day",
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(17, 0),
        GraceMinutes = 15,
        BreakMinutes = 60,
    };

    private static readonly DateOnly Date = new(2026, 8, 4);

    [Fact]
    public void CalculateStatus_ClockInWithinGrace_ReturnsPresent()
    {
        var clockIn = AttendanceStatusCalculator.ToUtc(Date, new TimeOnly(9, 10));
        var clockOut = AttendanceStatusCalculator.ToUtc(Date, new TimeOnly(17, 0));

        var status = AttendanceStatusCalculator.CalculateStatus(
            clockIn, clockOut, DayShift, Date, isHoliday: false, isOnLeave: false);

        status.Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public void CalculateStatus_ClockInAfterGrace_ReturnsLate()
    {
        var clockIn = AttendanceStatusCalculator.ToUtc(Date, new TimeOnly(9, 20));
        var clockOut = AttendanceStatusCalculator.ToUtc(Date, new TimeOnly(17, 0));

        var status = AttendanceStatusCalculator.CalculateStatus(
            clockIn, clockOut, DayShift, Date, isHoliday: false, isOnLeave: false);

        status.Should().Be(AttendanceStatus.Late);
    }

    [Fact]
    public void CalculateStatus_ApprovedLeave_ReturnsOnLeave()
    {
        var status = AttendanceStatusCalculator.CalculateStatus(
            clockInUtc: null,
            clockOutUtc: null,
            shift: DayShift,
            date: Date,
            isHoliday: false,
            isOnLeave: true);

        status.Should().Be(AttendanceStatus.OnLeave);
    }

    [Fact]
    public void CalculateStatus_PublicHoliday_ReturnsHoliday()
    {
        var status = AttendanceStatusCalculator.CalculateStatus(
            clockInUtc: null,
            clockOutUtc: null,
            shift: DayShift,
            date: Date,
            isHoliday: true,
            isOnLeave: false);

        status.Should().Be(AttendanceStatus.Holiday);
    }

    [Fact]
    public void CalculateOvertimeMinutes_WorkedPastShiftEnd_ReturnsOvertime()
    {
        var clockIn = AttendanceStatusCalculator.ToUtc(Date, new TimeOnly(9, 0));
        var clockOut = AttendanceStatusCalculator.ToUtc(Date, new TimeOnly(18, 30));

        var overtime = AttendanceStatusCalculator.CalculateOvertimeMinutes(
            clockIn, clockOut, DayShift, Date, overtimeThresholdMinutes: 0);

        overtime.Should().Be(90);
    }
}
