using Employee360.Application.Features.AuditLogs;
using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Application.Admin;

/// <summary>Tests for audit log query filtering (FR-ADM-004).</summary>
public class AuditLogsQueryTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetAuditLogsPaged_FiltersByUserEntityAndDateRange()
    {
        await using var context = CreateContext();
        SeedAuditLogs(context);

        var handler = new GetAuditLogsPagedHandler(context);

        var result = await handler.Handle(
            new GetAuditLogsPagedQuery(
                Page: 1,
                PageSize: 10,
                UserId: UserA,
                EntityName: "Employee",
                FromDate: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                ToDate: new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].EntityId.Should().Be("emp-1");
        result.Value.Items[0].UserId.Should().Be(UserA);
    }

    [Fact]
    public async Task ExportAuditLogsCsv_ReturnsFilteredRowsAsCsv()
    {
        await using var context = CreateContext();
        SeedAuditLogs(context);

        var handler = new ExportAuditLogsCsvHandler(context);

        var result = await handler.Handle(
            new ExportAuditLogsCsvQuery(EntityName: "Department"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var csv = System.Text.Encoding.UTF8.GetString(result.Value!);

        csv.Should().Contain("EntityName,EntityId,Action");
        csv.Should().Contain("Department");
        csv.Should().NotContain("emp-1");
    }

    [Fact]
    public void BuildCsv_EscapesCommasAndQuotes()
    {
        var rows = new List<AuditLog>
        {
            new()
            {
                EntityName = "Employee",
                EntityId = "1",
                Action = "Updated",
                OldValues = "{\"name\":\"Ada, Jr.\"}",
                NewValues = "value with \"quotes\"",
                Timestamp = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc),
            },
        };

        var csv = ExportAuditLogsCsvHandler.BuildCsv(rows);

        csv.Should().Contain("\"{\"\"name\"\":\"\"Ada, Jr.\"\"}\"");
        csv.Should().Contain("\"value with \"\"quotes\"\"\"");
    }

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"audit-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static void SeedAuditLogs(Employee360DbContext context)
    {
        context.AuditLogs.AddRange(
            new AuditLog
            {
                EntityName = "Employee",
                EntityId = "emp-1",
                Action = "Updated",
                UserId = UserA,
                Timestamp = new DateTime(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc),
            },
            new AuditLog
            {
                EntityName = "Employee",
                EntityId = "emp-2",
                Action = "Created",
                UserId = UserB,
                Timestamp = new DateTime(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc),
            },
            new AuditLog
            {
                EntityName = "Department",
                EntityId = "dept-1",
                Action = "Created",
                UserId = UserA,
                Timestamp = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
            });

        context.SaveChanges();
    }
}
