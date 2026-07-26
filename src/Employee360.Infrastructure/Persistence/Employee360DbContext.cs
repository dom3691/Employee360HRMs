using System.Reflection;
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
public class Employee360DbContext : DbContext, IUnitOfWork
{
    public Employee360DbContext(DbContextOptions<Employee360DbContext> options)
        : base(options)
    {
    }

    /// <summary>Immutable audit trail rows (written by the audit interceptor).</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // -----------------------------------------------------------------------
    // DbSets grow batch by batch:
    //   Batch 4+: Employees, Departments, Positions, Grades, EmployeeDocuments
    //   Batch 5+: Users, Roles, Permissions
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
