namespace Employee360.Application.Features.Reports;

/// <summary>
/// Canonical report identifiers (FR-RPT-001..015 + dashboards).
/// </summary>
public static class ReportIds
{
    public const string HeadcountSummary = "headcount-summary";       // RPT-001
    public const string HeadcountTrend = "headcount-trend";           // RPT-002
    public const string Attrition = "attrition";                       // RPT-003
    public const string LeaveBalance = "leave-balance";               // RPT-004
    public const string LeaveUtilization = "leave-utilization";       // RPT-005
    public const string LeaveHistory = "leave-history";               // RPT-006
    public const string AttendanceSummary = "attendance-summary";     // RPT-007
    public const string PayrollRegister = "payroll-register";         // RPT-008
    public const string PayrollCostByDepartment = "payroll-cost-by-department"; // RPT-009
    public const string PayeSchedule = "paye-schedule";               // RPT-010
    public const string PensionSchedule = "pension-schedule";         // RPT-011
    public const string NhfSchedule = "nhf-schedule";                 // RPT-012
    public const string EmployeeMasterList = "employee-master-list";  // RPT-014
    public const string BirthdayAnniversary = "birthday-anniversary"; // RPT-015
    public const string ExecutiveDashboard = "executive-dashboard";
    public const string HrDashboard = "hr-dashboard";
    public const string PayrollDashboard = "payroll-dashboard";

    public static IReadOnlyList<string> All { get; } =
    [
        HeadcountSummary, HeadcountTrend, Attrition,
        LeaveBalance, LeaveUtilization, LeaveHistory,
        AttendanceSummary,
        PayrollRegister, PayrollCostByDepartment, PayeSchedule, PensionSchedule, NhfSchedule,
        EmployeeMasterList, BirthdayAnniversary,
        ExecutiveDashboard, HrDashboard, PayrollDashboard,
    ];

    public static bool IsValid(string reportId) =>
        All.Contains(reportId, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Supported export formats for report endpoints.</summary>
public static class ReportFormats
{
    public const string Json = "json";
    public const string Csv = "csv";
    public const string Excel = "xlsx";

    public static bool IsValid(string? format) =>
        string.IsNullOrWhiteSpace(format) ||
        format.Equals(Json, StringComparison.OrdinalIgnoreCase) ||
        format.Equals(Csv, StringComparison.OrdinalIgnoreCase) ||
        format.Equals(Excel, StringComparison.OrdinalIgnoreCase);
}
