namespace Employee360.Domain.Constants;

/// <summary>Canonical email template codes from PRD Appendix I (EML-001..010).</summary>
public static class EmailTemplateCodes
{
    public const string LeaveSubmitted = "EML-001";
    public const string LeaveApproved = "EML-002";
    public const string LeaveRejected = "EML-003";
    public const string LeaveEscalated = "EML-004";
    public const string WelcomeAccountCreated = "EML-005";
    public const string PasswordReset = "EML-006";
    public const string PayslipAvailable = "EML-007";
    public const string PayrollPendingApproval = "EML-008";
    public const string ProfileChangeApproved = "EML-009";
    public const string OnboardingTaskAssigned = "EML-010";
    public const string OfferLetter = "EML-011";

    /// <summary>All template codes in catalog order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        LeaveSubmitted,
        LeaveApproved,
        LeaveRejected,
        LeaveEscalated,
        WelcomeAccountCreated,
        PasswordReset,
        PayslipAvailable,
        PayrollPendingApproval,
        ProfileChangeApproved,
        OnboardingTaskAssigned,
        OfferLetter,
    ];
}
