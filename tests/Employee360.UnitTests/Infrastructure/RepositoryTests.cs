using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using Employee360.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>
/// Tests for the generic <see cref="Repository{T}"/> over the real
/// <see cref="Employee360DbContext"/> (in-memory provider), using
/// <see cref="AuditLog"/> as the persisted aggregate.
/// </summary>
public class RepositoryTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"repo-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static AuditLog NewLog(string entityName = "Employee", string action = "Created") => new()
    {
        EntityName = entityName,
        EntityId = Guid.NewGuid().ToString(),
        Action = action,
        Timestamp = DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_ThenSave_ShouldPersistEntity()
    {
        await using var context = CreateContext();
        var repository = new Repository<AuditLog>(context);
        var log = NewLog();

        await repository.AddAsync(log);
        await context.SaveChangesAsync();

        var found = await repository.GetByIdAsync(log.Id);
        found.Should().NotBeNull();
        found!.EntityName.Should().Be("Employee");
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ShouldReturnNull()
    {
        await using var context = CreateContext();
        var repository = new Repository<AuditLog>(context);

        var found = await repository.GetByIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_ShouldFilterByPredicate()
    {
        await using var context = CreateContext();
        var repository = new Repository<AuditLog>(context);
        await repository.AddRangeAsync([
            NewLog(action: "Created"),
            NewLog(action: "Updated"),
            NewLog(action: "Created"),
        ]);
        await context.SaveChangesAsync();

        var created = await repository.FindAsync(l => l.Action == "Created");

        created.Should().HaveCount(2);
    }

    [Fact]
    public async Task CountAsync_And_AnyAsync_ShouldReflectStore()
    {
        await using var context = CreateContext();
        var repository = new Repository<AuditLog>(context);
        await repository.AddAsync(NewLog(entityName: "Department"));
        await context.SaveChangesAsync();

        (await repository.CountAsync()).Should().Be(1);
        (await repository.AnyAsync(l => l.EntityName == "Department")).Should().BeTrue();
        (await repository.AnyAsync(l => l.EntityName == "Payslip")).Should().BeFalse();
    }

    [Fact]
    public async Task Remove_ThenSave_ShouldDeleteNonSoftDeletableEntity()
    {
        await using var context = CreateContext();
        var repository = new Repository<AuditLog>(context);
        var log = NewLog();
        await repository.AddAsync(log);
        await context.SaveChangesAsync();

        repository.Remove(log);
        await context.SaveChangesAsync();

        (await repository.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UnitOfWork_SaveChangesAsync_ShouldReturnAffectedCount()
    {
        await using var context = CreateContext();
        var repository = new Repository<AuditLog>(context);
        await repository.AddAsync(NewLog());

        var affected = await ((Employee360DbContext)context).SaveChangesAsync();

        affected.Should().Be(1);
    }
}
