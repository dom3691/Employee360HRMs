using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Constants;
using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee360.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent seeder for the permission catalog, the seven system roles, and the
/// role-permission grants from the PRD RBAC matrix (Appendix A). Safe to run on
/// every startup: it only adds missing rows and never removes custom grants.
/// </summary>
public sealed class IdentityDataSeeder : IDataSeeder
{
    private readonly Employee360DbContext _context;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(Employee360DbContext context, ILogger<IdentityDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Role → permission grants per the PRD RBAC matrix (Appendix A).
    /// Spec note: Payroll.ViewOwnPayslips is granted to all employee-type roles;
    /// the PRD matrix shows it only for Payroll Officer, which is a matrix typo —
    /// employees viewing their own payslips is FR-PAY employee self-service.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> RolePermissionMatrix =
        new Dictionary<string, string[]>
        {
            [RoleNames.Employee] =
            [
                Permissions.Employees.ViewOwnProfile,
                Permissions.Employees.EditOwnProfile,
                Permissions.Documents.ViewOwn,
                Permissions.Leave.Apply,
                Permissions.Attendance.ClockInOut,
                Permissions.Payroll.ViewOwnPayslips,
            ],
            [RoleNames.LineManager] =
            [
                Permissions.Employees.ViewOwnProfile,
                Permissions.Employees.EditOwnProfile,
                Permissions.Employees.ViewTeam,
                Permissions.Documents.ViewOwn,
                Permissions.Leave.Apply,
                Permissions.Leave.ApproveTeam,
                Permissions.Leave.ViewTeamCalendar,
                Permissions.Attendance.ClockInOut,
                Permissions.Attendance.ViewTeam,
                Permissions.Payroll.ViewOwnPayslips,
                Permissions.Performance.ManageTeamReviews,
            ],
            [RoleNames.HRAdmin] =
            [
                Permissions.Employees.ViewOwnProfile,
                Permissions.Employees.EditOwnProfile,
                Permissions.Employees.ViewTeam,
                Permissions.Employees.ViewAll,
                Permissions.Employees.Create,
                Permissions.Employees.Update,
                Permissions.Employees.Deactivate,
                Permissions.Departments.View,
                Permissions.Departments.Manage,
                Permissions.Positions.View,
                Permissions.Positions.Manage,
                Permissions.Grades.View,
                Permissions.Grades.Manage,
                Permissions.Documents.ViewOwn,
                Permissions.Documents.ManageAll,
                Permissions.Leave.Apply,
                Permissions.Leave.ApproveTeam,
                Permissions.Leave.ApproveAll,
                Permissions.Leave.Configure,
                Permissions.Leave.AdjustBalances,
                Permissions.Leave.ViewTeamCalendar,
                Permissions.Attendance.ClockInOut,
                Permissions.Attendance.ViewTeam,
                Permissions.Attendance.Manage,
                Permissions.Payroll.ViewOwnPayslips,
                Permissions.Recruitment.Manage,
                Permissions.Performance.ManageTeamReviews,
                Permissions.Performance.Manage,
                Permissions.Reports.ViewHR,
                Permissions.Administration.SystemConfiguration,
            ],
            [RoleNames.HRManager] =
            [
                Permissions.Employees.ViewOwnProfile,
                Permissions.Employees.EditOwnProfile,
                Permissions.Employees.ViewTeam,
                Permissions.Employees.ViewAll,
                Permissions.Employees.Create,
                Permissions.Employees.Update,
                Permissions.Employees.Deactivate,
                Permissions.Departments.View,
                Permissions.Departments.Manage,
                Permissions.Positions.View,
                Permissions.Positions.Manage,
                Permissions.Grades.View,
                Permissions.Grades.Manage,
                Permissions.Documents.ViewOwn,
                Permissions.Documents.ManageAll,
                Permissions.Leave.Apply,
                Permissions.Leave.ApproveTeam,
                Permissions.Leave.ApproveAll,
                Permissions.Leave.Configure,
                Permissions.Leave.AdjustBalances,
                Permissions.Leave.ViewTeamCalendar,
                Permissions.Attendance.ClockInOut,
                Permissions.Attendance.ViewTeam,
                Permissions.Attendance.Manage,
                Permissions.Payroll.ViewOwnPayslips,
                Permissions.Payroll.Run,
                Permissions.Payroll.Approve,
                Permissions.Recruitment.Manage,
                Permissions.Performance.ManageTeamReviews,
                Permissions.Performance.Manage,
                Permissions.Reports.ViewHR,
                Permissions.Reports.ViewExecutive,
                Permissions.Administration.ViewAuditLogs,
            ],
            [RoleNames.PayrollOfficer] =
            [
                Permissions.Employees.ViewOwnProfile,
                Permissions.Employees.EditOwnProfile,
                Permissions.Employees.ViewAll,
                Permissions.Documents.ViewOwn,
                Permissions.Leave.Apply,
                Permissions.Attendance.ClockInOut,
                Permissions.Payroll.ViewOwnPayslips,
                Permissions.Payroll.ManageSalaryStructures,
                Permissions.Payroll.Run,
                Permissions.Payroll.Approve,
                Permissions.Payroll.ExportBankFile,
                Permissions.Reports.ViewHR,
            ],
            [RoleNames.SystemAdmin] =
            [
                Permissions.Administration.ManageRoles,
                Permissions.Administration.ViewAuditLogs,
                Permissions.Administration.SystemConfiguration,
            ],
            [RoleNames.Executive] =
            [
                Permissions.Reports.ViewExecutive,
            ],
        };

    private static readonly IReadOnlyDictionary<string, string> RoleDescriptions =
        new Dictionary<string, string>
        {
            [RoleNames.Employee] = "Self-service access: own profile, leave, documents, and payslips.",
            [RoleNames.LineManager] = "Team management: approvals, team calendar, attendance, and reviews for direct/indirect reports.",
            [RoleNames.HRAdmin] = "HR operations: employee records, org structure, leave configuration, and HR reports.",
            [RoleNames.HRManager] = "HR leadership: all HR Admin capabilities plus payroll approval, analytics, and audit visibility.",
            [RoleNames.PayrollOfficer] = "Payroll processing: salary structures, payroll runs, payslips, and bank files.",
            [RoleNames.SystemAdmin] = "Technical administration: roles, permissions, configuration, and audit logs. No HR data access by default.",
            [RoleNames.Executive] = "Read-only leadership dashboards and workforce analytics.",
        };

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);

