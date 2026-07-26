using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>Leave category, e.g. Annual, Sick, Maternity (PRD FR-LV-001).</summary>
public class LeaveType : AuditableEntity, ISoftDelete
{
    /// <summary>Display name (unique), e.g. "Annual Leave".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique code, e.g. "ANN".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>False for unpaid leave — no balance is enforced.</summary>
    public bool IsPaid { get; set; } = true;

    /// <summary>True when applications must carry an attachment (e.g. sick note).</summary>
    public bool RequiresAttachment { get; set; }

    /// <summary>Calendar display color (hex), e.g. "#2E7D32".</summary>
    public string? Color { get; set; }

    /// <summary>Inactive types cannot be applied for.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Accrual/carry-forward rules (1:1, FR-LV-002).</summary>
    public LeavePolicy? Policy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
