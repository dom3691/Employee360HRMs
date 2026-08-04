using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Daily attendance row per employee (PRD FR-ATT-001..003, FR-ATT-006..008).
/// Unique on (EmployeeId, Date).
/// </summary>
public class AttendanceRecord : AuditableEntity
{
    /// <summary>The employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Business date in WAT.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Clock-in instant (UTC), or null when absent/on leave/holiday.</summary>
    public DateTime? ClockIn { get; set; }

    /// <summary>Clock-out instant (UTC).</summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>Evaluated daily status (FR-ATT-003).</summary>
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    /// <summary>Event source (web, device, manual).</summary>
    public AttendanceSource Source { get; set; } = AttendanceSource.Web;

    /// <summary>Shift applied for status calculation, if any.</summary>
    public Guid? ShiftId { get; set; }

    /// <summary>Navigation to the shift.</summary>
    public Shift? Shift { get; set; }

    /// <summary>Client IP for web clock events (FR-ATT-001).</summary>
    public string? SourceIp { get; set; }

    /// <summary>Device identifier for biometric ingestion (FR-ATT-005).</summary>
    public string? DeviceId { get; set; }

    /// <summary>Mandatory reason when corrected manually (FR-ATT-006).</summary>
    public string? CorrectionReason { get; set; }

    /// <summary>Overtime minutes beyond shift end (FR-ATT-007).</summary>
    public int OvertimeMinutes { get; set; }
}
