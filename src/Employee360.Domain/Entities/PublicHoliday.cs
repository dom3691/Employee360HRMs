using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>Company public holiday (PRD FR-ADM-002); excluded from working days.</summary>
public class PublicHoliday : AuditableEntity
{
    /// <summary>The holiday date (unique).</summary>
    public DateOnly Date { get; set; }

    /// <summary>Holiday name, e.g. "Independence Day".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Calendar year (denormalized for fast lookups).</summary>
    public int Year { get; set; }

    /// <summary>National, Religious, or Regional.</summary>
    public string? HolidayType { get; set; }

    /// <summary>When true, the holiday repeats annually on the same calendar date.</summary>
    public bool IsRecurring { get; set; }

    /// <summary>Optional region scope (null = nationwide).</summary>
    public string? Region { get; set; }
}