        _logger.LogInformation("Identity seed data applied (roles, permissions, RBAC matrix)");
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Permissions
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var missing = Employee360.Domain.Constants.Permissions.GetAll()
            .Where(name => !existingSet.Contains(name))
            .Select(name => new Permission
            {
                Name = name,
                Module = name.Split('.')[0],
                Description = $"Allows: {name}",
            })
            .ToList();

        if (missing.Count > 0)
        {
            await _context.Permissions.AddRangeAsync(missing, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Roles
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var missing = RoleNames.All
            .Where(name => !existingSet.Contains(name))
            .Select(name => new Role
            {
                Name = name,
                Description = RoleDescriptions[name],
                IsSystemRole = true,
            })
            .ToList();

        if (missing.Count > 0)
        {
            await _context.Roles.AddRangeAsync(missing, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Where(r => r.IsSystemRole)
            .ToDictionaryAsync(r => r.Name, cancellationToken);

        var permissions = await _context.Permissions
            .ToDictionaryAsync(p => p.Name, cancellationToken);

        var existingGrants = await _context.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken);
        var existingSet = existingGrants
            .Select(g => (g.RoleId, g.PermissionId))
            .ToHashSet();

        var missing = new List<RolePermission>();

        foreach (var (roleName, permissionNames) in RolePermissionMatrix)
        {
            var role = roles[roleName];

            foreach (var permissionName in permissionNames)
            {
                var permission = permissions[permissionName];

                if (!existingSet.Contains((role.Id, permission.Id)))
                {
                    missing.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                    });
                }
            }
        }

        if (missing.Count > 0)
        {
            await _context.RolePermissions.AddRangeAsync(missing, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
