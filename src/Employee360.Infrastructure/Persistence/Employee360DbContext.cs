using System.Reflection;
using Employee360.Application.Common.Interfaces;
using Employee360.Domain.Entities;
using Employee360.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Infrastructure.Persistence;

/// <summary>
/// The Employee360 EF Core context. Acts as the Unit of Work: repositories stage
/// changes and a single <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
/// commits them atomically. Audit columns, soft deletes, and audit-log rows are
/// applied by <see cref="Interceptors.AuditableEntityInterceptor"/>.
/// </summary>
public class Employee360DbContext : DbContext, IUnitOfWork, IApplicationDbContext
{
    public Employee360DbContext(DbContextOptions<Employee360DbContext> options)
        : base(options)
    {
    }

    /// <summary>Immutable audit trail rows (written by the audit interceptor).</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Employee master records.</summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Departments.</summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>Positions / job titles.</summary>
    public DbSet<Position> Positions => Set<Position>();

    /// <summary>Salary grades / levels.</summary>
    public DbSet<Grade> Grades => Set<Grade>();

    /// <summary>Employee payroll bank accounts.</summary>
    public DbSet<EmployeeBankAccount> EmployeeBankAccounts => Set<EmployeeBankAccount>();

    /// <summary>Emergency contacts / next of kin.</summary>
    public DbSet<EmployeeContact> EmployeeContacts => Set<EmployeeContact>();

    /// <summary>Employee document metadata.</summary>
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    /// <summary>Profile change requests awaiting HR review.</summary>
    public DbSet<ProfileChangeRequest> ProfileChangeRequests => Set<ProfileChangeRequest>();

    /// <summary>Authentication accounts.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>RBAC roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Granular permission catalog.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Role ↔ permission grants.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>User ↔ role assignments.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Issued refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Password reset tokens.</summary>
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    /// <summary>Leave categories.</summary>
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    /// <summary>Leave accrual/carry-forward policies.</summary>
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();

    /// <summary>Per-employee, per-type, per-year balances.</summary>
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();

    /// <summary>Leave applications.</summary>
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    /// <summary>Approval step history.</summary>
    public DbSet<LeaveApproval> LeaveApprovals => Set<LeaveApproval>();

    /// <summary>Public holiday calendar.</summary>
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    /// <summary>In-app notifications.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    // -----------------------------------------------------------------------
    // DbSets grow batch by batch:
    //   Phase 2: AttendanceRecords, Shifts, PayrollRuns, Payslips
    // -----------------------------------------------------------------------

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
