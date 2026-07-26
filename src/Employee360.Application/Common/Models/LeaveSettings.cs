namespace Employee360.Application.Common.Models;

/// <summary>Leave module settings ("Leave" configuration section).</summary>
public sealed class LeaveSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Leave";

    /// <summary>Hours a request may stay pending before escalation (FR-LV-009, default 48).</summary>
    public int EscalationSlaHours { get; init; } = 48;
}
