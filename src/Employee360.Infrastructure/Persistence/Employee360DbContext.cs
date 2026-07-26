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

    /// <summary>Employee records (minimal shape; expanded by the Batch 5 slices).</summary>
    public DbSet<Employee> Employees => Set<Employee>();

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

    // -----------------------------------------------------------------------
    // DbSets grow batch by batch:
    //   Batch 5+: Departments, Positions, Grades, EmployeeDocuments
    //   Batch 6+: LeaveTypes, LeavePolicies, LeaveBalances, LeaveRequests
    //   Phase 2:  AttendanceRecords, Shifts, PayrollRuns, Payslips
    // -----------------------------------------------------------------------

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
