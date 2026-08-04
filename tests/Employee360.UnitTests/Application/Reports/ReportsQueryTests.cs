using Employee360.Application.Features.Reports;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence;
using Employee360.Infrastructure.Services.Reports;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Application.Reports;

public class ReportsQueryTests
{
    [Fact]
    public async Task AttritionReport_FiltersExitsByDateRange()
    {
        await using var context = CreateContext();
        SeedWorkforce(context);

        var range = new ReportDateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        var table = await AttritionReportGenerator.GenerateAsync(context, range, CancellationToken.None);

        table.Rows.Should().HaveCount(1);
        table.Rows[0][0].Should().Be("E-RES");
        table.Rows[0][3].Should().Be("Voluntary");

        var metrics = (ReportAttritionCalculator.AttritionMetrics)table.Summary!;
        metrics.VoluntaryExits.Should().Be(1);
        metrics.InvoluntaryExits.Should().Be(0);
    }

    [Fact]
    public async Task HeadcountTrendReport_CountsEmployeesPerMonth()
    {
        await using var context = CreateContext();
        SeedWorkforce(context);

        var range = new ReportDateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31));
        var table = await HeadcountReportGenerator.GenerateTrendAsync(context, range, CancellationToken.None);

        table.Rows.Should().HaveCount(3);
        table.Rows[0][1].Should().Be(+2);
        table.Rows[2][1].Should().Be(1);
    }

    [Fact]
    public void CsvExporter_MapsHeadersAndColumnsInOrder()
    {
        var exporter = new CsvExporter();
        var bytes = exporter.Export(
            ["Code", "Amount"],
            [[(object?)"E1", 250000.5m]]);

        var csv = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        csv.Should().Contain("Code,Amount");
        csv.Should().Contain("E1,250000.50");
    }

    [Fact]
    public void BirthdayAnniversary_IsOccasionInRange_MatchesMonthDayWithinRange()
    {
        var range = new ReportDateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));

        EmployeeReportGenerator.IsOccasionInRange(new DateOnly(1990, 7, 15), range)
            .Should().BeTrue();

        EmployeeReportGenerator.IsOccasionInRange(new DateOnly(1990, 8, 1), range)
            .Should().BeFalse();
    }

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"reports-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static void SeedWorkforce(Employee360DbContext context)
    {
        var active = new Employee
        {
            EmployeeCode = "E-ACT",
            FirstName = "Active",
            LastName = "Staff",
            Email = "active@co.ng",
            Status = EmployeeStatus.Active,
            JoinDate = new DateOnly(2026, 1, 1),
            ModifiedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        var resigned = new Employee
        {
            EmployeeCode = "E-RES",
            FirstName = "Former",
            LastName = "Staff",
            Email = "former@co.ng",
            Status = EmployeeStatus.Resigned,
            JoinDate = new DateOnly(2025, 1, 1),
            ModifiedAt = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
        };

        var terminatedOutsideRange = new Employee
        {
            EmployeeCode = "E-TERM",
            FirstName = "Old",
            LastName = "Exit",
            Email = "old@co.ng",
            Status = EmployeeStatus.Terminated,
            JoinDate = new DateOnly(2024, 1, 1),
            ModifiedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        context.Employees.AddRange(active, resigned, terminatedOutsideRange);
        context.SaveChanges();
    }
}
