namespace Employee360.Domain.Enums;

/// <summary>Profile change request workflow states (PRD FR-EMP-011).</summary>
public enum ProfileChangeRequestStatus
{
    /// <summary>Submitted and awaiting HR review.</summary>
    Pending = 0,

    /// <summary>Approved; changes applied to the employee record.</summary>
    Approved = 1,

    /// <summary>Rejected with a reason.</summary>
    Rejected = 2,
}
