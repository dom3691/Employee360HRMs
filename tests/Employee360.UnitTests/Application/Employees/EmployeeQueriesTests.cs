using Employee360.Application.Features.Employees.GetEmployeesPaged;
using Employee360.Application.Features.Employees.GetOrgChart;
using Employee360.Domain.Entities;
using Employee360.Domain.Enums;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Application.Employees;

/// <summary>Tests for the paged directory query and the org chart tree.</summary>
public class EmployeeQueriesTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"emp-queries-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static Employee NewEmployee(
        string code, string first, string last, string email,
        Guid? departmentId = null, Guid? managerId = null,
        EmployeeStatus status = EmployeeStatus.Active) => new()
    {
        EmployeeCode = code,
        FirstName = first,
        LastName = last,
        Email = email,
        DepartmentId = departmentId,
        ManagerId = managerId,
        Status = status,
    };

    [Fact]
    public async Task PagedQuery_ShouldFilterBySearch_AndPaginate()
    {
        await using var context = CreateContext();
        context.Employees.AddRange(
            NewEmployee("EMP-00001", "Ada", "Okafor", "ada@company.ng"),
            NewEmployee("EMP-00002", "Bola", "Adeyemi", "bola@company.ng"),
            NewEmployee("EMP-00003", "Chike", "Okafor", "chike@company.ng"));
        await context.SaveChangesAsync();

        var handler = new GetEmployeesPagedHandler(context);

        var result = await handler.Handle(
            new GetEmployeesPagedQuery(Page: 1, PageSize: 1, Search: "okafor"), default);

        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task PagedQuery_ShouldFilterByDepartmentAndStatus()
    {
        await using var context = CreateContext();
        var engineering = new Department { Name = "Engineering", Code = "ENG" };
        context.Departments.Add(engineering);
        context.Employees.AddRange(
            NewEmployee("EMP-00001", "Ada", "Okafor", "ada@company.ng", engineering.Id),
            NewEmployee("EMP-00002", "Bola", "Adeyemi", "bola@company.ng", engineering.Id,
                status: EmployeeStatus.Resigned),
            NewEmployee("EMP-00003", "Chike", "Eze", "chike@company.ng"));
        await context.SaveChangesAsync();

        var handler = new GetEmployeesPagedHandler(context);

        var result = await handler.Handle(
            new GetEmployeesPagedQuery(
                DepartmentId: engineering.Id,
                Status: EmployeeStatus.Active),
            default);

        result.Value.Items.Should().ContainSingle()
            .Which.FullName.Should().Be("Ada Okafor");
    }

    [Fact]
    public async Task OrgChart_ShouldBuildTree_WithIndirectReports()
    {
        await using var context = CreateContext();
        var ceo = NewEmployee("EMP-00001", "Ceo", "Boss", "ceo@company.ng");
        var manager = NewEmployee("EMP-00002", "Mid", "Manager", "mid@company.ng", managerId: ceo.Id);
        var report = NewEmployee("EMP-00003", "Ric", "Report", "ric@company.ng", managerId: manager.Id);
        context.Employees.AddRange(ceo, manager, report);
        await context.SaveChangesAsync();

        var handler = new GetOrgChartHandler(context);

        var result = await handler.Handle(new GetOrgChartQuery(), default);

        result.Value.Should().ContainSingle();
        var root = result.Value[0];
        root.FullName.Should().Be("Ceo Boss");
        root.Children.Should().ContainSingle()
            .Which.Children.Should().ContainSingle()
            .Which.FullName.Should().Be("Ric Report");
    }

    [Fact]
    public async Task OrgChart_ShouldExcludeInactiveEmployees()
    {
        await using var context = CreateContext();
        var boss = NewEmployee("EMP-00001", "Active", "Boss", "boss@company.ng");
        var gone = NewEmployee("EMP-00002", "Gone", "Person", "gone@company.ng",
            managerId: boss.Id, status: EmployeeStatus.Terminated);
        context.Employees.AddRange(boss, gone);
        await context.SaveChangesAsync();

        var handler = new GetOrgChartHandler(context);

        var result = await handler.Handle(new GetOrgChartQuery(), default);

        result.Value.Should().ContainSingle();
        result.Value[0].Children.Should().BeEmpty();
    }
}
