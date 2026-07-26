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

    /// <summary>Employee payroll bank accounts.</summary>
    DbSet<EmployeeBankAccount> EmployeeBankAccounts { get; }

    /// <summary>Emergency contacts / next of kin.</summary>
    DbSet<EmployeeContact> EmployeeContacts { get; }

    /// <summary>Employee document metadata.</summary>
    DbSet<EmployeeDocument> EmployeeDocuments { get; }

    /// <summary>Profile change requests awaiting HR review.</summary>
    DbSet<ProfileChangeRequest> ProfileChangeRequests { get; }

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

    /// <summary>Commits staged changes atomically (audit rules applied).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
