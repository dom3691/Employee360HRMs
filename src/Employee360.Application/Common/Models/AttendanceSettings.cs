namespace Employee360.Application.Common.Models;

/// <summary>Attendance module settings (FR-ATT-003, FR-ATT-007).</summary>
public sealed class AttendanceSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Attendance";

    /// <summary>
    /// Minimum percentage of expected shift duration required to avoid Half-Day
    /// (default 50).
    /// </summary>
    public int HalfDayThresholdPercent { get; init; } = 50;

    /// <summary>
    /// Minutes worked beyond scheduled end (after break) before overtime accrues
    /// (FR-ATT-007, default 0).
    /// </summary>
    public int OvertimeThresholdMinutes { get; init; } = 0;

    /// <summary>API key for biometric device ingestion (FR-ATT-005).</summary>
    public string DeviceIntegrationApiKey { get; init; } = string.Empty;
}
