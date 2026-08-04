using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Payroll bank details (PRD FR-EMP-014). The account number is AES-256 encrypted
/// at rest and masked to the last 4 digits in all responses (NFR-SEC-004).
/// </summary>
public class EmployeeBankAccount : AuditableEntity
{
    /// <summary>Owning employee (1:1).</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Navigation to the owning employee.</summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>Bank name, e.g. "GTBank".</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>NIBSS bank code for payment file export (FR-PAY-010).</summary>
    public string? BankCode { get; set; }

    /// <summary>NUBAN account number — AES-256 encrypted at rest.</summary>
    public string AccountNumberEncrypted { get; set; } = string.Empty;

    /// <summary>Account holder name as held by the bank.</summary>
    public string AccountName { get; set; } = string.Empty;
}
