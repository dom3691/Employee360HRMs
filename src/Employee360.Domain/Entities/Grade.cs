using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Salary grade / level (PRD data model: Grade). Positions reference a grade;
/// the salary band feeds payroll validation in Phase 2.
/// </summary>
public class Grade : AuditableEntity, ISoftDelete
{
    /// <summary>Grade name, e.g. "Officer II" (unique).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Numeric level for ordering (unique; higher = more senior).</summary>
    public int Level { get; set; }

    /// <summary>Minimum annual gross salary for the band (₦).</summary>
    public decimal MinSalary { get; set; }

    /// <summary>Maximum annual gross salary for the band (₦).</summary>
    public decimal MaxSalary { get; set; }

    /// <summary>Short unique code, e.g. "G5".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display rank label, e.g. "Level 5".</summary>
    public string LevelRank { get; set; } = string.Empty;

    /// <summary>Optional description of the grade band.</summary>
    public string? Description { get; set; }

    /// <summary>Positions assigned to this grade.</summary>
    public ICollection<Position> Positions { get; set; } = new List<Position>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
