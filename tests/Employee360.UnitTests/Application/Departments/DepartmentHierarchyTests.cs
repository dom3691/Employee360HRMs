using Employee360.Application.Features.Departments.CreateDepartment;
using Employee360.Application.Features.Departments.DeleteDepartment;
using Employee360.Application.Features.Departments.GetDepartments;
using Employee360.Application.Features.Departments.UpdateDepartment;
using Employee360.Domain.Entities;
using Employee360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Employee360.UnitTests.Application.Departments;

/// <summary>
/// Tests for department slices: circular-hierarchy prevention, unique codes,
/// delete guards, and the hierarchy tree.
/// </summary>
public class DepartmentHierarchyTests
{
    private static Employee360DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Employee360DbContext>()
            .UseInMemoryDatabase($"dept-tests-{Guid.NewGuid()}")
            .Options;

        return new Employee360DbContext(options);
    }

    private static Department NewDepartment(string name, string code, Guid? parentId = null) => new()
    {
        Name = name,
        Code = code,
        ParentDepartmentId = parentId,
    };

    /// <summary>Seeds: HQ → Operations → Lagos-Ops.</summary>
    private static async Task<(Department Hq, Department Ops, Department LagosOps)> SeedChainAsync(
        Employee360DbContext context)
    {
        var hq = NewDepartment("Headquarters", "HQ");
        var ops = NewDepartment("Operations", "OPS", hq.Id);
        var lagosOps = NewDepartment("Lagos Operations", "LAG-OPS", ops.Id);
        context.Departments.AddRange(hq, ops, lagosOps);
        await context.SaveChangesAsync();
        return (hq, ops, lagosOps);
    }

    [Fact]
    public async Task Update_SettingParentToDescendant_ShouldFail()
    {
        await using var context = CreateContext();
        var (hq, _, lagosOps) = await SeedChainAsync(context);
        var handler = new UpdateDepartmentHandler(context);

        // HQ's parent cannot be Lagos-Ops (a grandchild) — that's a cycle.
        var result = await handler.Handle(
            new UpdateDepartmentCommand(hq.Id, "Headquarters", "HQ", lagosOps.Id, null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("descendant");
    }

    [Fact]
    public async Task Update_SettingParentToItself_ShouldFail()
    {
        await using var context = CreateContext();
        var (hq, _, _) = await SeedChainAsync(context);
        var handler = new UpdateDepartmentHandler(context);

        var result = await handler.Handle(
            new UpdateDepartmentCommand(hq.Id, "Headquarters", "HQ", hq.Id, null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("own parent");
    }

    [Fact]
    public async Task Update_SettingParentToUnrelatedDepartment_ShouldSucceed()
    {
        await using var context = CreateContext();
        var (_, _, lagosOps) = await SeedChainAsync(context);
        var finance = NewDepartment("Finance", "FIN");
        context.Departments.Add(finance);
        await context.SaveChangesAsync();

        var handler = new UpdateDepartmentHandler(context);

        var result = await handler.Handle(
            new UpdateDepartmentCommand(lagosOps.Id, "Lagos Operations", "LAG-OPS", finance.Id, null),
            default);

        result.IsSuccess.Should().BeTrue();
        (await context.Departments.SingleAsync(d => d.Id == lagosOps.Id))
            .ParentDepartmentId.Should().Be(finance.Id);
    }

    [Fact]
    public async Task Create_DuplicateCode_ShouldFail()
    {
        await using var context = CreateContext();
        context.Departments.Add(NewDepartment("Engineering", "ENG"));
        await context.SaveChangesAsync();
        var handler = new CreateDepartmentHandler(context);

        var result = await handler.Handle(
            new CreateDepartmentCommand("Engineering Two", "eng", null, null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Update_TakingAnotherDepartmentsCode_ShouldFail()
    {
        await using var context = CreateContext();
        var eng = NewDepartment("Engineering", "ENG");
        var fin = NewDepartment("Finance", "FIN");
        context.Departments.AddRange(eng, fin);
        await context.SaveChangesAsync();
        var handler = new UpdateDepartmentHandler(context);

        var result = await handler.Handle(
            new UpdateDepartmentCommand(fin.Id, "Finance", "ENG", null, null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Delete_WithChildDepartments_ShouldFail()
    {
        await using var context = CreateContext();
        var (hq, _, _) = await SeedChainAsync(context);
        var handler = new DeleteDepartmentHandler(context);

        var result = await handler.Handle(new DeleteDepartmentCommand(hq.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("sub-departments");
    }

    [Fact]
    public async Task Delete_WithAssignedEmployees_ShouldFail()
    {
        await using var context = CreateContext();
        var department = NewDepartment("Engineering", "ENG");
        context.Departments.Add(department);
        context.Employees.Add(new Employee
        {
            EmployeeCode = "EMP-00001",
            FirstName = "Ada",
            LastName = "Okafor",
            Email = "ada@company.ng",
            DepartmentId = department.Id,
        });
        await context.SaveChangesAsync();
        var handler = new DeleteDepartmentHandler(context);

        var result = await handler.Handle(new DeleteDepartmentCommand(department.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("assigned employees");
    }

    [Fact]
    public async Task Delete_EmptyLeafDepartment_ShouldSucceed()
    {
        await using var context = CreateContext();
        var (_, _, lagosOps) = await SeedChainAsync(context);
        var handler = new DeleteDepartmentHandler(context);

        var result = await handler.Handle(new DeleteDepartmentCommand(lagosOps.Id), default);

        result.IsSuccess.Should().BeTrue();
        (await context.Departments.AnyAsync(d => d.Id == lagosOps.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Tree_ShouldNestDepartmentsByParent()
    {
        await using var context = CreateContext();
        await SeedChainAsync(context);
        var handler = new GetDepartmentTreeHandler(context);

        var result = await handler.Handle(new GetDepartmentTreeQuery(), default);

        result.Value.Should().ContainSingle();
        var root = result.Value[0];
        root.Code.Should().Be("HQ");
        root.Children.Should().ContainSingle()
            .Which.Children.Should().ContainSingle()
            .Which.Code.Should().Be("LAG-OPS");
    }
}
