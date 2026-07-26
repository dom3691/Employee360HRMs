namespace Employee360.Domain.Enums;

/// <summary>
/// Marital status as captured on the employee record (PRD FR-EMP-002).
/// </summary>
public enum MaritalStatus
{
    /// <summary>Single / never married.</summary>
    Single = 0,

    /// <summary>Married.</summary>
    Married = 1,

    /// <summary>Divorced.</summary>
    Divorced = 2,

    /// <summary>Widowed.</summary>
    Widowed = 3,

    /// <summary>Legally separated.</summary>
    Separated = 4,
}
