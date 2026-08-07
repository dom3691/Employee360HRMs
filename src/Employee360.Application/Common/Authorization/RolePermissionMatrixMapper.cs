using Employee360.Domain.Constants;

namespace Employee360.Application.Common.Authorization;

/// <summary>
/// Maps between the UI permission matrix (module × view/create/edit/delete/approve)
/// and the backend's flat permission name catalog.
/// </summary>
public static class RolePermissionMatrixMapper
{
    public static readonly IReadOnlyList<string> ModuleIds =
    [
        "employees", "leave", "payroll", "recruitment", "performance",
        "organization", "administration", "reports",
    ];

    public sealed record ModulePermissionFlags(
        bool View,
        bool Create,
        bool Edit,
        bool Delete,
        bool Approve);

    private static readonly Dictionary<string, ModulePermissionFlags> EmptyMatrix =
        ModuleIds.ToDictionary(m => m, _ => new ModulePermissionFlags(false, false, false, false, false));

    /// <summary>Builds the UI matrix from granted permission names.</summary>
    public static IReadOnlyDictionary<string, ModulePermissionFlags> ToMatrix(IEnumerable<string> permissionNames)
    {
        var granted = new HashSet<string>(permissionNames, StringComparer.Ordinal);
        var matrix = new Dictionary<string, ModulePermissionFlags>(StringComparer.Ordinal);

        foreach (var moduleId in ModuleIds)
        {
            matrix[moduleId] = moduleId switch
            {
                "employees" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Employees.ViewAll, Permissions.Employees.ViewTeam, Permissions.Employees.ViewOwnProfile),
                    Create: granted.Contains(Permissions.Employees.Create),
                    Edit: granted.Contains(Permissions.Employees.Update),
                    Delete: granted.Contains(Permissions.Employees.Deactivate),
                    Approve: false),

                "leave" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Leave.ApproveAll, Permissions.Leave.ViewTeamCalendar, Permissions.Leave.Apply),
                    Create: granted.Contains(Permissions.Leave.Apply),
                    Edit: granted.Contains(Permissions.Leave.Configure),
                    Delete: granted.Contains(Permissions.Leave.Configure),
                    Approve: HasAny(granted, Permissions.Leave.ApproveAll, Permissions.Leave.ApproveTeam)),

                "payroll" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Payroll.ViewOwnPayslips, Permissions.Payroll.ManageSalaryStructures, Permissions.Payroll.Run),
                    Create: granted.Contains(Permissions.Payroll.Run),
                    Edit: granted.Contains(Permissions.Payroll.ManageSalaryStructures),
                    Delete: granted.Contains(Permissions.Payroll.ManageSalaryStructures),
                    Approve: granted.Contains(Permissions.Payroll.Approve)),

                "recruitment" => FlagAll(granted.Contains(Permissions.Recruitment.Manage)),

                "performance" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Performance.Manage, Permissions.Performance.ManageTeamReviews),
                    Create: granted.Contains(Permissions.Performance.Manage),
                    Edit: granted.Contains(Permissions.Performance.Manage),
                    Delete: granted.Contains(Permissions.Performance.Manage),
                    Approve: granted.Contains(Permissions.Performance.Manage)),

                "organization" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Departments.View, Permissions.Positions.View, Permissions.Grades.View),
                    Create: HasAny(granted, Permissions.Departments.Manage, Permissions.Positions.Manage, Permissions.Grades.Manage),
                    Edit: HasAny(granted, Permissions.Departments.Manage, Permissions.Positions.Manage, Permissions.Grades.Manage),
                    Delete: HasAny(granted, Permissions.Departments.Manage, Permissions.Positions.Manage, Permissions.Grades.Manage),
                    Approve: false),

                "administration" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Administration.ViewAuditLogs, Permissions.Administration.SystemConfiguration, Permissions.Administration.ManageRoles),
                    Create: HasAny(granted, Permissions.Administration.ManageRoles, Permissions.Administration.SystemConfiguration),
                    Edit: HasAny(granted, Permissions.Administration.ManageRoles, Permissions.Administration.SystemConfiguration),
                    Delete: granted.Contains(Permissions.Administration.ManageRoles),
                    Approve: false),

                "reports" => new ModulePermissionFlags(
                    View: HasAny(granted, Permissions.Reports.ViewHR, Permissions.Reports.ViewExecutive),
                    Create: false,
                    Edit: false,
                    Delete: false,
                    Approve: false),

                _ => new ModulePermissionFlags(false, false, false, false, false),
            };
        }

        return matrix;
    }

    /// <summary>Converts a UI matrix to the flat permission names to persist.</summary>
    public static IReadOnlyList<string> FromMatrix(IReadOnlyDictionary<string, ModulePermissionFlags>? matrix)
    {
        if (matrix is null || matrix.Count == 0)
        {
            return Array.Empty<string>();
        }

        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (moduleId, flags) in matrix)
        {
            switch (moduleId)
            {
                case "employees":
                    if (flags.View) permissions.Add(Permissions.Employees.ViewAll);
                    if (flags.Create) permissions.Add(Permissions.Employees.Create);
                    if (flags.Edit) permissions.Add(Permissions.Employees.Update);
                    if (flags.Delete) permissions.Add(Permissions.Employees.Deactivate);
                    break;

                case "leave":
                    if (flags.View) permissions.Add(Permissions.Leave.ApproveAll);
                    if (flags.Create) permissions.Add(Permissions.Leave.Apply);
                    if (flags.Edit || flags.Delete) permissions.Add(Permissions.Leave.Configure);
                    if (flags.Approve)
                    {
                        permissions.Add(Permissions.Leave.ApproveAll);
                        permissions.Add(Permissions.Leave.ApproveTeam);
                    }
                    break;

                case "payroll":
                    if (flags.View) permissions.Add(Permissions.Payroll.ManageSalaryStructures);
                    if (flags.Create) permissions.Add(Permissions.Payroll.Run);
                    if (flags.Edit || flags.Delete) permissions.Add(Permissions.Payroll.ManageSalaryStructures);
                    if (flags.Approve) permissions.Add(Permissions.Payroll.Approve);
                    break;

                case "recruitment":
                    if (flags.View || flags.Create || flags.Edit || flags.Delete || flags.Approve)
                    {
                        permissions.Add(Permissions.Recruitment.Manage);
                    }
                    break;

                case "performance":
                    if (flags.View || flags.Approve) permissions.Add(Permissions.Performance.ManageTeamReviews);
                    if (flags.Create || flags.Edit || flags.Delete || flags.Approve)
                    {
                        permissions.Add(Permissions.Performance.Manage);
                    }
                    break;

                case "organization":
                    if (flags.View)
                    {
                        permissions.Add(Permissions.Departments.View);
                        permissions.Add(Permissions.Positions.View);
                        permissions.Add(Permissions.Grades.View);
                    }
                    if (flags.Create || flags.Edit || flags.Delete)
                    {
                        permissions.Add(Permissions.Departments.Manage);
                        permissions.Add(Permissions.Positions.Manage);
                        permissions.Add(Permissions.Grades.Manage);
                    }
                    break;

                case "administration":
                    if (flags.View) permissions.Add(Permissions.Administration.ViewAuditLogs);
                    if (flags.Create || flags.Edit) permissions.Add(Permissions.Administration.SystemConfiguration);
                    if (flags.Edit || flags.Delete) permissions.Add(Permissions.Administration.ManageRoles);
                    break;

                case "reports":
                    if (flags.View)
                    {
                        permissions.Add(Permissions.Reports.ViewHR);
                        permissions.Add(Permissions.Reports.ViewExecutive);
                    }
                    break;
            }
        }

        return permissions.OrderBy(p => p).ToList();
    }

    /// <summary>Returns an empty matrix with all modules present.</summary>
    public static IReadOnlyDictionary<string, ModulePermissionFlags> CreateEmptyMatrix()
        => ModuleIds.ToDictionary(
            m => m,
            m => EmptyMatrix[m],
            StringComparer.Ordinal);

    private static ModulePermissionFlags FlagAll(bool enabled)
        => new(enabled, enabled, enabled, enabled, enabled);

    private static bool HasAny(HashSet<string> granted, params string[] names)
        => names.Any(granted.Contains);
}
