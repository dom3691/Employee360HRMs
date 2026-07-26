namespace Employee360.Domain.Enums;

/// <summary>
/// Actions an approver can take on a workflow item (PRD FR-LV-008).
/// </summary>
public enum ApprovalAction
{
    /// <summary>Approve the request and advance the workflow.</summary>
    Approve = 0,

    /// <summary>Reject the request and end the workflow.</summary>
    Reject = 1,

    /// <summary>Return the request to the submitter for changes.</summary>
    ReturnForRevision = 2,
}
