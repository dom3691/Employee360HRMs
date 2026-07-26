using System.Reflection;

namespace Employee360.Domain.Constants;

/// <summary>
/// Canonical catalog of granular permissions (module.action), covering every module
/// in the PRD (Section 8 + Appendix A RBAC matrix). Names are stored in the
/// Permissions table, embedded as "permission" claims in JWTs, and referenced by
/// the [HasPermission] attribute.
/// </summary>
public static class Permissions
{
    /// <summary>Employee master data (PRD FR-EMP).</summary>
    public static class Employees
    {
        public const string ViewOwnProfile = "Employees.ViewOwnProfile";
        public const string EditOwnProfile = "Employees.EditOwnProfile";
        public const string ViewTeam = "Employees.ViewTeam";
        public const string ViewAll = "Employees.ViewAll";
        public const string Create = "Employees.Create";
        public const string Update = "Employees.Update";
        public const string Deactivate = "Employees.Deactivate";
    }

    /// <summary>Departments and org structure (PRD FR-EMP-009).</summary>
    public static class Departments
    {
        public const string View = "Departments.View";
        public const string Manage = "Departments.Manage";
    }

    /// <summary>Positions / job titles (PRD FR-EMP-010).</summary>
    public static class Positions
    {
        public const string View = "Positions.View";
        public const string Manage = "Positions.Manage";
    }

    /// <summary>Employee documents (PRD FR-EMP-007).</summary>
    public static class Documents
    {
        public const string ViewOwn = "Documents.ViewOwn";
        public const string ManageAll = "Documents.ManageAll";
    }

    /// <summary>Leave management (PRD FR-LV).</summary>
    public static class Leave
    {
        public const string Apply = "Leave.Apply";
        public const string ApproveTeam = "Leave.ApproveTeam";
        public const string ApproveAll = "Leave.ApproveAll";
        public const string Configure = "Leave.Configure";
        public const string AdjustBalances = "Leave.AdjustBalances";
        public const string ViewTeamCalendar = "Leave.ViewTeamCalendar";
    }

    /// <summary>Attendance and time (PRD FR-ATT, Phase 2).</summary>
    public static class Attendance
    {
        public const string ClockInOut = "Attendance.ClockInOut";
        public const string ViewTeam = "Attendance.ViewTeam";
        public const string Manage = "Attendance.Manage";
    }

    /// <summary>Payroll (PRD FR-PAY, Phase 2).</summary>
    public static class Payroll
    {
        public const string ViewOwnPayslips = "Payroll.ViewOwnPayslips";
        public const string ManageSalaryStructures = "Payroll.ManageSalaryStructures";
        public const string Run = "Payroll.Run";
        public const string Approve = "Payroll.Approve";
        public const string ExportBankFile = "Payroll.ExportBankFile";
    }

    /// <summary>Reports and analytics (PRD FR-RPT, Phase 2).</summary>
    public static class Reports
    {
        public const string ViewHR = "Reports.ViewHR";
        public const string ViewExecutive = "Reports.ViewExecutive";
    }

    /// <summary>Recruitment and onboarding (PRD FR-REC, Phase 3).</summary>
    public static class Recruitment
    {
        public const string Manage = "Recruitment.Manage";
    }

    /// <summary>Performance management (PRD FR-PERF, Phase 3).</summary>
    public static class Performance
    {
        public const string ManageTeamReviews = "Performance.ManageTeamReviews";
        public const string Manage = "Performance.Manage";
    }

    /// <summary>System administration (PRD FR-ADM).</summary>
    public static class Administration
    {
        public const string ManageRoles = "Administration.ManageRoles";
        public const string ViewAuditLogs = "Administration.ViewAuditLogs";
        public const string SystemConfiguration = "Administration.SystemConfiguration";
    }

    /// <summary>
    /// Returns every permission name declared in this catalog (used by the seeder
    /// and admin UI). Enumerated via reflection over the nested classes' constants.
    /// </summary>
    public static IReadOnlyList<string> GetAll() =>
        typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
}
