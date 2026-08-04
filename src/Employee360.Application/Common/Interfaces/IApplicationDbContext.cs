using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Employee360.Application.Common.Interfaces;

/// <summary>
/// Query/persistence abstraction over the EF Core context for Application handlers
/// that need rich queries (joins, includes) beyond the generic repository.
/// Implemented by Employee360DbContext; SaveChangesAsync triggers auditing.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Employee records.</summary>
    DbSet<Employee> Employees { get; }

    /// <summary>Departments.</summary>
    DbSet<Department> Departments { get; }

    /// <summary>Positions / job titles.</summary>
    DbSet<Position> Positions { get; }

    /// <summary>Salary grades / levels.</summary>
    DbSet<Grade> Grades { get; }

    /// <summary>Employee payroll bank accounts.</summary>
    DbSet<EmployeeBankAccount> EmployeeBankAccounts { get; }

    /// <summary>Emergency contacts / next of kin.</summary>
    DbSet<EmployeeContact> EmployeeContacts { get; }

    /// <summary>Employee document metadata.</summary>
    DbSet<EmployeeDocument> EmployeeDocuments { get; }

    /// <summary>Profile change requests awaiting HR review.</summary>
    DbSet<ProfileChangeRequest> ProfileChangeRequests { get; }

    /// <summary>Leave categories.</summary>
    DbSet<LeaveType> LeaveTypes { get; }

    /// <summary>Leave accrual/carry-forward policies.</summary>
    DbSet<LeavePolicy> LeavePolicies { get; }

    /// <summary>Per-employee, per-type, per-year balances.</summary>
    DbSet<LeaveBalance> LeaveBalances { get; }

    /// <summary>Leave applications.</summary>
    DbSet<LeaveRequest> LeaveRequests { get; }

    /// <summary>Approval step history.</summary>
    DbSet<LeaveApproval> LeaveApprovals { get; }

    /// <summary>Public holiday calendar.</summary>
    DbSet<PublicHoliday> PublicHolidays { get; }

    /// <summary>In-app notifications.</summary>
    DbSet<Notification> Notifications { get; }

    /// <summary>Authentication accounts.</summary>
    DbSet<User> Users { get; }

    /// <summary>RBAC roles.</summary>
    DbSet<Role> Roles { get; }

    /// <summary>Granular permission catalog.</summary>
    DbSet<Permission> Permissions { get; }

    /// <summary>Role ↔ permission grants.</summary>
    DbSet<RolePermission> RolePermissions { get; }

    /// <summary>User ↔ role assignments.</summary>
    DbSet<UserRole> UserRoles { get; }

    /// <summary>Issued refresh tokens.</summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>Password reset tokens.</summary>
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    /// <summary>Immutable audit trail rows.</summary>
    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>Organization company profile (singleton).</summary>
    DbSet<Company> Companies { get; }

    /// <summary>Configurable workflow email templates.</summary>
    DbSet<EmailTemplate> EmailTemplates { get; }

    /// <summary>Work shift definitions.</summary>
    DbSet<Shift> Shifts { get; }

    /// <summary>Daily attendance records.</summary>
    DbSet<AttendanceRecord> AttendanceRecords { get; }

    /// <summary>Weekly timesheets for non-shift staff.</summary>
    DbSet<Timesheet> Timesheets { get; }

    /// <summary>Salary structure templates (FR-PAY-001).</summary>
    DbSet<SalaryStructure> SalaryStructures { get; }

    /// <summary>Employee salary assignments (FR-PAY-002).</summary>
    DbSet<EmployeeSalary> EmployeeSalaries { get; }

    /// <summary>Configurable PAYE tax bands (FR-PAY-003).</summary>
    DbSet<TaxBand> TaxBands { get; }

    /// <summary>Statutory rate configuration (FR-PAY-004..006).</summary>
    DbSet<PayrollStatutoryRate> PayrollStatutoryRates { get; }

    /// <summary>Custom employee deductions (FR-PAY-007).</summary>
    DbSet<PayrollDeduction> PayrollDeductions { get; }

    /// <summary>Payroll run workflow records (FR-PAY-008..011).</summary>
    DbSet<PayrollRun> PayrollRuns { get; }

    /// <summary>Employee payslips per payroll run (FR-PAY-009).</summary>
    DbSet<Payslip> Payslips { get; }

    /// <summary>Open job requisitions (FR-REC-001).</summary>
    DbSet<JobPosting> JobPostings { get; }

    /// <summary>Recruitment pipeline candidates (FR-REC-002).</summary>
    DbSet<Candidate> Candidates { get; }

    /// <summary>Onboarding checklist tasks (FR-REC-006).</summary>
    DbSet<OnboardingTask> OnboardingTasks { get; }

    /// <summary>Performance review cycles (FR-PERF-001).</summary>
    DbSet<ReviewCycle> ReviewCycles { get; }

    /// <summary>Employee goals per review cycle (FR-PERF-002).</summary>
    DbSet<EmployeeGoal> EmployeeGoals { get; }

    /// <summary>Performance reviews (FR-PERF-003..005).</summary>
    DbSet<PerformanceReview> PerformanceReviews { get; }

    /// <summary>360 peer feedback (FR-PERF-006).</summary>
    DbSet<PeerFeedback> PeerFeedbacks { get; }

    /// <summary>Commits staged changes atomically (audit rules applied).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
