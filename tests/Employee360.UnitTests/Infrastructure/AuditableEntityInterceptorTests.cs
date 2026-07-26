using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Infrastructure;

/// <summary>
/// Tests for <see cref="AuditableEntityInterceptor"/>: audit column population,
/// soft-delete conversion, and audit-log row generation.
/// </summary>
public class AuditableEntityInterceptorTests
{
    private static readonly Guid UserId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly DateTime FixedUtcNow = new(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc);

    private static TestDbContext CreateContext()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(s => s.UserId).Returns(UserId);
        currentUser.SetupGet(s => s.Email).Returns("hr.admin@employee360.ng");

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(FixedUtcNow);

        var interceptor = new AuditableEntityInterceptor(currentUser.Object, clock.Object);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"audit-tests-{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task Adding_ShouldPopulateCreatedAuditColumns()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Adaeze Okafor" };

        context.Records.Add(record);
        await context.SaveChangesAsync();

        record.CreatedBy.Should().Be("hr.admin@employee360.ng");
        record.CreatedAt.Should().Be(FixedUtcNow);
        record.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Modifying_ShouldPopulateModifiedAuditColumns()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Adaeze Okafor" };
        context.Records.Add(record);
        await context.SaveChangesAsync();

        record.Name = "Adaeze Okafor-Eze";
        await context.SaveChangesAsync();

        record.ModifiedBy.Should().Be("hr.admin@employee360.ng");
        record.ModifiedAt.Should().Be(FixedUtcNow);
    }

    [Fact]
    public async Task Deleting_SoftDeletable_ShouldConvertToSoftDelete()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Adaeze Okafor" };
        context.Records.Add(record);
        await context.SaveChangesAsync();

        context.Records.Remove(record);
        await context.SaveChangesAsync();

        record.IsDeleted.Should().BeTrue();
        record.DeletedAt.Should().Be(FixedUtcNow);

        // Row still exists in the store when filters are bypassed.
        var stored = await context.Records.IgnoreQueryFilters()
            .SingleAsync(r => r.Id == record.Id);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleted_ShouldBeExcludedByGlobalQueryFilter()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Adaeze Okafor" };
        context.Records.Add(record);
        await context.SaveChangesAsync();

        context.Records.Remove(record);
        await context.SaveChangesAsync();

        var visible = await context.Records.ToListAsync();

        visible.Should().BeEmpty();
    }

    [Fact]
    public async Task Adding_ShouldWriteCreatedAuditLog()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Adaeze Okafor" };

        context.Records.Add(record);
        await context.SaveChangesAsync();

        var log = await context.AuditLogs.SingleAsync();
        log.EntityName.Should().Be(nameof(TestEmployeeRecord));
        log.EntityId.Should().Be(record.Id.ToString());
        log.Action.Should().Be("Created");
        log.OldValues.Should().BeNull();
        log.NewValues.Should().Contain("Adaeze Okafor");
        log.UserId.Should().Be(UserId);
        log.Timestamp.Should().Be(FixedUtcNow);
    }

    [Fact]
    public async Task Modifying_ShouldWriteUpdatedAuditLog_WithOnlyChangedColumns()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Old Name" };
        context.Records.Add(record);
        await context.SaveChangesAsync();

        record.Name = "New Name";
        await context.SaveChangesAsync();

        var log = await context.AuditLogs
            .Where(l => l.Action == "Updated")
            .SingleAsync();

        log.OldValues.Should().Contain("Old Name");
        log.NewValues.Should().Contain("New Name");
    }

    [Fact]
    public async Task Deleting_ShouldWriteDeletedAuditLog()
    {
        await using var context = CreateContext();
        var record = new TestEmployeeRecord { Name = "Adaeze Okafor" };
        context.Records.Add(record);
        await context.SaveChangesAsync();

        context.Records.Remove(record);
        await context.SaveChangesAsync();

        var log = await context.AuditLogs
            .Where(l => l.Action == "Deleted")
            .SingleAsync();

        log.EntityId.Should().Be(record.Id.ToString());
        log.OldValues.Should().Contain("Adaeze Okafor");
        log.NewValues.Should().BeNull();
    }

    [Fact]
    public async Task AuditLogRows_ShouldNotBeAuditedThemselves()
    {
        await using var context = CreateContext();
        context.Records.Add(new TestEmployeeRecord { Name = "One" });
        await context.SaveChangesAsync();

        var logs = await context.AuditLogs.ToListAsync();

        logs.Should().ContainSingle(); // only the record's log, no log-of-log
        logs[0].EntityName.Should().Be(nameof(TestEmployeeRecord));
    }
}
