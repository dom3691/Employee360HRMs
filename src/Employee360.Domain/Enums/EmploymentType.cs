namespace Employee360.Domain.Enums;

/// <summary>
/// Employment engagement types (PRD FR-EMP-003 specifies full-time, part-time,
/// contract; Internship and NYSC added for common Nigerian workforce categories —
/// extension beyond the literal PRD, tracked as a spec note).
/// </summary>
public enum EmploymentType
{
    /// <summary>Standard full-time employment.</summary>
    FullTime = 0,

    /// <summary>Part-time employment.</summary>
    PartTime = 1,

    /// <summary>Fixed-term or contractor engagement.</summary>
    Contract = 2,

    /// <summary>Student or graduate internship.</summary>
    Internship = 3,

    /// <summary>National Youth Service Corps placement.</summary>
    Nysc = 4,
}
