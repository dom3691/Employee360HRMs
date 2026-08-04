using Employee360.Domain.Entities;
using Employee360.Domain.Enums;

namespace Employee360.Application.Common.Services;

/// <summary>
/// Pure status evaluation logic (FR-ATT-003): Present, Absent, Late, Half-Day,
/// On Leave, Holiday. Used by clock handlers and the daily calculation job.
/// </summary>
public static class AttendanceStatusCalculator
{
    /// <summary>
    /// Evaluates daily attendance status from clock times and contextual flags.
    /// </summary>
    /// <param name="clockInUtc">Clock-in instant (UTC), if any.</param>
    /// <param name="clockOutUtc">Clock-out instant (UTC), if any.</param>
    /// <param name="shift">Applicable shift, or null for non-shift staff.</param>
    /// <param name="date">Business date in WAT.</param>
    /// <param name="isHoliday">True when the date is a public holiday.</param>
    /// <param name="isOnLeave">True when covered by approved leave.</param>
    /// <param name="halfDayThresholdPercent">Minimum % of expected duration (default 50).</param>
    /// <param name="evaluationEndUtc">
    /// When clock-out is missing, the end instant used to estimate worked time
    /// (typically shift end or current time).
    /// </param>
    public static AttendanceStatus CalculateStatus(
        DateTime? clockInUtc,
        DateTime? clockOutUtc,
        Shift? shift,
        DateOnly date,
        bool isHoliday,
        bool isOnLeave,
        int halfDayThresholdPercent = 50,
        DateTime? evaluationEndUtc = null)
    {
        if (isHoliday)
        {
            return AttendanceStatus.Holiday;
        }

        if (isOnLeave)
        {
            return AttendanceStatus.OnLeave;
        }

        if (clockInUtc is null)
        {
            return AttendanceStatus.Absent;
        }

        if (shift is null)
        {
            return AttendanceStatus.Present;
        }

        var clockInWat = ToWatTime(clockInUtc.Value);
        var graceEnd = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GraceMinutes));

        var expectedMinutes = ExpectedWorkMinutes(shift);
        var workedMinutes = WorkedMinutes(clockInUtc, clockOutUtc, shift, date, evaluationEndUtc);

        if (expectedMinutes > 0 &&
            workedMinutes < expectedMinutes * halfDayThresholdPercent / 100.0)
        {
            return AttendanceStatus.HalfDay;
        }

        if (clockInWat > graceEnd)
        {
            return AttendanceStatus.Late;
        }

        return AttendanceStatus.Present;
    }

    /// <summary>
    /// Calculates overtime minutes beyond shift end (FR-ATT-007).
    /// </summary>
    public static int CalculateOvertimeMinutes(
        DateTime? clockInUtc,
        DateTime? clockOutUtc,
        Shift shift,
        DateOnly date,
        int overtimeThresholdMinutes = 0)
    {
        if (clockInUtc is null || clockOutUtc is null)
        {
            return 0;
        }

        var shiftEndUtc = ToUtc(date, shift.EndTime);
        var overtimeStart = shiftEndUtc.AddMinutes(overtimeThresholdMinutes);

        if (clockOutUtc.Value <= overtimeStart)
        {
            return 0;
        }

        return (int)Math.Floor((clockOutUtc.Value - overtimeStart).TotalMinutes);
    }

    /// <summary>Expected productive minutes for a shift (end - start - break).</summary>
    public static double ExpectedWorkMinutes(Shift shift)
    {
        var span = shift.EndTime.ToTimeSpan() - shift.StartTime.ToTimeSpan();

        if (span.TotalMinutes <= 0)
        {
            span = span.Add(TimeSpan.FromHours(24));
        }

        return Math.Max(0, span.TotalMinutes - shift.BreakMinutes);
    }

    /// <summary>Actual worked minutes capped at shift window.</summary>
    public static double WorkedMinutes(
        DateTime? clockInUtc,
        DateTime? clockOutUtc,
        Shift shift,
        DateOnly date,
        DateTime? evaluationEndUtc)
    {
        if (clockInUtc is null)
        {
            return 0;
        }

        var endUtc = clockOutUtc
            ?? evaluationEndUtc
            ?? ToUtc(date, shift.EndTime);

        if (endUtc <= clockInUtc.Value)
        {
            return 0;
        }

        var rawMinutes = (endUtc - clockInUtc.Value).TotalMinutes;
        return Math.Max(0, rawMinutes - shift.BreakMinutes);
    }

    /// <summary>Converts a UTC instant to WAT <see cref="TimeOnly"/>.</summary>
    public static TimeOnly ToWatTime(DateTime utcInstant)
    {
        var wat = utcInstant.Kind == DateTimeKind.Utc
            ? utcInstant.AddHours(1)
            : DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc).AddHours(1);

        return TimeOnly.FromDateTime(wat);
    }

    /// <summary>Combines a WAT business date and local time into UTC.</summary>
    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        return DateTime.SpecifyKind(local.AddHours(-1), DateTimeKind.Utc);
    }
}
