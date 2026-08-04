using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>Onboarding checklist item for a new hire (FR-REC-006).</summary>
public class OnboardingTask : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string TaskName { get; set; } = string.Empty;
    public Guid AssignedToEmployeeId { get; set; }
    public Employee AssignedToEmployee { get; set; } = null!;
    public DateOnly DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
