using Employee360.Domain.Common;
using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>
/// Test-only auditable, soft-deletable entity for exercising the interceptor
/// and query-filter behavior before real entities exist (Batch 4+).
/// </summary>
public class TestEmployeeRecord : AuditableEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// In-memory context mirroring <see cref="Employee360DbContext"/> composition:
/// same soft-delete filter extension and an AuditLogs set for the interceptor.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestEmployeeRecord> Records => Set<TestEmployeeRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEmployeeRecord>().Ignore(e => e.DomainEvents);
        modelBuilder.Entity<AuditLog>().Ignore(e => e.DomainEvents);

        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
