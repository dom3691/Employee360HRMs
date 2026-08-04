using Employee360.Application.Features.Reports;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Domain.Interfaces;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Employee360.UnitTests.Application.Reports;

public class GetReportHandlerTests
{
    [Fact]
    public async Task Handle_CsvFormat_ReturnsExportBytesWithCorrectContentType()
    {
        await using var context = CreateContext();

        context.Employees.Add(new Employee
        {
            EmployeeCode = "E-ACT",
            FirstName = "Active",
            LastName = "Staff",
            Email = "active@co.ng",
            Status = EmployeeStatus.Active,
        });
        await context.SaveChangesAsync();

        var handler = new GetReportHandler(
            context,
            new Employee360.Infrastructure.Services.Reports.CsvExporter(),
            new Employee360.Infrastructure.Services.Reports.ExcelExporter(),
            Mock.Of<IDateTimeProvider>(c => c.TodayWat == new DateOnly(2026, 8, 4)));

        var result = await handler.Handle(
            new GetReportQuery(
                ReportIds.EmployeeMasterList,
                Format: ReportFormats.Csv),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExportBytes.Should().NotBeNullOrEmpty();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.FileName.Should().Be("employee-master-list.csv");
        result.Value.Data.Should().BeNull();

        var csv = System.Text.Encoding.UTF8.GetString(result.Value.ExportBytes!).TrimStart('\uFEFF');
        csv.Should().Contain("EmployeeCode,EmployeeName");
        csv.Should().Contain("E-ACT");
    }

    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"report-handler-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }
}
