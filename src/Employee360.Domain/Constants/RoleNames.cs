namespace Employee360.Domain.Constants;

/// <summary>
/// The seven system roles defined by the PRD (Section 6, User Personas &amp; Roles).
/// Seeded at startup and marked <c>IsSystemRole</c>.
/// </summary>
public static class RoleNames
{
    public const string Employee = "Employee";
    public const string LineManager = "LineManager";
    public const string HRAdmin = "HRAdmin";
    public const string HRManager = "HRManager";
    public const string PayrollOfficer = "PayrollOfficer";
    public const string SystemAdmin = "SystemAdmin";
    public const string Executive = "Executive";

    /// <summary>All system role names, in seeding order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Employee, LineManager, HRAdmin, HRManager, PayrollOfficer, SystemAdmin, Executive,
    ];
}
