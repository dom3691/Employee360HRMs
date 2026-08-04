using Employee360.Domain.Common;
using Employee360.Domain.Enums;

namespace Employee360.Domain.Entities;

/// <summary>
/// Core employee master record (PRD FR-EMP-001..006, FR-EMP-013/014).
/// Sensitive fields (NIN) are AES-256 encrypted at rest and masked in responses
/// (NFR-SEC-004); bank details live on <see cref="EmployeeBankAccount"/>.
/// </summary>
public class Employee : AuditableEntity, ISoftDelete
{
    /// <summary>Unique human-readable code, e.g. "EMP-00001" (FR-EMP-001).</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>First name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Middle name (optional).</summary>
    public string? MiddleName { get; set; }

    /// <summary>Last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Work email (unique among active employees).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Date of birth.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Gender (FR-EMP-002).</summary>
    public Gender Gender { get; set; } = Gender.PreferNotToSay;

    /// <summary>Marital status (FR-EMP-002).</summary>
    public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Single;

    /// <summary>Nationality, default context Nigerian.</summary>
    public string? Nationality { get; set; }

    /// <summary>National Identification Number — AES-256 encrypted at rest (NFR-SEC-004).</summary>
    public string? NinEncrypted { get; set; }

    /// <summary>Residential address.</summary>
    public string? Address { get; set; }

    /// <summary>Assigned department (FR-EMP-003).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Navigation to the department.</summary>
    public Department? Department { get; set; }

    /// <summary>Assigned position / job title (FR-EMP-003).</summary>
    public Guid? PositionId { get; set; }

    /// <summary>Navigation to the position.</summary>
    public Position? Position { get; set; }

    /// <summary>The employee's line manager, or null for the top of the hierarchy.</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Navigation to the line manager.</summary>
    public Employee? Manager { get; set; }

    /// <summary>Direct reports (inverse of <see cref="Manager"/>).</summary>
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    /// <summary>Engagement type (FR-EMP-003).</summary>
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    /// <summary>Date the employee joined.</summary>
    public DateOnly? JoinDate { get; set; }

    /// <summary>Work location (office name, "Remote", "Hybrid").</summary>
    public string? WorkLocation { get; set; }

    /// <summary>Lifecycle status (PRD FR-EMP-004).</summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Draft;

    /// <summary>Pension PIN issued by PFA (FR-PAY-013).</summary>
    public string? PensionPin { get; set; }

    /// <summary>Pension Fund Administrator name (FR-PAY-013).</summary>
    public string? PfaName { get; set; }

    /// <summary>Bank account for payroll (FR-EMP-014), 1:1.</summary>
    public EmployeeBankAccount? BankAccount { get; set; }

    /// <summary>Salary assignment history (FR-PAY-002).</summary>
    public ICollection<EmployeeSalary> Salaries { get; set; } = new List<EmployeeSalary>();

    /// <summary>Custom payroll deductions (FR-PAY-007).</summary>
    public ICollection<PayrollDeduction> PayrollDeductions { get; set; } = new List<PayrollDeduction>();

    /// <summary>Emergency contact and next of kin (FR-EMP-013).</summary>
    public ICollection<EmployeeContact> Contacts { get; set; } = new List<EmployeeContact>();

    /// <summary>Uploaded documents (FR-EMP-007).</summary>
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <summary>Full display name.</summary>
    public string FullName => string.Join(' ',
        new[] { FirstName, MiddleName, LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
