using Employee360.Domain.Common;

namespace Employee360.Domain.Entities;

/// <summary>
/// Configurable HTML email template for workflow notifications (PRD FR-ADM-003,
/// Appendix I: EML-001..010). Subject and body support merge tokens such as
/// {EmployeeName} and {Period}.
/// </summary>
public class EmailTemplate : AuditableEntity
{
    /// <summary>Template code, e.g. "EML-001" (unique).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable name, e.g. "Leave submitted".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email subject line (may contain merge tokens).</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>HTML body (may contain merge tokens).</summary>
    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>When false, the system falls back to built-in defaults.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Template category for admin UI grouping.</summary>
    public string Category { get; set; } = "System";

    /// <summary>Short description of when this template is sent.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>JSON array of merge token names, e.g. ["{{employeeName}}"].</summary>
    public string? VariablesJson { get; set; }
}
